using System;
using System.Collections.Generic;
using System.Linq;
using COLID.Common.Extensions;
using COLID.Exception.Models.Business;
using COLID.Graph.Metadata.Services;
using COLID.RegistrationService.Common.DataModel.Validation;
using COLID.RegistrationService.Repositories.Interface;
using COLID.RegistrationService.Services.Interface;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.Metadata.Extensions;

namespace COLID.RegistrationService.Services.Implementation
{
    internal class IdentifierService : IIdentifierService
    {
        private readonly IIdentifierRepository _identifierRepository;
        private readonly IMetadataService _metadataService;

        public IdentifierService(IIdentifierRepository identifierRepository, IMetadataService metadataService)
        {
            _identifierRepository = identifierRepository;
            _metadataService = metadataService;
        }

        public IList<string> GetOrphanedIdentifiersList()
        {
            return _identifierRepository.GetOrphanedIdentifiersList();
        }

        public void DeleteOrphanedIdentifier(string identifierUri)
        {
            if (Uri.TryCreate(identifierUri, UriKind.Absolute, out Uri uri))
            {
                _identifierRepository.DeleteOrphanedIdentifier(uri);
            }
            else
            {
                throw new InvalidFormatException(Graph.Metadata.Constants.Messages.Identifier.IncorrectIdentifierFormat, identifierUri);
            }
        }

        public IList<DuplicateResult> GetPidUriIdentifierOccurrences(string pidUri)
        {
            var types = _metadataService.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type.FirstResouceType);
            return _identifierRepository.GetPidUriIdentifierOccurrences(new Uri(pidUri), types);
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
                _identifierRepository.Delete(new Uri(uri));
            }
        }

        private IList<string> GetAllIdentifiersOfResource(Entity entity)
        {
            IList<string> pidUris = new List<string>();

            if (entity != null)
            {
                foreach (var property in entity.Properties)
                {
                    if (property.Key == Graph.Metadata.Constants.EnterpriseCore.PidUri || property.Key == Graph.Metadata.Constants.Resource.BaseUri)
                    {
                        pidUris.Add(property.Value?.FirstOrDefault()?.Id);
                    }
                    else if (property.Key == Graph.Metadata.Constants.Resource.Distribution || property.Key == Graph.Metadata.Constants.Resource.MainDistribution)
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
    }
}
