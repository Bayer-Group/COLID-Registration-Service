using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using COLID.Common.Extensions;
using COLID.Graph.Metadata.Constants;
using COLID.Graph.Metadata.Services;
using COLID.Graph.TripleStore.Extensions;
using COLID.RegistrationService.Common.DataModel.ExtendedUriTemplates;
using COLID.RegistrationService.Common.DataModel.ProxyConfiguration;
using COLID.RegistrationService.Common.DataModel.Resources;
using COLID.RegistrationService.Repositories.Interface;
using COLID.RegistrationService.Services.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Messages = COLID.RegistrationService.Common.Constants.Messages;
using Regex = System.Text.RegularExpressions.Regex;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2;
using COLID.MessageQueue.Configuration;
using COLID.MessageQueue.Datamodel;
using Microsoft.Extensions.Options;
using COLID.MessageQueue.Services;
using System.Threading.Tasks;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.MessageQueue.Configuration;
using COLID.MessageQueue.Datamodel;
using Microsoft.Extensions.Options;
using COLID.MessageQueue.Services;
using System.Threading.Tasks;

namespace COLID.RegistrationService.Services.Implementation
{
    public class ProxyConfigService : IProxyConfigService, IMessageQueuePublisher, IMessageQueueReceiver
    {
        private readonly string _colidFrontEndUrl;
        private readonly string _dmpFrontEndUrl;
        private readonly string _colidDomain;
        private readonly string _topicName;
        private readonly IResourceRepository _resourceRepository;
        private readonly IExtendedUriTemplateService _extendedUriTemplateService;
        private readonly IMetadataService _metadataService;
        private readonly ILogger<ProxyConfigService> _logger;
        private static IDictionary<string, Regex> CompiledRegex = new Dictionary<string, Regex>();
        private readonly string _nginxConfigDynamoDbTable;        
        private readonly IAmazonDynamoDB _amazonDynamoDbService;
        private readonly ColidMessageQueueOptions _mqOptions;
        public Action<string, string, BasicProperty> PublishMessage { get; set; }

        public IDictionary<string, Action<string>> OnTopicReceivers => new Dictionary<string, Action<string>>()
            {
            { _mqOptions.Topics[_topicName], AddUpdateNginxConfigRepository}
            };


        public ProxyConfigService(IConfiguration configuration,
            IOptionsMonitor<ColidMessageQueueOptions> messageQueuingOptionsAccessor,
            IResourceRepository resourceRepository,
            IExtendedUriTemplateService extendedUriTemplateService,
            IMetadataService metadataService,
            ILogger<ProxyConfigService> logger,
            IAmazonDynamoDB amazonDynamoDbService)
        {
            _mqOptions = messageQueuingOptionsAccessor.CurrentValue;
            _colidFrontEndUrl = configuration.GetConnectionString("colidFrontEndUrl");
            _dmpFrontEndUrl = configuration.GetConnectionString("dmpFrontEndUrl");
            _colidDomain = configuration.GetConnectionString("colidDomain");
            _topicName = "ProxyConfigRebuild";
            _resourceRepository = resourceRepository;
            _extendedUriTemplateService = extendedUriTemplateService;
            _metadataService = metadataService;
            _logger = logger;
            _nginxConfigDynamoDbTable = configuration.GetConnectionString("proxyDynamoDbTablename");
            _amazonDynamoDbService = amazonDynamoDbService;
        }

        /// <summary>
        /// Gets the current NGINX proxy configuration.
        /// All resources will be fetched and the config will be generated afterwards.
        /// </summary>
        /// <returns>Serialized NGINX configuration</returns>
        public string GetCurrentProxyConfiguration()
        {            
            var leafResourceTypes = _metadataService.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type.FirstResouceType);            
            var publishedResourceProxyDTOs = _resourceRepository.GetResourcesForProxyConfiguration(leafResourceTypes, _metadataService.GetInstanceGraph(PIDO.PidConcept));
            var proxyConfiguration = GenerateProxyConfig(publishedResourceProxyDTOs);

