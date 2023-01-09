using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using COLID.Cache.Services;
using COLID.Common.Extensions;
using COLID.Exception.Models;
using COLID.Graph.Metadata.Constants;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.Metadata.Services;
using COLID.Graph.TripleStore.DataModels.Attributes;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.Extensions;
using COLID.RegistrationService.Common.Extensions;
using COLID.RegistrationService.Repositories.Interface;
using Microsoft.AspNetCore.Hosting;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Shacl;
using VDS.RDF.Shacl.Validation;
using VDS.RDF.Writing;
using Entity = COLID.Graph.TripleStore.DataModels.Base.Entity;

namespace COLID.RegistrationService.Services.Validation
{
    internal class ValidationService : IValidationService
    {
        private readonly IMetadataService _metadataService;
        private readonly IEntityRepository _entityRepository;
        private readonly ICacheService _cacheService;
        private readonly IMapper _mapper;

        public ValidationService(IMetadataService metadataService, IEntityRepository entityRepository, ICacheService cacheService, IMapper mapper, IWebHostEnvironment webHostEnvironment)
        {
            _metadataService = metadataService;
            _entityRepository = entityRepository;
            _cacheService = cacheService;
            _mapper = mapper;
        }

        #region Shacl Validation
        /// <summary>
        /// has side effects! updates triplestore
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="metadataProperties"></param>
        /// <returns></returns>
        public async Task<ValidationResult> ValidateEntity(Entity entity, IList<MetadataProperty> metadataProperties, bool ignoreInvalidProperties = false)
        {
            var resourceGraph = GetResourceGraph(entity, metadataProperties);
            var shapesGraph = GetShapesGraph();

            var processor = new ShapesGraph(shapesGraph);
            var report = processor.Validate(resourceGraph);
            var validationResult = CreateValidationResult(report);
            
            //Remove NonMandatory properties if they have validtion error 
            if (ignoreInvalidProperties)
            {
                var validationresults = validationResult.Results.ToList();
                foreach (var errResult in validationresults)
                {
                    if (!CheckPropertyIsMandatory(errResult.Path, metadataProperties))
                    {
                        
                        if (entity.Properties.TryGetValue(errResult.Path, out var curProperty ))
                        {
                            if (curProperty.Count > 1)
                            {
                                //Remove From RequestResource
                                int curIndex = curProperty.IndexOf(errResult.ResultValue.Split("^^")[0]);
                                if (curIndex > -1)
                                {
                                    curProperty.RemoveAt(curIndex);
                                    
                                    //Remove triple from graph
                                    var curNode = resourceGraph.GetUriNode(new Uri(errResult.Path));
                                    var curTRiple = resourceGraph.GetTriplesWithPredicate(curNode).Where(s => s.Object.ToString() == errResult.ResultValue).FirstOrDefault();
                                    resourceGraph.Retract(curTRiple);

                                    //Remove from Validation error
                                    validationResult.Results.Remove(errResult);
                                }
                            }
                            else
                            {
                                //Remove From RequestResource
                                entity.Properties.Remove(errResult.Path);

                                //Remove triple from graph
                                var curNode = resourceGraph.GetUriNode(new Uri(errResult.Path));
                                var curTRiple = resourceGraph.GetTriplesWithPredicate(curNode).FirstOrDefault();
                                resourceGraph.Retract(curTRiple);

                                //Remove from Validation error
                                validationResult.Results.Remove(errResult);
                            }
                        }                                                                                              
                    }
                }
            }
            
            NTriplesWriter rdfNTriplesWriter = new NTriplesWriter();
            validationResult.Triples = VDS.RDF.Writing.StringWriter.Write(resourceGraph, rdfNTriplesWriter);
            return await Task.FromResult(validationResult);
        }

        public async Task<ValidationResult> ValidateEntity(List<Entity> entities, IList<MetadataProperty> metadataProperties)
        {
            var resourceGraph = GetResourceGraph(entities, metadataProperties);
            var shapesGraph = GetShapesGraph();
            
            var processor = new ShapesGraph(shapesGraph);
            var report = processor.Validate(resourceGraph);

            var validationResult = CreateValidationResult(report);
            
            
            
            return await Task.FromResult(validationResult);
        }

