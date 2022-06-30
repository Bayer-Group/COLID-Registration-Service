using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.RegistrationService.Common.DataModel.Resources;

namespace COLID.RegistrationService.Services.Interface
{
    /// <summary>
    /// Service to handle bulk import related operations.
    /// </summary>
    public interface IImportService
    {
        // <summary>
        /// Delete all messages from SQS-Queue
        /// </summary>        
        /// <returns></returns>
        Task<string> CleanUpBulkUploadSQSQueue();

        /// <summary>
        /// Start linking resources from SQS-Queue
        /// </summary>        
        /// <returns></returns>
        Task<string> StartLinkingResources();

        /// <summary>
        /// Checks and validates the given resources request and creates a new one, in case of success.
        /// </summary>
        /// <param name="resources">the new resource to create</param>
        Task<List<BulkUploadResult>> ValidateResource(List<ResourceRequestDTO> resources);
    }
}
