using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using COLID.Common.Extensions;
using COLID.Graph.Metadata.Services;
using COLID.Graph.TripleStore.Extensions;
using COLID.RegistrationService.Common.Constants;
using COLID.RegistrationService.Common.DataModel.ExtendedUriTemplates;
using COLID.RegistrationService.Common.DataModel.ProxyConfiguration;
using COLID.RegistrationService.Common.DataModel.Resources;
using COLID.RegistrationService.Repositories.Interface;
using COLID.RegistrationService.Services.Interface;
using COLID.StatisticsLog.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Regex = System.Text.RegularExpressions.Regex;

namespace COLID.RegistrationService.Services.Implementation
{
    internal class ProxyConfigService : IProxyConfigService
    {
        private readonly string _colidFrontEndUrl;
        private readonly string _colidDomain;
        private readonly IResourceRepository _resourceRepository;
        private readonly IExtendedUriTemplateService _extendedUriTemplateService;
        private readonly IMetadataService _metadataService;
        private readonly ILogger<ProxyConfigService> _logger;

        public ProxyConfigService(IConfiguration configuration,
            IResourceRepository resourceRepository,
            IExtendedUriTemplateService extendedUriTemplateService,
            IMetadataService metadataService,
            ILogger<ProxyConfigService> logger)
        {
            _colidFrontEndUrl = configuration.GetConnectionString("colidFrontEndUrl");
            _colidDomain = configuration.GetConnectionString("colidDomain");
            _resourceRepository = resourceRepository;
            _extendedUriTemplateService = extendedUriTemplateService;
            _metadataService = metadataService;
            _logger = logger;
        }

        /// <summary>
        /// Gets the current NGINX proxy configuration.
        /// All resources will be fetched and the config will be generated afterwards.
        /// </summary>
        /// <returns>Serialized NGINX configuration</returns>
        public string GetCurrentProxyConfiguration()
        {
            var leafResourceTypes = _metadataService.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type.FirstResouceType);
            var publishedResourceProxyDTOs = _resourceRepository.GetResourcesForProxyConfiguration(leafResourceTypes);
            var proxyConfiguration = GenerateProxyConfig(publishedResourceProxyDTOs);

            return proxyConfiguration;
        }

        /// <summary>
        /// Generates the NGINX proxy configuration from a list of resource proxies.
        /// </summary>
        /// <param name="resources">List of all resources with PID URLs and target URLs</param>
        /// <returns>Serialized NGINX configuration</returns>
        public string GenerateProxyConfig(IList<ResourceProxyDTO> resources)
        {
            var configSections = CreateConfigForResources(resources);
            return SerializeNginxConfigList(configSections);
        }

        /// <summary>
        /// Generates the NGINX configuration for all given resources and adds standard NGINX server information to the returned configuration.
        /// </summary>
        /// <param name="resources">List of resources to generate the NGINX proxy configuration from.</param>
        /// <returns>NGINX configuration sections for the NGINX servers and all given resources.</returns>
        private IList<NginxConfigSection> CreateConfigForResources(IList<ResourceProxyDTO> resources)
        {
            var sectionHttp = new NginxConfigSection
            {
                Name = "server",
                Attributes = new List<NginxAttribute>
                {
                    new NginxAttribute("listen", "80"),
                    new NginxAttribute("server_name", string.Concat(_colidDomain, " www.", _colidDomain)),
                    new NginxAttribute("return", "301 https://$server_name$request_uri"),
                }
            };
            var sectionHttps = new NginxConfigSection
            {
                Name = "server",
                Attributes = new List<NginxAttribute>
                {
                    new NginxAttribute("listen", "443 ssl http2"),
                    new NginxAttribute("include", "snippets/self-signed.conf"),
                    new NginxAttribute("include", "snippets/ssl-params.conf"),
                    new NginxAttribute("server_name", string.Concat(_colidDomain, " www.", _colidDomain)),
                    new NginxAttribute("proxy_set_header", "'Access-Control-Allow-Origin' \"*\""),
                    new NginxAttribute("proxy_set_header", "'Access-Control-Allow-Credentials' 'true'"),
                    new NginxAttribute("proxy_set_header", "'Access-Control-Allow-Methods' 'GET, POST, PUT, DELETE, OPTIONS'"),
                    new NginxAttribute("proxy_set_header", "'Access-Control-Allow-Headers' 'Accept,Authorization,Cache-Control,Content-Type,DNT,If-Modified-Since,Keep-Alive,Origin,User-Agent,X-Requested-With'"),
                },
                Subsections = resources != null ? GenerateConfigSections(resources) : new List<NginxConfigSection>()
            };

            sectionHttps.Subsections.Add(new NginxConfigSection
            {
                Name = "location",
                Parameters = "/",
                Attributes = new List<NginxAttribute>
                {
                    new NginxAttribute("rewrite ^.*", _colidFrontEndUrl)
                    //new NginxAttribute("proxy_pass", colidFrontEndUrl)
                    //new NginxAttribute("proxy_set_header", "X-Real-IP       $remote_addr")
                    //new NginxAttribute("proxy_set_header", "X-Forwarded-For $proxy_add_x_forwarded_for")
                }
            });

            return new List<NginxConfigSection> { sectionHttp, sectionHttps };
        }

