using System.Collections.Generic;
using System.Linq;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.Services.Validation.Models;
using COLID.Graph.TripleStore.DataModels.Base;

namespace COLID.RegistrationService.Services.Validation.Validators.Keys
{
    internal class AttachmentValidator : BaseValidator
    {
        private readonly IAttachmentService _attachmentService;
        protected override string Key => Graph.Metadata.Constants.AttachmentConstants.HasAttachment;

        public AttachmentValidator(IAttachmentService attachmentService)
        {
            _attachmentService = attachmentService;
        }

        protected override void InternalHasValidationResult(EntityValidationFacade validationFacade, KeyValuePair<string, List<dynamic>> properties)
        {
            // If there are already critical errors that apply to the property, no further validation is performed.
            if (properties.Value == null ||
                validationFacade.ValidationResults.Any(v => v.Node == validationFacade.RequestResource.Id &&
                                                            v.Path == properties.Key && v.ResultSeverity == ValidationResultSeverity.Violation))
            {
                return;
            }

            foreach (var propertyValue in properties.Value)
            {
                if (propertyValue is Entity propertyEntity)
                {
                    if (!_attachmentService.Exists(propertyEntity.Id.ToString()))
                    {
                        var validationResultProperty = new ValidationResultProperty(validationFacade.RequestResource.Id,
                            properties.Key, propertyEntity.Id,
                            string.Format(Common.Constants.Messages.Attachment.NotExists, propertyEntity.Id),
                            ValidationResultSeverity.Violation);

                        validationFacade.ValidationResults.Add(validationResultProperty);
                    }
                }
            };
        }
    }
}
