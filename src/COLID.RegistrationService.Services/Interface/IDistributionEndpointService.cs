using System;
using System.Threading.Tasks;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.RegistrationService.Common.DataModel.Resources;

namespace COLID.RegistrationService.Services.Interface
{
    /// <summary>
    /// Service to handle all distribution endpoint related operations.
    /// </summary>
    public interface IDistributionEndpointService
    {
        /// <summary>
        /// Creates a distribution endpoint and appends it to the resource of the given pid uri. If there is no draft resource, a new draft is created. This resource must be published separately.
        /// </summary>
        /// <param name="resourcePidUri">Pid uri of the resource to which the distribution endpoint is to be attached.</param>
        /// <param name="mainDistributionEndpoint">Specifies whether an endpoint is stored as a main distribution endpoint.</param>
        /// <param name="requestDistributionEndpoint">Distribution endpoint to create.</param>
        /// <returns></returns>
        Task<ResourceWriteResultCTO> CreateDistributionEndpoint(Uri resourcePidUri, bool createAsMainDistributionEndpoint, BaseEntityRequestDTO requestDistributionEndpoint);

        /// <summary>
        /// Edit a distribution endpoint and appends to given pid uri. If there is no draft resource, a new draft is created. This resource must be published separately.
        /// </summary>
        /// <param name="distributionEndpointPidUri">Pid uri of the distribution endpoint to be edited</param>
        /// <param name="editAsMainDistributionEndpoint">Specifies whether an endpoint is stored as a main distribution endpoint.</param>
        /// <param name="requestDistributionEndpoint">Distribution endpoint to be edited</param>
        /// <returns></returns>
        Task<ResourceWriteResultCTO> EditDistributionEndpoint(Uri distributionEndpointPidUri, bool editAsMainDistributionEndpoint, BaseEntityRequestDTO requestDistributionEndpoint);

        /// <summary>
        /// Deletes the endpoint distribution with the given pid uri
        /// </summary>
        /// <param name="distributionEndpointPidUri">Pid uri of the distribution endpoint to be deleted</param>
        /// <returns></returns>
        void DeleteDistributionEndpoint(Uri distributionEndpointPidUri);
    }
}
