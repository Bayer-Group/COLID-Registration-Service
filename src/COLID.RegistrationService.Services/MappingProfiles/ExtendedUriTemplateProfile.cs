using System;
using AutoMapper;
using COLID.Graph.TripleStore.Extensions;
using COLID.RegistrationService.Common.DataModel.ExtendedUriTemplates;

namespace COLID.RegistrationService.Services.MappingProfiles
{
    public class ExtendedUriTemplateProfile : Profile
    {
        public ExtendedUriTemplateProfile()
        {
            CreateMap<ExtendedUriTemplateRequestDTO, ExtendedUriTemplate>().ForMember(dest => dest.Id, opt => opt.MapFrom(t => Graph.Metadata.Constants.Entity.IdPrefix + Guid.NewGuid()));

            CreateMap<ExtendedUriTemplate, ExtendedUriTemplateResultDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(o => o.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(o => o.Properties.GetValueOrNull(Graph.Metadata.Constants.RDFS.Label, true)));
        }
    }
}
