using System;
using AutoMapper;
using COLID.RegistrationService.Common.DataModel.PidUriTemplates;

namespace COLID.RegistrationService.Services.MappingProfiles
{
    public class PidUriTemplateProfile : Profile
    {
        public PidUriTemplateProfile()
        {
            CreateMap<PidUriTemplateRequestDTO, PidUriTemplate>().ForMember(dest => dest.Id, opt => opt.MapFrom(t => Graph.Metadata.Constants.Entity.IdPrefix + Guid.NewGuid()));
            CreateMap<PidUriTemplate, PidUriTemplateResultDTO>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom<PidUriTemplateNameResolver>());
            CreateMap<PidUriTemplateResultDTO, PidUriTemplate>();
            CreateMap<PidUriTemplateResultDTO, PidUriTemplateRequestDTO>();
        }
    }
}
