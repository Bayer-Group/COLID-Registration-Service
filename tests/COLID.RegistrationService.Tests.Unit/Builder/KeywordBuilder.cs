using System;
using COLID.RegistrationService.Common.DataModel.Keywords;

namespace COLID.RegistrationService.Tests.Common.Builder
{
    public class KeywordBuilder : AbstractEntityBuilder<Keyword>
    {
        private Keyword _res = new Keyword();

        public override Keyword Build()
        {
            _res.Properties = _prop;
            return _res;
        }

        /// <summary>
        /// <b>Caution</b>: may override existing content, use it right after:
        /// <code>new KeywordBuilder().GenerateSampleData().With(...)</code>
        /// </summary>
        public KeywordBuilder GenerateSampleData()
        {
            WithId(Graph.Metadata.Constants.Entity.IdPrefix + Guid.NewGuid());
            WithType();
            WithLabel("JustAnotherKeyword");

            return this;
        }

        public KeywordBuilder WithId(string id)
        {
            _res.Id = id;
            return this;
        }

        public KeywordBuilder WithType()
        {
            CreateOrOverwriteProperty(Graph.Metadata.Constants.RDF.Type, Graph.Metadata.Constants.Keyword.Type);
            return this;
        }

        public KeywordBuilder WithLabel(string label)
        {
            CreateOrOverwriteProperty(Graph.Metadata.Constants.RDFS.Label, label);
            return this;
        }
    }
}
