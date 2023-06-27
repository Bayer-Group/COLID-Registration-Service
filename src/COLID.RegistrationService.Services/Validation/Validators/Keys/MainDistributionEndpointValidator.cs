using System.Collections.Generic;
using System.Linq;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.RegistrationService.Services.Validation.Models;

namespace COLID.RegistrationService.Services.Validation.Validators.Keys
{
    internal class MainDistributionEndpointValidator : BaseValidator
    {
        protected override string Key => Graph.Metadata.Constants.Resource.MainDistribution;

        protected override void InternalHasValidationResult(EntityValidationFacade validationFacade, KeyValuePair<string, List<dynamic>> properties)
        {
            var mainEndpoints = properties.Value;

            foreach (var endpoint in mainEndpoints)
            {
                if (endpoint.Properties.TryGetValue(Graph.Metadata.Constants.Resource.DistributionEndpoints.DistributionEndpointLifecycleStatus, out List<dynamic> lifecycleStatus) && lifecycleStatus.Any(s => s == Common.Constants.DistributionEndpoint.LifeCycleStatus.Deprecated))
                {
                    validationFacade.ValidationResults.Add(new ValidationResultProperty(endpoint.Id, Graph.Metadata.Constants.Resource.DistributionEndpoints.DistributionEndpointLifecycleStatus, null, Common.Constants.Messages.DistributionEndpoint.InvalidLifecycleStatus, ValidationResultSeverity.Violation));
                }
            }
        }
    }
}
