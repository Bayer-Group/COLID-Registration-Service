using System;
using System.Collections.Generic;

namespace COLID.IronMountainService.Common.Models
{
    public class IronMountainRequestDto
    {
        public string pidUri { get; set; }
        public IList<string> dataCategories { get; set; }
        public IList<string> countryContext { get; set; }
    }
}
