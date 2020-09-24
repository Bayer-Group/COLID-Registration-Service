using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using COLID.Graph.Metadata.DataModels.Metadata.Comparison;
using COLID.RegistrationService.Services.Implementation.Comparison;
using Xunit;

namespace COLID.RegistrationService.Tests.Unit.Services.Comparison
{
    public class ComparisonExtensionsTests
    {
        private MetadataComparisonProperty CreateTestData(dynamic nodeKindA = null, dynamic dataTypeA = null, dynamic nodeKindB = null, dynamic dataTypeB = null)
        {
            var properties = new Dictionary<string, IDictionary<string, dynamic>>();

            var genericDatasetSubProbs = new Dictionary<string, dynamic>();
            var ontologySubProbs = new Dictionary<string, dynamic>();

            if (nodeKindA != null)
            {
                genericDatasetSubProbs.Add(Graph.Metadata.Constants.Shacl.NodeKind, nodeKindA);
            }

            if(dataTypeA != null)
            {
                genericDatasetSubProbs.Add(Graph.Metadata.Constants.Shacl.Datatype, dataTypeA);
            }

            if(nodeKindA != null || dataTypeA != null)
            {
                properties.Add(Graph.Metadata.Constants.Resource.Type.GenericDataset, genericDatasetSubProbs);
            }

            if (nodeKindB != null)
            {
                ontologySubProbs.Add(Graph.Metadata.Constants.Shacl.NodeKind, nodeKindB);
            }

            if (dataTypeB != null)
            {
                ontologySubProbs.Add(Graph.Metadata.Constants.Shacl.Datatype, dataTypeB);
            }

            if (nodeKindB != null || dataTypeB != null)
            {
                properties.Add(Graph.Metadata.Constants.Resource.Type.Ontology, ontologySubProbs);
            }

            return new MetadataComparisonProperty(Graph.Metadata.Constants.Resource.HasLabel, properties, null);
        }

        [Fact]
        public void ContainsOneDatatype_SingleResourceType_NoMetadata()
        {
            var properties = new Dictionary<string, IDictionary<string, dynamic>>();
            var genericDatasetSubProbs = new Dictionary<string, dynamic>();
            properties.Add(Graph.Metadata.Constants.Resource.Type.GenericDataset, genericDatasetSubProbs);

            var metadataComparisonPropertyUnderTest = new MetadataComparisonProperty(Graph.Metadata.Constants.Resource.HasLabel, properties, null);

            var result = metadataComparisonPropertyUnderTest.ContainsOneDatatype(out var resultNodeKind, out var resultDataType);

            Assert.False(result);
            Assert.Null(resultNodeKind);
            Assert.Null(resultDataType);
        }

        [Fact]
        public void ContainsOneDatatype_MultiResourceType_NoMetadata()
        {
            var properties = new Dictionary<string, IDictionary<string, dynamic>>();
            var genericDatasetSubProbs = new Dictionary<string, dynamic>();
            var ontologySubProbs = new Dictionary<string, dynamic>();
            properties.Add(Graph.Metadata.Constants.Resource.Type.GenericDataset, genericDatasetSubProbs);
            properties.Add(Graph.Metadata.Constants.Resource.Type.Ontology, ontologySubProbs);

            var metadataComparisonPropertyUnderTest = new MetadataComparisonProperty(Graph.Metadata.Constants.Resource.HasLabel, properties, null);

            var result = metadataComparisonPropertyUnderTest.ContainsOneDatatype(out var resultNodeKind, out var resultDataType);

            Assert.False(result);
            Assert.Null(resultNodeKind);
            Assert.Null(resultDataType);
        }

