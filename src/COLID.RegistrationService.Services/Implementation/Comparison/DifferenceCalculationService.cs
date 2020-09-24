using System;
using System.Collections.Generic;
using System.Linq;
using COLID.Common.Extensions;
using COLID.Graph.Metadata.DataModels.Metadata.Comparison;
using COLID.Graph.TripleStore.Extensions;
using COLID.RegistrationService.Services.Interface;
using DiffPlex;
using DiffPlex.Chunkers;
using DiffPlex.Model;
using Entity = COLID.Graph.TripleStore.DataModels.Base.Entity;

namespace COLID.RegistrationService.Services.Implementation.Comparison
{
    public class DifferenceCalculationService : IDifferenceCalculationService
    {
        private const string diffInsertionStart = "<span class=\"diff insertion\">";
        private const string diffInsertionEnd   = "</span>";
        private const string diffDeletionStart  = "<span class=\"diff deletion\">";
        private const string diffDeletionEnd    = "</span>";

        public IDictionary<string, IList<dynamic>> Calculate(MetadataComparisonProperty metadataComparisonProperty, Entity[] resources)
        {
            IDictionary<string, IList<dynamic>> props = new Dictionary<string, IList<dynamic>>();

            if (metadataComparisonProperty.ContainsOneDatatype(out var nodeKind, out var _))
            {
                if (nodeKind == Graph.Metadata.Constants.Shacl.NodeKinds.IRI)
                {
                    props = CalculateIRIDifference(metadataComparisonProperty, resources);
                }
                else if (nodeKind == Graph.Metadata.Constants.Shacl.NodeKinds.Literal)
                {
                    props = CalculateLiteralValuesDifference(metadataComparisonProperty.Key, resources);
                }
            }

            return props;
        }

        /// <summary>
        /// Calculates the differences for properties of NodeKind IRI. IRI properties could contain nested metadata, so these need to compared additionally.
        /// Otherwise just copy the resource property values to the result list.
        /// </summary>
        /// <param name="metadataComparisonProperty">The current metadata property to compare in the given resources</param>
        /// <param name="resources">resources to compare</param>
        /// <returns>The comparison result for a specific property. While the returned key is the metadata key, the list contains the compared properties of both resources.</returns>
        private IDictionary<string, IList<dynamic>> CalculateIRIDifference(MetadataComparisonProperty metadataComparisonProperty, params Entity[] resources)
        {
            if (metadataComparisonProperty.NestedMetadata.IsNullOrEmpty())
            {
                return ExtractIRIValues(metadataComparisonProperty, resources);
            }
            else
            {
                return CalculateIRINestedValuesDifference(metadataComparisonProperty, resources);
            }
        }

        /// <summary>
        /// Copies the valus from the result properties.
        /// </summary>
        /// <param name="metadataComparisonProperty">The current metadata property to compare in the given resources</param>
        /// <param name="resources">resources to compare</param>
        /// <exception cref="KeyNotFoundException">If both resources don't contain the key in the properties.</exception>
        /// <returns>The comparison result for a specific property. While the returned key is the metadata key, the list contains the compared properties of both resources.</returns>
        private IDictionary<string, IList<dynamic>> ExtractIRIValues(MetadataComparisonProperty metadataComparisonProperty, params Entity[] resources)
        {
            var propertyResults = new Dictionary<string, IList<dynamic>>();

            if (resources[0].Properties.TryGetValue(metadataComparisonProperty.Key, out List<dynamic> firstValue))
            {
                propertyResults.Add(resources[0].Id, firstValue);
            }

            if (resources[1].Properties.TryGetValue(metadataComparisonProperty.Key, out List<dynamic> secondValue))
            {
                propertyResults.Add(resources[1].Id, secondValue);
            }

            if (propertyResults.Count == 0)
            {
                throw new KeyNotFoundException($"Key {metadataComparisonProperty.Key} not found in properties.");
            }

            return propertyResults;
        }

