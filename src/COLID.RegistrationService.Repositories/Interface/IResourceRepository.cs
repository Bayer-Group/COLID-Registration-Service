using System;
using System.Collections.Generic;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.TripleStore.Repositories;
using COLID.RegistrationService.Common.DataModel.DistributionEndpoints;
using COLID.RegistrationService.Common.DataModel.Resources;
using COLID.RegistrationService.Common.DataModel.Search;
using COLID.RegistrationService.Common.DataModel.Validation;

namespace COLID.RegistrationService.Repositories.Interface
{
    /// <summary>
    /// Repository to handle all resource related operations.
    /// </summary>
    // TODO remove all occurrences of "Resource" in method names
    public interface IResourceRepository : IBaseRepository<Resource>
    {
        /// <summary>
        /// Gets the single resource with its properties and nested objects. References to other resources (linked resources) will be ignored.
        /// </summary>
        /// <param name="id">The unique id of the resource</param>
        /// <param name="resourceTypes">the resource type list to filter by</param>
        /// <returns>The resource containing the given PID URI</returns>
        Resource GetById(string id, IList<string> resourceTypes);

        /// <summary>
        /// Gets the single resource with its properties and nested objects. References to other resources (linked resources) will be ignored.
        /// </summary>
        /// <param name="pidUri">The unique PID URI of the resource</param>
        /// <param name="resourceTypes">the resource type list to filter by</param>
        /// <returns>The resource containing the given PID URI</returns>
        Resource GetByPidUri(Uri pidUri, IList<string> resourceTypes);

        Resource GetByPidUriAndColidEntryLifecycleStatus(Uri pidUri, Uri status, IList<string> resourceTypes);

        /// <summary>
        /// Gets the main resources with its properties and nested objects. References to other resources (linked resources) will be ignored.
        /// <list type="bullet">
        ///     <item><description>If a published state of the resource exists, the published resource will be returned</description></item>
        ///     <item><description>If the published state of the resources does not exist, the draft resource will be returned</description></item>
        /// </list>
        /// </summary>
        /// <param name="pidUri">The unique PID URI of the resource</param>
        /// <param name="resourceTypes">the resource type list to filter by</param>
        /// <returns>The main resource containing the given PID URI</returns>
        Resource GetMainResourceByPidUri(Uri pidUri, IList<string> resourceTypes);

        /// <summary>
        /// Gets both lifecycle states (draft and published) of a resource, if present.
        /// </summary>
        /// <param name="pidUri">The unique PID URI of the resource</param>
        /// <param name="resourceTypes">the resource type list to filter by</param>
        /// <returns>A transport object containing two different lifecycle versions of resources</returns>
        ResourcesCTO GetResourcesByPidUri(Uri pidUri, IList<string> resourceTypes);

        /// <summary>
        /// Gets the list of all distribution endpoints of one resource.
        /// </summary>
        /// <param name="pidUri">The unique PID URI of the resource</param>
        /// <param name="pidConceptsTypes">all sub types of pid concepts</param>
        /// <returns>List of all distribution endpints of one resource</returns>
        IList<DistributionEndpoint> GetDistributionEndpoints(Uri pidUri, IList<string> pidConceptsTypes);

        /// <summary>
        /// Gets the list of PID URIs of all inbound linked resources to a resource.
        /// </summary>
        /// <param name="pidUri">The unique PID URI of the resource</param>
        /// <returns>List of PID URIs of all inbound linked resources</returns>
        IList<string> GetAllInboundLinkedResourcePidUris(Uri pidUri);

        /// <summary>
        /// Gets the PID URI of the resource, which contains the distribution endpoint.
        /// </summary>
        /// <param name="pidUri">The unique PID URI of the distribution endpoint</param>
        /// <returns>The PID URI of the resource, null if distribution endpoint does not exists</returns>
        Uri GetPidUriByDistributionEndpointPidUri(Uri pidUri);

        /// <summary>
        /// Gets the Active Directory (AD) role for the distribution endpoint pid uri.
        /// An endpoint belongs to a resource. Each resource has a consumer group referenced, which is allowed to edit this resource.
        /// Each consumer group has one AD role attached.
        /// </summary>
        /// <param name="pidUri">PID URI of the distribution endpoint</param>
        /// <returns>The AD role indirectly attached to the distribution endpoint. <see cref="null"/> if no AD role present</returns>
        string GetAdRoleByDistributionEndpointPidUri(Uri pidUri);

        /// <summary>
        /// Gets a list of all target URI occurences.
        /// </summary>
        /// <param name="targetUri">target uri to search for</param>
        /// <param name="resourceTypes">the resource type list to filter by</param>
        /// <returns>List of <see cref="DuplicateResult"/>, empty list if no occurence</returns>
        IList<DuplicateResult> CheckTargetUriDuplicate(string targetUri, IList<string> resourceTypes);

        /// <summary>
        /// Searches for resources filtered by given criteria parameters.
        /// </summary>
        /// <param name="searchCriteria">Criteria parameters to search for</param>
        /// <param name="resourceTypes">the resource type list to filter by</param>
        /// <returns>Overview of resources matching the search criteria</returns>
        // TODO: add limit and offset to ResourceOverviewCTO
        ResourceOverviewCTO SearchByCriteria(ResourceSearchCriteriaDTO searchCriteria, IList<string> resourceTypes);

        /// <summary>
        /// Gets list of all resource versions by given PID URI.
        /// </summary>
        /// <param name="pidUri">PID URI of the resource</param>
        /// <returns>List of <see cref="VersionOverviewCTO"/>, empty list of no version found</returns>
        IList<VersionOverviewCTO> GetAllVersionsOfResourceByPidUri(Uri pidUri);

