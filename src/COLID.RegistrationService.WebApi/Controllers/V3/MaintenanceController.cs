using COLID.Maintenance.DataType;
using COLID.Maintenance.Services;
using COLID.StatisticsLog.Type;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using COLID.Identity.Requirements;
using COLID.RegistrationService.WebApi.Constants;

namespace COLID.Maintenance.Controller
{
    /// <summary>
    /// 
    /// </summary>
    [ApiController]
    [Authorize]
    [ApiVersion(API.Version.V1)]
    [ApiVersion(API.Version.V2)]
    [ApiVersion(API.Version.V3)]
    [Route("api/v{version:apiVersion}/maintenance")]
    public class MaintenanceController : ControllerBase
    {
        private readonly IMaintenanceService _maintenanceService;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="maintenanceService"></param>
        public MaintenanceController(IMaintenanceService maintenanceService)
        {
            _maintenanceService = maintenanceService;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inMaintenanceModeValue"></param>
        /// <returns></returns>
        [HttpPut]
        [Log(LogType.AuditTrail)]
        [AllowMaintenance]
        [Authorize(Policy = nameof(SuperadministratorRequirement))]
        public IActionResult UpdateInMaintenanceMode([FromBody] bool inMaintenanceModeValue = false)
        {
            _maintenanceService.UpdateInMaintenanceMode(inMaintenanceModeValue);
            return Ok();
        }
    }
}
