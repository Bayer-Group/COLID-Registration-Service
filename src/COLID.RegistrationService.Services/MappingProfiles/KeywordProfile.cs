using System;
using AutoMapper;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.Extensions;
using COLID.RegistrationService.Common.DataModel.Keywords;

namespace COLID.RegistrationService.Services.MappingProfiles
{
    public class KeywordProfile : Profile
    {
        public KeywordProfile()
        {
            CreateMap<KeywordRequestDTO, Keyword>().ForMember(dest => dest.Id, opt => opt.MapFrom(t => Graph.Metadata.Constants.Entity.IdPrefix + Guid.NewGuid()));

            CreateMap<Keyword, BaseEntityResultDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(o => o.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(o => o.Properties.GetValueOrNull(Graph.Metadata.Constants.RDFS.Label, true)));
        }
    }
}
