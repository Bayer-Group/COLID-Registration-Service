using System.Net.Mime;
using COLID.Cache.Services;
using Microsoft.Extensions.Logging;

namespace COLID.Maintenance.Services
{
    public class MaintenanceService : IMaintenanceService
    {
        private const string InMaintenanceModeKey = "InMaintenanceMode";
        private readonly ICacheService _cacheService;
        private readonly ILogger<MaintenanceService> _logger;
        public string RetryAfterInSeconds() => "1800";
        public string ContentType() => MediaTypeNames.Application.Json;
        public object DefaultResponse() => new { message = "Our site is under maintenance" };

        public MaintenanceService(ICacheService cacheService, ILogger<MaintenanceService> logger)
        {
            _cacheService = cacheService;
            _logger = logger;
        }

        public bool IsInMaintenance()
        {
            return _cacheService.GetValue<bool>(InMaintenanceModeKey);
        }

        public void UpdateInMaintenanceMode(bool inMaintenanceModeValue)
        {
            _logger.LogInformation("Services set to InMaintenance = {inMaintenanceModeValue}", inMaintenanceModeValue);
            _cacheService.Set<bool>(InMaintenanceModeKey, inMaintenanceModeValue);
        }

    }
}
