using System.Collections.Generic;
using AutoMapper;
using COLID.Cache.Extensions;
using COLID.Cache.Services;
using COLID.Common.Extensions;
using COLID.Exception.Models;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.Metadata.Services;
using COLID.Graph.TripleStore.DataModels.Attributes;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.TripleStore.Services;
using COLID.RegistrationService.Common.DataModel.Keywords;
using COLID.RegistrationService.Repositories.Interface;
using COLID.RegistrationService.Services.Extensions;
using COLID.RegistrationService.Services.Interface;
using Microsoft.Extensions.Logging;

namespace COLID.RegistrationService.Services.Implementation
{
    internal class KeywordService : BaseEntityService<Keyword, KeywordRequestDTO, KeywordResultDTO, KeywordWriteResultCTO, IKeywordRepository>, IKeywordService
    {
        private readonly ICacheService _cacheService;

        public KeywordService(
            IMapper mapper,
            ILogger<KeywordService> logger,
            IKeywordRepository keywordRepository,
            IMetadataService metadataService,
            IValidationService validationService,
            ICacheService cacheService) : base(mapper, metadataService, validationService, keywordRepository, logger)
        {
            _cacheService = cacheService;
        }

        public override void DeleteEntity(string id)
        {
            base.DeleteEntity(id);
            _cacheService.DeleteRelatedCacheEntries<KeywordService, Keyword>(id);
        }

        public override IList<KeywordResultDTO> GetEntities(EntitySearch search)
        {
            var cacheKey = search == null ? Type : search.CalculateHash();
            return _cacheService.GetOrAdd($"entities:{cacheKey}", () => base.GetEntities(search));
        }

        public override KeywordResultDTO GetEntity(string id)
        {
            return _cacheService.GetOrAdd($"id:{id}", () => base.GetEntity(id));
        }

        /// <summary>
        /// Create a new keyword, identified by the given label.
        /// <para><b>NOTE:</b>a validation will not be done.</para>
        /// </summary>
        /// <param name="label">the keyword label</param>
        /// <returns>The Keywords Id</returns>
        public string CreateKeyword(string label)
        {
            var keywordRequest = new KeywordRequestDTO();
            keywordRequest.Properties.Add(Graph.Metadata.Constants.RDF.Type, new List<dynamic>() { Graph.Metadata.Constants.Keyword.Type });
            keywordRequest.Properties.Add(Graph.Metadata.Constants.RDFS.Label, new List<dynamic>() { label });

            var keyword = _mapper.Map<Keyword>(keywordRequest);

            // Get the metadata to create the keyword
            var metadata = _metadataService.GetMetadataForEntityType(Graph.Metadata.Constants.Keyword.Type);

            _repository.CreateEntity(keyword, metadata);
            _cacheService.DeleteRelatedCacheEntries<KeywordService, Keyword>();

            return keyword.Id;
        }

        protected override IList<ValidationResultProperty> CustomValidation(Keyword keyword, Keyword repoKeyword, IList<MetadataProperty> metadataProperties)
        {
            string label = keyword.Properties.GetValueOrNull(Graph.Metadata.Constants.RDFS.Label, true);

            if (_repository.CheckIfKeywordLabelExists(label, out string id))
            {
                throw new BusinessException("A keyword with this label already exists.");
            }

            return new List<ValidationResultProperty>();
        }

        /// <summary>
        /// Checks if a keyword with a certain label exists.
        /// </summary>
        /// <param name="label">Label of the keyword to be checked</param>
        /// <param name="id">The Id of the keyword, if it exists</param>
        /// <returns>Returns a boolean value for whether a keyword exists.</returns>
        public bool CheckIfKeywordExists(string label, out string id)
        {
            return _repository.CheckIfKeywordLabelExists(label, out id);
        }
    }
}
