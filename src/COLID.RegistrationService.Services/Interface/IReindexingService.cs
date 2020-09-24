using System.Collections.Generic;
using System.Threading.Tasks;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.RegistrationService.Common.DataModel.Resources;

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
        /// Published actual resource and finds all deleted links to update these resources as well.
        ///
        /// <para>
        /// Outbound<br />
        ///   - Actual -> Crawler<br />
        ///   - Deleted -> Registration (Only Update)<br />
        /// Inbound -> Crawler<br />
        /// Versions -> Crawler
        /// </para>
        /// </summary>
        /// <param name="resource">Actual resource</param>
        /// <param name="repoResource">Related resource in repository</param>
        void SendResourcePublished(Resource resource, Entity repoResource, IList<MetadataProperty> metadataProperties);

        /// <summary>
        /// Delete resource and update all linked resources.
        ///
        /// Outbound -> RegistrationService
        /// Inbound -> RegistrationService
        /// Versions -> Update one of the versioned resources in the crawler
        ///
        /// Crawler check if resource is published
        /// </summary>
        /// <param name="resource">Resource to be deleted</param>
        void SendResourceDeleted(Resource resource, IList<string> inboundProperties, IList<MetadataProperty> metadataProperties);

        /// <summary>
        /// Update actual linked resource.
        /// </summary>
        /// <param name="resource">Pid entry</param>
        void SendResourceLinked(Resource resource);

        /// <summary>
        /// Updated the resource that is no longer linked and a resource from the versions chain to update the complete chain via crawler.
        /// </summary>
        /// <param name="resource"></param>
        void SendResourceUnlinked(Resource resource);
    }
}
