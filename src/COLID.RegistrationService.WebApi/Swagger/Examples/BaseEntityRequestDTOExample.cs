using System.Collections.Generic;
using COLID.Graph.Metadata.Constants;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.RegistrationService.Common.Constants;
using Swashbuckle.AspNetCore.Filters;
using Entity = COLID.Graph.TripleStore.DataModels.Base.Entity;

namespace COLID.RegistrationService.WebApi.Swagger.Examples
{
    /// <summary>
    /// Example for the BaseEntityRequestDto
    /// </summary>
    public class BaseEntityRequestDTOExample : IExamplesProvider<BaseEntityRequestDTO>
    {
        /// <summary>
        /// Generates and returns an example for the class to be shown in Swagger UI.
        /// This method gets called via reflection by Swashbuckle.
        /// </summary>
        /// <returns>An example of the class</returns>
        public BaseEntityRequestDTO GetExamples()
        {
            return new BaseEntityRequestDTO()
            {
                Properties = new Dictionary<string, List<dynamic>>()
                {
                    [RDF.Type] = new List<dynamic>() { DistributionEndpoint.Types.MaintenancePoint },
                    [Resource.DistributionEndpoints.HasContactPerson] = new List<dynamic>() { "any@bayer.com" },
                    [Resource.DistributionEndpoints.HasNetworkedResourceLabel] = new List<dynamic>() { "Direct link for editing" },
                    [Resource.DistributionEndpoints.DistributionEndpointLifecycleStatus] = new List<dynamic>() { DistributionEndpoint.LifeCycleStatus.Active },
                    [Resource.DistributionEndpoints.HasNetworkAddress] = new List<dynamic>() { "https://example.com/tool/table/graph.editor" },
                    [EnterpriseCore.PidUri] = new List<dynamic>()
                    {
                        new Entity()
                        {
                            Id = null,
                            Properties = new Dictionary<string, List<dynamic>>()
                            {
                                [RDF.Type] = new List<dynamic>() { Identifier.Type },
                                [Identifier.HasUriTemplate] = new List<dynamic>() { "https://pid.bayer.com/kos/19050#13cd004a-a410-4af5-a8fc-eecf9436b58b" }
                            }
                        }
                    }
                }
            };
        }
    }
}
