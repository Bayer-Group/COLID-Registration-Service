using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.RegistrationService.Common.DataModel.Search
{
    public class SearchResultDto
    {
        public string OriginalSearchTerm { get; set; }

        public string SuggestedSearchTerm { get; set; }

        public HitDto Hits { get; set; }

        public dynamic Suggest { get; set; }
    }
}
