using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.RegistrationService.Common.DataModel.DistributionEndpoints;
using COLID.RegistrationService.Common.DataModel.Resources;
using COLID.RegistrationService.Common.DataModel.Search;
using Entity = COLID.Graph.TripleStore.DataModels.Base.Entity;


namespace COLID.RegistrationService.Services.Interface
{
    /// <summary>
    /// Service to handle all resource revision related operations.
    /// </summary>
    public interface IRevisionService
    {
        /// <summary>
        /// Creates the additionals and removals revision graphs for the updated resource
        /// </summary>
        /// <param name="published">the published resource that should be updated</param>
        /// <param name="draftToBePublished">the draft resource whose values should be used to update the published resource</param>
        Task<Resource> AddAdditionalsAndRemovals(Entity published, Entity draftToBePublished);

        /// <summary>
        /// Creates the additionals and removals revision graphs for the updated resource by checking only specified metadata
        /// </summary>
        /// <param name="published">the published resource that should be updated</param>
        /// <param name="draftToBePublished">the draft resource whose values should be used to update the published resource</param>
        /// <param name="metaDataToCheck">metadata properties to be checked</param>
        Task<Resource> AddAdditionalsAndRemovals(Entity Published, Entity draftToBePublished, List<MetadataProperty> metaDataToCheck);

        /// <summary>
        /// Creates the additional revision graph when a draft resource is published the first time
        /// </summary>
        /// <param name="resource">the new resource to create</param>
        Task InitializeResourceInAdditionalsGraph(Resource ResourceToBeCreated, IList<Graph.Metadata.DataModels.Metadata.MetadataProperty> allMetaData);
    }
}
