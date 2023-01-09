using System;
using System.Collections.Generic;
using System.Linq;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.Metadata.Constants;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.Repositories;
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.TripleStore.Repositories;
using COLID.Graph.TripleStore.Transactions;
using COLID.Graph.TripleStore.DataModels.Taxonomies;
using COLID.RegistrationService.Repositories.Interface;
using Microsoft.Extensions.Logging;
using VDS.RDF.Query;
using Microsoft.Extensions.Configuration;

namespace COLID.RegistrationService.Repositories.Implementation
{
    internal class TaxonomyRepository : BaseRepository<Taxonomy>, ITaxonomyRepository
    {
        public TaxonomyRepository(
            IConfiguration configuration,
            ITripleStoreRepository tripleStoreRepository,
            ILogger<TaxonomyRepository> logger) : base(configuration, tripleStoreRepository, logger)
        {
        }

        public IList<Taxonomy> GetTaxonomiesByIdentifier(string identifier, ISet<Uri> metadataNamedGraphs)
        {
            CheckArgumentForValidUri(identifier);

            var parameterizedString = new SparqlParameterizedString();
            parameterizedString.CommandText =
                @"SELECT ?subject ?predicate ?object
                  @fromMetadataNamedGraph
                  WHERE {
                      @subject rdf:type ?type.
                      ?subject @broader* @subject.
                      ?subject rdf:type ?type.
                      ?subject ?predicate ?object
                  }";

            parameterizedString.SetPlainLiteral("fromMetadataNamedGraph", metadataNamedGraphs.JoinAsFromNamedGraphs());

            parameterizedString.SetUri("subject", new Uri(identifier));
            parameterizedString.SetUri("broader", new Uri(Graph.Metadata.Constants.SKOS.Broader));

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            var taxonomies = TransformQueryResults(results);

            if (!taxonomies.Any())
            {
                return new List<Taxonomy>();
            }

            return taxonomies;
        }
        /// <summary>
        /// To get all taxonomy to resolve labels in excel export
        /// </summary>
        /// <param name="metadataNamedGraphs"></param>
        /// <returns></returns>
        public IList<Taxonomy> GetTaxonomies(ISet<Uri> metadataNamedGraphs)
        {
            var parameterizedString = new SparqlParameterizedString();
            parameterizedString.CommandText =
                @"SELECT ?subject ?predicate ?object
                  @fromMetadataNamedGraph
                  WHERE {
                        ?subject rdf:type ?type.
                        ?subject @broader* ?subject.
                        ?subject rdf:type ?type.
                        ?subject ?predicate ?object
                        FILTER(lang(str(?type)) IN (@language , """"))
                  }";

            parameterizedString.SetPlainLiteral("fromMetadataNamedGraph", metadataNamedGraphs.JoinAsFromNamedGraphs());

            parameterizedString.SetUri("broader", new Uri(Graph.Metadata.Constants.SKOS.Broader));

            parameterizedString.SetLiteral("language", COLID.Graph.Metadata.Constants.I18n.DefaultLanguage);

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            var taxonomies = TransformQueryResults(results);

            if (!taxonomies.Any())
            {
                return new List<Taxonomy>();
            }

            return taxonomies;
        }
        public IList<Taxonomy> GetTaxonomies(string type, ISet<Uri> metadataNamedGraphs)
        {
            CheckArgumentForValidUri(type);

            var parameterizedString = new SparqlParameterizedString();
            parameterizedString.CommandText =
                @"SELECT ?subject ?predicate ?object
                  @fromMetadataNamedGraph
                  WHERE {
                      ?subject rdf:type @type.
                      ?subject ?predicate ?object
                  }";

            parameterizedString.SetPlainLiteral("fromMetadataNamedGraph", metadataNamedGraphs.JoinAsFromNamedGraphs());

            parameterizedString.SetUri("type", new Uri(type));

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            var taxonomies = TransformQueryResults(results);

            if (!taxonomies.Any())
            {
                return new List<Taxonomy>();
            }

            //Handle null Name for Keywords
            //We are comparing keywords from requested type
            if(type == Graph.Metadata.Constants.Keyword.Type)
            {
                //we are checking  Label key in property filed
                return taxonomies.Where(x => x.Properties.ContainsKey(Graph.Metadata.Constants.RDFS.Label)).ToList<Taxonomy>();
            }

            return taxonomies;
        }

        public IList<Taxonomy> BuildTaxonomy(string type, ISet<Uri> metadataNamedGraphs)
        {
            var parameterizedString = new SparqlParameterizedString();
            parameterizedString.CommandText =
            @"CONSTRUCT{
              ?subject @broader ?parent.
              ?subject @prefLabel ?subjectLabel.
              ?subject rdf:type @type.
              ?topConcept rdf:type @type.
              ?topConcept @prefLabel ?label.
                }
              @fromMetadataNamedGraph
              WHERE {
               {
                OPTIONAL {?s @topConcept ?topConcept}.
  				?topConcept @prefLabel ?label.
                ?subject @broader+ ?topConcept ;
                 @broader ?parent;
                 @prefLabel ?subjectLabel.
                ?parent  @prefLabel ?parentLabel.
                 FILTER (lang(?label) IN (@language , """"))  
                 FILTER (lang(?subjectLabel) IN (@language , """"))  
                }}ORDER BY ?parent ?subjectLabel";

            parameterizedString.SetPlainLiteral("fromMetadataNamedGraph", metadataNamedGraphs.JoinAsFromNamedGraphs());

            parameterizedString.SetUri("broader", new Uri(Graph.Metadata.Constants.SKOS.Broader));
            
            parameterizedString.SetUri("prefLabel", new Uri(Graph.Metadata.Constants.SKOS.PrefLabel));

            parameterizedString.SetUri("type", new Uri(type));

            parameterizedString.SetUri("topConcept", new Uri(Graph.Metadata.Constants.SKOS.TopConcept));

            parameterizedString.SetLiteral("language", COLID.Graph.Metadata.Constants.I18n.DefaultLanguage);

            var results = _tripleStoreRepository.QueryTripleStoreGraphResult(parameterizedString);
            if (results.IsEmpty)
            {
                return new List<Taxonomy>();
            }
            var subjects = results.Triples.SubjectNodes;
            
            var taxonomies = new List<Taxonomy>();

            foreach (var item in subjects)
            {
                var groupSubjects = results.GetTriplesWithSubject(item);

                var subGroupedResults = groupSubjects.GroupBy(
                                p => p.Predicate,p=>p.Object)
                                .ToDictionary(g => g.Key, g => g.ToList());

                Taxonomy newEntity = new Taxonomy
                {
                    Id = item.ToString(),
                    Properties = subGroupedResults.ToDictionary(x => x.Key.ToString(),
                                x =>
                                {
                                    if (x.Key.ToString() == Graph.Metadata.Constants.SKOS.PrefLabel)
                                    {
                                        return x.Value.Select(x => ((VDS.RDF.BaseLiteralNode)x).Value).ToList<dynamic>();

                                    }
                                    else
                                    {
                                        return x.Value.Select(y=>((VDS.RDF.BaseUriNode)y).Uri.AbsoluteUri).ToList<dynamic>();
                                    }
                                }
                                )
                };
                taxonomies.Add(newEntity);
            }

            return taxonomies;
        }

        public override IList<Taxonomy> GetEntities(EntitySearch entitySearch, IList<string> types, ISet<Uri> namedGraphs)
        {
            throw new NotImplementedException();
        }

        public override Taxonomy GetEntityById(string id, ISet<Uri> namedGraphs)
        {
            throw new NotImplementedException();
        }

        public override void CreateEntity(Taxonomy newEntity, IList<MetadataProperty> metadataProperty, Uri namedGraph)
        {
            throw new NotImplementedException();
        }

        public override void UpdateEntity(Taxonomy entity, IList<MetadataProperty> metadataProperties, Uri namedGraph)
        {
            throw new NotImplementedException();
        }

        public override void DeleteEntity(string id, Uri namedGraph)
        {
            throw new NotImplementedException();
        }

        public override ITripleStoreTransaction CreateTransaction()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get all taxonomy labels for excel export
        /// </summary>
        /// <param name="metadataNamedGraphs"></param>
        /// <returns></returns>
        public IList<TaxonomyLabel> GetTaxonomyLabels(ISet<Uri> metadataNamedGraphs)
        {
            var parameterizedString = new SparqlParameterizedString();
            parameterizedString.CommandText =
                @"SELECT ?subject ?label
                  @fromMetadataNamedGraph
                  WHERE {
                        ?subject rdfs:label | @prefLabel ?label.
                  }";

            parameterizedString.SetPlainLiteral("fromMetadataNamedGraph", metadataNamedGraphs.JoinAsFromNamedGraphs());
            parameterizedString.SetUri("prefLabel", new Uri(COLID.Graph.Metadata.Constants.SKOS.PrefLabel));

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);
            var taxonomyLabels = new List<TaxonomyLabel>();
            if (!results.IsEmpty)
            {
                taxonomyLabels = results.Select(rslt =>
                {
                    return new TaxonomyLabel
                    {
                        Id = rslt.GetNodeValuesFromSparqlResult("subject").Value,
                        Label = rslt.GetNodeValuesFromSparqlResult("label").Value
                    };
                }).ToList();
            }

            return taxonomyLabels;
        }
    }
}
