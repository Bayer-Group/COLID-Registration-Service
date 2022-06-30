using System;
using System.Collections.Generic;
using COLID.Graph.TripleStore.Repositories;
using COLID.Graph.Metadata.DataModels.MetadataGraphConfiguration;

namespace COLID.Graph.Metadata.Repositories
{
    /// <summary>
    /// Repository to handle all metadata graph configuration related operations.
    /// </summary>
    public interface IMetadataGraphConfigurationRepository : IBaseRepository<MetadataGraphConfiguration>
    {
        /// <summary>
        /// Gets a list of metadata graph configuration items of current and old ones. All ones are sorted by it's start date.
        /// </summary>
        /// <returns>Configration overview list</returns>
        IList<MetadataGraphConfigurationOverviewDTO> GetConfigurationOverview();

        /// <summary>
        /// Gets the latest metadata graph configuration, determined by the start date.
        /// </summary>
        /// <returns>the latest configuration</returns>
        MetadataGraphConfiguration GetLatestConfiguration();

        /// <summary>
        /// Gets a single grapgh, by the given graph type. The latest config will be used, if no unique configuration identifier
        /// is passed also.
        /// </summary>
        /// <param name="graphType">the graphtype to use</param>
        /// <param name="config">the config to use</param>
        /// <returns>A List of graphs for a given graph type and optional configuration</returns>
        ISet<Uri> GetGraphs(string graphType, string config = "");

        /// <summary>
        /// Get all graphs to a given graph type and returns them in a list.
        /// </summary>
        /// <param name="graphTypes">the list of graphtypes to use</param>
        /// <returns>a list of found graphs</returns>
        ISet<Uri> GetGraphs(IEnumerable<string> graphTypes);

        /// <summary>
        /// Gets only a single (first) graph for a given graph type.
        /// Note: if more than one graph was found, a BusinessException will be thrown.
        /// </summary>
        /// <param name="graphType">the graphtype to use</param>
        /// <returns>a single graph</returns>
        string GetSingleGraph(string graphType);
    }
}
