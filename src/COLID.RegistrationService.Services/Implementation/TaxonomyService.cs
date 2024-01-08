using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using COLID.Cache.Services;
using COLID.Exception.Models;
using COLID.Exception.Models.Business;
using COLID.Graph.Metadata.Constants;
using COLID.Graph.Metadata.Services;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.DataModels.Taxonomies;
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.TripleStore.Services;
using COLID.RegistrationService.Repositories.Interface;
using COLID.RegistrationService.Services.Interface;
using Microsoft.Extensions.Logging;

namespace COLID.RegistrationService.Services.Implementation
{
    public class TaxonomyService : BaseEntityService<Taxonomy, TaxonomyRequestDTO, TaxonomyResultDTO, BaseEntityResultCTO, ITaxonomyRepository>, ITaxonomyService
    {
        private readonly ITaxonomyRepository _taxonomyRepository;
        private readonly ICacheService _cacheService;
        private readonly IMetadataGraphConfigurationService _metadataGraphConfigurationService;

        private static readonly List<string> _searchIgnoredProperties = new List<string>()
        {
            RDF.Type,
            SKOS.Broader,
        };


        public TaxonomyService(
            IMapper mapper,
            IMetadataService metadataService,
            IValidationService validationService,
            ITaxonomyRepository taxonomyRepository,
            ILogger<TaxonomyService> logger,
            IMetadataGraphConfigurationService metadataGraphConfigurationService,
            ICacheService cacheService) : base(mapper, metadataService, validationService, taxonomyRepository, logger)
        {
            _taxonomyRepository = taxonomyRepository;
            _cacheService = cacheService;
            _metadataGraphConfigurationService = metadataGraphConfigurationService;
        }

        public override TaxonomyResultDTO GetEntity(string id)
        {
            var taxonomy = _cacheService.GetOrAdd($"id:{id}", () =>
            {
                var graphs = _metadataService.GetAllGraph();
                var taxonomies = _taxonomyRepository.GetTaxonomiesByIdentifier(id, graphs);
                var transformed = TransformTaxonomyListToHierarchy(taxonomies, id).FirstOrDefault();

                if (transformed == null)
                {
                    throw new EntityNotFoundException(Common.Constants.Messages.Taxonomy.NotFound, id);
                }

                return transformed;
            });

            return taxonomy;
        }

        public IList<TaxonomyResultDTO> GetTaxonomies(string taxonomyType)
        {
            var taxonomies = _cacheService.GetOrAdd($"type:{taxonomyType}", () =>
            {
                // This block fetches multiple graphs for a field type from metadata config
                // This can be dynamically extended for future taxonomies which need multiple graphs
                var taxonomyList = new List<Taxonomy>();
                var configurationGraphs = new HashSet<Uri>();
                var latestMetadataGraphConfiguration = _metadataGraphConfigurationService.GetLatestConfiguration();
                var graphList = latestMetadataGraphConfiguration.Properties.GetValueOrNull(taxonomyType, false);

                if (graphList.Count > 0)
                {
                    foreach (var graphitem in graphList)
                    {
                        configurationGraphs.Add(new Uri(graphitem));
                    }
                    taxonomyList = (List<Taxonomy>)_taxonomyRepository.BuildTaxonomy(taxonomyType, configurationGraphs);
                }
                else
                {
                    var graphs = _metadataService.GetMultiInstanceGraph(taxonomyType);
                    taxonomyList = (List<Taxonomy>)_taxonomyRepository.GetTaxonomies(taxonomyType, graphs);
                }
                return TransformTaxonomyListToHierarchy(taxonomyList);
            });
            return taxonomies;
        }

