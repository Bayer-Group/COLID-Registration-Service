using System.Collections.Generic;
using COLID.Identity.Constants;
using COLID.RegistrationService.Services.Authorization.UserInfo;
using COLID.RegistrationService.Services.Validation.Models;

namespace COLID.RegistrationService.Services.Validation.Validators.Keys
{
    internal class LastChangeUserValidator : BaseValidator
    {
        private readonly IUserInfoService _userInfoService;

        protected override string Key => Graph.Metadata.Constants.Resource.LastChangeUser;

        public LastChangeUserValidator(IUserInfoService userInfoService)
        {
            _userInfoService = userInfoService;
        }

        protected override void InternalHasValidationResult(EntityValidationFacade validationFacade, KeyValuePair<string, List<dynamic>> property)
        {
            if (!_userInfoService.HasApiToApiPrivileges() && _userInfoService.GetEmail() != Users.BackgroundProcessUser)
            {
                // Set user email as author
                validationFacade.RequestResource.Properties[property.Key] = new List<dynamic>() { _userInfoService.GetEmail() };
            }
        }
    }
}
