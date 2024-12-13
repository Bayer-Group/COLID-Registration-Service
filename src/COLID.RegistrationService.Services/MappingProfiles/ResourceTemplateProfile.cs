using System;
using AutoMapper;
using COLID.Graph.TripleStore.Extensions;
using COLID.RegistrationService.Common.DataModel.ResourceTemplates;

namespace COLID.RegistrationService.Services.MappingProfiles
{
    public class ResourceTemplateProfile : Profile
    {
        public ResourceTemplateProfile()
        {
            CreateMap<ResourceTemplateRequestDTO, ResourceTemplate>().ForMember(dest => dest.Id, opt => opt.MapFrom(t => Graph.Metadata.Constants.Entity.IdPrefix + Guid.NewGuid()));
            CreateMap<ResourceTemplate, ResourceTemplateResultDTO>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(o => o.Properties.GetValueOrNull(Graph.Metadata.Constants.RDFS.Label, true)));
            CreateMap<ResourceTemplateResultDTO, ResourceTemplate>();
            CreateMap<ResourceTemplateResultDTO, ResourceTemplateRequestDTO>();
        }
    }
}
