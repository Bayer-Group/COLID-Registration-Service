using AutoMapper;
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.TripleStore.DataModels.Taxonomies;
using COLID.RegistrationService.Services.Interface;

namespace COLID.RegistrationService.Services.MappingProfiles
{
    public class TaxonomyNameResolver : IValueResolver<Taxonomy, TaxonomyResultDTO, string>
    {
        private readonly IPidUriTemplateService _pidUriTemplateService;

        public TaxonomyNameResolver(IPidUriTemplateService pidUriTemplateService)
        {
            _pidUriTemplateService = pidUriTemplateService;
        }

        public string Resolve(Taxonomy source, TaxonomyResultDTO destination, string destMember, ResolutionContext context)
        {
            string prefLabel = source.Properties.GetValueOrNull(Graph.Metadata.Constants.SKOS.PrefLabel, true);
            string rdfLabel = source.Properties.GetValueOrNull(Graph.Metadata.Constants.RDFS.Label, true);

            string entityType = source.Properties.GetValueOrNull(Graph.Metadata.Constants.RDF.Type, true);

            switch (entityType)
            {
                case Common.Constants.PidUriTemplate.Type:
                    var flatPidUriTemplate = _pidUriTemplateService.GetFlatPidUriTemplateByPidUriTemplate(source);
                    return _pidUriTemplateService.FormatPidUriTemplateName(flatPidUriTemplate);
            }

            return prefLabel ?? rdfLabel;
        }
    }
}
