using System.Linq;
using System.Net;
using System.Text;
using COLID.Maintenance.DataType;
using COLID.Maintenance.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;

namespace COLID.Maintenance.Filters
{
    public class MaintenanceFilter : IActionFilter
    {
        private readonly IMaintenanceService _maintenanceService;

        public MaintenanceFilter(IMaintenanceService maintenanceService)
        {
            _maintenanceService = maintenanceService;
        }

        public async void OnActionExecuting(ActionExecutingContext context)
        {
            if (ShallSkip(context)) return;

            if (_maintenanceService.IsInMaintenance())
            {
                // set the code to 503 for SEO reasons
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                context.HttpContext.Response.Headers.Add("Retry-After", _maintenanceService.RetryAfterInSeconds());
                context.HttpContext.Response.ContentType = _maintenanceService.ContentType();
                await context.HttpContext
                    .Response
                    .WriteAsync(JsonConvert.SerializeObject(_maintenanceService.DefaultResponse()), Encoding.UTF8).ConfigureAwait(false);
            }
        }

        private static bool ShallSkip(ActionExecutingContext context)
        {
            return context.ActionDescriptor.FilterDescriptors.Any(x => x.Filter is AllowMaintenanceAttribute);
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            /* Do nothing */
        }
    }
}
