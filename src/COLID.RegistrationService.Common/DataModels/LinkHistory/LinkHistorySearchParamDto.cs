using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.RegistrationService.Common.DataModels.LinkHistory
{
    public class LinkHistorySearchParamDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Email { get; set; }
        public Uri linkType { get; set; }
        public string OrderByColumn { get; set; }
        public bool OrderDescending { get; set; }
        /// <summary>
        /// Page number
        /// </summary>
        public int From { get; set; }
        /// <summary>
        /// Page Size
        /// </summary>
        public int Size { get; set; }
    }
}
