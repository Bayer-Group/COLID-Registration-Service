using System.Collections.Generic;
using COLID.RegistrationService.Services.Validation.Models;

namespace COLID.RegistrationService.Services.Validation.Validators
{
    public interface IEntityValidator
    {
        int Priority { get; }

        void HasValidationResult(EntityValidationFacade validationFacade, KeyValuePair<string, List<dynamic>> prop);
    }
}
