using System.Collections.Generic;
using System.Linq;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.Services.Validation.Models;

namespace COLID.RegistrationService.Services.Validation.Validators.Keys
{
    internal class ConsumerGroupValidator : BaseValidator
    {
        public override int Priority => 1;

        protected override string Key => Graph.Metadata.Constants.Resource.HasConsumerGroup;

        private readonly IConsumerGroupService _consumerGroupService;

        public ConsumerGroupValidator(IConsumerGroupService consumerGroupService)
        {
            _consumerGroupService = consumerGroupService;
        }

        protected override void InternalHasValidationResult(EntityValidationFacade validationFacade, KeyValuePair<string, List<dynamic>> properties)
        {
            // If there are already critical errors that apply to the property, no further validation is performed.
            if (validationFacade.ValidationResults.Any(v => v.Node == validationFacade.RequestResource.Id && v.Path == properties.Key && v.ResultSeverity == ValidationResultSeverity.Violation))
            {
                return;
            }

            // Only consumer groups that are active and for which the current user has the appropriate rights will be returned.
            // Therefore, the next step checks if a forbidden consumer group is used.
            var activeConsumerGroups = _consumerGroupService.GetActiveEntities();

            foreach (var propertyValue in properties.Value)
            {
                if (propertyValue is null)
                {
                    continue;
                }

                if (activeConsumerGroups.All(cg => cg.Id != propertyValue))
                {
                    validationFacade.ValidationResults.Add(new ValidationResultProperty(validationFacade.RequestResource.Id, properties.Key, propertyValue, string.Format(Common.Constants.Messages.Resource.ForbiddenConsumerGroup, propertyValue), ValidationResultSeverity.Violation));
                }
            };
        }
    }
}
