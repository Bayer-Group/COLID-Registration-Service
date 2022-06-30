using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.DataModels.Resources;
namespace COLID.RegistrationService.Services.Interface
{
    /// <summary>
    /// Service to handle all reindexing related operations.
    /// </summary>
    public interface IReindexingService
    {
        /// <summary>
        /// Starts the re-indexing on indexing crawler service.
        /// </summary>
        Task Reindex();
                
        /// <summary>
        /// Starts the indexing process of the new resource towards on indexing crawler service.  
        /// </summary>
        /// <param name="pidUri">Pid Uri of resource to be indexed</param>
        /// <param name="resource">Resource to be indexed</param>        /// <param name="resourceVersions">Versions of resource to be indexed</param>
        void IndexNewResource(Uri pidUri, Resource resource, IList<VersionOverviewCTO> resourceVersions);

        /// <summary>
        /// Starts the indexing process of the updated resource towards on indexing crawler service.
        ///
        /// Besides the current resource (draft) also the indexed published resource is updated.
        /// </summary>
        /// <param name="pidUri">Pid Uri of resource to be indexed</param>
        /// <param name="resource">Resource to be indexed</param>
        /// <param name="repoResources">Resources that were stored in the database before the current process.</param>
        void IndexUpdatedResource(Uri pidUri, Entity resource, ResourcesCTO repoResources);

        /// <summary>
        /// Starts the indexing process of the updated (linked) resource towards on indexing crawler service.
        /// </summary>
        /// <param name="pidUri">Pid Uri of resource to be indexed</param>
        /// <param name="resource">Resource to be indexed</param>
        /// <param name="repoResources">Resources that were stored in the database before the current process.</param>
        void IndexLinkedResource(Uri pidUri, Entity resource, ResourcesCTO repoResources);

        /// <summary>
        /// Starts the indexing process of the updated resource on indexing crawler service,
        /// as well as a resource which is still in the linked list.
        /// This will update all other versioned resources.
        /// </summary>
        /// <param name="pidUri">Pid Uri of resource to be indexed</param>
        /// <param name="resource">Resource to be indexed.</param>
        ///  /// <param name="unlinkedPidUri">Unlinked Pid Uri of resource to be indexed</param>
        /// <param name="unlinkedListResource">Unlinked resource to be indexed.</param>
        void IndexUnlinkedResource(Uri pidUri, ResourcesCTO resource, Uri unlinkedPidUri, ResourcesCTO unlinkedListResource);

        /// <summary>
        /// Starts the indexing process of the published resource towards on indexing crawler service.
        /// Draft resource will be deleted on index.
        /// </summary>
        /// <param name="pidUri">Pid Uri of resource to be indexed</param>
        /// <param name="resource">Resource to be indexed</param>
        /// <param name="repoResources">Resources that were stored in the database before the current process.</param>
        void IndexPublishedResource(Uri pidUri, Entity resource, ResourcesCTO repoResources);

        /// <summary>
        /// Starts the indexing process of the marked for deletion resource towards on indexing crawler service.
        /// </summary>
        /// <param name="pidUri">Pid Uri of resource to be indexed</param>
        /// <param name="resource">Resource to be indexed</param>
        /// <param name="repoResources">Resources that were stored in the database before the current process.</param>
        void IndexMarkedForDeletionResource(Uri pidUri, Entity resource, ResourcesCTO repoResources);

        /// <summary>
        /// Starts the deletion process of the resource towards on indexing crawler service.
        /// 
        /// depending on the lifecycle status only the draft is deleted.
        /// If there is only one published resource in the index,
        /// it will be removed and the resource is no longer present in the index. 
        /// </summary>
        /// <param name="pidUri">Pid Uri of resource to be indexed</param>
        /// <param name="resource">Resource to be indexed</param>
        /// <param name="repoResources">Resources that were stored in the database before the current process.</param>
        void IndexDeletedResource(Uri pidUri, Entity resource, ResourcesCTO repoResources);
    }
}
