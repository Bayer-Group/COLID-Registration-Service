using System.Collections.Generic;
using System.Linq;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.TripleStore.DataModels.Taxonomies;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.Services.Validation.Models;

namespace COLID.RegistrationService.Services.Validation.Validators.Others
{
    internal class TaxonomyValidator : BaseValidator
    {
        private readonly ITaxonomyService _taxonomyService;

        protected override bool IsTaxonomy => true;
        public override int Priority => 1;

        public TaxonomyValidator(ITaxonomyService taxonomyService)
        {
            _taxonomyService = taxonomyService;
        }

        protected override void InternalHasValidationResult(EntityValidationFacade validationFacade, KeyValuePair<string, List<dynamic>> property)
        {
            var metadataProperty = validationFacade.MetadataProperties.FirstOrDefault(t => t.Properties.GetValueOrNull(Graph.Metadata.Constants.EnterpriseCore.PidUri, true) == property.Key);

            // No key check needed
            string range = metadataProperty?.Properties.GetValueOrNull(Graph.Metadata.Constants.Shacl.Range, true);

            var taxonomies = _taxonomyService.GetTaxonomiesAsPlainList(range);
            var taxonomiesDictionary = taxonomies.ToDictionary(t => t.Id, t => t);

            IList<dynamic> values = property.Value.ToList();

            foreach (var taxonomy in taxonomies)
            {
                values = CheckParent(taxonomy, values, taxonomiesDictionary);
            }

            foreach (var value in values)
            {
                if (taxonomies.All(t => t != null && t.Id != value))
                {
                    validationFacade.ValidationResults.Add(new ValidationResultProperty(validationFacade.RequestResource.Id,
                        property.Key, value, string.Format(Common.Constants.Messages.Taxonomy.InvalidSelection, value), ValidationResultSeverity.Violation));
                }
            }

            validationFacade.RequestResource.Properties[property.Key] = values.ToList();
        }

        private IList<dynamic> CheckParent(TaxonomyResultDTO taxonomy, IList<dynamic> values, IDictionary<string, TaxonomyResultDTO> taxonomiesDictionary)
        {
            if (taxonomy.Children.Count != 0 && taxonomy.Children.All(n => values.Contains(n.Id)))
            {
                values = values.Where(v => !taxonomy.Children.Any(n => n.Id == v)).ToList();
                values.Add(taxonomy.Id);

                if (taxonomy.HasParent)
                {
                    foreach (var parentId in taxonomy.Properties[Graph.Metadata.Constants.SKOS.Broader])
                    {
                        if (taxonomiesDictionary.TryGetValue(parentId, out TaxonomyResultDTO parent))
                        {
                            values = CheckParent(parent, values, taxonomiesDictionary);
                        }
                    }
                }
            }
            return values;
        }
    }
}
