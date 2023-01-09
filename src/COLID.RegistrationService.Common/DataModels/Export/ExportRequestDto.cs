using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace COLID.RegistrationService.Common.DataModel.Search
{
    public class ExportRequestDto
    {
        public ExportRequestDto()
        {
            exportSettings = new ExportDto();
            searchRequest = new SearchRequestDto();
            pidUris = new List<Uri>();
        }

        public ExportDto exportSettings { get; set; }
        public SearchRequestDto searchRequest { get; set; }
        public List<Uri> pidUris { get; set; }
    }
}