        /// <summary>
        /// Calculates for resource properties with NodeKind IRI and nested metadata the difference between all subproperties in the list.
        /// </summary>
        /// <param name="metadataComparisonProperty">The current metadata property to compare in the given resources</param>
        /// <param name="resources">resources to compare</param>
        /// <returns>The comparison result for a specific property. While the returned key is the metadata key, the list contains the compared properties of both resources.</returns>
        private IDictionary<string, IList<dynamic>> CalculateIRINestedValuesDifference(MetadataComparisonProperty metadataComparisonProperty, params Entity[] resources)
        {
            var propertyResults = new Dictionary<string, IList<dynamic>>();

            var allFirstNestedEntities = resources[0].Properties.ContainsKey(metadataComparisonProperty.Key) ?
                                            resources[0].Properties[metadataComparisonProperty.Key]?.Select(x => ((Entity)x)).ToList() :
                                            null;

            var allSecondNestedEntities = resources[1].Properties.ContainsKey(metadataComparisonProperty.Key) ?
                                            resources[1].Properties[metadataComparisonProperty.Key]?.Select(x => ((Entity)x)).ToList() :
                                            null;

            if (allFirstNestedEntities != null)
            {
                propertyResults.Add(resources[0].Id, new List<dynamic>());

                if (allSecondNestedEntities != null)
                {
                    propertyResults.Add(resources[1].Id, new List<dynamic>());
                }

                // Compare all nested properties that are present in both resources.
                // The existence of the nested properties is given by the fact that both nested properties have the same PID URI and
                // the same resource type, e.g. Browsable Resource
                foreach (var firstEntity in allFirstNestedEntities)
                {
                    if (firstEntity.Properties.TryGetValue(Graph.Metadata.Constants.EnterpriseCore.PidUri, out List<dynamic> pidUriA))
                    {
                        var secondEntity = allSecondNestedEntities?.SingleOrDefault(b =>
                        {
                            if (b.Properties.TryGetValue(Graph.Metadata.Constants.EnterpriseCore.PidUri, out List<dynamic> pidUriB))
                            {
                                if (((Entity)pidUriA.First()).Id != ((Entity)pidUriB.First()).Id)
                                {
                                    return false;
                                }

                                if (firstEntity.Properties.TryGetValue(Graph.Metadata.Constants.RDF.Type, out List<dynamic> typeA) &&
                                    b.Properties.TryGetValue(Graph.Metadata.Constants.RDF.Type, out List<dynamic> typeB))
                                {
                                    if (typeA[0] == typeB[0])
                                    {
                                        return true;
                                    }
                                }
                            }

                            return false;
                        });

                        // If the second resource has no nested property with the same PID URI,
                        // then add the found property to the first resource, mark all sub properties as deleted
                        // and continue with the next property
                        if (secondEntity == null)
                        {
                            // TODO SL: set all entries in entityA to <diffDel>
                            propertyResults[resources[0].Id].Add(firstEntity);
                            continue;
                        }

                        // If both properties are found with the same PID URI and the same resource type,
                        // compare all sub - properties using the resource metadata and calculate the differences
                        if (firstEntity.Properties.TryGetValue(Graph.Metadata.Constants.RDF.Type, out List<dynamic> typeA))
                        {
                            var metadata = metadataComparisonProperty.NestedMetadata.First(m => m.Key == typeA.First());

                            var firstComparedEntity = new Entity() { Id = firstEntity.Id };
                            var secondComparedEntity = new Entity() { Id = secondEntity.Id };

                            foreach (var prop in metadata.Properties)
                            {
                                try
                                {
                                    if (prop.Key == Graph.Metadata.Constants.EnterpriseCore.PidUri)
                                    {
                                        firstComparedEntity.Properties.Add(prop.Key, firstEntity.Properties[Graph.Metadata.Constants.EnterpriseCore.PidUri]);
                                        secondComparedEntity.Properties.Add(prop.Key, secondEntity.Properties[Graph.Metadata.Constants.EnterpriseCore.PidUri]);
                                        continue;
                                    }

                                    var compareResult = CalculateLiteralValuesDifference(prop.Key, new Entity[] { firstEntity, secondEntity });
                                    firstComparedEntity.Properties.Add(prop.Key, compareResult[firstEntity.Id].ToList());
                                    secondComparedEntity.Properties.Add(prop.Key, compareResult[secondEntity.Id].ToList());
                                }
                                catch (KeyNotFoundException)
                                {
                                }
                            }

                            propertyResults[resources[0].Id].Add(firstComparedEntity);
                            propertyResults[resources[1].Id].Add(secondComparedEntity);
                        }
                    }
                }
            }

            // In the previous comparison it was ignored that there are properties of the second resource
            // that were not present in the first one. These must now be added and all sub properties must be marked as inserted.
            var secondEntitiesNotFoundInFirst = allSecondNestedEntities?.Where(b => {

                if(b.Properties.TryGetValue(Graph.Metadata.Constants.EnterpriseCore.PidUri, out List<dynamic> pidUriB))
                {
                    if (allFirstNestedEntities.Any(a => ((Entity)a.Properties[Graph.Metadata.Constants.EnterpriseCore.PidUri].First()).Id == ((Entity)pidUriB.First()).Id))
                    {
                        return false;
                    }
                    return true;
                }

                return false;
            });

            if(secondEntitiesNotFoundInFirst != null)
            {
                // TODO SL: set all entries in entity to <diffIns>
                propertyResults[resources[1].Id].AddRange(secondEntitiesNotFoundInFirst);
            }

            return propertyResults;
        }

