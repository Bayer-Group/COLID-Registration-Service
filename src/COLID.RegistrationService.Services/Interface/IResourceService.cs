using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.RegistrationService.Common.DataModel.DistributionEndpoints;
using COLID.RegistrationService.Common.DataModel.Resources;
using COLID.RegistrationService.Common.DataModel.Search;

namespace COLID.RegistrationService.Services.Interface
{
    /// <summary>
    /// Service to handle all resource related operations.
    /// </summary>
    public interface IResourceService
    {
        /// <summary>
        /// Gets the single resource with its properties and nested objects. References to other resources (linked resources) will be ignored.
        /// </summary>
        /// <param name="id">The unique id of the resource</param>
        /// <param name="resourceTypes">the resource type list to filter by</param>
        /// <returns>The resource containing the given PID URI</returns>
        Resource GetById(string id);

        /// <summary>
        /// Gets the single resource with its properties and nested objects. References to other resources (linked resources) will be ignored.
        /// </summary>
        /// <param name="pidUri">The unique PID URI of the resource</param>
        /// <returns>The resource containing the given PID URI</returns>
        Resource GetByPidUri(Uri pidUri);

        /// <summary>
        /// Gets the single resource with its properties and nested objects. References to other resources (linked resources) will be ignored.
        /// </summary>
        /// <param name="pidUri">The unique PID URI of the resource</param>
        /// <param name="lifecycleStatus">The lifecycle status of the resource</param>
        /// <returns></returns>
        Resource GetByPidUriAndLifecycleStatus(Uri pidUri, Uri lifecycleStatus);

        /// <summary>
        /// Gets the main resources with its properties and nested objects. References to other resources (linked resources) will be ignored.
        /// <list type="bullet">
        ///     <item><description>If a published state of the resource exists, the published resource will be returned</description></item>
        ///     <item><description>If the published state of the resources does not exist, the draft resource will be returned</description></item>
        /// </list>
        /// </summary>
        /// <param name="pidUri">The unique PID URI of the resource</param>
        /// <returns>The main resource containing the given PID URI</returns>
        Resource GetMainResourceByPidUri(Uri pidUri);

        /// <summary>
        /// Gets both lifecycle states (draft and published) of a resource, if present.
        /// </summary>
        /// <param name="pidUri">The unique PID URI of the resource</param>
        /// <returns>A transport object containing two different lifecycle versions of resources</returns>
        ResourcesCTO GetResourcesByPidUri(Uri pidUri);

        /// <summary>
        /// Searches for resources filtered by given criteria parameters.
        /// </summary>
        /// <param name="searchCriteria">Criteria parameters to search for</param>
        /// <returns>Overview of resources matching the search criteria</returns>
        ResourceOverviewCTO SearchByCriteria(ResourceSearchCriteriaDTO resourceSearchObject);

        /// <summary>
        /// Gets the list of all distribution endpoints of one resource.
        /// </summary>
        /// <param name="pidUri">The unique PID URI of the resource</param>
        /// <returns>List of all distribution endpints of one resource</returns>
        IList<DistributionEndpoint> GetDistributionEndpoints(Uri pidUri);

        /// <summary>
        /// Gets the PID URI of the resource, which contains the distribution endpoint.
        /// </summary>
        /// <param name="pidUri">The unique PID URI of the distribution endpoint</param>
        /// <returns>The PID URI of the resource, null if distribution endpoint does not exists</returns>
        Uri GetPidUriByDistributionEndpointPidUri(Uri pidUri);

        /// <summary>
        /// Searches for a valid resource to the given pid uri and marks this resource for deletion.
        /// </summary>
        /// <param name="pidUri">The unique PID URI of the resource</param>
        /// <param name="requester">The user who wants to mark the resource for deletion</param>
        /// <returns>a message of the successful mark</returns>
        Task<string> MarkResourceAsDeletedAsync(Uri pidUri, string requester);

        /// <summary>
        /// Searches for a valid resource to the given pid uri and unmarks this resource for deletion.
        /// </summary>
        /// <param name="pidUri"></param>
        /// <returns>a message of the successful unmark</returns>
        string UnmarkResourceAsDeleted(Uri pidUri);

        /// <summary>
        /// Gets the Active Directory (AD) role for the resource pid uri.
        /// Each resource has a consumer group referenced, which is allowed to edit this resource. Each consumer group has one AD role attached.
        /// </summary>
        /// <param name="pidUri">PID URI of the resource</param>
        /// <returns>The AD role indirectly attached to the resource. <see cref="null"/> if no AD role present</returns>
        string GetAdRoleForResource(Uri pidUri);

        /// <summary>
        /// Gets the Active Directory (AD) role for the distribution endpoint pid uri.
        /// An endpoint belongs to a resource. Each resource has a consumer group referenced, which is allowed to edit this resource. Each consumer group has one AD role attached.
        /// </summary>
        /// <param name="pidUri">PID URI of the distribution endpoint</param>
        /// <returns>The AD role indirectly attached to the distribution endpoint. <see cref="null"/> if no AD role present</returns>
        string GetAdRoleByDistributionEndpointPidUri(Uri pidUri);

        /// <summary>
        /// Checks and validates the given resource request and creates a new one, in case of success.
        /// </summary>
        /// <param name="resource">the new resource to create</param>
        Task<ResourceWriteResultCTO> CreateResource(ResourceRequestDTO resource);

        /// <summary>
        /// Edits the resource, determined by the given pid uri. The passed resource object will be used
        /// to edit the existing resource. A validation will also be accomplished. The edit of a resource
        /// includes the deletion and creation of related identifiers
        /// </summary>
        /// <param name="pidUri">the resource identfier to updaet</param>
        /// <param name="resource">the new resource to exchange</param>
        Task<ResourceWriteResultCTO> EditResource(Uri pidUri, ResourceRequestDTO resource);

        /// <summary>
        /// Publish the resource, determined by the given pid uri. A validation will also be accomplished.
        /// This process also deletes found draft version and has some similarities like editing of a resource.
        /// </summary>
        /// <param name="pidUri">the resource identfier to updaet</param>
        Task<ResourceWriteResultCTO> PublishResource(Uri pidUri);

        /// <summary>
        /// Deletes a resource from the triple store by a given PID URI. This includes:
        /// <list type="bullet">
        ///   <item>deletion of properties</item>
        ///   <item>deletion of draft resources</item>
        ///   <item>resource unlinking</item>
        ///   <item>resources that has been marked for deletion (if group admin)</item>
        /// </list>
        /// </summary>
        /// <param name="pidUri">The PID URI</param>
        string DeleteResource(Uri pidUri);

        /// <summary>
        /// Delete multiple ressources, that are marked for deletion (published) or drafts, identified by the given list of pidUris.
        /// </summary>
        /// <param name="pidUris">a list of pidUris to delete</param>
        /// <returns>a list of failures during deletion with uri and reason</returns>
        IList<ResourceMarkedOrDeletedResult> DeleteMarkedForDeletionResources(IList<Uri> pidUris);

        /// <summary>
        /// Unmark multiple ressources from deletion, that are marked yet. Those will be identified by the given list of pidUris.
        /// </summary>
        /// <param name="pidUris">a list of pidUris to unmark</param>
        /// <returns>a list of failures during unmark/rejecting with uri and reason</returns>
        IList<ResourceMarkedOrDeletedResult> UnmarkResourcesAsDeleted(IList<Uri> pidUris);

    }
}
