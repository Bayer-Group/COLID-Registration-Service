using System;
using AutoMapper;
using COLID.Graph.Metadata.DataModels.Resources;

namespace COLID.Graph.TripleStore.MappingProfiles
{
    public class ResourceProfile : Profile
    {
        public ResourceProfile()
        {
            CreateMap<ResourceRequestDTO, Resource>().ForMember(dest => dest.Id, opt => opt.MapFrom(src => Metadata.Constants.Entity.IdPrefix + new Guid()));
            CreateMap<Resource, ResourceRequestDTO>();
        }
    }
}