        /// <summary>
        /// Generates NGINX configuration sections for all given resource proxies. Additionally the extended URI templates are applied to the configuration.
        /// </summary>
        /// <param name="resourceProxies">List of resources to generate the NGINX proxy configuration from.</param>
        /// <returns>NGINX configuration sections for all given resources and their extended URI templates matches.</returns>
        private IList<NginxConfigSection> GenerateConfigSections(IList<ResourceProxyDTO> resourceProxies)
        {
            IList<NginxConfigSection> nginxConfigSections = new List<NginxConfigSection>();

            var extendedUris = _extendedUriTemplateService.GetEntities(null);

            foreach (var resourceProxy in resourceProxies)
            {
                try
                {
                    ConvertToNginxConfigSectionsAndAppendTemplates(resourceProxy, extendedUris, nginxConfigSections);
                }
                catch (System.Exception exception)
                {
                    _logger.LogError(exception, Messages.Proxy.ResourceProxy, resourceProxy);
                }
            }

            nginxConfigSections = FilterDuplicateConfigurationSections(nginxConfigSections);
            nginxConfigSections = OrderConfigurationSections(nginxConfigSections);

            return nginxConfigSections;
        }

        /// <summary>
        /// Creates NGINX specific configuration sections with locations and rewrite attributes.
        /// Additionally, appends regex locations for all matching extended PID uri templates.
        /// </summary>
        /// <param name="resourceProxy">List of resources to generate the NGINX proxy configuration from.</param>
        /// <param name="extendedUris">List of extended URI templates.</param>
        /// <param name="nginxConfigSections">List of NGINX configuration sections to append.</param>
        private void ConvertToNginxConfigSectionsAndAppendTemplates(ResourceProxyDTO resourceProxy, IList<ExtendedUriTemplateResultDTO> extendedUris, IList<NginxConfigSection> nginxConfigSections = null)
        {
            if (nginxConfigSections == null)
            {
                nginxConfigSections = new List<NginxConfigSection>();
            }

            // If this given PID URL does not start with the PID internal prefix (e.g. an external base URL), this does not need be further processed.
            if (GetUrlPath(resourceProxy.PidUrl) != null && !string.IsNullOrWhiteSpace(GetUrlPath(resourceProxy.PidUrl)))
            {
                var matchedExtendedUris = GetMatchedExtendedUriTemplate(resourceProxy, extendedUris);

                var pidUri = GetUrlPath(resourceProxy.PidUrl);
                var newParamater = $"= {pidUri}";

                var targetUrl = resourceProxy.TargetUrl ?? $"{_colidFrontEndUrl}resource?pidUri={WebUtility.UrlEncode(resourceProxy.PidUrl)}";
                var nginxAttr = new NginxAttribute("rewrite ^.*", targetUrl);

                // Base URIs should proxy to the latest version of the version list of a PID entry / resource
                // If this Base URI is already set in the proxy config list, compare the resource version and
                // override if to the later version.
                if (nginxConfigSections.FirstOrDefault(section => section.Parameters == newParamater) == null)
                {
                    if (Uri.IsWellFormedUriString(targetUrl, UriKind.Absolute))
                    {
                        nginxConfigSections.Add(new NginxConfigSection
                        {
                            Name = "location",
                            Parameters = newParamater,
                            Version = resourceProxy.ResourceVersion,
                            Attributes = new List<NginxAttribute> { nginxAttr }
                        });

                        nginxConfigSections.AddRange(CreateNginxConfigSectionForExtendedUriTemplates(resourceProxy, matchedExtendedUris));
                    }
                }
                else
                {
                    var config = nginxConfigSections.FirstOrDefault(section => section.Parameters == newParamater);

                    if (resourceProxy.ResourceVersion != null && config.Version.CompareVersionTo(resourceProxy.ResourceVersion) < 0)
                    {
                        config.Version = resourceProxy.ResourceVersion;
                        config.Attributes = new List<NginxAttribute> { nginxAttr };

                        if (Uri.IsWellFormedUriString(targetUrl, UriKind.Absolute))
                        {
                            nginxConfigSections.AddRange(CreateNginxConfigSectionForExtendedUriTemplates(resourceProxy, matchedExtendedUris));
                        }
                    }
                }
            }

            if (resourceProxy.NestedProxies != null)
            {
                foreach (var rp in resourceProxy.NestedProxies)
                {
                    try
                    {
                        ConvertToNginxConfigSectionsAndAppendTemplates(rp, extendedUris, nginxConfigSections);
                    }
                    catch (System.Exception exception)
                    {
                        _logger.LogError(exception, Messages.Proxy.NestedProxy, rp);
                    }
                }
            }
        }

