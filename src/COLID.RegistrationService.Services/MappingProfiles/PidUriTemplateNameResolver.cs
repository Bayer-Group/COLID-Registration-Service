using AutoMapper;
using COLID.RegistrationService.Common.DataModel.PidUriTemplates;
using COLID.RegistrationService.Services.Interface;

namespace COLID.RegistrationService.Services.MappingProfiles
{
    public class PidUriTemplateNameResolver : IValueResolver<PidUriTemplate, PidUriTemplateResultDTO, string>
    {
        private readonly IPidUriTemplateService _pidUriTemplateService;

        public PidUriTemplateNameResolver(IPidUriTemplateService pidUriTemplateService)
        {
            _pidUriTemplateService = pidUriTemplateService;
        }

        public string Resolve(PidUriTemplate source, PidUriTemplateResultDTO destination, string destMember, ResolutionContext context)
        {
            var flatPidUriTemplate = _pidUriTemplateService.GetFlatPidUriTemplateByPidUriTemplate(source);
            return _pidUriTemplateService.FormatPidUriTemplateName(flatPidUriTemplate);
        }
    }
}