        public IList<TaxonomyResultDTO> GetTaxonomySearchHits(string taxonomyType, string searchTerm)
        {
            var taxonomies = _cacheService.GetOrAdd($"type:{taxonomyType}", () =>
            {
                // This block fetches multiple graphs for a field type from metadata config
                // This can be dynamically extended for future taxonomies which need multiple graphs
                var taxonomyList = new List<Taxonomy>();
                var configurationGraphs = new HashSet<Uri>();
                var latestMetadataGraphConfiguration = _metadataGraphConfigurationService.GetLatestConfiguration();
                var graphList = latestMetadataGraphConfiguration.Properties.GetValueOrNull(taxonomyType, false);

                if (graphList.Count > 0)
                {
                    foreach (var graphitem in graphList)
                    {
                        configurationGraphs.Add(new Uri(graphitem));
                    }
                    taxonomyList = (List<Taxonomy>)_taxonomyRepository.BuildTaxonomy(taxonomyType, configurationGraphs);
                }
                else
                {
                    var graphs = _metadataService.GetMultiInstanceGraph(taxonomyType);
                    taxonomyList = (List<Taxonomy>)_taxonomyRepository.GetTaxonomies(taxonomyType, graphs);
                }
                return TransformTaxonomyListToHierarchy(taxonomyList);
            });

            foreach (var taxonomy in taxonomies)
            {
                CheckIfTaxonomyContainsSearchText(taxonomy, searchTerm);
            }

            return taxonomies;
        }

        public static bool CheckIfTaxonomyContainsSearchText(TaxonomyResultDTO taxonomy, string searchTerm)
        {
            bool expandTaxonomy = false;
            foreach (var prop in taxonomy.Properties)
            {
                if (!_searchIgnoredProperties.Contains(prop.Key))
                {
                    if (prop.Value.Any(val => ((string)val).ToLower().Contains(searchTerm.ToLower(), StringComparison.Ordinal)))
                    {
                        taxonomy.FoundInSearch = true;
                        expandTaxonomy = true;
                    }
                }
            }

            if (taxonomy.HasChild)
            {
                foreach (var child in taxonomy.Children)
                {
                    if (CheckIfTaxonomyContainsSearchText(child, searchTerm))
                    {
                        expandTaxonomy = true;
                        taxonomy.Expanded = true;
                    }
                }
            }

            return expandTaxonomy;
        }

        public IList<TaxonomyLabel> GetTaxonomyLabels()
        {
            var taxonomies = _cacheService.GetOrAdd($"type:TaxonomyLabels", () =>
            {
                var latestGraphs = _metadataGraphConfigurationService.GetLatestConfiguration();

                latestGraphs.Properties.TryRemoveKey(COLID.Graph.Metadata.Constants.MetadataGraphConfiguration.Type);
                latestGraphs.Properties.TryRemoveKey(COLID.Graph.Metadata.Constants.MetadataGraphConfiguration.HasConsumerGroupGraph);
                latestGraphs.Properties.TryRemoveKey(COLID.Graph.Metadata.Constants.MetadataGraphConfiguration.HasExtendedUriTemplateGraph);
                latestGraphs.Properties.TryRemoveKey(COLID.Graph.Metadata.Constants.MetadataGraphConfiguration.HasCategoryFilterGraph);
                latestGraphs.Properties.TryRemoveKey(COLID.Graph.Metadata.Constants.MetadataGraphConfiguration.HasPidUriTemplatesGraph);
                latestGraphs.Properties.TryRemoveKey(COLID.Graph.Metadata.Constants.MetadataGraphConfiguration.HasResourcesGraph);
                latestGraphs.Properties.TryRemoveKey(COLID.Graph.Metadata.Constants.MetadataGraphConfiguration.HasResourcesDraftGraph);
                latestGraphs.Properties.TryRemoveKey(COLID.Graph.Metadata.Constants.MetadataGraphConfiguration.HasLinkHistoryGraph);
                latestGraphs.Properties.TryRemoveKey(COLID.Graph.Metadata.Constants.MetadataGraphConfiguration.HasResourceHistoryGraph);
                latestGraphs.Properties.TryRemoveKey(EnterpriseCore.HasStartDateTime);
                
                ISet<Uri> graphs = new HashSet<Uri>();
                foreach (var typ in latestGraphs.Properties)
                {
                    foreach(string grph in typ.Value)
                    {
                        if (Uri.TryCreate(grph, UriKind.Absolute, out Uri graphUri))
                        {
                            graphs.Add(graphUri);
                        }
                    }
                }

                var taxonomyList = _taxonomyRepository.GetTaxonomyLabels(graphs);
                return taxonomyList;
            });
            return taxonomies;
        }

