using COLID.Common.Utilities;
using COLID.Identity.Authorization;
using COLID.Identity.Requirements;
using COLID.Identity.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace COLID.Identity
{
    public static class IdentityModule
    {
        /// <summary>
        /// This will register all the supported functionality by service authentication module.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> object for registration.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> object for registration.</param>
        public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
        {
            Guard.ArgumentNotNull(configuration.GetSection("AzureAd"), "Configuration:AzureAd");
            services.Configure<AzureADOptions>(configuration.GetSection("AzureAd"));

            services.AddSingleton(typeof(ITokenService<>), typeof(TokenService<>));
            services.AddAuthorizationModule(configuration);
            return services;
        }

        /// <summary>
        ///     This will configure the authorization for the web api.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> object for registration.</param>
        /// <param name="configuration">The <see cref="IConfiguration" /> object for registration.</param>
        public static IServiceCollection AddAuthorizationModule(this IServiceCollection services, IConfiguration configuration)
        {
            var allowAnonymous = configuration.GetValue<bool>("AllowAnonymous");

            services.Configure<AzureADOptions>(configuration.GetSection("AzureAd"));

            if (allowAnonymous)
            {
                services.AddAuthentication(IISDefaults.AuthenticationScheme);
                services.AddSingleton<IAuthorizationHandler, AllowAnonymousHandler>();
            }
            else
            {
                services.AddAuthentication(AzureADDefaults.JwtBearerAuthenticationScheme)
                    .AddAzureADBearer(options => configuration.Bind("AzureAd", options))
                    .AddCookie();
            }

            services.AddAuthorization(options =>
            {
                options.AddPolicy(nameof(AdministratorRequirement), policy =>
                    policy.RequireRole(Constants.AuthorizationRoles.Admin, Constants.AuthorizationRoles.SuperAdmin));

                options.AddPolicy(nameof(SuperadministratorRequirement), policy =>
                    policy.RequireRole(Constants.AuthorizationRoles.SuperAdmin));
            });

            return services;
        }
    }
}
