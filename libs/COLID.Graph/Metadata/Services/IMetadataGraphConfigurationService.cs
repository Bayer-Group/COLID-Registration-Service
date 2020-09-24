using System.Collections.Generic;
using COLID.Graph.TripleStore.Services;
using COLID.Graph.Metadata.DataModels.MetadataGraphConfiguration;
using COLID.Graph.Metadata.Repositories;

namespace COLID.Graph.Metadata.Services
{
    /// <summary>
    /// Service to handle all metadata graph confiugration related operations.
    /// </summary>
    public interface IMetadataGraphConfigurationService : IBaseEntityService<MetadataGraphConfiguration, MetadataGraphConfigurationRequestDTO, MetadataGraphConfigurationResultDTO, MetadataGraphConfigurationWriteResultCTO, IMetadataGraphConfigurationRepository>
    {
        /// <summary>
        /// Gets a list of metadata graph configuration items of current and old ones. 
        /// All ones are sorted by it's start date (desc).
        /// </summary>
        /// <returns>Configration overview list</returns>
        IList<MetadataGraphConfigurationOverviewDTO> GetConfigurationOverview();

        /// <summary>
        /// Gets the latest metadata graph configuration, determined by the start date.
        /// </summary>
        /// <returns>the latest configuration</returns>
        MetadataGraphConfigurationResultDTO GetLatestConfiguration();
    }
}