        public IList<TaxonomyResultDTO> GetTaxonomiesAsPlainList(string taxonomyType)
        {
            var plainTaxonomies = _cacheService.GetOrAdd($"list:type:{taxonomyType}", () =>
            {
                var taxonomies = new List<Taxonomy>();
                var configurationGraphs = new HashSet<Uri>();

                // This block fetches multiple graphs for a field type from metadata config
                // This can be dynamically extended for future taxonomies which need multiple graphs
                var latestMetadataGraphConfiguration = _metadataGraphConfigurationService.GetLatestConfiguration();
                var graphList = latestMetadataGraphConfiguration.Properties.GetValueOrNull(taxonomyType, false);
                
                if (graphList.Count > 0)
                {
                    foreach (var graphitem in graphList)
                    {
                        configurationGraphs.Add(new Uri(graphitem));
                    }
                    taxonomies = (List<Taxonomy>)_taxonomyRepository.BuildTaxonomy(taxonomyType, configurationGraphs);
                }
                else
                {
                    var graphs = _metadataService.GetMultiInstanceGraph(taxonomyType);
                    taxonomies = (List<Taxonomy>)_taxonomyRepository.GetTaxonomies(taxonomyType, graphs);
                }
                return CreateHierarchicalStructureFromTaxonomyList(taxonomies);
            });

            return plainTaxonomies;
        }

        /// <summary>
        /// Transforms a given taxonomy list to a hierarchical structure and returns the top parents and their children.
        /// If a TopTaxonomyIdentifier is given, this taxonomy and its children are returned.
        /// </summary>
        /// <param name="taxonomyList">Plain list of all taxonomies without any hierarchy</param>
        /// <param name="topTaxonomyIdentifier">Identifier of the taxonomy item, that should get returned</param>
        /// <returns>Returns the matching taxonomy and its children, if topTaxonomyIdentifier is set. Otherwise, returns whole taxonomy structure.</returns>
        private IList<TaxonomyResultDTO> TransformTaxonomyListToHierarchy(IEnumerable<Taxonomy> taxonomyList, string topTaxonomyIdentifier = null)
        {
            var taxonomyHierarchy = CreateHierarchicalStructureFromTaxonomyList(taxonomyList);

            if (!string.IsNullOrWhiteSpace(topTaxonomyIdentifier))
            {
                return taxonomyHierarchy.Where(t => t.Id == topTaxonomyIdentifier).OrderBy(t => t.Properties.GetValueOrNull(RDFS.Label, true)).ToList();
            }

            return taxonomyHierarchy.Where(t => !t.HasParent).OrderBy(t => t.Properties.GetValueOrNull(RDFS.Label, true)).ToList();
        }

        /// <summary>
        /// Creates a hierarchical structure of the given plain list of taxonomies by adding object references of children to their parent taxonomies.
        /// All children, which have been added to the parents, remain in the main list.
        /// </summary>
        /// <param name="taxonomyList">Plain list of all taxonomies without any hierarchy</param>
        /// <returns>A list of all taxonomies with a hierarchical structure of child taxonomies</returns>
        private IList<TaxonomyResultDTO> CreateHierarchicalStructureFromTaxonomyList(IEnumerable<Taxonomy> taxonomyList)
        {
            var taxonomies = taxonomyList.ToDictionary(t => t.Id, t => _mapper.Map<TaxonomyResultDTO>(t));

            foreach (var taxonomy in taxonomies)
            {
                var child = taxonomy.Value;

                if (child.Properties.TryGetValue(SKOS.Broader, out var parents))
                {
                    foreach (var parent in parents)
                    {
                        if (taxonomies.TryGetValue(parent, out TaxonomyResultDTO parentTaxonomy))
                        {
                            parentTaxonomy.Children.Add(child);
                        }
                        // commented as the returned ordered list is not used anywehere
                        //parentTaxonomy.Children.OrderBy(t => t.Properties.GetValueOrNull(RDFS.Label, true));
                    }
                }
            }

            return taxonomies.Values.ToList();
        }
    }
}
