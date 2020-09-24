using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.Metadata.Extensions;
using COLID.Graph.TripleStore.Extensions;
using COLID.RegistrationService.Services.Validation.Models;
using Microsoft.Extensions.Configuration;

namespace COLID.RegistrationService.Services.Validation.Validators.Keys
{
    internal class PidUriValidator : BaseValidator
    {
        public override int Priority => 1;

        protected override string Key => Graph.Metadata.Constants.EnterpriseCore.PidUri;

        private readonly string _colidDomain;

        public PidUriValidator(IConfiguration configuration)
        {
            _colidDomain = configuration.GetConnectionString("colidDomain");
        }

        protected override void InternalHasValidationResult(EntityValidationFacade validationFacade, KeyValuePair<string, List<dynamic>> properties)
        {
            // If there are already critical errors that apply to the property, no further validation is performed.
            if (validationFacade.ValidationResults.Any(v => v.Node == validationFacade.RequestResource.Id && v.Path == properties.Key && v.ResultSeverity == ValidationResultSeverity.Violation))
            {
                return;
            }

            var firstIdentifier = properties.Value.FirstOrDefault();

            // Identifiers must always be specified as entity. If the format does not match, stop validation here.
            if (!DynamicExtension.IsType<Entity>(firstIdentifier, out Entity uriEntity))
            {
                return;
            }

            // In some cases wrong identifier types are given. Therefore the type is removed and added correctly.
            uriEntity.Properties.AddOrUpdate(Graph.Metadata.Constants.RDF.Type, new List<dynamic>() { Graph.Metadata.Constants.Identifier.Type });

            // Trimming the PID URI for whitespaces
            uriEntity.Id = uriEntity.Id?.Trim();

            Uri uriResult = new Uri(uriEntity.Id, UriKind.Absolute);

            // Check if uri start with pid host -> it is different for every environment
            if (!uriResult.Host.StartsWith(_colidDomain))
            {
                validationFacade.ValidationResults.Add(new ValidationResultProperty(validationFacade.RequestResource.Id, properties.Key, uriEntity.Id, string.Format(Graph.Metadata.Constants.Messages.Identifier.InvalidPrefix, _colidDomain), ValidationResultSeverity.Violation));
            }
            // Check if the uri is not only the host, but has also been extended
            else if (string.IsNullOrEmpty(uriResult.AbsolutePath) || uriResult.AbsolutePath == "/")
            {
                validationFacade.ValidationResults.Add(new ValidationResultProperty(validationFacade.RequestResource.Id, properties.Key, uriEntity.Id, string.Format(Graph.Metadata.Constants.Messages.Identifier.IdenticalToPrefix, _colidDomain), ValidationResultSeverity.Violation));
            }
            // Identifiers must not contain the defined pid host more than once.
            else if (Regex.Matches(uriResult.OriginalString, _colidDomain).Count > 1)
            {
                validationFacade.ValidationResults.Add(new ValidationResultProperty(validationFacade.RequestResource.Id, properties.Key, uriEntity.Id, string.Format(Graph.Metadata.Constants.Messages.Identifier.SeveralPrefixUsage, _colidDomain), ValidationResultSeverity.Violation));
            }

            validationFacade.RequestResource.Properties[properties.Key] = new List<dynamic>() { uriEntity };
        }
    }
}
