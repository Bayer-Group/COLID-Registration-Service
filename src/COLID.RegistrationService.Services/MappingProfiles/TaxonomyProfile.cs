using System.Collections.Generic;
using AutoMapper;
using COLID.Graph.TripleStore.DataModels.Taxonomies;

namespace COLID.RegistrationService.Services.MappingProfiles
{
    public class TaxonomyProfile : Profile
    {
        public TaxonomyProfile()
        {
            CreateMap<Taxonomy, TaxonomyResultDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(o => o.Id))
                .ForMember(dest => dest.Children, opt => opt.MapFrom(o => new List<Taxonomy>()))
                .ForMember(dest => dest.Name, opt => opt.MapFrom<TaxonomyNameResolver>());
        }
    }
}
