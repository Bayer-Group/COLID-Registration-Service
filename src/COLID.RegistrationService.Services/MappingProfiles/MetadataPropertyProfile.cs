using AutoMapper;
using COLID.Graph.Metadata.DataModels.MessageQueuing;
using COLID.Graph.Metadata.DataModels.Metadata;

namespace COLID.RegistrationService.Services.MappingProfiles
{
    public class MetadataPropertyProfile : Profile
    {
        public MetadataPropertyProfile()
        {
            CreateMap<MetadataProperty, MetadataPropertyDTO>();
        }
    }
}
