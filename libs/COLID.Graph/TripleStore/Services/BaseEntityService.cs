using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using COLID.Common.Extensions;
using COLID.Exception.Models.Business;
using COLID.Graph.Metadata.Constants;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.Metadata.Exceptions;
using COLID.Graph.Metadata.Services;
using COLID.Graph.TripleStore.DataModels.Attributes;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.TripleStore.Repositories;
using Microsoft.Extensions.Logging;
using Entity = COLID.Graph.TripleStore.DataModels.Base.Entity;

namespace COLID.Graph.TripleStore.Services
{
    public abstract class BaseEntityService<TEntity, TEntityRequest, TEntityResult, TEntityWriteResult, TRepository> : IBaseEntityService<TEntity, TEntityRequest, TEntityResult, TEntityWriteResult, TRepository>
        where TEntity : Entity, new()
        where TEntityRequest : BaseEntityRequestDTO
        where TEntityResult : BaseEntityResultDTO
        where TEntityWriteResult : BaseEntityResultCTO, new()
        where TRepository : IBaseRepository<TEntity>
    {
        protected IMapper _mapper;
        protected IMetadataService _metadataService;
        protected IValidationService _validationService;
        protected TRepository _repository;
        protected ILogger _logger;

        protected string Type => typeof(TEntity).GetAttributeValue((TypeAttribute type) => type.Type);

        protected BaseEntityService(IMapper mapper, IMetadataService metadataService, IValidationService validationService, TRepository repository, ILogger logger)
        {
            _mapper = mapper;
            _metadataService = metadataService;
            _validationService = validationService;
            _repository = repository;
            _logger = logger;
        }

        public virtual async Task<TEntityWriteResult> CreateEntity(TEntityRequest baseEntityRequest)
        {
            var entity = _mapper.Map<TEntity>(baseEntityRequest);

            // Check whether the correct entity type is specified.
            _validationService.CheckType<TEntity>(entity);

            var metadataProperties = _metadataService.GetMetadataForEntityType(Type);
            var validationResult = await ValidateEntity(entity, metadataProperties);
            var entityResult = _mapper.Map<TEntityResult>(entity);

            if (!validationResult.Conforms && validationResult.Severity != ValidationResultSeverity.Info)
            {
                throw new EntityValidationException(validationResult, entityResult);
            }

            _repository.CreateEntity(entity, metadataProperties, GetInstanceGraph());

            return new TEntityWriteResult() { Entity = entityResult, ValidationResult = validationResult };
        }

        public virtual void DeleteEntity(string id)
        {
            CheckIfEntityExists(id);
            _repository.DeleteEntity(id, GetInstanceGraph());
        }

        private async Task<ValidationResult> ValidateEntity(TEntity entity, IList<MetadataProperty> metadataProperties, TEntity repoEntity = null)
        {
            var validationTask = _validationService.ValidateEntity(entity, metadataProperties).ConfigureAwait(true);

            var customValidationResults = CustomValidation(entity, repoEntity, metadataProperties);

            // Check whether forbidden properties are contained in the entity.
            var forbiddenPropertiesValidationResults = _validationService.CheckForbiddenProperties(entity);

            var validationResults = await validationTask;

            validationResults.Results = validationResults.Results
                .Concat(customValidationResults)
                .Concat(forbiddenPropertiesValidationResults)
                .ToList();

            return validationResults;
        }

        protected virtual IList<ValidationResultProperty> CustomValidation(TEntity entity, TEntity repoEntity, IList<MetadataProperty> metadataProperties)
        {
            return new List<ValidationResultProperty>();
        }

        public virtual TEntityWriteResult EditEntity(string identifier, TEntityRequest baseEntityRequest)
        {
            var repoEntity = _repository.GetEntityById(identifier, GetAllInstanceGraphs());

            var entity = _mapper.Map<TEntity>(baseEntityRequest);
            entity.Id = identifier;

            // Check whether the correct entity type is specified.
            _validationService.CheckType(entity);

            var metadataProperties = _metadataService.GetMetadataForEntityType(Type);
            var validationResult = ValidateEntity(entity, metadataProperties, repoEntity).GetAwaiter().GetResult();
            var entityResult = _mapper.Map<TEntityResult>(entity);

            if (!validationResult.Conforms && validationResult.Severity != ValidationResultSeverity.Info)
            {
                throw new EntityValidationException(validationResult, entityResult);
            }

            _repository.UpdateEntity(entity, metadataProperties, GetInstanceGraph());

            return new TEntityWriteResult { Entity = entityResult, ValidationResult = validationResult };
        }

        public virtual IList<TEntityResult> GetEntities(EntitySearch search)
        {
            var type = string.IsNullOrWhiteSpace(search?.Type) ? Type : search.Type;
            var types = _metadataService.GetLeafEntityTypes(type);
            // TODO: Fix graphs
            var entities = _repository.GetEntities(search, types, GetAllInstanceGraphs());

            return entities
                    .Where(c => c.Id.IsValidBaseUri())
                    .Select(c => _mapper.Map<TEntityResult>(c))
                    .OrderBy(c => c.Name).ToList();
        }

        public virtual TEntityResult GetEntity(string id)
        {
            var entity = _repository.GetEntityById(id, GetAllInstanceGraphs());
            return _mapper.Map<TEntityResult>(entity);
        }

        public virtual void CheckIfEntityExists(string id)
        {
            var types = _metadataService.GetLeafEntityTypes(Type);

            if (!_repository.CheckIfEntityExists(id, types, GetAllInstanceGraphs()))
            {
                throw new EntityNotFoundException(Metadata.Constants.Messages.Entity.NotFound, id);
            }
        }

        protected Uri GetInstanceGraph()
        {
            string name = typeof(TEntity).GetAttributeValue((TypeAttribute type) => type.Type);
            var instanceGraph = _metadataService.GetInstanceGraph(name);
            return instanceGraph;
        }

        private ISet<Uri> GetAllInstanceGraphs()
        {
            return _metadataService.GetAllGraph();
        }

        public bool CheckIfPropertyValueExists(Uri predicate, string obj, string entityType, out string id)
        {
            var entityInstanceGraph = _metadataService.GetInstanceGraph(entityType);
            return _repository.CheckIfPropertyValueExists(predicate, obj, entityType, entityInstanceGraph, out id);
        }
    }
}
