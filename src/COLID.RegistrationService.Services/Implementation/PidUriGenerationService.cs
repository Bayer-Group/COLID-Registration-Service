﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using COLID.Common.Extensions;
using COLID.Graph.Metadata.Constants;
using COLID.RegistrationService.Common.DataModel.PidUriTemplates;
using COLID.RegistrationService.Repositories.Interface;
using COLID.RegistrationService.Services.Extensions;
using COLID.RegistrationService.Services.Interface;
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.Metadata.Extensions;
using COLID.Graph.Metadata.Services;
using Entity = COLID.Graph.TripleStore.DataModels.Base.Entity;

namespace COLID.RegistrationService.Services.Implementation
{
    internal class PidUriGenerationService : IPidUriGenerationService
    {
        private readonly IPidUriTemplateRepository _pidUriTemplateRepository;
        private readonly IMetadataService _metadataService;

        public IList<string> GeneratedIdentifier { get; }

        public PidUriGenerationService(IPidUriTemplateRepository pidUriTemplateRepository, IMetadataService metataService)
        {
            _pidUriTemplateRepository = pidUriTemplateRepository;
            GeneratedIdentifier = new List<string>();
            _metadataService = metataService;
        }

        public string GenerateIdentifierFromTemplate(PidUriTemplateFlattened pidUriTemplateFlat, Entity resource)
        {
            string prefix = pidUriTemplateFlat.BaseUrl + pidUriTemplateFlat.Route;
            int idLength = pidUriTemplateFlat.IdLength;
            string id;
            if (pidUriTemplateFlat.IdType == Common.Constants.PidUriTemplateIdType.Guid)
            {
                id = Guid.NewGuid().ToString();
            }
            else if (pidUriTemplateFlat.IdType == Common.Constants.PidUriTemplateIdType.Number)
            {
                var regexForExistingPidUris = pidUriTemplateFlat.GetRegex();
                var existingPidUrisForTemplate = GetMatchingPidUris(resource, regexForExistingPidUris);

                existingPidUrisForTemplate.AddRange(GeneratedIdentifier.Where(t => Regex.IsMatch(t, regexForExistingPidUris)));

                var pidUriNumbers = existingPidUrisForTemplate
                    .SelectMany(e => Regex.Matches(e, regexForExistingPidUris).Select(r => r.Groups[1]?.Value))
                    .Select(s =>
                    {
                        long pidUriAsNumber = -1;
                        if (long.TryParse(s, out pidUriAsNumber))
                            return pidUriAsNumber;
                        else
                            return -1;
                    })
                    .OrderBy(d => d);
                long nextFreeNumber = 1;
                foreach (double number in pidUriNumbers)
                {
                    if (number < nextFreeNumber)
                    {
                        // in case we start at negative value
                        continue;
                    }
                    else if (number == nextFreeNumber)
                    {
                        // move forward and check next number
                        nextFreeNumber++;
                    }
                    else if (number > nextFreeNumber)
                    {
                        // found a free spot
                        break;
                    }
                }
                if (idLength > 0)
                {
                    if (nextFreeNumber.ToString().Count() > idLength)
                    {
#pragma warning disable CA2201 // Do not raise reserved exception types
                        throw new System.Exception($"Next free id number '{nextFreeNumber}' exceeds the defined id length of '{idLength}'.");
#pragma warning restore CA2201 // Do not raise reserved exception types
                    }
                    var format = "D" + idLength;
                    id = nextFreeNumber.ToString(format);
                }
                else
                {
                    id = nextFreeNumber.ToString();
                }
            }
            else
            {
                throw new System.ArgumentException($"Unrecognized id type {pidUriTemplateFlat.IdType}.");
            }

            var pidUri = prefix + id + pidUriTemplateFlat.Suffix;

            return pidUri;
        }

        private IList<string> GetMatchingPidUris(Entity resource, string regexForExistingPidUris)
        {
            var matchingPidUris = new List<string>();

            var activePidUris = _pidUriTemplateRepository.GetMatchingPidUris(regexForExistingPidUris, _metadataService.GetInstanceGraph(COLID.Graph.Metadata.Constants.PIDO.PidConcept), _metadataService.GetInstanceGraph("draft"));
            matchingPidUris.AddRange(activePidUris);

            Entity resourcePidUri = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.EnterpriseCore.PidUri, true);
            Entity resourceBaseUri = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.BaseUri, true);

            if (!string.IsNullOrWhiteSpace(resourcePidUri?.Id))
            {
                matchingPidUris.Add(resourcePidUri.Id);
            }

            if (!string.IsNullOrWhiteSpace(resourceBaseUri?.Id))
            {
                matchingPidUris.Add(resourceBaseUri.Id);
            }

            foreach (var property in resource.Properties)
            {
                foreach (var prop in property.Value)
                {
                    if (DynamicExtension.IsType<Entity>(prop, out Entity entity))
                    {
                        Entity nestedPidUri = entity.Properties.GetValueOrNull(Graph.Metadata.Constants.EnterpriseCore.PidUri, true);
                        if (nestedPidUri != null && !string.IsNullOrWhiteSpace(nestedPidUri.Id) && Regex.IsMatch(nestedPidUri.Id, regexForExistingPidUris))
                        {
                            matchingPidUris.Add(nestedPidUri.Id);
                        }

                        Entity nestedBaseUri = entity.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.BaseUri, true);
                        if (nestedBaseUri != null && !string.IsNullOrWhiteSpace(nestedBaseUri.Id) && Regex.IsMatch(nestedBaseUri.Id, regexForExistingPidUris))
                        {
                            matchingPidUris.Add(nestedBaseUri.Id);
                        }
                    }
                }
            }

            return matchingPidUris;
        }
    }
}
