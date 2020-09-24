using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.Services.Validation.Models;

namespace COLID.RegistrationService.Services.Validation.Validators.Keys
{
    internal class KeywordValidator : BaseValidator
    {
        private readonly IKeywordService _keywordService;
        protected override string Key => Graph.Metadata.Constants.Resource.Keyword;

        public KeywordValidator(IKeywordService keywordService)
        {
            _keywordService = keywordService;
        }

        protected override void InternalHasValidationResult(EntityValidationFacade validationFacade, KeyValuePair<string, List<dynamic>> property)
        {
            if (property.Value is null)
            {
                return;
            }

            validationFacade.RequestResource.Properties[Graph.Metadata.Constants.Resource.Keyword] = property.Value.Select(keyword =>
            {
                if (!Regex.IsMatch(keyword, Common.Constants.Regex.ResourceKey))
                {
                    if (!_keywordService.CheckIfKeywordExists(keyword, out string keywordId))
                    {
                        return _keywordService.CreateKeyword(keyword);
                    }

                    return keywordId;
                }

                return keyword;
            }).ToList();
        }
    }
}
