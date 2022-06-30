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
        }

        public ExportDto exportSettings { get; set; }
        public SearchRequestDto searchRequest { get; set; }

    }
}