        /// <summary>
        /// Compares the values of a single value of a property of two incoming resources and summarizes the comparison results in the target result.
        /// If more than just one value is set for the property (from the SHACLs point of view, if MaxCount > 1), these are first sorted alphabetically
        /// and must then be compared as an entire list. 
        /// </summary>
        /// <param name="metadataKey">The current metadata property key to compare in the given resources</param>
        /// <param name="resources">resources to compare</param>
        /// <returns>The comparison result for a specific property. While the returned key is the metadata key, the list contains the compared properties of both resources.</returns>
        private IDictionary<string, IList<dynamic>> CalculateLiteralValuesDifference(string metadataKey, params Entity[] resources)
        {
            var propertyResults = new Dictionary<string, IList<dynamic>>();
            try
            {
                var firstResourceData = resources[0].Properties[metadataKey].Select(x => ((object)x).ToString()).ToList();
                var secondResourceData = resources[1].Properties[metadataKey].Select(x => ((object)x).ToString()).ToList();

                firstResourceData.Sort();
                secondResourceData.Sort();

                Tuple<IList<string>, IList<string>> diffResult;

                if (firstResourceData.Count == 1 && secondResourceData.Count == 1)
                {
                    diffResult = CalculateLiteralSingleValueDiff(firstResourceData[0], secondResourceData[0]);
                }
                else
                {
                    diffResult = CalculateLiteralListDifference(firstResourceData, secondResourceData);
                }

                var resultA = new List<dynamic>(diffResult.Item1);
                var resultB = new List<dynamic>(diffResult.Item2);

                propertyResults.Add(resources[0].Id, resultA);
                propertyResults.Add(resources[1].Id, resultB);

                return propertyResults;
            }
            catch (KeyNotFoundException ex)
            {
                if(resources[0].Properties.TryGetValue(metadataKey, out List<dynamic> firstValue))
                {
                    propertyResults.Add(resources[0].Id, firstValue);
                }

                if (resources[1].Properties.TryGetValue(metadataKey, out List<dynamic> secondValue))
                {
                    propertyResults.Add(resources[1].Id, secondValue);
                }

                if (propertyResults.Count == 0)
                {
                    throw new KeyNotFoundException(ex.Message);
                }

                return propertyResults;
            }
        }

