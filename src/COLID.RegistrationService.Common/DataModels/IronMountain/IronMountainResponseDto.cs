using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.IronMountainService.Common.Models
{
    public class IronMountainResponseDto
    {
        public string pidUri { get; set; }

        public IList<RetentionClassPolicies> retentionClassPolicies { get; set; }

    }

    public class RetentionClassPolicies
    {
        public string classId { get; set; }

        public string className { get; set; }

        public string classDescription { get; set; }

        public IList<Policy> policies { get; set; }

    }
    
    public class Policy
    {
        public string url { get; set; }
        public string ruleName { get; set; }
        public string jurisdiction { get; set; }
        public string retentionTriggerId { get; set; }
        public string retentionTrigger { get; set; }
        public string retentionTriggerDescription { get; set; }
        public string minRetentionPeriod { get; set; }
        public string minRetentionPeriodUnits { get; set; }
        public string maxRetentionPeriod { get; set; }
        public string maxRetentionPeriodUnits { get; set; }
    }
}
