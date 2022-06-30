using System;
using System.Collections.Generic;
using System.Linq;
using COLID.Common.Extensions;
using COLID.Exception.Models.Business;
using COLID.Graph.Metadata.Constants;
using COLID.Graph.Metadata.Services;
using COLID.RegistrationService.Common.DataModel.Validation;
using COLID.RegistrationService.Repositories.Interface;
using COLID.RegistrationService.Services.Interface;
using COLID.Graph.Metadata.Extensions;
using Entity = COLID.Graph.TripleStore.DataModels.Base.Entity;
using Microsoft.Extensions.Logging;
using COLID.RegistrationService.Common.DataModel.Identifier;
using System.Threading.Tasks;

namespace COLID.RegistrationService.Services.Implementation
{
    internal class IdentifierService : IIdentifierService
    {
        private readonly IIdentifierRepository _identifierRepository;
        private readonly IMetadataService _metadataService;
        private readonly ILogger<IdentifierService> _logger;
        public IdentifierService(IIdentifierRepository identifierRepository, IMetadataService metadataService, ILogger<IdentifierService> logger)
        {
            _identifierRepository = identifierRepository;
            _metadataService = metadataService;
            _logger = logger;
        }

        public IList<string> GetOrphanedIdentifiersList()
        {
            return _identifierRepository.GetOrphanedIdentifiersList( GetInstanceGraph(), GetResourceDraftInstanceGraph(), GetHistoricInstanceGraph());
        }

        public void DeleteOrphanedIdentifier(string identifierUri, bool checkInOrphanedList = true)
        {
            if (Uri.TryCreate(identifierUri, UriKind.Absolute, out Uri uri))
            {
                _identifierRepository.DeleteOrphanedIdentifier(uri, GetInstanceGraph(), GetResourceDraftInstanceGraph(), GetHistoricInstanceGraph(), checkInOrphanedList);
            }
            else
            {
                throw new InvalidFormatException(Graph.Metadata.Constants.Messages.Identifier.IncorrectIdentifierFormat,
                    identifierUri);
            }
        }

        public async Task<List<OrphanResultDto>> DeleteOrphanedIdentifierList(List<string> identifierUris)
        {
            CheckDeletionidentityCount(identifierUris);
            var orphanedDeletionFailedUris = new List<OrphanResultDto>();
            var getOrphanedList = this.GetOrphanedIdentifiersList();
            foreach (var identifierUri in identifierUris)
            {
                try
                {
                    if (getOrphanedList.Contains(identifierUri)){
                        DeleteOrphanedIdentifier(identifierUri,false);
                    }
                }
                catch (System.Exception ex)
                {
                    var failedDelete = new OrphanResultDto(identifierUri, ex.Message, false);
                    orphanedDeletionFailedUris.Add(failedDelete);
                    _logger.LogError(ex.Message);
                }
            }

            return orphanedDeletionFailedUris;
        }

        //check list is more than 500 or empty
        private void CheckDeletionidentityCount(IList<string> pidUris)
        {
            if (pidUris == null || pidUris.Count == 0)
            {
                throw new RequestException("The deletion request is empty.");
            }
            else if (pidUris.Count > 500)
            {
                throw new RequestException("The deletion request has more than 500 record.");
            }
        }

        public IList<DuplicateResult> GetPidUriIdentifierOccurrences(string pidUri)
        {
            var types = _metadataService.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type
                .FirstResouceType);

             var mergedDuplicateResults = new List<DuplicateResult>();

            var instance = _identifierRepository.GetPidUriIdentifierOccurrences(new Uri(pidUri), types, GetInstanceGraph());
            var draft = _identifierRepository.GetPidUriIdentifierOccurrences(new Uri(pidUri), types, GetResourceDraftInstanceGraph());

             mergedDuplicateResults.AddRange(instance);  // Todo: Look in both graphs!!!!!!!
             mergedDuplicateResults.AddRange(draft);  // Todo: Look in both graphs!!!!!!!
                         
            return mergedDuplicateResults;
          }

        /// <summary>
        /// Delete all Identifiers, that belong to a resource.
        /// </summary>
        /// <param name="resource">The resource to delete from</param>
        public void DeleteAllUnpublishedIdentifiers(Entity resource)
        {
            if (null == resource)
            {
                throw new ArgumentNullException(nameof(resource), Common.Constants.Messages.Resource.NullResource);
            }

            var actualPidUris = GetAllIdentifiersOfResource(resource);

            foreach (var uri in actualPidUris)
            {
                _identifierRepository.Delete(new Uri(uri), GetResourceDraftInstanceGraph());
            }
        }

        private IList<string> GetAllIdentifiersOfResource(Entity entity)
        {
            IList<string> pidUris = new List<string>();

            if (entity != null)
            {
                foreach (var property in entity.Properties)
                {
                    if (property.Key == Graph.Metadata.Constants.EnterpriseCore.PidUri ||
                        property.Key == Graph.Metadata.Constants.Resource.BaseUri)
                    {
                        pidUris.Add(property.Value?.FirstOrDefault()?.Id);
                    }
                    else if (property.Key == Graph.Metadata.Constants.Resource.Distribution ||
                             property.Key == Graph.Metadata.Constants.Resource.MainDistribution)
                    {
                        foreach (var prop in property.Value)
                        {
                            if (DynamicExtension.IsType<Entity>(prop, out Entity parsedProp))
                            {
                                IList<string> nestedUris = GetAllIdentifiersOfResource(parsedProp);
                                pidUris.AddRange(nestedUris);
                            }
                        }
                    }
                }
            }

            return pidUris.Where(uri => !string.IsNullOrWhiteSpace(uri)).ToList();
        }

        private Uri GetInstanceGraph()
        {
            var graph = _metadataService.GetInstanceGraph(PIDO.PidConcept);
            return graph;
        }

        private Uri GetResourceDraftInstanceGraph()
        {
            return _metadataService.GetInstanceGraph("draft");
        }

        private Uri GetHistoricInstanceGraph()
        {
            var graph = _metadataService.GetHistoricInstanceGraph();
            return graph;
        }
    }
}
