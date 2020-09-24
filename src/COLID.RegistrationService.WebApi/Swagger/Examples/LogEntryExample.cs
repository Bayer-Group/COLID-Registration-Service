using System.Collections.Generic;
using COLID.StatisticsLog.DataModel;
using Swashbuckle.AspNetCore.Filters;

namespace COLID.RegistrationService.WebApi.Swagger.Examples
{
    /// <summary>
    /// Example for the LogEntry
    /// </summary>
    public class LogEntryExample : IExamplesProvider<LogEntry>
    {
        /// <summary>
        /// Generates and returns an example for the class to be shown in Swagger UI.
        /// This method gets called via reflection by Swashbuckle.
        /// </summary>
        /// <returns>An example of the class</returns>
        public LogEntry GetExamples()
        {
            return new LogEntry()
            {
                AdditionalInfo = new Dictionary<string, object>()
                {
                    ["0"] = new List<object>(),
                    ["user-agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.130 Safari/537.36"
                },
                AppId = "",
                ElapsedMilliseconds = null,
                HostName = "",
                Layer = "angular_client",
                Location = "",
                Message = "PID_WELCOME_PAGE_OPENED",
                Product = "6af602d8-71f2-4653-8009-68923720f3e4",
                UserId = ""
            };
        }
    }
}
