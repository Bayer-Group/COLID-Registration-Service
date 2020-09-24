using System.Collections.Generic;
using System.Linq;
using COLID.Graph.TripleStore.DataModels.Taxonomies;
using COLID.RegistrationService.Tests.Functional.DataModel.V1;

namespace COLID.RegistrationService.Tests.Functional.DataModel.V2
{
    public class TaxonomyResultDtoV2 : BaseEntityResultDtoV1
    {
        public bool HasParent => Properties.Any(p => p.Key == Graph.Metadata.Constants.SKOS.Broader);

        public bool HasChild => Children.Any();

        public IList<TaxonomyResultDtoV2> Children { get; set; }

        public TaxonomyResultDtoV2()
        {
        }

        public TaxonomyResultDtoV2(TaxonomyResultDTO taxonomyResult)
        {
            Subject = taxonomyResult.Id;
            Name = taxonomyResult.Name;
            Properties = taxonomyResult.Properties;
            Children = new List<TaxonomyResultDtoV2>(taxonomyResult.Children.Select(c => new TaxonomyResultDtoV2(c)));
        }
    }
}
