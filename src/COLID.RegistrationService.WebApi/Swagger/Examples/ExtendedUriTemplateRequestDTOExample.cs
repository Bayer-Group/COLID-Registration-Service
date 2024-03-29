﻿using System.Collections.Generic;
using COLID.Graph.Metadata.Constants;
using COLID.RegistrationService.Common.DataModel.ExtendedUriTemplates;
using Swashbuckle.AspNetCore.Filters;

namespace COLID.RegistrationService.WebApi.Swagger.Examples
{
    /// <summary>
    /// Example for the ExtendedUriTemplateRequestDTO
    /// </summary>
    public class ExtendedUriTemplateRequestDTOExample : IExamplesProvider<ExtendedUriTemplateRequestDTO>
    {
        /// <summary>
        /// Generates and returns an example for the class to be shown in Swagger UI.
        /// This method gets called via reflection by Swashbuckle.
        /// </summary>
        /// <returns>An example of the class</returns>
        public ExtendedUriTemplateRequestDTO GetExamples()
        {
            return new ExtendedUriTemplateRequestDTO
            {
                Properties = new Dictionary<string, List<dynamic>>()
                {
                    [RDF.Type] = new List<dynamic>() { COLID.Graph.Metadata.Constants.ExtendedUriTemplate.Type },
                    [COLID.Graph.Metadata.Constants.ExtendedUriTemplate.HasTargetUriMatchRegex] = new List<dynamic>() { "^(.*)$" },
                    [COLID.Graph.Metadata.Constants.ExtendedUriTemplate.HasPidUriSearchRegex] = new List<dynamic>() { "^https://qa-pid.bayer.com(.*)$" },
                    [COLID.Graph.Metadata.Constants.ExtendedUriTemplate.HasReplacementString] = new List<dynamic>() { "{targetUri}$1" },
                    [COLID.Graph.Metadata.Constants.ExtendedUriTemplate.UseHttpScheme] = new List<dynamic>() { "true" },
                    [COLID.Graph.Metadata.Constants.ExtendedUriTemplate.HasOrder] = new List<dynamic>() { "4" },
                    [RDFS.Label] = new List<dynamic>() { "Standard case" }
                }
            };
        }
    }
}
