using System;
using AutoMapper;
using COLID.Graph.Metadata.DataModels.MetadataGraphConfiguration;

namespace COLID.RegistrationService.Services.MappingProfiles
{
    public class MetadataGraphConfigurationProfile : Profile
    {
        public MetadataGraphConfigurationProfile()
        {
            CreateMap<MetadataGraphConfigurationRequestDTO, MetadataGraphConfiguration>().ForMember(dest => dest.Id, opt => opt.MapFrom(t => Graph.Metadata.Constants.Entity.IdPrefix + Guid.NewGuid()));

            CreateMap<MetadataGraphConfiguration, MetadataGraphConfigurationResultDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(o => o.Id));
        }
    }
}