        /// <summary>
        /// Gets a list of all PID URI / Target Uri combinations of all published resources for NGINX proxy configuration.
        /// </summary>
        /// <param name="resourceTypes">the resource type list to filter by</param>
        /// <returns>List of <see cref="ResourceProxyDTO"/>, containing all PID URIs and target URIs of the published resources</returns>
        IList<ResourceProxyDTO> GetResourcesForProxyConfiguration(IList<string> resourceTypes);

        /// <summary>
        /// Gets the Active Directory (AD) role for the resource pid uri.
        /// Each resource has a consumer group referenced, which is allowed to edit this resource. Each consumer group has one AD role attached.
        /// </summary>
        /// <param name="pidUri">PID URI of the resource</param>
        /// <returns>The AD role indirectly attached to the resource. <see cref="null"/> if no AD role present</returns>
        string GetAdRoleForResource(Uri pidUri);

        /// <summary>
        /// Creates a new resource within the triple store (registry) including properties.
        /// </summary>
        /// <param name="resourceToCreate">the new resource to create</param>
        /// <param name="metadataProperties">the properties defined for the resource</param>
        void Create(Resource resourceToCreate, IList<MetadataProperty> metadataProperties);

        /// <summary>
        /// Deletes the draft resource from the triple store by a given pid URI.
        /// </summary>
        /// <param name="pidUri">the pidUri of entry to delete</param>
        /// <param name="toObject">the new object to add links to</param>
        void DeleteDraft(Uri pidUri, Uri toObject = null);

        /// <summary>
        /// Deletes the published resource from the triple store by a given pid URI.
        /// </summary>
        /// <param name="pidUri">the pidUri of entry to delete</param>
        /// <param name="toObject">the new object to add links to</param>
        void DeletePublished(Uri pidUri, Uri toObject = null);

        /// <summary>
        /// Deletes the marked for deletion resource from the triple store by a given pid URI.
        /// </summary>
        /// <param name="pidUri">the pidUri of entry to delete</param>
        /// <param name="toObject">the new object to add links to</param>
        void DeleteMarkedForDeletion(Uri pidUri, Uri toObject = null);

        /// <summary>
        /// Validate the existence of a resource by a given pidUri.
        /// </summary>
        /// <param name="pidUri">the pid uri to check for</param>
        /// <param name="resourceTypes">the resource type list to filter by</param>
        /// <returns>true if resource exists, otherwise false</returns>
        bool CheckIfExist(Uri pidUri, IList<string> resourceTypes);

        /// <summary>
        /// Determines all links pointing to an entry of a pid uri and links them to the new entry.  (re-linking).
        /// </summary>
        /// <param name="pidUri">the pid uri of entry to update links of</param>
        /// <param name="toObject">the Id of new object to add links to</param>
        void Relink(Uri pidUri, Uri toObject);

        /// <summary>
        /// Creates a link between the new published resource and the latest resource history version.
        /// </summary>
        /// <param name="resourcePidUri">the pid uri of the resource to link</param>
        void CreateLinkOnLatestHistorizedResource(Uri resourcePidUri);

        /// <summary>
        /// Creates links between the versions of an entry of both pid uris
        /// </summary>
        /// <param name="pidUri">the pid uri of the entry the link is based on</param>
        /// <param name="propertyUri">the predicate of the property to insert</param>
        /// <param name="linkToPidUri">the pid uri of the entry to which the link refers</param>
        void CreateLinkingProperty(Uri pidUri, Uri propertyUri, Uri linkToPidUri);

        /// <summary>
        /// Creates a link between two given lifecycle status of an entry, e.g. between the published and draft of a resource
        /// </summary>
        /// <param name="pidUri">the pid uri of the entry the link is based on</param>
        /// <param name="propertyUri">the predicate of the property to insert</param>
        /// <param name="lifeCycleStatus">the lifecycle status of the entry the link is based on</param>
        /// <param name="linkToLifceCycleStatus">the lifecycle status of the entry to which the link refers</param>
        void CreateLinkingProperty(Uri pidUri, Uri propertyUri, string lifeCycleStatus, string linkToLifceCycleStatus);

        /// <summary>
        /// Delete links between the versions of an entry of both pid uris
        /// </summary>
        /// <param name="pidUri">the pid uri of the entry the link is based on</param>
        /// <param name="propertyUri">the predicate of the property to insert</param>
        /// <param name="linkToPidUri">the pid uri of the entry to which the link refers</param>
        void DeleteLinkingProperty(Uri pidUri, Uri propertyUri, Uri linkToPidUri);

        /// <summary>
        /// Insert a new property into the graph (triple store).
        /// </summary>
        /// <param name="subject">the subject to insert</param>
        /// <param name="predicate">the predicate to insert</param>
        /// <param name="obj">the object to insert</param>
        void CreateProperty(Uri subject, Uri predicate, Uri obj);

        /// <summary>
        /// Insert a new property into the graph (triple store).
        /// </summary>
        /// <param name="subject">the subject to insert</param>
        /// <param name="predicate">the predicate to insert</param>
        /// <param name="literal">the literal to insert</param>
        void CreateProperty(Uri subject, Uri predicate, string literal);

        /// <summary>
        /// Delete a property, identified by all triple parameters.
        /// </summary>
        /// <param name="id">the Id</param>
        /// <param name="propertyUri">the predicate</param>
        /// <param name="obj">the object</param>
        void DeleteProperty(Uri id, Uri propertyUri, Uri obj);

        /// <summary>
        /// Delete all properties that matches the given id and predicates with wildcard objects.
        /// </summary>
        /// <param name="id">the id</param>
        /// <param name="predicate">the predicate</param>
        void DeleteAllProperties(Uri id, Uri predicate);
    }
}