            return proxyConfiguration;
        }

        /// <summary>
        /// Gets the NGINX proxy configuration for a PidUri.
        ///Resource for the given pidUri will be fetched and the config will be generated afterwards..
        /// </summary>
        /// <param name="pidUri">pid uri</param>
        /// <returns>Serialized NGINX configuration</returns>
        private string GetProxyConfigurationByPidUri(Uri pidUri)
        {            
            var leafResourceTypes = _metadataService.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type.FirstResouceType);
            var publishedResourceProxyDTOs = _resourceRepository.GetResourcesForProxyConfiguration(leafResourceTypes, _metadataService.GetInstanceGraph(PIDO.PidConcept), pidUri);
            var nginxConfigSections = GenerateConfigSections(publishedResourceProxyDTOs);
            nginxConfigSections = FilterDuplicateConfigurationSections(nginxConfigSections);
            nginxConfigSections = OrderConfigurationSections(nginxConfigSections);
            return SerializeNginxConfigList(nginxConfigSections); ;
        }

        /// <summary>
        /// Gets the NGINX proxy configuration for a resource.        
        /// </summary>
        /// <param name="resource">Resource</param>
        /// <returns>Serialized NGINX configuration</returns>
        private string GetProxyConfigurationByResource(ResourceRequestDTO resource)
        {           
            List<ResourceProxyDTO> publishedResourceProxyDTOs = new List<ResourceProxyDTO>();
            publishedResourceProxyDTOs.Add(ConvertResourceToProxyDto(resource));

            var nginxConfigSections = GenerateConfigSections(publishedResourceProxyDTOs);
            nginxConfigSections = FilterDuplicateConfigurationSections(nginxConfigSections);
            nginxConfigSections = OrderConfigurationSections(nginxConfigSections);
            return SerializeNginxConfigList(nginxConfigSections); ;
        }

        public string GetProxyConfigForNewEnvironment()
        {
            var leafResourceTypes = _metadataService.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type.FirstResouceType);
            var publishedResourceProxyDTOs = _resourceRepository.GetResourcesForProxyConfiguration(leafResourceTypes, _metadataService.GetInstanceGraph(PIDO.PidConcept));
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("worker_processes 1;");
            sb.AppendLine(CreateEventsSection());
            sb.AppendLine("pid /tmp/nginx.pid;");
            sb.AppendLine(CreateHttpSection(publishedResourceProxyDTOs));

            return sb.ToString();
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
        /// Returns a precompiled Regex for the given pattern.
        /// </summary>
        /// <param name="pattern">Regex Pattern.</param>
        /// <returns>A precompiled Regex for the pattern.</returns>
        private static Regex GetCompiledRegex(string pattern)
        {
            Regex regex;
            if (CompiledRegex.TryGetValue(pattern, out regex))
            {
                return regex;
            }
            regex = new Regex(pattern, System.Text.RegularExpressions.RegexOptions.Compiled);
            CompiledRegex[pattern] = regex;
            return regex;
        }

        private string CreateEventsSection()
        {
            var sectionEvents = new NginxConfigSection
            {
                Name = "events",
                Attributes = new List<NginxAttribute>
                {
                    new NginxAttribute("worker_connections", "1024")
                }
            };

            return SerializeNginxConfigList(new List<NginxConfigSection> { sectionEvents });
        }

        private string CreateHttpSection(IList<ResourceProxyDTO> resources)
        {
            var tmpPath = "/var/cache/nginx/ 1 2";
            var configs = GenerateConfigSections(resources);

            var sectionHttp = new NginxConfigSection
            {
                Name = "http",
                Attributes = new List<NginxAttribute>
                {
                    new NginxAttribute("client_body_temp_path", tmpPath),
                    new NginxAttribute("proxy_temp_path", tmpPath),
                    new NginxAttribute("fastcgi_temp_path", tmpPath),
                    new NginxAttribute("uwsgi_temp_path", tmpPath),
                    new NginxAttribute("scgi_temp_path", tmpPath),
                    new NginxAttribute("proxy_buffering", "off")
                },
                Subsections = new List<NginxConfigSection>
                {
                    new NginxConfigSection
                    {
                        Name="server",
                        Attributes=new List<NginxAttribute>
                        {
                            new NginxAttribute("server_tokens", "off"),
                            new NginxAttribute("listen", "8081"),
                            new NginxAttribute("access_log", "/dev/null"),
                            new NginxAttribute("error_log", "/dev/null")
                        },
                        Subsections = new List<NginxConfigSection> {
                            new NginxConfigSection
                            {
                                Name="location",
                                Parameters="= /",
                                Attributes = new List<NginxAttribute> { new NginxAttribute( "rewrite ^.*", "https://www.bayer.com/") }
                            }
                        }
                    },
                    new NginxConfigSection
                    {
                        Name = "server",
                        Attributes = new List<NginxAttribute>
                        {
                            new NginxAttribute("server_tokens", "off"),
                            new NginxAttribute("listen", "8080"),
                            new NginxAttribute("root", "/usr/share/nginx/html/"),
                            new NginxAttribute("index", "index.html index.htm"),
                            new NginxAttribute("include", "/etc/nginx/mime.types"),
                            new NginxAttribute("proxy_set_header", "'Access-Control-Allow-Origin' \"*\""),
                            new NginxAttribute("proxy_set_header", "'Access-Control-Allow-Credentials' 'true'"),
                            new NginxAttribute("proxy_set_header", "'Access-Control-Allow-Methods' 'GET, POST, PUT, DELETE, OPTIONS'"),
                            new NginxAttribute("proxy_set_header", "'Access-Control-Allow-Headers' 'Accept,Authorization,Cache-Control,Content-Type,DNT,If-Modified-Since,Keep-Alive,Origin,User-Agent,X-Requested-With'"),
                        },
                        Subsections = resources != null ? configs : new List<NginxConfigSection>()
                    }
                }
            };
            return SerializeNginxConfigList(new List<NginxConfigSection> { sectionHttp });
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
                    new NginxAttribute("rewrite ^.*", _dmpFrontEndUrl)
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

            var urlPath = GetUrlPath(resourceProxy.PidUrl);

            // If this given PID URL does not start with the PID internal prefix (e.g. an external base URL), this does not need be further processed.
            if (urlPath != null && !string.IsNullOrWhiteSpace(urlPath))
            {
                var matchedExtendedUris = GetMatchedExtendedUriTemplate(resourceProxy, extendedUris);

                var pidUri = urlPath;
                var newParamater = $"= {pidUri}";

                var targetUrl = resourceProxy.TargetUrl ?? $"{_dmpFrontEndUrl}/resource-detail?pidUri={WebUtility.UrlEncode(resourceProxy.PidUrl)}";
                var nginxAttr = new NginxAttribute("rewrite ^.*", targetUrl);

                // Base URIs should proxy to the latest version of the version list of a PID entry / resource
                // If this Base URI is already set in the proxy config list, compare the resource version and
                // override if to the later version.
                if (nginxConfigSections.FirstOrDefault(section => section.Parameters == newParamater) == null)
                {
                    if (Uri.TryCreate(targetUrl, UriKind.Absolute, out _))
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

                        if (Uri.TryCreate(targetUrl, UriKind.Absolute, out _))
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
                    string replacementString = extendedUri.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.ExtendedUriTemplate.HasReplacementString, true);
                    string pidUriSearchRegex = extendedUri.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.ExtendedUriTemplate.HasPidUriSearchRegex, true);
   
                    replacementString = replacementString
                        .Replace("{targetUri}", resourceProxy.TargetUrl, StringComparison.Ordinal) 
                        .Replace("{encodedPidUri}", TransformAndEncodePidUrl(resourceProxy.PidUrl, extendedUri), StringComparison.Ordinal) 
                        .Replace("{base64PidUri}", TransformAndBase64EncodePidUrl(resourceProxy.PidUrl, extendedUri), StringComparison.Ordinal)
                        .Replace("{base64BaseUri}", resourceProxy.BaseUrl == null ? string.Empty : TransformAndBase64EncodePidUrl(resourceProxy.BaseUrl, extendedUri), StringComparison.Ordinal)
                        .Replace(" ", string.Empty, StringComparison.Ordinal);

                    var parameters = GenerateNginxRegexLocation(pidUriSearchRegex, resourceProxy.PidUrl);

                    if (!string.IsNullOrWhiteSpace(parameters))
                    {
                        var nginxAttributes = new List<NginxAttribute>();
                        var nginxAttr = new NginxAttribute($"rewrite {parameters}", replacementString);
                        var hasBaseUriReplacement = extendedUri.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.ExtendedUriTemplate.HasReplacementString, true);
                        if (!string.IsNullOrWhiteSpace(resourceProxy.BaseUrl) && resourceProxy.PidUrl == resourceProxy.BaseUrl & hasBaseUriReplacement.Contains("{base64BaseUri}"))
                        {
                            var baseUrIBuilder = new UriBuilder(resourceProxy.BaseUrl)
                            {
                                Port = -1,
                                Host = new Uri(Graph.Metadata.Constants.Resource.PidUrlPrefix).Host
                            };
                            string httpScheme = extendedUri.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.ExtendedUriTemplate.UseHttpScheme, true);

                            if (!string.IsNullOrWhiteSpace(httpScheme) && httpScheme == Graph.Metadata.Constants.Boolean.True)
                            {
                                baseUrIBuilder.Scheme = Uri.UriSchemeHttp;
                            }
                            string baseUrl = baseUrIBuilder.Uri.ToString();
                            var base64Attributes = new List<NginxAttribute>
                                {
                                    new NginxAttribute("set $extendeduri", $"{baseUrl}$1"),
                                    new NginxAttribute("set_encode_base64", "$accurid $extendeduri" ),

                                };
                            nginxAttr = new NginxAttribute($"rewrite {parameters}", $"{resourceProxy.TargetUrl}$accurid");
                            nginxAttributes.AddRange(base64Attributes);
                        }
                        nginxAttributes.Add(nginxAttr);
                        nginxConfigSections.Add(new NginxConfigSection
                        {
                            Name = "location",
                            Parameters = $"~ {parameters}",
                            Order = extendedUri.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.ExtendedUriTemplate.HasOrder, true),
                            Attributes = nginxAttributes ,
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

            string httpScheme = extendedUriTemplate.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.ExtendedUriTemplate.UseHttpScheme, true);

            if (!string.IsNullOrWhiteSpace(httpScheme) && httpScheme == Graph.Metadata.Constants.Boolean.True)
            {
                builder.Scheme = Uri.UriSchemeHttp;
            }

            return WebUtility.UrlEncode(builder.Uri.ToString());
        }

        /// <summary>
        /// Transforms and encodes the given PID URL for the base64-encoded URI part of extended PID URIs.
        /// Transformation removes the port and sets the correct protocol on the returned value (http or https).
        /// </summary>
        /// <param name="pidUrl">The PID URL to transform.</param>
        /// <returns>The base64-encoded PID URL without the environment prefix "dev-pid" or "qa-pid".</returns>
        private static string TransformAndBase64EncodePidUrl(string pidUrl, ExtendedUriTemplateResultDTO extendedUriTemplate)
        {
            var builder = new UriBuilder(pidUrl)
            {
                Port = -1,
                Host = new Uri(Graph.Metadata.Constants.Resource.PidUrlPrefix).Host
            };

            string httpScheme = extendedUriTemplate.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.ExtendedUriTemplate.UseHttpScheme, true);

            if (!string.IsNullOrWhiteSpace(httpScheme) && httpScheme == Graph.Metadata.Constants.Boolean.True)
            {
                builder.Scheme = Uri.UriSchemeHttp;
            }

            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(builder.Uri.ToString()));
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
                string pidUrlRegex = t.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.ExtendedUriTemplate.HasPidUriSearchRegex, true);
                string targetUrlRegex = t.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.ExtendedUriTemplate.HasTargetUriMatchRegex, true);

                if (string.IsNullOrWhiteSpace(pidUrlRegex) || string.IsNullOrWhiteSpace(targetUrlRegex) || string.IsNullOrWhiteSpace(resourceProxy.TargetUrl))
                {
                    return false;
                }
                return GetCompiledRegex(pidUrlRegex).IsMatch(resourceProxy.PidUrl) && GetCompiledRegex(targetUrlRegex).IsMatch(resourceProxy.TargetUrl);
                //return Regex.IsMatch(resourceProxy.PidUrl, pidUrlRegex) && Regex.IsMatch(resourceProxy.TargetUrl, targetUrlRegex);
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
                if (keyValuePair.Key == "set_encode_base64")
                {
                    sb.AppendLine($"{nextIndentation}{ keyValuePair.Key }{" "}{ keyValuePair.Value };");
                }
                else 
                { 
                    var escapedConfigValue = (sectionHead.Equals("server") || sectionHead.Equals("\tserver") || sectionHead.Equals("events") || sectionHead.Equals("http")) ? string.Empty : "\"";
                    sb.AppendLine($"{nextIndentation}{ keyValuePair.Key } {escapedConfigValue}{ keyValuePair.Value }{escapedConfigValue};");
                }
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

        public void AddUpdateNginxConfigRepository(string pidUriString)
        {
            Uri pidUri = new Uri(pidUriString);
            Dictionary<string, AttributeValue> attributes = new Dictionary<string, AttributeValue>();
            
            // PidUri is hash-key
            attributes["pid_uri"] = new AttributeValue { S = pidUri.ToString() };

            //Try deleting if there is an existing entry in dynamoDB
            try
            {
                _amazonDynamoDbService.DeleteItemAsync(_nginxConfigDynamoDbTable, attributes);
            }
            catch (System.Exception ex)
            {
                _logger.LogInformation($"ProxyConfigService: could not delete Nginx Config before Added/Updated for : {pidUri} Error {ex.StackTrace}", pidUri, ex);
            }

            // Title is range-key
            attributes["configString"] = new AttributeValue { S = GetProxyConfigurationByPidUri(pidUri) };

            //Add new Entry
            try
            {
                _amazonDynamoDbService.PutItemAsync(_nginxConfigDynamoDbTable, attributes);
                _logger.LogInformation($"ProxyConfigService: Nginx Config Added/Updated for : {pidUri}", pidUri);
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"ProxyConfigService: Error occured while Add & Update DynamoDb: {ex.StackTrace}", ex);
            }            
        }

        public void AddUpdateNginxConfigRepository(ResourceRequestDTO resource)
        {
            var hasPid = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.hasPID, true);
            string pidUri = hasPid != null ? hasPid.Id : "";
            Dictionary<string, AttributeValue> attributes = new Dictionary<string, AttributeValue>();

            // PidUri is hash-key
            attributes["pid_uri"] = new AttributeValue { S = pidUri };

            //Try deleting if there is an existing entry in dynamoDB
            try
            {
                _amazonDynamoDbService.DeleteItemAsync(_nginxConfigDynamoDbTable, attributes);
            }
            catch (System.Exception ex)
            {
                _logger.LogInformation($"ProxyConfigService: could not delete Nginx Config before Added/Updated for : {pidUri} Error {ex.StackTrace}", pidUri, ex);
            }

            // Title is range-key
            attributes["configString"] = new AttributeValue { S = GetProxyConfigurationByResource(resource) };

            //Add new Entry
            try
            {
                _amazonDynamoDbService.PutItemAsync(_nginxConfigDynamoDbTable, attributes);
                _logger.LogInformation($"ProxyConfigService: Nginx Config Added/Updated for : {pidUri}", pidUri);
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"ProxyConfigService: Error occured while Add & Update DynamoDb: {ex.StackTrace}", ex);
            }
        }
        public void DeleteNginxConfigRepository(Uri pidUri)
        {
            Dictionary<string, AttributeValue> attributes = new Dictionary<string, AttributeValue>();
            // PidUri is hash-key
            attributes["pid_uri"] = new AttributeValue { S = pidUri.ToString() };
            try
            {
                _amazonDynamoDbService.DeleteItemAsync(_nginxConfigDynamoDbTable, attributes);
                _logger.LogInformation($"ProxyConfigService: Nginx Config deleted for : {pidUri}", pidUri);
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"ProxyConfigService: Error occured while deleting from DynamoDb: {ex.StackTrace}", ex);
            }                        
        }

        public void proxyConfigRebuild()
        {
            clearProxyConfigDynamoAsync();

            var pidUriList = _resourceRepository.GetAllPidUris(_metadataService.GetInstanceGraph(PIDO.PidConcept), _metadataService.GetMetadataGraphs());
            
            var numberOfResults = pidUriList.Count;
            _logger.LogInformation($"Number of pid Uris : {numberOfResults}", numberOfResults);

            foreach (string pidUri in pidUriList)
            {
                PublishMessage(_mqOptions.Topics[_topicName], pidUri, null);
            }
        }

        private void clearProxyConfigDynamoAsync()
        {
            List<Dictionary<string, AttributeValue>> resultList = new List<Dictionary<string, AttributeValue>>();
            Dictionary<string, AttributeValue> lastKeyEvaluated = null;
            do
            {
                var request = new ScanRequest
                {
                    TableName = _nginxConfigDynamoDbTable,
                    Limit = 1000000,
                    ExclusiveStartKey = lastKeyEvaluated,
                };

                var response = _amazonDynamoDbService.ScanAsync(request).Result;

                resultList.AddRange(response.Items);
                lastKeyEvaluated = response.LastEvaluatedKey;
            } while (lastKeyEvaluated != null && lastKeyEvaluated.Count != 0);

            var numberOfResults = resultList.Count;
            _logger.LogInformation($"Number of dynamo items : {numberOfResults}", numberOfResults);


            foreach (var item in resultList)
            {
                var pidUri = item.GetValueOrDefault("pid_uri").S;
                var request = new DeleteItemRequest
                {
                    TableName = _nginxConfigDynamoDbTable,
                    Key = new Dictionary<string, AttributeValue>() { { "pid_uri", new AttributeValue { S = pidUri } } },
                };

                _amazonDynamoDbService.DeleteItemAsync(request);
            }

        }
        
 


        private ResourceProxyDTO ConvertResourceToProxyDto(ResourceRequestDTO resource)
        {
            var hasPid = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.hasPID, true);
            string pidUri = hasPid != null ? hasPid.Id : "";

            var hasBaseUri = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.BaseUri, true);
            string baseUri = hasBaseUri != null ? hasBaseUri.Id : null;

            ResourceProxyDTO resourceProxyDto = new ResourceProxyDTO
            {
                PidUrl = pidUri,
                TargetUrl = null,
                ResourceVersion = null,
                NestedProxies = new List<ResourceProxyDTO>()
            };

            // Get distribution
            if (resource.Properties.ContainsKey(Graph.Metadata.Constants.Resource.Distribution))
            {
                List<dynamic> distList = resource.Properties[Graph.Metadata.Constants.Resource.Distribution];
                foreach (dynamic dist in distList)
                {
                    string distributionPidUri = ((COLID.Graph.TripleStore.DataModels.Base.EntityBase)dist).Properties[Graph.Metadata.Constants.Resource.hasPID][0].Id;
                    string distributionNetworkAddress = ((COLID.Graph.TripleStore.DataModels.Base.EntityBase)dist).Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.DistributionEndpoints.HasNetworkAddress, true);
                    bool isDistributionEndpointDeprecated = ((COLID.Graph.TripleStore.DataModels.Base.EntityBase)dist).Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.DistributionEndpoints.DistributionEndpointLifecycleStatus, true) == Common.Constants.DistributionEndpoint.LifeCycleStatus.Deprecated;
                    //string distBaseUri = ((COLID.Graph.TripleStore.DataModels.Base.EntityBase)dist).Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.DistributionEndpoints.HasNetworkAddress, true);


                    resourceProxyDto.NestedProxies.Add(
                        new ResourceProxyDTO
                        {
                            PidUrl = distributionPidUri,
                            TargetUrl = isDistributionEndpointDeprecated ? pidUri : distributionNetworkAddress,
                            ResourceVersion = null,
                            BaseUrl = baseUri
                        });
                }
            }

            //Get Main distribution
            string baseUriDistTargetUrl = "";
            if (resource.Properties.ContainsKey(Graph.Metadata.Constants.Resource.MainDistribution))
            {
                List<dynamic> distList = resource.Properties[Graph.Metadata.Constants.Resource.MainDistribution];
                foreach (dynamic dist in distList)
                {
                    string mainDistributionPidUri = ((COLID.Graph.TripleStore.DataModels.Base.EntityBase)dist).Properties[Graph.Metadata.Constants.Resource.hasPID][0].Id;
                    string mainDistributionNetworkAddress = ((COLID.Graph.TripleStore.DataModels.Base.EntityBase)dist).Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.DistributionEndpoints.HasNetworkAddress, true);
                    bool isDistributionEndpointDeprecated = ((COLID.Graph.TripleStore.DataModels.Base.EntityBase)dist).Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.DistributionEndpoints.DistributionEndpointLifecycleStatus, true) == Common.Constants.DistributionEndpoint.LifeCycleStatus.Deprecated;
                    //string distBaseUri = ((COLID.Graph.TripleStore.DataModels.Base.EntityBase)dist).Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.DistributionEndpoints.HasNetworkAddress, true);
                    baseUriDistTargetUrl = isDistributionEndpointDeprecated ? pidUri : mainDistributionNetworkAddress;

                    resourceProxyDto.NestedProxies.Add(
                        new ResourceProxyDTO
                        {
                            PidUrl = mainDistributionPidUri,
                            TargetUrl = baseUriDistTargetUrl,
                            ResourceVersion = null,
                            BaseUrl = baseUri
                        });
                }
            }

            // Proxy for base URI                        
            if (!string.IsNullOrWhiteSpace(baseUri))
            {
                string resourceVersion = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasVersion, true);

                // distribution target uri is null -> base uri have to redirect to resourcePidUri
                if (!string.IsNullOrWhiteSpace(baseUriDistTargetUrl))
                {
                    if (Uri.TryCreate(baseUriDistTargetUrl, UriKind.Absolute, out _))
                    {
                        resourceProxyDto.NestedProxies.Add(new ResourceProxyDTO { PidUrl = baseUri, TargetUrl = string.IsNullOrWhiteSpace(baseUriDistTargetUrl) ? pidUri : baseUriDistTargetUrl, ResourceVersion = resourceVersion, BaseUrl = baseUri });
                    }
                    else
                    {
                        resourceProxyDto.NestedProxies.Add(new ResourceProxyDTO { PidUrl = baseUri, TargetUrl = pidUri, ResourceVersion = resourceVersion, BaseUrl = baseUri });
                    }
                }
                else
                {
                    resourceProxyDto.NestedProxies.Add(new ResourceProxyDTO { PidUrl = baseUri, TargetUrl = pidUri, ResourceVersion = resourceVersion });
                }
            }

            return resourceProxyDto;
        }
    }
}

