using System;
using AutoMapper;
using COLID.Graph.TripleStore.DataModels.ConsumerGroups;
using COLID.Graph.TripleStore.Extensions;

namespace COLID.RegistrationService.Services.MappingProfiles
{
    public class ConsumerGroupProfile : Profile
    {
        public ConsumerGroupProfile()
        {
            CreateMap<ConsumerGroupRequestDTO, ConsumerGroup>().ForMember(dest => dest.Id, opt => opt.MapFrom(t => Graph.Metadata.Constants.Entity.IdPrefix + Guid.NewGuid()));
            CreateMap<ConsumerGroupResultDTO, ConsumerGroup>();
            CreateMap<ConsumerGroup, ConsumerGroupResultDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(o => o.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(o => o.Properties.GetValueOrNull(Graph.Metadata.Constants.RDFS.Label, true)));
        }
    }
}
