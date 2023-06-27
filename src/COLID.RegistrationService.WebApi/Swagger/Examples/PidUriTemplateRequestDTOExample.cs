using System.Collections.Generic;
using COLID.Common.Extensions;
using COLID.Graph.Metadata.Constants;
using COLID.RegistrationService.Common.DataModel.PidUriTemplates;
using COLID.RegistrationService.Common.Enums.PidUriTemplate;
using COLID.RegistrationService.Common.Extensions;
using Swashbuckle.AspNetCore.Filters;

namespace COLID.RegistrationService.WebApi.Swagger.Examples
{
    /// <summary>
    /// Example for the PidUriTemplateRequestDTO
    /// </summary>
    public class PidUriTemplateRequestDTOExample : IExamplesProvider<PidUriTemplateRequestDTO>
    {
        /// <summary>
        /// Generates and returns an example for the class to be shown in Swagger UI.
        /// This method gets called via reflection by Swashbuckle.
        /// </summary>
        /// <returns>An example of the class</returns>
        public PidUriTemplateRequestDTO GetExamples()
        {
            return new PidUriTemplateRequestDTO
            {
                Properties = new Dictionary<string, List<dynamic>>()
                {
                    [RDF.Type] = new List<dynamic>() { COLID.Graph.Metadata.Constants.PidUriTemplate.Type },
                    [COLID.Graph.Metadata.Constants.PidUriTemplate.HasBaseUrl] = new List<dynamic>() { "https://qa-pid.bayer.com/" },
                    [COLID.Graph.Metadata.Constants.PidUriTemplate.HasRoute] = new List<dynamic>() { "rnd/" },
                    [COLID.Graph.Metadata.Constants.PidUriTemplate.HasPidUriTemplateIdType] = new List<dynamic>() { IdType.Guid.GetDescription() },
                    [COLID.Graph.Metadata.Constants.PidUriTemplate.HasIdLength] = new List<dynamic>() { "0" },
                    [COLID.Graph.Metadata.Constants.PidUriTemplate.HasPidUriTemplateSuffix] = new List<dynamic>() { Suffix.Slash.GetDescription() }
                }
            };
        }
    }
}
