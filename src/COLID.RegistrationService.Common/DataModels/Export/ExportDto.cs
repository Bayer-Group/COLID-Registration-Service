using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace COLID.RegistrationService.Common.DataModel.Search
{
    public class ExportDto
    {
        public ExportDto()
        {
            includeHeader = false;
            exportContent = "onlyUri";
            exportFormat = "readableExcel";
            readableValues = "uris";
        }

        public bool includeHeader { get; set; }
        public string exportContent { get; set; }
        public string exportFormat { get; set; }
        public string readableValues { get; set; }

    }
}
