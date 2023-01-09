using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using COLID.AWS.DataModels;
using COLID.RegistrationService.Common.DataModel.Search;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System;

namespace COLID.RegistrationService.Services.Interface
{
    /// <summary>
    /// This service is responsible for all file attachment operations, that are related or assigned to
    /// a single resource.
    /// </summary>
    public interface IExportService
    {
        /// <summary>
        /// Export resource as per search criteria
        /// </summary>
        /// <param name="exportRequest">search criteria</param>
        void Export(ExportRequestDto exportRequest);       

        //MemoryStream generateExcelTemplate(List<Dictionary<string, List<dynamic>>> resources);       
    }
}
