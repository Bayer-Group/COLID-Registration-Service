using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.RegistrationService.Services.Validation.Models;

namespace COLID.RegistrationService.Services.Validation.Validators.Datatypes
{
    internal class DatetimeValidator : BaseValidator
    {
        // Lower priority than other datetime validators
        public override int Priority => 2;

        protected override string Datatype => Graph.Metadata.Constants.Shacl.DataTypes.DateTime;

        protected override void InternalHasValidationResult(EntityValidationFacade validationFacade, KeyValuePair<string, List<dynamic>> property)
        {
            validationFacade.RequestResource.Properties[property.Key] = property.Value.Select(value =>
            {
                try
                {
                    // If the value is empty, then no validation should be done.
                    if (ReferenceEquals(null, value) || ValueIsNullOrEmptyString(value))
                    {
                        return value;
                    }

                    return Convert.ToDateTime(value).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fff'Z'"); //ToString("o", DateTimeFormatInfo.InvariantInfo);
                }
                catch (FormatException)
                {
                    validationFacade.ValidationResults.Add(new ValidationResultProperty(validationFacade.RequestResource.Id, property.Key, value, Common.Constants.Messages.DatetimeMsg.InvalidFormat, ValidationResultSeverity.Violation));
                    return value;
                }
            }).ToList();
        }

        private static bool ValueIsNullOrEmptyString(dynamic value)
        {
            return value is string && string.IsNullOrWhiteSpace(value);
        }
    }
}
