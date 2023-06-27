using System;
using System.Collections.Generic;

namespace COLID.IronMountainService.Common.Models
{
    public class IronMountainRentionScheduleDto
    { 
        public IList<IronMountainRecordClass> retentionSchedule { get; set; }
    }

    public class IronMountainRecordClass
    {
        public string recordClassId { get; set; }

        public string recordClassCd { get; set; }

        public string recordClassName { get; set; }

        public string recordClassDescription { get; set; }

        public IList<IronMountainRecordClassRules> rules { get; set; }

        public IList<IronMountainRecordClass> children { get; set; }
    }

    public class IronMountainRecordClassRules
    {
        public string ruleId { get; set; }
        public string ruleCd { get; set; }
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
