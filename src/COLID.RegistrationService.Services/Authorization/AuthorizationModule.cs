using COLID.RegistrationService.Services.Authorization.Handlers;
using COLID.RegistrationService.Services.Authorization.Requirements;
using COLID.RegistrationService.Services.Authorization.UserInfo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace COLID.RegistrationService.Services.Authorization
{
    public static class AuthorizationModule
    {
        /// <summary>
        ///     This will configure the authorization for the web api.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> object for registration.</param>
        /// <param name="configuration">The <see cref="IConfiguration" /> object for registration.</param>
        public static IServiceCollection AddRegistrationServiceAuthorizationModule(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddTransient<IAuthorizationHandler, ResourceAuthHandler>();
            services.AddTransient<IAuthorizationHandler, CreateDistributionEndpointAuthHandler>();
            services.AddTransient<IAuthorizationHandler, EditDistributionEndpointAuthHandler>();

            var allowAnonymous = configuration.GetValue<bool>("AllowAnonymous");

            if (allowAnonymous)
            {
                services.AddScoped<IUserInfoService, AnonymousUserInfoService>();
            }
            else
            {
                services.AddScoped<IUserInfoService, UserInfoService>();
            }

            services.AddAuthorization(options =>
            {
                options.AddPolicy(nameof(ResourceRequirement), policy =>
                    policy.Requirements.Add(new ResourceRequirement()));

                options.AddPolicy(nameof(CreateDistributionEndpointRequirement), policy =>
                    policy.Requirements.Add(new CreateDistributionEndpointRequirement()));

                options.AddPolicy(nameof(EditDistributionEndpointRequirement), policy =>
                    policy.Requirements.Add(new EditDistributionEndpointRequirement()));
            });

            return services;
        }
    }
}