        /// <summary>
        /// Maps the report of the shacl validator to the colid internal validation result object
        /// </summary>
        /// <param name="report">Report of shacl validator</param>
        /// <param name="markedResultsAsCritical">Indicates whether the validation results should be defined as critical</param>
        /// <returns>Validation resilt of shacl validation</returns>
        private ValidationResult CreateValidationResult(Report report)
        {
            IList<ValidationResultProperty> validationResults = report
                .Results
                .Select(r => _mapper.Map<ValidationResultProperty>(r))
                .ToList();

            var validationResult = new ValidationResult() { Results = validationResults };

            return validationResult;
        }

        /// <summary>
        /// Converts the entity into an rdf graph
        /// has side effects! updates triplestore
        /// </summary>
        /// <param name="entity">Entity to be converted</param>
        /// <param name="metadataProperties">Metadata of the entity to be converted</param>
        /// <returns>RDf graph of entity</returns>
        private IGraph GetResourceGraph(Entity entity, IList<MetadataProperty> metadataProperties)
        {
            var store = new TripleStore();

            //Create InsertString from resource
            if (metadataProperties.IsNullOrEmpty())
            {
                string entityType = entity.Properties.GetValueOrNull(Graph.Metadata.Constants.RDF.Type, true).ToString();
                metadataProperties = _metadataService.GetMetadataForEntityType(entityType);
            }
            var resourceInsertString = _entityRepository.GenerateInsertQuery(entity, metadataProperties, null);

            store.ExecuteUpdate(resourceInsertString.ToString());
            //store.SaveToFile("abcd",);
            
            return store.Graphs.FirstOrDefault();
        }

        /// <summary>
        /// Converts the entity into an rdf graph
        /// </summary>
        /// <param name="entities">List of entities to be converted</param>
        /// <param name="metadataProperties">Metadata of the entity to be converted</param>
        /// <returns>RDf graph of entity</returns>
        private IGraph GetResourceGraph(List<Entity> entities, IList<MetadataProperty> metadataProperties)
        {
            var store = new TripleStore();

            //Create InsertString from resource
            foreach (Entity entity in entities)
            {
                var resourceInsertString = _entityRepository.GenerateInsertQuery(entity, metadataProperties, null);
                store.ExecuteUpdate(resourceInsertString.ToString());
            }
            
            return store.Graphs.FirstOrDefault();
        }

        /// <summary>
        /// Returns all metadata and the stored instances as graph. 
        /// There are some adjustments to the data to make the metadata compatible with the Shacl validator.
        /// </summary>
        /// <returns></returns>
        private IGraph GetShapesGraph()
        {
            var shapes = _cacheService.GetOrAdd($"shapes-graph", () =>
            {
                var store = new TripleStore();
                var data = _metadataService.GetAllShaclAsGraph();
                
                store.Add(data);

                ModifiyShapesForTargetClass(store);
                ModifyShapesForShaclClass(store);

                var dataGraph = store.Graphs.FirstOrDefault(t => t.BaseUri?.OriginalString == data.BaseUri.OriginalString);

                if (dataGraph == null)
                {
                    throw new ArgumentNullException("Shapes graph is null");
                }

                NTriplesWriter writer = new NTriplesWriter(NTriplesSyntax.Original);
                var shapes = VDS.RDF.Writing.StringWriter.Write(dataGraph, writer);

                return shapes;
            });

            var shapesGraph = new VDS.RDF.Graph(true);
            var reader = new StringReader(shapes);

            var nTriplesParser = new NTriplesParser();
            nTriplesParser.Load(shapesGraph, reader);

            return shapesGraph;
        }

