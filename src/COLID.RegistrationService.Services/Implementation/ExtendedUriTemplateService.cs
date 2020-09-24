using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using COLID.Cache.Extensions;
using COLID.Cache.Services;
using COLID.Common.Extensions;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.Metadata.Services;
using COLID.Graph.TripleStore.DataModels.Attributes;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.TripleStore.Services;
using COLID.RegistrationService.Common.DataModel.ExtendedUriTemplates;
using COLID.RegistrationService.Repositories.Interface;
using COLID.RegistrationService.Services.Extensions;
using COLID.RegistrationService.Services.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ExtendedUriTemplate = COLID.RegistrationService.Common.DataModel.ExtendedUriTemplates.ExtendedUriTemplate;

namespace COLID.RegistrationService.Services.Implementation
{
    internal class ExtendedUriTemplateService : BaseEntityService<ExtendedUriTemplate, ExtendedUriTemplateRequestDTO, ExtendedUriTemplateResultDTO, ExtendedUriTemplateWriteResultCTO, IExtendedUriTemplateRepository>, IExtendedUriTemplateService
    {
        private readonly string _colidDomain;
        private readonly ICacheService _cacheService;

        public ExtendedUriTemplateService(
            IConfiguration configuration,
            IMapper mapper,
            ILogger<ExtendedUriTemplateService> logger,
            IExtendedUriTemplateRepository extendedUriTemplateRepository,
            IMetadataService metadataService,
            IValidationService validationService,
            ICacheService cacheService) : base(mapper, metadataService, validationService, extendedUriTemplateRepository, logger)
        {
            _colidDomain = configuration.GetConnectionString("colidDomain");
            _cacheService = cacheService;
        }

        public override ExtendedUriTemplateWriteResultCTO EditEntity(string identifier, ExtendedUriTemplateRequestDTO baseEntityRequest)
        {
            var updatedEntity = base.EditEntity(identifier, baseEntityRequest);
            _cacheService.DeleteRelatedCacheEntries<ExtendedUriTemplateService, ExtendedUriTemplate>(identifier);

            return updatedEntity;
        }

        public override void DeleteEntity(string id)
        {
            base.DeleteEntity(id);
            _cacheService.DeleteRelatedCacheEntries<ExtendedUriTemplateService, ExtendedUriTemplate>(id);
        }

        protected override IList<ValidationResultProperty> CustomValidation(ExtendedUriTemplate extendedUriTemplate, ExtendedUriTemplate repoExtendedUriTemplate, IList<MetadataProperty> metadataProperties)
        {
            var validationResults = new List<ValidationResultProperty>();

            var orders = _cacheService.GetOrAdd("extended-uri-template-order", () => _repository.GetExtendedUriTemplateOrders());

            // This is the only way to iterrate, since the values are not changed. Otherwise use the same function as in preprocessservice.
            foreach (var property in extendedUriTemplate.Properties)
            {
                switch (property.Key)
                {
                    case Common.Constants.ExtendedUriTemplate.HasPidUriSearchRegex:
                        foreach (var prop in property.Value)
                        {
                            var prefix = $"^https://{_colidDomain}";
                            string valueString = prop;
                            if (!valueString.StartsWith(prefix))
                            {
                                validationResults.Add(new ValidationResultProperty(extendedUriTemplate.Id, property.Key, valueString, $"The regex has to start with prefix {prefix}", ValidationResultSeverity.Violation));
                            }
                        }
                        break;

                    case Common.Constants.ExtendedUriTemplate.HasOrder:
                        foreach (var propValue in property.Value)
                        {
                            if (orders.TryGetValue(propValue, out string id) && id != extendedUriTemplate.Id)
                            {
                                orders.TryRemoveKey(extendedUriTemplate.Id);

                                var message = $"The number of order corresponds to an order of another template. The following numbers are already in use: {string.Join(" , ", orders.Keys)}";
                                validationResults.Add(new ValidationResultProperty(extendedUriTemplate.Id, property.Key, propValue, message, ValidationResultSeverity.Violation));
                            }
                        }
                        break;
                }
            }

            return validationResults;
        }

        public override IList<ExtendedUriTemplateResultDTO> GetEntities(EntitySearch search)
        {
            var cacheKey = search == null ? Type : search.CalculateHash();
            var extendedUriTemplateList =
                _cacheService.GetOrAdd($"entities:{cacheKey}", () =>
                {
                    var extendedUriTemplate = base.GetEntities(search);
                    return extendedUriTemplate
                        .OrderBy(x => x.Properties.GetValueOrNull(Common.Constants.ExtendedUriTemplate.HasOrder, true))
                        .ToList();
                });

            return extendedUriTemplateList;
        }

        public override ExtendedUriTemplateResultDTO GetEntity(string id)
        {
            return _cacheService.GetOrAdd($"id:{id}", () => base.GetEntity(id));
        }
    }
}
