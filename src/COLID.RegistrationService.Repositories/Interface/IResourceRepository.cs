using System;
using System.Collections.Generic;
using COLID.Graph.Metadata.DataModels.Metadata;

using COLID.Graph.Metadata.DataModels.Resources;

using COLID.Graph.TripleStore.Repositories;

using COLID.RegistrationService.Common.DataModel.DistributionEndpoints;

using COLID.RegistrationService.Common.DataModel.Resources;

using COLID.RegistrationService.Common.DataModel.Search;

using COLID.RegistrationService.Common.DataModel.Validation;

using COLID.RegistrationService.Common.DataModels.LinkHistory;
using COLID.RegistrationService.Common.DataModels.Resources;

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
        /// <param name="namedGraph">Named graph for current resources</param> 
        /// <returns>The resource containing the given PID URI</returns> 
        Resource GetById(string id, IList<string> resourceTypes, Uri namedGraph);

        /// <summary> 
        /// Gets the single resource with its properties and nested objects. References to other resources (linked resources) will be ignored. 
        /// </summary> 
        /// <param name="pidUri">The unique PID URI of the resource</param> 
        /// <param name="resourceTypes">the resource type list to filter by</param> 
        /// <param name="namedGraph">Named graph for current resources</param> 
        /// <returns>The resource containing the given PID URI</returns> 
        Resource GetByPidUri(Uri pidUri, IList<string> resourceTypes, Dictionary<Uri, bool> graphsToSearchIn);

        /// <summary> 
        /// Gets the list of resources with its properties and nested objects. References to other resources (linked resources) will be ignored. 
        /// </summary> 
        /// <param name="pidUris">The unique PID URIs of the resource</param> 
        /// <param name="resourceTypes">the resource type list to filter by</param> 
        /// <param name="namedGraph">Named graph for current resources</param> 
        /// <returns>The resource containing the given PID URI</returns> 
        IList<Resource> GetByPidUris(List<Uri> pidUris, IList<string> resourceTypes, Uri namedGraph);


        Resource GetByPidUriAndColidEntryLifecycleStatus(Uri pidUri, Uri status, IList<string> resourceTypes, Dictionary<Uri, bool> namedGraphs);

        /// <summary> 
        /// Gets the main resources with its properties and nested objects. References to other resources (linked resources) will be ignored. 
        /// <list type="bullet"> 
        ///     <item><description>If a published state of the resource exists, the published resource will be returned</description></item> 
        ///     <item><description>If the published state of the resources does not exist, the draft resource will be returned</description></item> 
        /// </list> 
        /// </summary> 
        /// <param name="pidUri">The unique PID URI of the resource</param> 
        /// <param name="resourceTypes">the resource type list to filter by</param> 
        /// <param name="namedGraph">Named graph for current resources</param> 
        /// <returns>The main resource containing the given PID URI</returns> 
        Resource GetMainResourceByPidUri(Uri pidUri, IList<string> resourceTypes, Dictionary<Uri, bool> namedGraphs);

        /// <summary> 
        /// Gets both lifecycle states (draft and published) of a resource, if present. 
        /// </summary> 
        /// <param name="pidUri">The unique PID URI of the resource</param> 
        /// <param name="resourceTypes">the resource type list to filter by</param> 
        /// <param name="namedGraph">Named graph for current resources</param> 
        /// <returns>A transport object containing two different lifecycle versions of resources</returns> 
        ResourcesCTO GetResourcesByPidUri(Uri pidUri, IList<string> resourceTypes, Dictionary<Uri, bool> namedGraphs);

        /// <summary> 
        /// Gets the list of all distribution endpoints of one resource. 
        /// </summary> 
        /// <param name="pidUri">The unique PID URI of the resource</param> 
        /// <param name="pidConceptsTypes">all sub types of pid concepts</param> 
        /// <param name="namedGraph">Named graph for current resources</param> 
        /// <returns>List of all distribution endpints of one resource</returns> 
        IList<DistributionEndpoint> GetDistributionEndpoints(Uri pidUri, IList<string> pidConceptsTypes, Uri namedGraph);

        /// <summary>

        /// Get the list of all distribution endpoints of all resources

        /// </summary>

        /// <param name="resourceType"></param>

        /// <param name="namedGraph"></param>

        /// <returns></returns> 
        IList<DistributionEndpointsTest> GetDistributionEndpoints(string resourceType, Uri namedGraph);

        /// <summary> 
        /// Gets the list of PID URIs of all inbound linked resources to a resource. 
        /// </summary> 
        /// <param name="pidUri">The unique PID URI of the resource</param> 
        /// <param name="namedGraph">Named graph for current resources</param> 
        /// <returns>List of PID URIs of all inbound linked resources</returns> 
        Dictionary<string, List<LinkingMapping>> GetInboundLinksOfPublishedResource(Uri pidUri, Uri namedGraph, ISet<string> LinkTypeList);

        /// <summary> 
        /// Gets the PID URI of the resource, which contains the distribution endpoint. 
        /// </summary> 
        /// <param name="pidUri">The unique PID URI of the distribution endpoint</param> 
        /// <param name="namedGraph">Named graph for current resources</param> 
        /// <returns>The PID URI of the resource, null if distribution endpoint does not exists</returns> 
        Uri GetPidUriByDistributionEndpointPidUri(Uri pidUri, ISet<Uri> namedGraph);

        /// <summary> 
        /// Gets all resource links of the published Graph 
        /// </summary> 
        /// <param name="pidUri">The unique PID URI of the resource</param> 
        /// <param name="namedGraph">Named graph for current resources</param> 
        /// <returns>The outbound links of the resource, null if no link exists</returns>
        Dictionary<string, List<LinkingMapping>> GetOutboundLinksOfPublishedResource(Uri pidUri, Uri namedGraph, ISet<string> LinkTypeList);

        /// <summary>
        /// Gets all resource links of the published Graph 
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="pidUris"></param>
        /// <param name="namedGraph"></param>
        /// <param name="LinkTypeList"></param>
        /// <returns>The outbound/inbound links of the resource, null if no link exists</returns>
        void GetLinksOfPublishedResources(List<Resource> resources, IList<Uri> pidUris, Uri namedGraph, ISet<string> LinkTypeList);

        /// <summary> 
        /// Retrieves all resources of a specified consumer group from today to the specified end date for review 
        /// </summary> 
        /// <param name="consumerGroup"></param> 
        /// <param name="endDate"></param> 
        /// <returns>Retrieves all resources of a specified consumer group from today to the specified end date for review</returns> 
        IList<Resource> GetDueResources(Uri consumerGroup, DateTime endDate, Uri namedGraph, IList<string> resourceTypes);

        /// <summary> 
        /// Gets the Active Directory (AD) role for the distribution endpoint pid uri. 
        /// An endpoint belongs to a resource. Each resource has a consumer group referenced, which is allowed to edit this resource. 
        /// Each consumer group has one AD role attached. 
        /// </summary> 
        /// <param name="pidUri">PID URI of the distribution endpoint</param> 
        /// <param name="resourceNamedGraph">Named graph for current resources</param> 
        /// <param name="consumerGroupNamedGraph">Named graph for consumer groups</param> 
        /// <returns>The AD role indirectly attached to the distribution endpoint. <see cref="null"/> if no AD role present</returns> 
        string GetAdRoleByDistributionEndpointPidUri(Uri pidUri, ISet<Uri> resourceNamedGraph, Uri consumerGroupNamedGraph);

        /// <summary> 
        /// Gets a list of all target URI occurences. 
        /// </summary> 
        /// <param name="targetUri">target uri to search for</param> 
        /// <param name="resourceTypes">the resource type list to filter by</param> 
        /// <param name="resourceNamedGraph">Named graph for current resources</param> 
        /// <param name="metaDataNamedGraphs">Metadata Named Graphs</param> 
        /// <returns>List of <see cref="DuplicateResult"/>, empty list if no occurence</returns> 
        IList<DuplicateResult> CheckTargetUriDuplicate(string targetUri, IList<string> resourceTypes, ISet<Uri> namedGraph, ISet<Uri> metaDataNamedGraphs);

        /// <summary> 
        /// Searches for resources filtered by given criteria parameters. 
        /// </summary> 
        /// <param name="searchCriteria">Criteria parameters to search for</param> 
        /// <param name="resourceTypes">the resource type list to filter by</param> 
        /// <param name="namedGraph">Named graph for current resources</param> 
        /// <returns>Overview of resources matching the search criteria</returns> 
        // TODO: add limit and offset to ResourceOverviewCTO 
        ResourceOverviewCTO SearchByCriteria(ResourceSearchCriteriaDTO searchCriteria, IList<string> resourceTypes, Uri publishedGraph, Uri draftGraph);

        /// <summary> 
        /// Gets list of all resource versions by given PID URI. 
        /// </summary> 
        /// <param name="pidUri">PID URI of the resource</param> 
        /// <param name="namedGraph">Named graph for current resources</param> 
        /// <returns>List of <see cref="VersionOverviewCTO"/>, empty list of no version found</returns> 
        IList<VersionOverviewCTO> GetAllVersionsOfResourceByPidUri(Uri pidUri, ISet<Uri> namedGraph);



        /// <summary> 
        /// Creates Additionals and Removals Graphs for each update of the published resource 
        /// </summary> 
        /// <param name="additionals">Resource properties that are new or updated</param> 
        /// <param name="removals">Resource properties that have been changed</param> 
        /// <param name="allMetaData">resource metadata</param> 
        /// <param name="id">ID of the resource</param> 
        /// <param name="revisionGraphPrefix">Name prefix of the new Revision graphs</param> 
        /// <returns>List of <see cref="VersionOverviewCTO"/>, empty list of no version found</returns>

        void CreateAdditionalsAndRemovalsGraphs(Dictionary<string, List<dynamic>> additionals, Dictionary<string, List<dynamic>> removals, IList<MetadataProperty> allMetaData, string id, string revisionGraphPrefix);



        /// <summary> 
        /// Gets a list of all PID URI / Target Uri combinations of all published resources for NGINX proxy configuration. 
        /// </summary> 
        /// <param name="resourceTypes">the resource type list to filter by</param> 
        /// <param name="namedGraph">Named graph for current resources</param> 
        /// <param name="pidUri">Optional pidUri to get a single resource</param> 
        /// <returns>List of <see cref="ResourceProxyDTO"/>, containing all PID URIs and target URIs of the published resources</returns> 
        IList<ResourceProxyDTO> GetResourcesForProxyConfiguration(IList<string> resourceTypes, Uri namedGraph, Uri pidUri = null);

        /// <summary> 
        /// Gets the Active Directory (AD) role for the resource pid uri. 
        /// Each resource has a consumer group referenced, which is allowed to edit this resource. Each consumer group has one AD role attached. 
        /// </summary> 
        /// <param name="pidUri">PID URI of the resource</param> 
        /// <param name="resourceNamedGraph">Named graph for current resources</param> 
        /// <param name="consumerGroupNamedGraph">Named graph for consumer groups</param> 
        /// <returns>The AD role indirectly attached to the resource. <see cref="null"/> if no AD role present</returns> 
        string GetAdRoleForResource(Uri pidUri, ISet<Uri> namedGraph, Uri consumerGroupNamedGraph);

        /// <summary> 
        /// Creates a new resource within the triple store (registry) including properties. 
        /// </summary> 
        /// <param name="resourceToCreate">the new resource to create</param> 
        /// <param name="metadataProperties">the properties defined for the resource</param> 
        /// <param name="namedGraph">Named graph for current resources</param> 
        void Create(Resource resourceToCreate, IList<MetadataProperty> metadataProperties, Uri namedGraph);

        /// <summary> 
        /// Deletes the draft resource from the triple store by a given pid URI. 
        /// </summary> 
        /// <param name="pidUri">the pidUri of entry to delete</param> 
        /// <param name="toObject">the new object to add links to</param> 
        void DeleteDraft(Uri pidUri, Uri toObject, Uri namedGraph);

        /// <summary> 
        /// Deletes the published resource from the triple store by a given pid URI. 
        /// </summary> 
        /// <param name="pidUri">the pidUri of entry to delete</param> 
        /// <param name="toObject">the new object to add links to</param> 
        /// <param name="namedGraph">Named graph for current resources</param> 
        void DeletePublished(Uri pidUri, Uri toObject, Uri namedGraph);

        /// <summary> 
        /// Deletes the marked for deletion resource from the triple store by a given pid URI. 
        /// </summary> 
        /// <param name="pidUri">the pidUri of entry to delete</param> 
        /// <param name="toObject">the new object to add links to</param> 
        /// <param name="namedGraph">Named graph for current resources</param> 
        void DeleteMarkedForDeletion(Uri pidUri, Uri toObject, Uri namedGraph);



        /// <summary> 
        /// Creates a linkhistory entry of a specific link between two resources or sets the status of an existing linkhistory entry to 'created'. 
        /// </summary> 
        /// <param name="linkHistory">the linkhistory entry to add in the link history graph</param> 
        /// <param name="namedGraph">Named of the link history graph</param>

        void CreateLinkHistoryEntry(LinkHistoryCreateDto linkHistory, Uri namedGraph, Uri CreateLinkHistoryEntry);







        /// <summary> 
        /// Checks if the Link entry with same link type exists in linkhistory'. 
        /// </summary> 
        /// <param name="linkStart">the piduri of the source resource</param> 
        /// <param name="linkType">the link type</param> 
        /// <param name="linkEnd">the piduri of the target resource</param> 
        /// <param name="linkHistoryGraph">Named of the link history graph</param>

        Uri GetLinkHistoryRecord(Uri linkStart, Uri linkType, Uri linkEnd, Uri linkHistoryGraph, Uri resourceGraph);
        List<LinkHistoryCreateDto> GetLinkHistoryRecords(Uri linkHistoryGraph);


        /// <summary> 
        /// Validate the existence of a resource by a given pidUri. 
        /// </summary> 
        /// <param name="pidUri">the pid uri to check for</param> 
        /// <param name="resourceTypes">the resource type list to filter by</param> 
        /// <returns>true if resource exists, otherwise false</returns> 
        /// <param name="namedGraph">Named graph for current resources</param> 
        bool CheckIfExist(Uri pidUri, IList<string> resourceTypes, Uri namedGraph);

        /// <summary> 
        /// Determines all links pointing to an entry of a pid uri and links them to the new entry.  (re-linking). 
        /// </summary> 
        /// <param name="pidUri">the pid uri of entry to update links of</param> 
        /// <param name="toObject">the Id of new object to add links to</param> 
        /// <param name="namedGraph">Named graph for current resources</param> 
        void Relink(Uri pidUri, Uri toObject, Uri namedGraph);

        /// <summary> 
        /// Creates a link between the new published resource and the latest resource history version. 
        /// </summary> 
        /// <param name="resourcePidUri">the pid uri of the resource to link</param> 
        /// <param name="resourceNamedGraph">Named graph for current resources</param> 
        /// <param name="historicNamedGraph">Named graph for historic resources</param> 
        void CreateLinkOnLatestHistorizedResource(Uri resourcePidUri, Uri resourceNamedGraph, Uri historicNamedGraph);

        /// <summary> 
        /// Creates links between the versions of an entry of both pid uris 
        /// </summary> 
        /// <param name="pidUri">the pid uri of the entry the link is based on</param> 
        /// <param name="propertyUri">the predicate of the property to insert</param> 
        /// <param name="linkToPidUri">the pid uri of the entry to which the link refers</param> 
        /// <param name="namedGraph">Named graph for current resources</param> 
        void CreateLinkingProperty(Uri pidUri, Uri propertyUri, Uri linkToPidUri, Uri namedGraph);



        /// <summary> 
        /// Creates a link between two given lifecycle status of an entry, e.g. between the published and draft of a resource 
        /// </summary> 
        /// <param name="pidUri">the pid uri of the entry the link is based on</param> 
        /// <param name="propertyUri">the predicate of the property to insert</param> 
        /// <param name="lifeCycleStatus">the lifecycle status of the entry the link is based on</param> 
        /// <param name="linkToLifeCycleStatus">the lifecycle status of the entry to which the link refers</param> 
        /// <param name="namedGraph">Named graph for current resources</param> 
        void CreateLinkingProperty(Uri pidUri, Uri propertyUri, string lifeCycleStatus, string linkToLifeCycleStatus, Uri namedGraph);

        /// <summary> 
        /// Gets the PidUri of a resource based on the source Id 
        /// </summary> 
        /// <param name="sourceId">the source id of the entry</param> 
        /// <param name="namedGraph">Named graphs that should be searched</param> 
        Uri GetPidUriBySourceId(string sourceId, ISet<Uri> namedGraph);

        /// <summary> 
        /// Gets the internal identifier of a resource based on the pid uri 
        /// </summary> 
        /// <param name="pidUri">the pid uri of the entry</param> 
        /// <param name="namedGraph">Named graphs that should be searched</param> 
        Uri GetIdByPidUri(Uri pidUri, ISet<Uri> namedGraph);

        /// <summary> 
        /// Delete links between the versions of an entry of both pid uris 
        /// </summary> 
        /// <param name="pidUri">the pid uri of the entry the link is based on</param> 
        /// <param name="propertyUri">the predicate of the property to insert</param> 
        /// <param name="linkToPidUri">the pid uri of the entry to which the link refers</param> 
        /// <param name="namedGraph">Named graph for current resources</param> 
        void DeleteLinkingProperty(Uri pidUri, Uri propertyUri, Uri linkToPidUri, Uri namedGraph);

        /// <summary> 
        /// Insert a new property into the graph (triple store). 
        /// </summary> 
        /// <param name="subject">the subject to insert</param> 
        /// <param name="predicate">the predicate to insert</param> 
        /// <param name="obj">the object to insert</param> 
        /// <param name="namedGraph">Named graph for current resources</param> 
        void CreateProperty(Uri subject, Uri predicate, Uri obj, Uri namedGraph);

        /// <summary> 
        /// Insert a new property into the graph (triple store). 
        /// </summary> 
        /// <param name="subject">the subject to insert</param> 
        /// <param name="predicate">the predicate to insert</param> 
        /// <param name="literal">the literal to insert</param> 
        /// <param name="namedGraph">Named graph for current resources</param> 
        void CreateProperty(Uri subject, Uri predicate, string literal, Uri namedGraph);



        /// <summary> 
        /// Creates a linkingproperty between source and target resource based on the PidUri of the target Resource. 
        /// </summary> 
        /// <param name="id">Internal identifier of the source resource</param> 
        /// <param name="predicate">the linktype</param> 
        /// <param name="pidUri">the pidUri of the target resource</param> 
        /// <param name="namedGraph">Named graph for current resources</param>

        void CreateLinkPropertyWithGivenPid(Uri id, Uri predicate, string pidUri, Uri namedGraph);



        /// <summary> 
        /// Deletes a linkingproperty between source and target resource based on the PidUri of the target Resource. 
        /// </summary> 
        /// <param name="id">Internal identifier of the source resource</param> 
        /// <param name="predicate">the linktype</param> 
        /// <param name="pidUri">the pidUri of the target resource</param> 
        /// <param name="namedGraph">Named graph for current resources</param>

        void DeleteLinkPropertyWithGivenPid(Uri id, Uri predicate, string pidUri, Uri namedGraph);



        /// <summary> 
        /// Delete a property, identified by all triple parameters. 
        /// </summary> 
        /// <param name="id">the Id</param> 
        /// <param name="propertyUri">the predicate</param> 
        /// <param name="literal">the literal to insert</param> 
        /// <param name="namedGraph">Named graph for current resources</param> 
        void DeleteProperty(Uri id, Uri propertyUri, string literal, Uri namedGraph);

        /// <summary> 
        /// Delete a property, identified by all triple parameters. 
        /// </summary> 
        /// <param name="id">the Id</param> 
        /// <param name="propertyUri">the predicate</param> 
        /// <param name="obj">the object</param> 
        /// <param name="namedGraph">Named graph for current resources</param> 
        void DeleteProperty(Uri id, Uri propertyUri, Uri obj, Uri namedGraph);

        /// <summary> 
        /// Delete all properties that matches the given id and predicates with wildcard objects. 
        /// </summary> 
        /// <param name="id">the id</param> 
        /// <param name="predicate">the predicate</param> 
        /// <param name="namedGraph">Named graph for current resources</param> 
        void DeleteAllProperties(Uri id, Uri predicate, Uri namedGraph);
        Uri GetPidUriById(Uri uri, Uri draftGraph, Uri publishedGraph);

        /// <summary>
        /// Gets the Table and Column resource with its properties and nested objects. References to other resources (linked resources) will be ignored.
        /// </summary>
        /// <param name="id">The unique id of the resource</param>
        /// <param name="resourceTypes">the resource type list to filter by</param>
        /// <param name="namedGraph">Named graph for current resources</param>
        /// <returns>The resource containing the given PID URI</returns>
        DisplayTableAndColumn GetTableAndColumnById(Uri pidUri, IList<string> resourceTypes, Uri namedGraph);
        IList<string> GetAllPidUris(Uri namedGraph, ISet<Uri> metadataGraphs);

        /// <summary>
        /// Gets the resource type base on PidUri
        /// </summary>
        /// <param name="pidUri">The unique id of the resource</param>
        /// <param name="namedGraphUri">Named graph for current resources</param>
        /// <param name="publishedGraph">Published graph</param>
        /// <returns>Uri</returns>
        Uri GetResourceTypeByPidUri(Uri pidUri, Uri namedGraphUri, Dictionary<Uri, bool> publishedGraph);
    }
}
