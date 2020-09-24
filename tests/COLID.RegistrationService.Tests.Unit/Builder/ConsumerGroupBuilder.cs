
using COLID.Graph.TripleStore.DataModels.ConsumerGroups;
using COLID.Graph.TripleStore.Extensions;

namespace COLID.RegistrationService.Tests.Common.Builder
{
    public class ConsumerGroupBuilder : AbstractEntityBuilder<ConsumerGroup>
    {
        private ConsumerGroup _cg = new ConsumerGroup();

        public override ConsumerGroup Build()
        {
            _cg.Properties = _prop;
            return _cg;
        }

        public ConsumerGroupResultDTO BuildResultDTO()
        {
            return new ConsumerGroupResultDTO()
            {
                Id = _cg.Id,
                Name = _prop.GetValueOrNull(Graph.Metadata.Constants.RDFS.Label, true),
                Properties = _prop
            };
        }

        public ConsumerGroupBuilder GenerateSampleData()
        {
            WithType();
            WithLifecycleStatus(Graph.Metadata.Constants.ConsumerGroup.LifecycleStatus.Active);
            WithLabel("DINOS");
            WithContactPerson("superadmin@bayer.com");
            WithAdRole("PID.Group02Data.ReadWrite");
            WithPidUriTemplate("https://pid.bayer.com/kos/19050#14d9eeb8-d85d-446d-9703-3a0f43482f5a");
            WithDefaultPidUriTemplate("https://pid.bayer.com/kos/19050#14d9eeb8-d85d-446d-9703-3a0f43482f5a");

            return this;
        }

        public ConsumerGroupBuilder WithId(string id)
        {
            _cg.Id = id;
            return this;
        }

        public ConsumerGroupBuilder WithType()
        {
            CreateOrOverwriteProperty(Graph.Metadata.Constants.RDF.Type, Graph.Metadata.Constants.ConsumerGroup.Type);
            return this;
        }

        public ConsumerGroupBuilder WithLifecycleStatus(string lifecycleStatus)
        {
            CreateOrOverwriteProperty(Graph.Metadata.Constants.ConsumerGroup.HasLifecycleStatus, lifecycleStatus);
            return this;
        }

        public ConsumerGroupBuilder WithContactPerson(string contactPerson)
        {
            CreateOrOverwriteProperty(Graph.Metadata.Constants.ConsumerGroup.HasContactPerson, contactPerson);
            return this;
        }

        public ConsumerGroupBuilder WithLabel(string label)
        {
            CreateOrOverwriteProperty(Graph.Metadata.Constants.RDFS.Label, label);
            return this;
        }

        public ConsumerGroupBuilder WithAdRole(string adRole)
        {
            CreateOrOverwriteProperty(Graph.Metadata.Constants.ConsumerGroup.AdRole, adRole);
            return this;
        }

        public ConsumerGroupBuilder WithPidUriTemplate(string template)
        {
            CreateOrOverwriteProperty(Graph.Metadata.Constants.ConsumerGroup.HasPidUriTemplate, template);
            return this;
        }

        public ConsumerGroupBuilder WithDefaultPidUriTemplate(string template)
        {
            CreateOrOverwriteProperty(Graph.Metadata.Constants.ConsumerGroup.HasDefaultPidUriTemplate, template);
            return this;
        }
    }
}
