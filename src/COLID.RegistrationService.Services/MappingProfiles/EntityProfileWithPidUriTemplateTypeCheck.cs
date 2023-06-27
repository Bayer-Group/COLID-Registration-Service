using System;
using AutoMapper;
using COLID.Graph.TripleStore.DataModels.Base;

namespace COLID.RegistrationService.Services.MappingProfiles
{
    public class EntityWithPidUriTemplateTypeCheckProfile : Profile
    {
        public EntityWithPidUriTemplateTypeCheckProfile()
        {
            CreateMap<BaseEntityRequestDTO, Entity>().ForMember(dest => dest.Id, opt => opt.MapFrom(t => Graph.Metadata.Constants.Entity.IdPrefix + Guid.NewGuid()));

            CreateMap<Entity, BaseEntityResultDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(o => o.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom<EntityNameWithPidUriTemplateTypeCheckResolver>());
        }
    }
}
