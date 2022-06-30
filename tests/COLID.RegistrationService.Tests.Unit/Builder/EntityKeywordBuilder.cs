using System;
using COLID.Graph.TripleStore.DataModels.Base;

namespace COLID.RegistrationService.Tests.Common.Builder
{
    public class EntityKeywordBuilder : AbstractEntityBuilder<Entity>
    {
        private Entity _res = new Entity();

        public override Entity Build()
        {
            _res.Properties = _prop;
            return _res;
        }

        /// <summary>
        /// <b>Caution</b>: may override existing content, use it right after:
        /// <code>new EntityKeywordBuilder().GenerateSampleData().With(...)</code>
        /// </summary>
        public EntityKeywordBuilder GenerateSampleData()
        {
            WithId(Graph.Metadata.Constants.Entity.IdPrefix + Guid.NewGuid());
            WithType();
            WithLabel("JustAnotherKeyword");

            return this;
        }

        public EntityKeywordBuilder WithId(string id)
        {
            _res.Id = id;
            return this;
        }

        public EntityKeywordBuilder WithType()
        {
            CreateOrOverwriteProperty(Graph.Metadata.Constants.RDF.Type, Graph.Metadata.Constants.Keyword.Type);
            return this;
        }

        public EntityKeywordBuilder WithLabel(string label)
        {
            CreateOrOverwriteProperty(Graph.Metadata.Constants.RDFS.Label, label);
            return this;
        }
    }
}
