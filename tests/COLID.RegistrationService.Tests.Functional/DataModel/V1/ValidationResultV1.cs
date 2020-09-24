using System.Collections.Generic;
using System.Linq;

namespace COLID.RegistrationService.Tests.Functional.DataModel.V1
{
    public enum ConformLevel
    {
        SUCCESS,
        WARNING,
        CRITICAL
    }

    //Model for nodeJs result
    public class ValidationResultV1
    {
        public ConformLevel Conforms
        {
            get
            {
                if (this.Results.Any())
                {
                    if (this.Results.Any(r => r.Critical))
                    {
                        return ConformLevel.CRITICAL;
                    }
                    return ConformLevel.WARNING;
                }
                return ConformLevel.SUCCESS;
            }
        }

        public IList<ValidationResultPropertyV1> Results { get; set; }

        public ValidationResultV1()
        {
            Results = new List<ValidationResultPropertyV1>();
        }
    }
}
