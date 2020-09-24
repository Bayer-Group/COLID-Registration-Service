using COLID.Graph.TripleStore.DataModels.Base;

namespace COLID.RegistrationService.Tests.Common.Builder
{
    public class PidUriBuilder : AbstractEntityBuilder<Entity>
    {
        private Entity _pidUri = new Entity();

        public override Entity Build()
        {
            _pidUri.Properties = _prop;
            return _pidUri;
        }

        public PidUriBuilder WithId(string id)
        {
            _pidUri.Id = id;
            return this;
        }

        public PidUriBuilder WithType()
        {
            CreateOrOverwriteProperty(Graph.Metadata.Constants.RDF.Type, Graph.Metadata.Constants.Identifier.Type);
            return this;
        }

        public PidUriBuilder WithPidUriTemplate(string uriTemplate)
        {
            CreateOrOverwriteProperty(Graph.Metadata.Constants.Identifier.HasUriTemplate, uriTemplate);
            return this;
        }
    }
}
