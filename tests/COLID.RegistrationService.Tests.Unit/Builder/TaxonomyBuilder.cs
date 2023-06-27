
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.TripleStore.DataModels.Taxonomies;

namespace COLID.RegistrationService.Tests.Common.Builder
{
    public class TaxonomyBuilder : AbstractEntityBuilder<Taxonomy>
    {
        private Taxonomy _tx = new Taxonomy();

        public override Taxonomy Build()
        {
            _tx.Properties = _prop;
            return _tx;
        }

        public TaxonomyResultDTO BuildResultDTO()
        {
            return new TaxonomyResultDTO()
            {
                Id = _tx.Id,
                Name = _prop.GetValueOrNull(Graph.Metadata.Constants.SKOS.PrefLabel, true),
                Properties = _prop
            };
        }

        public TaxonomyBuilder WithId(string id)
        {
            _tx.Id = id;
            return this;
        }

        public TaxonomyBuilder WithType(string type)
        {
            CreateOrOverwriteProperty(Graph.Metadata.Constants.RDF.Type, type);
            return this;
        }

        public TaxonomyBuilder WithPrefLabel(string label)
        {
            CreateOrOverwriteProperty(Graph.Metadata.Constants.SKOS.PrefLabel, label);
            return this;
        }

        public TaxonomyBuilder WithBroader(string broader)
        {
            CreateOrOverwriteProperty(Graph.Metadata.Constants.SKOS.Broader, broader);
            return this;
        }
    }
}
