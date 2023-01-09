using System.Collections.Generic;
using COLID.Graph.TripleStore.DataModels.Resources;
using COLID.Graph.TripleStore.Extensions;
using COLID.Identity.Constants;
using COLID.RegistrationService.Services.Authorization.UserInfo;
using COLID.RegistrationService.Services.Validation.Models;

namespace COLID.RegistrationService.Services.Validation.Validators.Keys
{
    internal class AuthorValidator : BaseValidator
    {
        private readonly IUserInfoService _userInfoService;

        protected override string Key => Graph.Metadata.Constants.Resource.Author;

        public AuthorValidator(IUserInfoService userInfoService)
        {
            _userInfoService = userInfoService;
        }

        protected override void InternalHasValidationResult(EntityValidationFacade validationFacade, KeyValuePair<string, List<dynamic>> properties)
        {
            // If a entry is created, the author have to be validated
            if (validationFacade.ResourceCrudAction == ResourceCrudAction.Create)
            {
                // API to API users doesn't have an email to override, therefor the resource mail is used
                if (_userInfoService.HasApiToApiPrivileges() || _userInfoService.GetEmail() == Users.BackgroundProcessUser)
                {
                    return;
                }

                // Set user email as author
                validationFacade.RequestResource.Properties[properties.Key] = new List<dynamic>() { _userInfoService.GetEmail() };
            }
            else
            {
                // If the entry is updated, overwrite author with the author, which is saved in repository
                validationFacade.RequestResource.Properties[properties.Key] = validationFacade.ResourcesCTO
                    .GetDraftOrPublishedVersion().Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.Author, false);
            }
        }
    }
}