        /// <summary>
        /// Creates NGINX specific configuration sections for the given resource proxy and its matching extended URI templates.
        /// </summary>
        /// <param name="resourceProxy">Resource Proxy to match the extended URI templates to.</param>
        /// <param name="extendedUris">List of all extended URI templates.</param>
        /// <returns>List of NGINX configuration sections matching to the regular expressions of the extended URI templates.</returns>
        private IList<NginxConfigSection> CreateNginxConfigSectionForExtendedUriTemplates(ResourceProxyDTO resourceProxy, IList<ExtendedUriTemplateResultDTO> extendedUris)
        {
            var nginxConfigSections = new List<NginxConfigSection>();

            foreach (var extendedUri in extendedUris)
            {
                try
                {
                    string replacementString = extendedUri.Properties.GetValueOrNull(Common.Constants.ExtendedUriTemplate.HasReplacementString, true);
                    string pidUriSearchRegex = extendedUri.Properties.GetValueOrNull(Common.Constants.ExtendedUriTemplate.HasPidUriSearchRegex, true);

                    replacementString = replacementString
                        .Replace("{targetUri}", resourceProxy.TargetUrl, StringComparison.Ordinal)
                        .Replace("{encodedPidUri}", TransformAndEncodePidUrl(resourceProxy.PidUrl, extendedUri), StringComparison.Ordinal)
                        .Replace(" ", string.Empty, StringComparison.Ordinal);

                    var parameters = GenerateNginxRegexLocation(pidUriSearchRegex, resourceProxy.PidUrl);

                    if (!string.IsNullOrWhiteSpace(parameters))
                    {
                        var nginxAttr = new NginxAttribute($"rewrite {parameters}", replacementString);

                        nginxConfigSections.Add(new NginxConfigSection
                        {
                            Name = "location",
                            Parameters = $"~ {parameters}",
                            Order = extendedUri.Properties.GetValueOrNull(Common.Constants.ExtendedUriTemplate.HasOrder, true),
                            Attributes = new List<NginxAttribute> { nginxAttr }
                        });
                    }
                }
                catch (System.Exception exception)
                {
                    _logger.LogError(exception, Messages.Proxy.ExtendedUri, extendedUri, resourceProxy);
                }
            }

            return nginxConfigSections;
        }

