using VDS.RDF;
using VDS.RDF.Query;
using VDS.RDF.Writing;
using COLID.Graph.TripleStore.DataModels.Sparql;
using COLID.Graph.Metadata.Constants;

namespace COLID.Graph.TripleStore.Extensions
{
    public static class SparqlResultExtension
    {
        public static SparqlResponseProperty GetNodeValuesFromSparqlResult(this SparqlResult sparqlResult, string value)
        {
            INode node;
            var data = new SparqlResponseProperty();
            if (sparqlResult.TryGetValue(value, out node))
            {
                if (node != null)
                {
                    data.Type = node.NodeType.ToString().ToLower();

                    switch (node.NodeType)
                    {
                        case NodeType.Uri:
                            data.Type = Shacl.NodeKinds.IRI;
                            data.Value = ((IUriNode)node).Uri.AbsoluteUri;
                            break;

                        case NodeType.Blank:
                            data.Type = Shacl.NodeKinds.BlankNode;
                            data.Value = ((IBlankNode)node).InternalID;
                            break;

                        case NodeType.Literal:
                            //You may want to inspect the DataType and Language properties and generate
                            //a different string here
                            data.Type = Shacl.NodeKinds.Literal;
                            data.Value = ((ILiteralNode)node).Value;
                            data.DataType = ((ILiteralNode)node).DataType?.OriginalString;
                            break;

                        default:
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                            throw new RdfOutputException("Unexpected Node Type");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                    }
                }
            }
            return data;
        }
    }
}
