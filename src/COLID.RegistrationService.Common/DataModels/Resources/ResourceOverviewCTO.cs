using System.Collections.Generic;

namespace COLID.RegistrationService.Common.DataModel.Resources
{
    public class ResourceOverviewCTO
    {
        public string Total { get; set; }

        public IList<ResourceOverviewDTO> Items { get; set; }

        public ResourceOverviewCTO(string total, IList<ResourceOverviewDTO> items)
        {
            Total = total;
            Items = items;
        }
    }
}
