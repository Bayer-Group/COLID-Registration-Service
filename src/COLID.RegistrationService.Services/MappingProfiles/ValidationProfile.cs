using AutoMapper;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.RegistrationService.Common.Extensions;
using VDS.RDF.Shacl.Validation;

namespace COLID.RegistrationService.Services.MappingProfiles
{
    public class ValidationProfile : Profile
    {
        public ValidationProfile()
        {
            CreateMap<Result, ValidationResultProperty>()
                .ForMember(dest => dest.Node, opt => opt.MapFrom(t => t.FocusNode))
                .ForMember(dest => dest.Path, opt => opt.MapFrom(t => t.ResultPath))
                .ForMember(dest => dest.Message, opt => opt.MapFrom(t => t.Message.Value))
                .ForMember(dest => dest.ResultValue, opt => opt.MapFrom(t => t.ResultValue))
                .ForMember(dest => dest.SourceConstraintComponent, opt => opt.MapFrom(t => t.SourceConstraintComponent))
                .ForMember(dest => dest.ResultSeverity, opt => opt.MapFrom(t => EnumExtension.GetValueFromEnumMember<ValidationResultSeverity>(t.Severity.ToString())))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(t => ValidationResultPropertyType.SHACL));
        }
    }
}
