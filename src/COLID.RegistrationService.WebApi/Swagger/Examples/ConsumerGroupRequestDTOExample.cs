using System.Collections.Generic;
using COLID.Graph.Metadata.Constants;
using COLID.Graph.TripleStore.DataModels.ConsumerGroups;
using Swashbuckle.AspNetCore.Filters;

namespace COLID.RegistrationService.WebApi.Swagger.Examples
{
    /// <summary>
    /// Example for the ConsumerGroupRequestDTO
    /// </summary>
    public class ConsumerGroupRequestDTOExample : IExamplesProvider<ConsumerGroupRequestDTO>
    {
        /// <summary>
        /// Generates and returns an example for the class to be shown in Swagger UI.
        /// This method gets called via reflection by Swashbuckle.
        /// </summary>
        /// <returns>An example of the class</returns>
        public ConsumerGroupRequestDTO GetExamples()
        {
            return new ConsumerGroupRequestDTO
            {
                Properties = new Dictionary<string, List<dynamic>>()
                {
                    [RDF.Type] = new List<dynamic>() { Graph.Metadata.Constants.ConsumerGroup.Type },
                    [Graph.Metadata.Constants.ConsumerGroup.AdRole] = new List<dynamic>() { "PID.Group10Data.ReadWrite" },
                    [Graph.Metadata.Constants.ConsumerGroup.HasPidUriTemplate] = new List<dynamic>() { "https://pid.bayer.com/kos/19050#daff4c0a-27d9-4ddc-9365-95fca2deed41" },
                    [RDFS.Label] = new List<dynamic>() { "Research & Development" }
                }
            };
        }
    }
}