        /// <summary>
        /// Transforms and encodes the given PID URL for the encoded URI part of extended PID URIs.
        /// Transformation removes the port and sets the correct protocol on the returned value (http or https).
        /// </summary>
        /// <param name="pidUrl">The PID URL to transform.</param>
        /// <returns>The encoded PID URL without the environment prefix "dev-pid" or "qa-pid".</returns>
        private static string TransformAndEncodePidUrl(string pidUrl, ExtendedUriTemplateResultDTO extendedUriTemplate)
        {
            var builder = new UriBuilder(pidUrl)
            {
                Port = -1,
                Host = new Uri(Graph.Metadata.Constants.Resource.PidUrlPrefix).Host
            };

            string httpScheme = extendedUriTemplate.Properties.GetValueOrNull(Common.Constants.ExtendedUriTemplate.UseHttpScheme, true);

            if (!string.IsNullOrWhiteSpace(httpScheme) && httpScheme == Graph.Metadata.Constants.Boolean.True)
            {
                builder.Scheme = Uri.UriSchemeHttp;
            }

            return WebUtility.UrlEncode(builder.Uri.ToString());
        }

        /// <summary>
        /// Returns a list of matching extended uris templates of a given resource proxy, by matching the regular expression of PID URLs and target URLs.
        /// </summary>
        /// <param name="resourceProxy">Resource Proxy to match the extended URI templates to.</param>
        /// <param name="extendedUris">List of all extended URI templates.</param>
        /// <returns>All templates matching the current location URL and target URL.></returns>
        private IList<ExtendedUriTemplateResultDTO> GetMatchedExtendedUriTemplate(ResourceProxyDTO resourceProxy, IList<ExtendedUriTemplateResultDTO> extendedUris)
        {
            return extendedUris.Where(t =>
            {
                string pidUrlRegex = t.Properties.GetValueOrNull(Common.Constants.ExtendedUriTemplate.HasPidUriSearchRegex, true);
                string targetUrlRegex = t.Properties.GetValueOrNull(Common.Constants.ExtendedUriTemplate.HasTargetUriMatchRegex, true);

                if (string.IsNullOrWhiteSpace(pidUrlRegex) || string.IsNullOrWhiteSpace(targetUrlRegex) || string.IsNullOrWhiteSpace(resourceProxy.TargetUrl))
                {
                    return false;
                }
                return Regex.IsMatch(resourceProxy.PidUrl, pidUrlRegex) && Regex.IsMatch(resourceProxy.TargetUrl, targetUrlRegex);
            }).ToList();
        }

        /// <summary>
        /// Generates the correct location for a PID URL and its matching PID URL Search Regex of an extended URI template.
        /// </summary>
        /// <param name="pidUrlSearchRegex">Regular expression the PID URL should match to.</param>
        /// <param name="pidUrl">PID URL to generate the regular expression location from.</param>
        /// <returns>The NGINX regular expression location.</returns>
        private string GenerateNginxRegexLocation(string pidUrlSearchRegex, string pidUrl)
        {
            if (pidUrl == null)
            {
                return null;
            }

            var extractedPidUrlPath = GetUrlPath(pidUrl);

            if (!pidUrlSearchRegex.Contains("(.*)"))
            {
                return $"^{extractedPidUrlPath}$";
            }

            // Host and Scheme is removed as it requires a valid regexes of a pid uri.
            // This part of the regex is no longer needed because the regex has already been applied to the pid uri.
            var pidUrlSearchRegexWithoutHost = pidUrlSearchRegex.RemoveFirst($"https://{_colidDomain}");

            var extractedMatch = Regex.Match(extractedPidUrlPath, pidUrlSearchRegexWithoutHost);

            var trimmedPidUrlSearchRegex = pidUrlSearchRegexWithoutHost.TrimStart('^').TrimEnd('$');

            //Regex match extractedMatch
            if (extractedMatch.Success)
            {
                // Special case, if the regex consists exclusively of a group, the group regey must be adapted to the string.
                if (trimmedPidUrlSearchRegex == "(.*)" || trimmedPidUrlSearchRegex == "/(.*)" || trimmedPidUrlSearchRegex == "/(.*)/")
                {
                    return $"^{extractedPidUrlPath + trimmedPidUrlSearchRegex}$";
                }
                //In all other cases the matched path will be replaced by the regex
                else
                {
                    return $"^{extractedPidUrlPath.Replace(extractedMatch.Value, trimmedPidUrlSearchRegex)}";
                }
            }

            throw new System.Exception($"The regex of the extended uri template { pidUrlSearchRegex } failed on pidurl { pidUrl}.");
        }

