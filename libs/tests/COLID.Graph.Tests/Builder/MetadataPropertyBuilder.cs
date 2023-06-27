using System.Collections.Generic;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.TripleStore.Extensions;

namespace COLID.Graph.Tests.Builder
{
    public class MetadataPropertyBuilder
    {
        private MetadataProperty _prop = new MetadataProperty();

        public MetadataProperty Build()
        {
            return _prop;
        }

        public MetadataPropertyBuilder WithPidUri(string value)
        {
            CreateAndAddPropety(Graph.Metadata.Constants.EnterpriseCore.PidUri, value);
            return this;
        }

        public MetadataPropertyBuilder WithGroup(MetadataPropertyGroup group)
        {
            _prop.Properties.AddOrUpdate(Graph.Metadata.Constants.Shacl.Group, group);
            return this;
        }

        public MetadataPropertyBuilder WithPath(string path)
        {
            CreateAndAddPropety(Graph.Metadata.Constants.Shacl.Path, path);
            return this;
        }

        public MetadataPropertyBuilder WithType(string type)
        {
            CreateAndAddPropety(Graph.Metadata.Constants.RDF.Type, type);
            return this;
        }

        public MetadataPropertyBuilder WithNodekind(string nodekind)
        {
            CreateAndAddPropety(Graph.Metadata.Constants.Shacl.NodeKind, nodekind);
            return this;
        }

        public MetadataPropertyBuilder WithRange(string range)
        {
            CreateAndAddPropety(Graph.Metadata.Constants.Shacl.Range, range);
            return this;
        }

        public MetadataPropertyBuilder WithClass(string range)
        {
            CreateAndAddPropety(Graph.Metadata.Constants.Shacl.Class, range);
            return this;
        }

        public MetadataPropertyBuilder WithLabel(string label)
        {
            CreateAndAddPropety(Graph.Metadata.Constants.RDFS.Label, label);
            return this;
        }

        public MetadataPropertyBuilder WithDataType(string dataType)
        {
            CreateAndAddPropety(Graph.Metadata.Constants.Shacl.Datatype, dataType);
            return this;
        }

        public MetadataPropertyBuilder WithName(string name)
        {
            CreateAndAddPropety(Graph.Metadata.Constants.Shacl.Name, name);
            return this;
        }

        public MetadataPropertyBuilder WithMaxCount(string maxCount)
        {
            CreateAndAddPropety(Graph.Metadata.Constants.Shacl.MaxCount, maxCount);
            return this;
        }

        public MetadataPropertyBuilder WithMinCount(string minCount)
        {
            CreateAndAddPropety(Graph.Metadata.Constants.Shacl.MinCount, minCount);
            return this;
        }

        public MetadataPropertyBuilder WithOrder(string order)
        {
            CreateAndAddPropety(Graph.Metadata.Constants.Shacl.Order, order);
            return this;
        }

        public MetadataPropertyBuilder WithComment(string comment)
        {
            CreateAndAddPropety(Graph.Metadata.Constants.RDFS.Comment, comment);
            return this;
        }

        public MetadataPropertyBuilder WithDomain(string domain)
        {
            CreateAndAddPropety(Graph.Metadata.Constants.RDFS.Domain, domain);
            return this;
        }

        public MetadataPropertyBuilder WithSubPropertyOf(string subPropertyOf)
        {
            CreateAndAddPropety(Graph.Metadata.Constants.RDFS.SubPropertyOf, subPropertyOf);
            return this;
        }

        public MetadataPropertyBuilder WithDescription(string definition)
        {
            CreateAndAddPropety(Graph.Metadata.Constants.Shacl.Description, definition);
            return this;
        }

        public MetadataPropertyBuilder WithFieldType(string fieldType)
        {
            CreateAndAddPropety(Graph.Metadata.Constants.PIDO.Shacl.FieldType, fieldType);
            return this;
        }

        public MetadataPropertyBuilder WithNestedMetadata(IList<Graph.Metadata.DataModels.Metadata.Metadata> nestedMetadata)
        {
            _prop.NestedMetadata = nestedMetadata;
            return this;
        }

        #region Helper fuctions

        private void CreateAndAddPropety(string metadataKey, string content)
        {
            _prop.Properties.AddOrUpdate(metadataKey, content);
        }

        #endregion Helper fuctions
    }
}
