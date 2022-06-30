using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.Graph.Metadata.DataModels.Validation
{
    public class BulkUploadResult
    {
        public string Triples { get; set; }
        public string InstanceGraph { get; set; }
        public string ActionDone { get; set; }
        public string ErrorMessage { get; set; }
        public string ResourceId { get; set; }
        public string SourceId { get; set; }
        public string ResourceLabel { get; set; }
        public string pidUri { get; set; }
        public string ResourceDefinition { get; set; }
        public IDictionary<string, string> DistributionEndPoint { get; set; }
        public IList<ValidationResultProperty> Results { get; set; }
        public string TimeTaken { get; set; }
        public List<IDictionary<string, string>> StateItems { get; set; }
        public BulkUploadResult()
        {
            Results = new List<ValidationResultProperty>();
            Triples = "";
            DistributionEndPoint = new Dictionary<string, string>();
            StateItems = new List<IDictionary<string, string>>();
        }
    }
}