        /// <summary>
        /// Extracts the path and fragment of the given url.
        /// </summary>
        /// <param name="pidUrl">The URL to extract the path and fragment from.</param>
        /// <returns>Path and segment of the given url.</returns>
        private string GetUrlPath(string pidUrl)
        {
            if (pidUrl == null)
            {
                return null;
            }

            var pidUri = new Uri(pidUrl);

            if (pidUri.Host != _colidDomain)
            {
                return null;
            }

            return pidUri.AbsolutePath + pidUri.Fragment;
        }

        /// <summary>
        /// Serializes the final NGINX configuration list.
        /// </summary>
        /// <param name="configSections">List of all NGINX configuration sections to serialize.</param>
        /// <returns>Serialized NGINX configuration.</returns>
        private string SerializeNginxConfigList(IList<NginxConfigSection> configSections)
       {
            StringBuilder sb = new StringBuilder();

            foreach (var section in configSections)
            {
                SerializeNginxConfig(section, sb, 0);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Serializes a single NGINX configuration and appends this to the StringBuilder.
        /// </summary>
        /// <param name="configSection">NGINX configuration section to serialize.</param>
        /// <param name="sb">StringBuilder to append the serialized NGINX configuration.</param>
        /// <param name="depth">Depth of serialization.</param>
        private void SerializeNginxConfig(NginxConfigSection configSection, StringBuilder sb, int depth)
        {
            var nextDepth = depth + 1;
            var indentation = new string('\t', depth);
            var nextIndentation = new string('\t', nextDepth);
            var sectionHead = indentation +
                              configSection.Name +
                              (string.IsNullOrWhiteSpace(configSection.Parameters) ?
                                  string.Empty :
                                  " " + configSection.Parameters);

            sb.AppendLine(sectionHead + " {");

            foreach (var keyValuePair in configSection.Attributes)
            {
                var escapedConfigValue = sectionHead.Equals("server") ? String.Empty : "\"";
                sb.AppendLine($"{nextIndentation}{ keyValuePair.Key } {escapedConfigValue}{ keyValuePair.Value }{escapedConfigValue};");
            }

            foreach (var subsection in configSection.Subsections)
            {
                SerializeNginxConfig(subsection, sb, nextDepth);
            }

            sb.AppendLine(indentation + "}");
        }

        /// <summary>
        /// Filters duplicate configuration sections by the parameter of the NGINX configuration section. The parameter with the lower order number is used.
        /// </summary>
        /// <param name="nginxConfigSections">List of NGINX configuration sections to filter.</param>
        /// <returns>Filtered list of NGINX configuration sections.</returns>
        private static IList<NginxConfigSection> FilterDuplicateConfigurationSections(IList<NginxConfigSection> nginxConfigSections)
        {
            // Group the sections by their parameters, e.g. their location / PID URI
            var groupedResults = nginxConfigSections.GroupBy(t => t.Parameters);

            // Filter out same locations, use the lowest order number of section, e.g. order number of extended URI templates
            return groupedResults.Select(t =>
            {
                return t.Aggregate((u, v) => u.OrderInt < v.OrderInt ? u : v);
            }).ToList();
        }

        /// <summary>
        /// Orders NGINX configuration sections by multiple stages.
        /// 1. Order of PID URI template. Is null, if no PID URI template matched.
        /// 2. Length of the NGINX location. More specific regular expression location paths match first.
        /// 3. Alphabetical order, if the first two orderings are equal.
        /// </summary>
        /// <param name="nginxConfigSections">List of NGINX configuration sections to sort.</param>
        /// <returns>Sorted list of NGINX configuration sections.</returns>
        private static IList<NginxConfigSection> OrderConfigurationSections(IList<NginxConfigSection> nginxConfigSections)
        {
            nginxConfigSections = nginxConfigSections.OrderBy(t => t.OrderInt).ThenByDescending(t => t.Parameters.Length).ThenBy(t => t.Parameters).ToList();
            return nginxConfigSections;
        }
    }
}
