﻿using COLID.Common.Extensions;
using COLID.RegistrationService.Common.DataModel.PidUriTemplates;
using COLID.RegistrationService.Common.Enums.PidUriTemplate;
using COLID.RegistrationService.Common.Extensions;
using COLID.RegistrationService.Tests.Common.Utils;

namespace COLID.RegistrationService.Tests.Common.Builder
{
    public class PidUriTemplateBuilder : AbstractEntityBuilder<PidUriTemplate>
    {
        private PidUriTemplate _tpl = new PidUriTemplate();

        public override PidUriTemplate Build()
        {
            _tpl.Properties = _prop;
            return _tpl;
        }

        public PidUriTemplateBuilder GenerateSampleData()
        {
            WithType();
            WithId(TestUtils.GenerateRandomId());
            WithBaseUrl(Graph.Metadata.Constants.Resource.PidUrlPrefix);
            WithIdLength(1);
            WithPidUriTemplateIdType(TestUtils.GetRandomEnumValue<IdType>());
            WithPidUriTemplateSuffix(TestUtils.GetRandomEnumValue<Suffix>());
            WithPidUriTemplateLifecycleStatus(LifecycleStatus.Active);
            WithRoute("SUSHI/");

            return this;
        }

        public PidUriTemplateBuilder WithType()
        {
            CreateOrOverwriteProperty(Graph.Metadata.Constants.RDF.Type, COLID.Graph.Metadata.Constants.PidUriTemplate.Type);
            return this;
        }

        public PidUriTemplateBuilder WithId(string id)
        {
            _tpl.Id = id;
            return this;
        }

        public PidUriTemplateBuilder WithBaseUrl(string baseUrl)
        {
            CreateOrOverwriteProperty(COLID.Graph.Metadata.Constants.PidUriTemplate.HasBaseUrl, baseUrl);
            return this;
        }

        public PidUriTemplateBuilder WithIdLength(int idLength)
        {
            CreateOrOverwriteProperty(COLID.Graph.Metadata.Constants.PidUriTemplate.HasIdLength, idLength.ToString());
            return this;
        }

        public PidUriTemplateBuilder WithPidUriTemplateIdType(string idType)
        {
            CreateOrOverwriteProperty(COLID.Graph.Metadata.Constants.PidUriTemplate.HasPidUriTemplateIdType, idType);
            return this;
        }

        public PidUriTemplateBuilder WithPidUriTemplateLifecycleStatus(LifecycleStatus status)
        {
            CreateOrOverwriteProperty(COLID.Graph.Metadata.Constants.PidUriTemplate.HasLifecycleStatus, status.GetDescription());
            return this;
        }

        public PidUriTemplateBuilder WithPidUriTemplateSuffix(string suffix)
        {
            CreateOrOverwriteProperty(COLID.Graph.Metadata.Constants.PidUriTemplate.HasPidUriTemplateSuffix, suffix);
            return this;
        }

        public PidUriTemplateBuilder WithRoute(string route)
        {
            CreateOrOverwriteProperty(COLID.Graph.Metadata.Constants.PidUriTemplate.HasRoute, route);
            return this;
        }
    }
}
