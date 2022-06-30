using AutoMapper;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.Extensions;
using COLID.RegistrationService.Services.Interface;

namespace COLID.RegistrationService.Services.MappingProfiles
{
    public class EntityNameWithPidUriTemplateTypeCheckResolver : IValueResolver<Entity, BaseEntityResultDTO, string>
    {
        private readonly IPidUriTemplateService _pidUriTemplateService;

        public EntityNameWithPidUriTemplateTypeCheckResolver(IPidUriTemplateService pidUriTemplateService)
        {
            _pidUriTemplateService = pidUriTemplateService;
        }

        public string Resolve(Entity source, BaseEntityResultDTO destination, string destMember, ResolutionContext context)
        {
            string label = source?.Properties.GetValueOrNull(Graph.Metadata.Constants.RDFS.Label, true);

            if (!string.IsNullOrWhiteSpace(label))
            {
                return label;
            }

            string entityType = source?.Properties.GetValueOrNull(Graph.Metadata.Constants.RDF.Type, true);

            switch (entityType)
            {
                case COLID.Graph.Metadata.Constants.PidUriTemplate.Type:
                    var flatPidUriTemplate = _pidUriTemplateService.GetFlatPidUriTemplateByPidUriTemplate(source);
                    return _pidUriTemplateService.FormatPidUriTemplateName(flatPidUriTemplate);
            }

            return string.Empty;
        }
    }
}
