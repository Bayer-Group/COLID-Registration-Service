using System.Collections.Generic;
using System.Linq;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.Extensions;
using COLID.Graph.TripleStore.Extensions;
using COLID.RegistrationService.Services.Validation.Models;

namespace COLID.RegistrationService.Services.Validation.Validators
{
    internal abstract class BaseValidator : IEntityValidator
    {
        public virtual int Priority => 0;

        protected virtual string Datatype { get; }

        protected virtual string Key { get; }

        protected virtual string Range { get; }

        protected virtual string Group { get; }

        protected virtual string FieldType { get; }

        public void HasValidationResult(EntityValidationFacade validationFacade, KeyValuePair<string, List<dynamic>> property)
        {
            var metadataProperty = validationFacade.MetadataProperties.FirstOrDefault(t => t.Key == property.Key);

            if (metadataProperty == null || !IsMatch(property.Key, metadataProperty))
            {
                return;
            }

            InternalHasValidationResult(validationFacade, property);
        }

        protected abstract void InternalHasValidationResult(EntityValidationFacade validationFacade, KeyValuePair<string, List<dynamic>> property);

        protected bool IsMatch(string key, MetadataProperty metadata)
        {
            return MetadataKeyMatches(key) || MetadataFieldTypeMatches(metadata) ||  MetadataRangeMatches(metadata) || MetadataDatatypeMatches(metadata) || MetadataGroupMatches(metadata) || MetadataTaxonomyMatches(metadata);
        }

        private bool MetadataFieldTypeMatches(MetadataProperty metadata)
        {
            return !string.IsNullOrWhiteSpace(FieldType) && metadata.Properties.TryGetValue(Graph.Metadata.Constants.PIDO.Shacl.FieldType, out var fieldType) &&  fieldType == FieldType;
        }

        private bool MetadataKeyMatches(string key)
        {
            return Key == key;
        }

        private bool MetadataRangeMatches(MetadataProperty metadata)
        {
            return !string.IsNullOrWhiteSpace(Range) && metadata.Properties.TryGetValue(Graph.Metadata.Constants.Shacl.Range, out var range) && range == Range;
        }

        private bool MetadataGroupMatches(MetadataProperty metadata)
        {
            var group = metadata.GetMetadataPropertyGroup();

            return !string.IsNullOrWhiteSpace(Group) && group != null && group.Key == Group;
        }

        private bool MetadataDatatypeMatches(MetadataProperty metadata)
        {
            return (!string.IsNullOrWhiteSpace(Datatype) && metadata.Properties.TryGetValue(Graph.Metadata.Constants.Shacl.Datatype, out var datatype) && datatype == Datatype);
        }

        private bool MetadataTaxonomyMatches(MetadataProperty metadata)
        {
            return metadata != null && metadata.IsControlledVocabulary(out var range) && FieldType == Graph.Metadata.Constants.PIDO.Shacl.FieldTypes.Hierarchy;
        }
    }
}