        [Theory]
        [InlineData(Graph.Metadata.Constants.Shacl.NodeKinds.Literal, Graph.Metadata.Constants.DataTypes.Boolean, null, null)]
        [InlineData(Graph.Metadata.Constants.Shacl.NodeKinds.Literal, Graph.Metadata.Constants.DataTypes.Boolean, Graph.Metadata.Constants.Shacl.NodeKinds.Literal, Graph.Metadata.Constants.DataTypes.Boolean)]
        public void MultiTestDatatypes_EqualNodeKinds_EqualDataTypes(string nodeKindA, string dataTypeA, string nodeKindB, string dataTypeB)
        {
            var metadataComparisonPropertyUnderTest = CreateTestData(nodeKindA, dataTypeA, nodeKindB, dataTypeB);

            var result = metadataComparisonPropertyUnderTest.ContainsOneDatatype(out var resultNodeKind, out var resultDataType);

            Assert.True(result);
            Assert.Equal(Graph.Metadata.Constants.Shacl.NodeKinds.Literal, resultNodeKind);
            Assert.Equal(Graph.Metadata.Constants.DataTypes.Boolean, resultDataType);
        }

        [Theory]
        [InlineData(Graph.Metadata.Constants.Shacl.NodeKinds.Literal, Graph.Metadata.Constants.DataTypes.Boolean, Graph.Metadata.Constants.Shacl.NodeKinds.Literal, null)]
        [InlineData(Graph.Metadata.Constants.Shacl.NodeKinds.Literal, null, null, null)]
        [InlineData(Graph.Metadata.Constants.Shacl.NodeKinds.Literal, Graph.Metadata.Constants.DataTypes.Boolean, Graph.Metadata.Constants.Shacl.NodeKinds.Literal, Graph.Metadata.Constants.DataTypes.String)]
        [InlineData(Graph.Metadata.Constants.Shacl.NodeKinds.Literal, Graph.Metadata.Constants.DataTypes.String, Graph.Metadata.Constants.Shacl.NodeKinds.Literal, Graph.Metadata.Constants.DataTypes.Boolean)]
        public void MultiTestDatatypes_EqualNodeKinds_DifferentDataTypes(string nodeKindA, string dataTypeA, string nodeKindB, string dataTypeB)
        {
            var metadataComparisonPropertyUnderTest = CreateTestData(nodeKindA, dataTypeA, nodeKindB, dataTypeB);

            var result = metadataComparisonPropertyUnderTest.ContainsOneDatatype(out var resultNodeKind, out var resultDataType);

            Assert.True(result);
            Assert.Equal(Graph.Metadata.Constants.Shacl.NodeKinds.Literal, resultNodeKind);
            Assert.Null(resultDataType);
        }

        [Theory]
        [InlineData(null, Graph.Metadata.Constants.DataTypes.Boolean, null, null)]
        [InlineData(Graph.Metadata.Constants.Shacl.NodeKinds.Literal, null, Graph.Metadata.Constants.Shacl.NodeKinds.IRI, null)]
        [InlineData(Graph.Metadata.Constants.Shacl.NodeKinds.IRI, null, Graph.Metadata.Constants.Shacl.NodeKinds.Literal, null)]
        [InlineData(Graph.Metadata.Constants.Shacl.NodeKinds.IRI, Graph.Metadata.Constants.DataTypes.Boolean, Graph.Metadata.Constants.Shacl.NodeKinds.Literal,  null)]
        [InlineData(Graph.Metadata.Constants.Shacl.NodeKinds.IRI, null, Graph.Metadata.Constants.Shacl.NodeKinds.Literal, Graph.Metadata.Constants.DataTypes.Boolean)]
        [InlineData(Graph.Metadata.Constants.Shacl.NodeKinds.IRI, Graph.Metadata.Constants.DataTypes.Boolean, Graph.Metadata.Constants.Shacl.NodeKinds.Literal, Graph.Metadata.Constants.DataTypes.Boolean)]

        public void MultiTestDatatypes_MultiNodeKinds_MultiDataTypes(string nodeKindA, string dataTypeA, string nodeKindB, string dataTypeB)
        {
            var metadataComparisonPropertyUnderTest = CreateTestData(nodeKindA, dataTypeA, nodeKindB, dataTypeB);

            var result = metadataComparisonPropertyUnderTest.ContainsOneDatatype(out var resultNodeKind, out var resultDataType);

            Assert.False(result);
            Assert.Null(resultNodeKind);
            Assert.Null(resultDataType);
        }
    }
}
