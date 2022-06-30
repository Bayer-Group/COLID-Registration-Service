using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.Extensions;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.Services.Validation.Models;

namespace COLID.RegistrationService.Services.Validation.Validators.Keys
{
    internal class ExtendableListValidator : BaseValidator
    {
        private readonly IEntityService _entityService;

        protected override string FieldType => Graph.Metadata.Constants.PIDO.Shacl.FieldTypes.ExtendableList;

        public ExtendableListValidator(IEntityService entityService)
        {
            _entityService = entityService;
        }

        protected override void InternalHasValidationResult(EntityValidationFacade validationFacade, KeyValuePair<string, List<dynamic>> property)
        {
            if (property.Value is null)
            {
                return;
            }

            var metadataProperty = validationFacade.MetadataProperties.FirstOrDefault(t => t.Properties.GetValueOrNull(Graph.Metadata.Constants.EnterpriseCore.PidUri, true) == property.Key);
            string range = metadataProperty?.Properties.GetValueOrNull(Graph.Metadata.Constants.Shacl.Range, true);

            // Value can be the identifier of the entity or the label of a new entity to be created
            validationFacade.RequestResource.Properties[property.Key] = property.Value.Select(value =>
            {
                if (!Regex.IsMatch(value, Common.Constants.Regex.ResourceKey))
                {
                    var labelUri = new Uri(Graph.Metadata.Constants.RDFS.Label);
                    if (!_entityService.CheckIfPropertyValueExists(labelUri, value, range, out string entityId))
                    {
                        var entityRequest = new BaseEntityRequestDTO();
                        entityRequest.Properties.Add(Graph.Metadata.Constants.RDF.Type, new List<dynamic>() { range });
                        entityRequest.Properties.Add(Graph.Metadata.Constants.RDFS.Label, new List<dynamic>() { value });

                        var createdEntity = _entityService.CreateEntity(entityRequest).Result;

                        return createdEntity.Entity.Id;
                    }

                    return entityId;
                }

                return value;
            }).ToList();
        }
    }
}
