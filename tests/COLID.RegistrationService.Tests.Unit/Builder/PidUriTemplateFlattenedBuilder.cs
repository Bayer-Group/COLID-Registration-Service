using COLID.RegistrationService.Common.DataModel.PidUriTemplates;
using COLID.RegistrationService.Common.Enums.PidUriTemplate;
using COLID.RegistrationService.Common.Extensions;

namespace COLID.RegistrationService.Tests.Common.Builder
{
    public class PidUriTemplateFlattenedBuilder
    {
        private PidUriTemplateFlattened _template = new PidUriTemplateFlattened();

        public PidUriTemplateFlattened Build()
        {
            return _template;
        }

        public PidUriTemplateFlattenedBuilder GenerateSampleData()
        {
            WithId("https://pid.bayer.com/kos/19050#9e8fb007-9cfd-4cf9-9084-2412cd159410");
            WithBaseUrl("https://pid.bayer.com/");
            WithIdLength();
            WithIdType(IdType.Guid);
            WithSuffix(Suffix.Empty);
            WithRoute("DINOS/");

            return this;
        }

        public PidUriTemplateFlattenedBuilder WithId(string id)
        {
            _template.Id = id;
            return this;
        }

        public PidUriTemplateFlattenedBuilder WithBaseUrl(string baseUrl = "")
        {
            _template.BaseUrl = baseUrl;
            return this;
        }

        public PidUriTemplateFlattenedBuilder WithIdLength(int idLength = 0)
        {
            _template.IdLength = idLength;
            return this;
        }

        public PidUriTemplateFlattenedBuilder WithIdType(IdType idType)
        {
            _template.IdType = idType.GetEnumMember();
            return this;
        }

        public PidUriTemplateFlattenedBuilder WithSuffix(Suffix suffix)
        {
            _template.Suffix = suffix.GetEnumMember();
            return this;
        }

        public PidUriTemplateFlattenedBuilder WithRoute(string route = "")
        {
            _template.Route = route;
            return this;
        }
    }
}
