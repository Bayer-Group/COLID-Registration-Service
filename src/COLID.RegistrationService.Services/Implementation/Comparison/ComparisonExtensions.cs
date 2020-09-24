using System.Collections.Generic;
using System.Linq;
using COLID.Graph.Metadata.DataModels.Metadata.Comparison;
using COLID.Graph.TripleStore.Extensions;

namespace COLID.RegistrationService.Services.Implementation.Comparison
{
    /// <summary>
    /// Extensions for the comparison
    /// </summary>
    public static class ComparisonExtensions
    {
        /// <summary>
        /// Checks the inner properties of MetadataComparisonProperty for NodeKind and DataType
        /// </summary>
        /// <param name="metadataComparisonProperty">The metadata properties for comparison</param>
        /// <param name="nodeKind">Nodekind of the current metadata, e.g. IRI or Literal</param>
        /// <param name="dataType">DataType of the current metadata, e.g. boolean or string</param>
        /// <returns></returns>
        public static bool ContainsOneDatatype(this MetadataComparisonProperty metadataComparisonProperty, out string nodeKind, out string dataType)
        {
            var rdfsTypes = new List<string>();

            // First check if the current resource types in comparison contain different NodeKinds
            // e.g. shacl:Literal or shacl:IRI
            var nodeKinds = metadataComparisonProperty.Properties.Select(c => c.Value.GetValueOrNull(Graph.Metadata.Constants.Shacl.NodeKind, true));

            if (nodeKinds.Any(nk => nk is IEnumerable<dynamic> enumNodeKinds))
            {
                throw new Exception.Models.BusinessException("Given metadata is incorrect, only single node kinds allowed");
            }

            if (IsNotSingleValue(nodeKinds))
            {
                nodeKind = null;
                dataType = null;
                return false;
            }

            // If we don't have different NodeKinds, check if the current resource types in comparison contain different data types.
            // Note: if the NodeKind is not shacl:Literal, the SHACL data type is not present in the resource types properties
            // e.g. rdfs:HTML, xmls:boolean, xmls:string
            var shaclDatatypes = metadataComparisonProperty.Properties.Select(c => c.Value.GetValueOrNull(Graph.Metadata.Constants.Shacl.Datatype, true));

            if (shaclDatatypes.Any(dt => dt is IEnumerable<dynamic> enumDataTypes))
            {
                throw new Exception.Models.BusinessException("Given metadata is incorrect, only single data types allowed");
            }

            if (IsNotSingleValue(shaclDatatypes))
            {
                nodeKind = nodeKinds.First().ToString();
                dataType = null;
                return true;
            }

            nodeKind = nodeKinds.First().ToString();
            dataType = shaclDatatypes?.First().ToString();
            return true;
        }

        private static bool IsNotSingleValue(IEnumerable<dynamic> properties)
        {
            var distinctNodeKinds = properties.Distinct();
            return distinctNodeKinds.Count() != 1 || distinctNodeKinds.Any(n => n == null);
        }
    }
}
