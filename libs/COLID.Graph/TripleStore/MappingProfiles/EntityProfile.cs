using System;
using AutoMapper;
using COLID.Graph.TripleStore.DataModels.Base;

namespace COLID.Graph.TripleStore.MappingProfiles
{
    public class EntityProfile : Profile
    {
        public EntityProfile()
        {
            CreateMap<BaseEntityRequestDTO, Entity>().ForMember(dest => dest.Id, opt => opt.MapFrom(t => Metadata.Constants.Entity.IdPrefix + Guid.NewGuid()));

            CreateMap<Entity, BaseEntityResultDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(o => o.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom<EntityNameResolver>());
        }
    }
}