        /// <summary>
        /// Currently the referenced instances are not contained in the resource graph. 
        /// The shacl validator would therefore throw a error, which is suppressed by this function to the metadata graph. 
        /// </summary>
        /// <param name="store">InMemory store with metadata</param>
        private void ModifyShapesForShaclClass(VDS.RDF.TripleStore store)
        {
            store.ExecuteUpdate(@"
                                    PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
                                    PREFIX sh: <http://www.w3.org/ns/shacl#>
                                    DELETE { graph ?g { ?s  sh:class  ?o } }
                                    WHERE { graph ?g { ?s  sh:class  ?o } }
            ");
        }

        /// <summary>
        /// Classes must be referenced as target class. This must be added to the shacls. 
        /// </summary>
        /// <param name="store">InMemory store with metadata</param>
        private void ModifiyShapesForTargetClass(VDS.RDF.TripleStore store)
        {
            store.ExecuteUpdate(@"
                                    PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
                                    PREFIX sh: <http://www.w3.org/ns/shacl#>
                                    INSERT { graph ?g { ?subclass sh:targetClass ?subclass } }
                                    WHERE { graph ?g { ?subclass a ?type } }
            ");
        }
        #endregion

        #region Type Validation
        public void CheckType<TEntity>(TEntity entity) where TEntity : EntityBase
        {
            CheckType(entity, _metadataService.GetLeafEntityTypes);
        }

        public void CheckInstantiableEntityType<TEntity>(TEntity entity) where TEntity : EntityBase
        {
            CheckType(entity, _metadataService.GetInstantiableEntityTypes);
        }

        private void CheckType<TEntity>(TEntity entity, Func<string, IList<string>> getEntityTypes) where TEntity : EntityBase
        {
            string name = typeof(TEntity).GetAttributeValue((TypeAttribute type) => type.Type);
            var leafTypes = string.IsNullOrWhiteSpace(name) ? new List<string>() : getEntityTypes.Invoke(name);
            string entityType = entity.Properties.GetValueOrNull(Graph.Metadata.Constants.RDF.Type, true);

            if (string.IsNullOrWhiteSpace(entityType))
            {
                throw new BusinessException(string.Format(Common.Constants.Messages.Exception.MissingProperty, Graph.Metadata.Constants.RDF.Type));
            }

            if (!leafTypes.Any() || !leafTypes.Contains(entityType))
            {
                throw new BusinessException(Common.Constants.Messages.Exception.ForbiddenEntityType);
            }
        }
        #endregion

        #region Property Validation
        public IList<ValidationResultProperty> CheckForbiddenProperties<TEntity>(TEntity entity) where TEntity : Entity
        {
            string enitityType = entity.Properties.GetValueOrNull(Graph.Metadata.Constants.RDF.Type, true);
            var metadataProperties = _metadataService.GetMetadataForEntityType(enitityType);
            var metadataKeys = metadataProperties.Select(p => p.Properties.GetValueOrNull(Graph.Metadata.Constants.EnterpriseCore.PidUri, true) as string).ToList();

            metadataKeys.Add(Graph.Metadata.Constants.Resource.HasLaterVersion);

            IList<ValidationResultProperty> validationResults = new List<ValidationResultProperty>();

            // for each entity property it checks if the key is present in the metadata, otherwise a validation result is generated.
            foreach (var property in entity.Properties)
            {
                if (!metadataKeys.Contains(property.Key))
                {
                    validationResults.Add(new ValidationResultProperty(entity.Id, property.Key,null, "This property must not be stored because it was not defined by the ontology.", ValidationResultSeverity.Violation));
                }
            }

            return validationResults;
        }

        public bool CheckPropertyIsMandatory(string Property, IList<Graph.Metadata.DataModels.Metadata.MetadataProperty> metadata)
        {
            var curMetadata = metadata.Where(cond => cond.Key == Property).FirstOrDefault();
            if (curMetadata != null)
            {
                if (curMetadata.Properties.ContainsKey(COLID.Graph.Metadata.Constants.Shacl.MinCount) && int.Parse(curMetadata.Properties[COLID.Graph.Metadata.Constants.Shacl.MinCount]) > 0)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion
    }
}
