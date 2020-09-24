using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using COLID.Common.Extensions;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.TripleStore.Extensions;
using COLID.RegistrationService.Repositories.Interface;
using COLID.RegistrationService.Services.Validation.Models;

namespace COLID.RegistrationService.Services.Validation.Validators.Keys
{
    internal class VersionValidator : BaseValidator
    {
        private readonly IResourceRepository _resourceRepository;

        protected override string Key => Graph.Metadata.Constants.Resource.HasVersion;

        public VersionValidator(IResourceRepository resourceRepository)
        {
            _resourceRepository = resourceRepository;
        }

        protected override void InternalHasValidationResult(EntityValidationFacade validationFacade, KeyValuePair<string, List<dynamic>> properties)
        {
            // Create temporary validation error
            var validationResult = new ValidationResultProperty(validationFacade.RequestResource.Id, Graph.Metadata.Constants.Resource.HasVersion, null, null, ValidationResultSeverity.Violation);

            // Get version
            string newVersion = properties.Value.FirstOrDefault();

            // Check if version is given
            if (!string.IsNullOrWhiteSpace(newVersion))
            {
                // Check if version match version regex. Only numbers with points are allowed.
                if (!Regex.IsMatch(newVersion, Common.Constants.Regex.Version))
                {
                    validationResult.Message = "The version does not have the correct format. Only numbers and points are allowed. For example: 1 , 1.2 , 1.2.1";
                    validationResult.ResultValue = newVersion;
                    validationFacade.ValidationResults.Add(validationResult);
                    return;
                }

                // If the entry is new, search the version list for previous entry
                string pidUriString;

                if (validationFacade.ResourcesCTO.IsEmpty)
                {
                    if (string.IsNullOrWhiteSpace(validationFacade.PreviousVersion))
                    {
                        return;
                    }

                    pidUriString = validationFacade.PreviousVersion;
                }
                else
                {
                    // Get repo version
                    string repoVersion = validationFacade.ResourcesCTO.GetDraftOrPublishedVersion().Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasVersion, true);

                    // If repo version is equal to actual version, then everything is okay.
                    if (repoVersion == newVersion)
                    {
                        return;
                    }

                    pidUriString = validationFacade.ResourcesCTO.GetDraftOrPublishedVersion().Properties.GetValueOrNull(Graph.Metadata.Constants.EnterpriseCore.PidUri, true)?.Id;
                }

                if (!Uri.TryCreate(pidUriString, UriKind.Absolute, out var pidUri))
                {
                    return;
                }
                
                var versions = _resourceRepository.GetAllVersionsOfResourceByPidUri(pidUri).ToArray();

                if (versions.IsNullOrEmpty())
                {
                    return;
                }

                // Check if actual version is already in list
                if (versions.Any(r => r.Version.CompareVersionTo(newVersion) == 0))
                {
                    validationResult.Message = $"The new version already exists in the list, the last version is {versions.Last().Version}.";
                    validationFacade.ValidationResults.Add(validationResult);
                    return;
                }

                // If no entry with same pid uri in list, a new resource will be created
                if (!versions.Any(r => r.PidUri == validationFacade.RequestResource.PidUri?.ToString()))
                {
                    return;
                }

                // Check if entry is already in list and check if the current version is still between the two linked neighbors.
                // -> case: if someone changes the version of an already linked resource.
                // Get index of actual version
                var index = Array.FindIndex(versions, r => r.PidUri == validationFacade.RequestResource.PidUri.ToString());

                // If actual entry is not last entry
                if (versions.Last().PidUri != validationFacade.RequestResource.PidUri.ToString())
                {
                    // Check if actual version is lower than the version of the next entry
                    if (versions[index + 1].Version.CompareVersionTo(newVersion) <= 0)
                    {
                        validationResult.Message = $"The new version is larger than the later entry in the list (must be < {versions[index + 1].Version}).";
                        validationFacade.ValidationResults.Add(validationResult);
                        return;
                    }
                }
                // If actual entry is not first entry
                if (versions.First().PidUri != validationFacade.RequestResource.PidUri.ToString())
                {
                    // Check if actual version is higher than the version of the previous entry
                    if (versions[index - 1].Version.CompareVersionTo(newVersion) >= 0)
                    {
                        validationResult.Message = $"The new version is smaller than the previous entry in the list (must be > {versions[index - 1].Version}).";
                        validationFacade.ValidationResults.Add(validationResult);
                        return;
                    }
                }
            }
        }
    }
}
