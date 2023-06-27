using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using COLID.Common.Utilities;
using COLID.RegistrationService.WebApi.Swagger.Filters;
using COLID.Swagger.Configuration;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace COLID.Swagger
{
    /// <summary>
    /// Contains the configuration of swagger documentation including all variable environments and versioning.
    /// </summary>
    public static class SwaggerModule
    {
        public static void AddColidSwaggerGeneration(this IServiceCollection services, IConfiguration configuration, params string[] apiVersionsAscending)
        {
            services.Configure<ColidSwaggerOptions>(configuration.GetSection(nameof(ColidSwaggerOptions)));

            var azureAdOptions = ParseAzureADOptions(configuration);
            var swaggerOptions = ParseColidSwaggerOptions(configuration);

            // The examples must be added before the Swagger generation.
            services.AddSwaggerExamplesFromAssemblies(Assembly.GetEntryAssembly());

            // IMPORTANT to set latest version on top!
            // If no version is given, generate the Swagger Documentation for a blank API v1 and map the API methods to it.
            var apiVersions = apiVersionsAscending != null && apiVersionsAscending.Count() > 0 ? apiVersionsAscending.Reverse() : new List<string>() { "1" };

            services.AddSwaggerGen(c =>
            {
                foreach (string versionNumber in apiVersions)
                {
                    c.SwaggerDoc($"v{versionNumber}", GetSwaggerInformation(swaggerOptions, $"v{versionNumber}"));
                }

                c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    In = ParameterLocation.Header,
                    Flows = new OpenApiOAuthFlows
                    {
                        Implicit = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri($"https://login.microsoftonline.com/{azureAdOptions.TenantId}/oauth2/authorize"),
                            Scopes = new Dictionary<string, string>(swaggerOptions.Scopes)
                        }
                    }
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" }
                        },
                        new List<string>(swaggerOptions.Scopes.Keys.ToArray())
                    }
                });

                c.ExampleFilters();

                if (apiVersionsAscending != null && apiVersionsAscending.Count() > 0)
                {
                    MapApiVersions(c, apiVersions);
                }

                // Set the comments path for the Swagger JSON and UI.
                var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{AppDomain.CurrentDomain.FriendlyName}.xml");
                c.IncludeXmlComments(xmlPath);

                // It is important that this filter is at the end of the swagger generation function
                c.OperationFilter<PolicyOperationFilter>();
            });
        }

        public static void UseColidSwaggerUI(this IApplicationBuilder app, IConfiguration configuration, params string[] apiVersionsAscending)
        {
            var azureAdOptions = ParseAzureADOptions(configuration);
            var swaggerOptions = ParseColidSwaggerOptions(configuration);

            // IMPORTANT to set latest version on top!
            // If no version is given, generate the Swagger Documentation for a blank API v1 and map the API methods to it.
            var apiVersions = apiVersionsAscending != null && apiVersionsAscending.Count() > 0 ? apiVersionsAscending.Reverse() : new List<string>() { "1" };
            var serviceName = GetServiceName();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                foreach (string versionNumber in apiVersions)
                {
                    c.SwaggerEndpoint($"/swagger/v{versionNumber}/swagger.json", $"COLID {serviceName} API v{versionNumber}");
                }
                c.OAuthClientId(swaggerOptions.ClientId);
                c.OAuthRealm(azureAdOptions.ClientId);
                c.OAuthAppName($"COLID {serviceName} API {swaggerOptions.EnvironmentLabel}");
                c.OAuthScopeSeparator(" ");
                c.OAuthAdditionalQueryStringParams(new Dictionary<string, string> { { "resource", azureAdOptions.ClientId } });
            });
        }

        /// <summary>
        /// Maps the API versions defined in controllers to the correct Swagger documentation version number
        /// </summary>
        /// <param name="c">The current SwaggerGenOptions to modify</param>
        private static void MapApiVersions(SwaggerGenOptions swaggerGenOptions, IEnumerable<string> apiVersions)
        {
            swaggerGenOptions.OperationFilter<RemoveVersionFromParameterFilter>();
            swaggerGenOptions.DocumentFilter<ReplaceVersionWithExactValueInPathFilter>();

            // Ensure the routes are added to the right Swagger doc
            swaggerGenOptions.DocInclusionPredicate((version, desc) =>
            {
                if (!desc.TryGetMethodInfo(out MethodInfo methodInfo))
                {
                    return false;
                }

                var methodName = methodInfo.ToString();

                var mapToApiVersionAttributes = methodInfo
                    .GetCustomAttributes()
                    .OfType<MapToApiVersionAttribute>()
                    .SelectMany(attr => attr.Versions)
                    .ToList();

                var apiVersionAttribute = desc
                    .CustomAttributes()
                    .OfType<ApiVersionAttribute>()
                    .SelectMany(attr => attr.Versions)
                    .ToList();

                Debug.WriteLine($"Method: {methodName} | " +
                    $"mapToApiVersionAttributes: {string.Join(',', mapToApiVersionAttributes.Select(v => v.ToString()))} | " +
                    $"apiVersionAttribute: {string.Join(',', apiVersionAttribute.Select(a => a.ToString()))} | " +
                    $"Desc: {desc.HttpMethod} ({version}) - {desc.RelativePath}");

                // IMPORTANT: Descending list order necessecary here
                string latestVersion = $"v{apiVersions.First()}";

                bool controllerContainsVersion = apiVersionAttribute.Any(v => $"v{v}" == version);

                if (!controllerContainsVersion)
                {
                    return false;
                }

                bool methodContainsVersion = mapToApiVersionAttributes.Any(v => $"v{v}" == version);
                bool noAnnotationAvailable = !mapToApiVersionAttributes.Any();

                // if no MapToAPIVersion set, then use latest one
                // else get mapped versions
                return (noAnnotationAvailable || methodContainsVersion);
            });
        }

        /// <summary>
        /// Generates the Open API Info for the Swagger documentation page.
        /// </summary>
        /// <param name="colidSwaggerOptions">the configuration for the Swagger module</param>
        /// <param name="version">The current version the Open API Info is for.</param>
        /// <returns>Open API Info Object, it provides the metadata about the Open API.</returns>
        private static OpenApiInfo GetSwaggerInformation(ColidSwaggerOptions colidSwaggerOptions, string version)
        {
            var description = CreateOpenApiInfoDescription(colidSwaggerOptions);
            var serviceName = GetServiceName();

            return new OpenApiInfo
            {
                Version = version,
                Title = $"COLID {serviceName} API {colidSwaggerOptions.EnvironmentLabel}",
                Description = description,
                Contact = new OpenApiContact
                {
                    Name = "Contact COLID team",
                    Email = colidSwaggerOptions.ContactEmail,
                    Url = new Uri(colidSwaggerOptions.DocumentationUrl)
                },
            };
        }

        /// <summary>
        /// Extracts the service name from the current app domain
        /// </summary>
        /// <returns>The service name</returns>
        private static string GetServiceName()
        {
            try
            {
                var serviceName = AppDomain.CurrentDomain.FriendlyName.Split('.')[1];
                serviceName = serviceName.Replace("Service", string.Empty, StringComparison.Ordinal);

                return serviceName;
            }
            catch(IndexOutOfRangeException)
            {
                return string.Empty;
            }
        }

        private static string CreateOpenApiInfoDescription(ColidSwaggerOptions colidSwaggerOptions)
        {
            var description = $"The {colidSwaggerOptions.EnvironmentLabel} API of COLID (Corporate Linked Data).";

            if (!string.IsNullOrWhiteSpace(colidSwaggerOptions.DocumentationApiUrl))
            {
                description += $"<br>Also see <b><a href='{colidSwaggerOptions.DocumentationUrl}'>API Documentation</a></b>";
            }

            description += $"<br><br><b>Note:</b>You need to authorize using the Authorize button prior using!";
            return description;
        }

        /// <summary>
        /// Gets the COLID AzureAd Options from AppSettings.
        /// </summary>
        /// <exception cref="ArgumentNullException">If the options are not set.</exception>
        /// <param name="configuration">key/value application configuration properties</param>
        /// <returns>configuration for azure ad</returns>
        private static AzureADOptions ParseAzureADOptions(IConfiguration configuration)
        {
            Guard.ArgumentNotNull(configuration.GetSection("AzureAd"), "Configuration:AzureAd");
            var azureADOptions = new AzureADOptions();
            configuration.Bind("AzureAd", azureADOptions);

            return azureADOptions;
        }

        /// <summary>
        /// Gets the COLID Swagger Options from AppSettings.
        /// </summary>
        /// <exception cref="ArgumentNullException">If the options are not set.</exception>
        /// <param name="configuration">key/value application configuration properties</param>
        /// <returns>configuration for swagger</returns>
        private static ColidSwaggerOptions ParseColidSwaggerOptions(IConfiguration configuration)
        {
            Guard.ArgumentNotNull(configuration.GetSection("ColidSwaggerOptions"), "Configuration:ColidSwaggerOptions");
            var colidSwaggerOptions = new ColidSwaggerOptions();
            configuration.Bind(nameof(ColidSwaggerOptions), colidSwaggerOptions);

            return colidSwaggerOptions;
        }
    }
}
