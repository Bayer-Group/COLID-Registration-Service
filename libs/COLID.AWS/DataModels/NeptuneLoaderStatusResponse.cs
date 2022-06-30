using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.AWS.DataModels
{
    public class NeptuneLoaderStatusResponse
    {
        public string Status { get; set; }
        public string LoadStatus { get; set; }
        public string StartTime { get; set; }
    }
}
