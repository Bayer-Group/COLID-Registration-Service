using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.RegistrationService.Common.DataModel.Resources;
using Microsoft.AspNetCore.Http;

namespace COLID.RegistrationService.Services.Interface
{
    /// <summary>
    /// Service to handle bulk import related operations.
    /// </summary>
    public interface IImportService
    {
        ///// <summary>
        ///// Start linking resources from SQS-Queue
        ///// </summary>        
        ///// <returns></returns>
        //Task<string> StartLinkingResources();

        /// <summary>
        /// Checks and validates the given resources request and creates a new one, in case of success.
        /// </summary>
        /// <param name="resources">the new resource to create</param>
        Task<List<BulkUploadResult>> ValidateResource(List<ResourceRequestDTO> resources, bool ignoreNonMandatory = false);

        /// <summary>
        /// Imports updated data from exported excel template
        /// </summary>
        /// <param name="file"></param>
        Task ImportExcel();

        /// <summary>
        /// Upload excel template file to import S3 bucket 
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        Task UploadImportExcelToS3(IFormFile file);

        //Task<MemoryStream> ExecuteImportExcel(MemoryStream inputstream);
    }
}
