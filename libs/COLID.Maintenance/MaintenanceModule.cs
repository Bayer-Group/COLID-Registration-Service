using COLID.Maintenance.Filters;
using COLID.Maintenance.Services;
using Microsoft.Extensions.DependencyInjection;

namespace COLID.Maintenance
{
    public static class MaintenanceModule
    {
        public static IServiceCollection AddMaintenanceModule(this IServiceCollection services, IMvcBuilder mvcBuilder)
        {
            services.AddSingleton<IMaintenanceService, MaintenanceService>();
            services.AddTransient<MaintenanceFilter>();
            mvcBuilder.AddApplicationPart(typeof(MaintenanceModule).Assembly);
            mvcBuilder.AddMvcOptions(options => options.Filters.AddService<MaintenanceFilter>());
            return services;
        }
    }
}