        /// <summary>
        /// Calculates the differences of two literals on a character basis. This means that changes of single characters can be fully traced.
        /// </summary>
        /// <param name="firstLiteral">First literal to compare</param>
        /// <param name="secondLiteral">Second literal to compare</param>
        /// <returns>Returns the result of the comparison, where all changes can be tracked with tags.</returns>
        private Tuple<IList<string>, IList<string>> CalculateLiteralSingleValueDiff(string firstLiteral, string secondLiteral)
        {
            IDiffer differ = new Differ();

            var resourcePropertyValueDifference = differ.CreateDiffs(firstLiteral, secondLiteral, false, false, new CharacterChunker());
            var result = GenerateDifferenceOutputFormat(resourcePropertyValueDifference);

            return new Tuple<IList<string>, IList<string>>(new List<string>() { result.Item1 }, new List<string>() { result.Item2 });
        }

        /// <summary>
        /// Calculates the difference between two incoming lists on a per-line basis.
        /// The lists are first combined as single lines, then a line-by-line change is calculated and reassembled.
        /// </summary>
        /// <param name="firstResourceData">First list of literals to compare</param>
        /// <param name="secondResouceData">Second list of literals to compare</param>
        /// <returns>Returns the result of the comparison, where all line changes can be tracked with tags.</returns>
        private Tuple<IList<string>, IList<string>> CalculateLiteralListDifference(IList<string> firstResourceData, IList<string> secondResouceData)
        {
            IDiffer differ = new Differ();
            const string lineEndingSeperator = "\n";

            // combine two
            string resourceAFlattenedCompareList = string.Join(lineEndingSeperator, firstResourceData);
            string resourceBFlattenedCompareList = string.Join(lineEndingSeperator, secondResouceData);

            var resourcePropertyValueDifference = differ.CreateDiffs(resourceAFlattenedCompareList, resourceBFlattenedCompareList, false, false, new LineChunker());
            var result = GenerateDifferenceOutputFormat(resourcePropertyValueDifference, lineEndingSeperator);

            return new Tuple<IList<string>, IList<string>>(result.Item1.Split(lineEndingSeperator), result.Item2.Split(lineEndingSeperator));
        }

        /// <summary>
        /// Maps the DiffPlex result to a literal based tuple and inserts tags that allow to track changes between the two literals.
        /// </summary>
        /// <param name="diffResult">The Diffplex result containing changes between two literal results</param>
        /// <param name="lineEndingSeperator">If the result contains multiple literals on both sides (lists), the line ending seperator is added to the end of a line.</param>
        /// <returns>literal based tuple containing start and end tags to track changes.</returns>
        private Tuple<string, string> GenerateDifferenceOutputFormat(DiffResult diffResult, string lineEndingSeperator = "")
        {
            string resultA = string.Empty;
            string resultB = string.Empty;

            for(int i = 0; i <= diffResult.PiecesOld.Count(); i++)
            {
                foreach(var diffBlock in diffResult.DiffBlocks)
                {   
                    if(i == diffBlock.DeleteStartA)
                    {
                        resultA += diffDeletionStart;
                    }

                    if(i == diffBlock.DeleteStartA + diffBlock.DeleteCountA)
                    {
                        resultA += diffDeletionEnd;
                    }
                }

                if (i < diffResult.PiecesOld.Count())
                {
                    resultA += diffResult.PiecesOld[i];
                }

                if (i < diffResult.PiecesNew.Count() - 1)
                {
                    resultA += lineEndingSeperator;
                }
            }

            for (int i = 0; i <= diffResult.PiecesNew.Count(); i++)
            {
                foreach (var diffBlock in diffResult.DiffBlocks)
                {
                    if (i == diffBlock.InsertStartB)
                    {
                        resultB += diffInsertionStart;
                    }

                    if (i == diffBlock.InsertStartB + diffBlock.InsertCountB)
                    {
                        resultB += diffInsertionEnd;
                    }
                }

                if (i < diffResult.PiecesNew.Count())
                {
                    resultB += diffResult.PiecesNew[i];
                }

                if (i < diffResult.PiecesNew.Count() - 1)
                {
                    resultB += lineEndingSeperator;
                }
            }

            var result = new Tuple<string, string>(resultA, resultB);
            return result;
        }
    }
}
