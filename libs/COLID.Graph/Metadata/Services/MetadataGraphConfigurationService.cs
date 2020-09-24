using System;
using System.Collections.Generic;
using System.Linq;
using COLID.Common.Extensions;
using System.Threading.Tasks;
using COLID.Cache.Services;
using Microsoft.Extensions.Logging;
using COLID.Graph.TripleStore.Services;
using COLID.Graph.Metadata.DataModels.MetadataGraphConfiguration;
using COLID.Graph.Metadata.Repositories;
using COLID.Graph.TripleStore.Repositories;
using AutoMapper;
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.Metadata.DataModels.Metadata;

namespace COLID.Graph.Metadata.Services
{
    internal class MetadataGraphConfigurationService :
        BaseEntityService<MetadataGraphConfiguration, MetadataGraphConfigurationRequestDTO, MetadataGraphConfigurationResultDTO, MetadataGraphConfigurationWriteResultCTO, IMetadataGraphConfigurationRepository>,
        IMetadataGraphConfigurationService
    {
        private readonly IMetadataGraphConfigurationRepository _metadataGraphConfigurationRepository;
        private readonly IGraphRepository _graphRepository;
        private readonly ICacheService _cacheService;

        public MetadataGraphConfigurationService(
            IMapper mapper,
            IMetadataService metadataService,
            IValidationService validationService,
            IMetadataGraphConfigurationRepository metadataGraphConfigurationRepository,
            IGraphRepository graphRepository,
            ILogger<MetadataGraphConfigurationService> logger, 
            ICacheService cacheService) : base(mapper, metadataService, validationService, metadataGraphConfigurationRepository, logger)
        {
            _metadataGraphConfigurationRepository = metadataGraphConfigurationRepository;
            _graphRepository = graphRepository;
            _cacheService = cacheService;
        }

        public override MetadataGraphConfigurationResultDTO GetEntity(string id)
        {
            return _cacheService.GetOrAdd($"id:{id}", () => base.GetEntity(id));
        }

        public IList<MetadataGraphConfigurationOverviewDTO> GetConfigurationOverview()
        {
            var historicOverviewList = _metadataGraphConfigurationRepository.GetConfigurationOverview(); // TODO: check caching inside repo because other classes use this too
            return historicOverviewList;
        }

        public MetadataGraphConfigurationResultDTO GetLatestConfiguration()
        {
            var latestMetadataGraphConfiguration = _metadataGraphConfigurationRepository.GetLatestConfiguration();
            return _mapper.Map<MetadataGraphConfigurationResultDTO>(latestMetadataGraphConfiguration);
        }

        public override async Task<MetadataGraphConfigurationWriteResultCTO> CreateEntity(MetadataGraphConfigurationRequestDTO metadataGraphConfiguration)
        {
            metadataGraphConfiguration.Properties.TryRemoveKey(Constants.EnterpriseCore.HasStartDateTime);
            metadataGraphConfiguration.Properties.AddOrUpdate(Constants.EnterpriseCore.HasStartDateTime, new List<dynamic>() { DateTime.UtcNow.ToString("o") });
            var entity = await base.CreateEntity(metadataGraphConfiguration);
            _logger.LogInformation("MetadataGraphConfiguration created and reindexing triggered.");
            return entity;
        }

        protected override IList<ValidationResultProperty> CustomValidation(MetadataGraphConfiguration metadataGraphConfiguration, MetadataGraphConfiguration repoMetadataGraphConfiguration,  IList<MetadataProperty> metadataProperties)
        {
            var validationResults = new List<ValidationResultProperty>();

            validationResults.AddRange(CheckGraphsExistenceForType(metadataGraphConfiguration, Constants.MetadataGraphConfiguration.HasConsumerGroupGraph));
            validationResults.AddRange(CheckGraphsExistenceForType(metadataGraphConfiguration, Constants.MetadataGraphConfiguration.HasECOGraph));
            validationResults.AddRange(CheckGraphsExistenceForType(metadataGraphConfiguration, Constants.MetadataGraphConfiguration.HasExtendedUriTemplateGraph));
            validationResults.AddRange(CheckGraphsExistenceForType(metadataGraphConfiguration, Constants.MetadataGraphConfiguration.HasKeywordsGraph));
            validationResults.AddRange(CheckGraphsExistenceForType(metadataGraphConfiguration, Constants.MetadataGraphConfiguration.HasMetadataGraph));
            validationResults.AddRange(CheckGraphsExistenceForType(metadataGraphConfiguration, Constants.MetadataGraphConfiguration.HasPidUriTemplatesGraph));
            validationResults.AddRange(CheckGraphsExistenceForType(metadataGraphConfiguration, Constants.MetadataGraphConfiguration.HasResourcesGraph));
            validationResults.AddRange(CheckGraphsExistenceForType(metadataGraphConfiguration, Constants.MetadataGraphConfiguration.HasResourceHistoryGraph));
            validationResults.AddRange(CheckGraphsExistenceForType(metadataGraphConfiguration, Constants.MetadataGraphConfiguration.HasShaclConstraintsGraph));

            return validationResults;
        }

        private IList<ValidationResultProperty> CheckGraphsExistenceForType(MetadataGraphConfiguration metadataGraphConfiguration, string type)
        {
            var validationResults = new List<ValidationResultProperty>();

            if (metadataGraphConfiguration.Properties.TryGetValue(type, out List<dynamic> graphs))
            {
                // TODO: As soon as the validation errors can be added to the contents of multiple selection fields in the frontend, individual validation errors must be generated for each graph instead of one result.
                var missingGraphsList = graphs.Where(g => Uri.TryCreate(g, UriKind.Absolute, out Uri graphUri) && !_graphRepository.CheckIfNamedGraphExists(graphUri));

                if (!missingGraphsList.IsNullOrEmpty())
                {
                    var missingGraphs = string.Join(" , ", missingGraphsList);
                    validationResults.Add(new ValidationResultProperty(metadataGraphConfiguration.Id, type, missingGraphs, $"The following graph names do not exist in the database: {missingGraphs}", ValidationResultSeverity.Violation));
                }
            }

            return validationResults;
        }
    }
}
