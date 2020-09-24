using COLID.RegistrationService.Services.Validation.Models;

namespace COLID.RegistrationService.Services.Validation.Validators
{
    public interface IEntityPropertyValidator
    {
        void Validate(string key, EntityValidationFacade validationFacade);
    }
}
