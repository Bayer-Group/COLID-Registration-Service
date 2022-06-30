using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.SQS.Model
{
    public class AWSSQSConfiguration
    {     
        public string ResourceInputQueueUrl { get; set; }
        public string ResourceOutputQueueUrl { get; set; }
        public string LinkingInputQueueUrl { get; set; }
        public string LinkingOutputQueueUrl { get; set; }
    }
}
