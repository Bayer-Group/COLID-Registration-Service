using System;
using System.Collections.Generic;

namespace COLID.IronMountainService.Common.Models
{
    public class IronMountainRequestDto
    {
        public string pidUri { get; set; }
        public List<string> dataCategories { get; set; }
        public List<string> countryContext { get; set; }
    }
}
