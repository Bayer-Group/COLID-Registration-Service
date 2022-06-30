using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using COLID.AWS.DataModels;

namespace COLID.AWS.Interface
{
    /// <summary>
    /// AWS Neptune related operations are handled within this class.<br />
    /// <b>Note:</b>
    /// In order to process correctly, it's necessary to attach the ColidTripleStoreOptions
    /// (for loader endpoint url) as well as the AmazonWebServicesOptions.
    /// </summary>
    public interface INeptuneLoaderConnector
    {
        /// <summary>
        /// Import a graph with the given name into AWS Neptune. The ttl file has to get uploaded to S3 first in order
        /// to import it.
        /// </summary>
        /// <param name="s3Key">Path to ttl file in s3</param>
        /// <param name="graphName">the graph name to store as an Uri</param>
        /// <exception cref="NeptuneLoaderException">In case of errors</exception>
        public Task<NeptuneLoaderResponse> LoadGraph(string s3Key, Uri graphName);

        /// <summary>
        /// Get the import-status for the given load id.
        /// </summary>
        /// <param name="loadId">the id to fetch the status for</param>
        public Task<NeptuneLoaderStatusResponse> GetStatus(Guid loadId);
    }
}
