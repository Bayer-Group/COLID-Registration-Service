using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoMapper;
using COLID.Cache.Exceptions;
using COLID.Cache.Services.Lock;
using COLID.Common.Utilities;
using COLID.Exception.Models;
using COLID.Exception.Models.Business;
using COLID.Graph.HashGenerator.Services;
using COLID.Graph.Metadata.Constants;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.Metadata.Comparison;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.Metadata.Services;
using COLID.Graph.TripleStore.DataModels.Resources;
using COLID.Graph.TripleStore.Extensions;
using COLID.RegistrationService.Common.DataModel.DistributionEndpoints;
using COLID.RegistrationService.Common.DataModel.Resources;
using COLID.RegistrationService.Common.DataModel.Search;
using COLID.RegistrationService.Common.DataModels.LinkHistory;
using COLID.RegistrationService.Repositories.Interface;
using COLID.RegistrationService.Services.Authorization.UserInfo;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.Services.Validation.Exceptions;
using COLID.RegistrationService.Services.Validation.Models;
using COLID.StatisticsLog.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Entity = COLID.Graph.TripleStore.DataModels.Base.Entity;
using Resource = COLID.Graph.Metadata.DataModels.Resources.Resource;
using ColidConstants = COLID.RegistrationService.Common.Constants;

namespace COLID.RegistrationService.Services.Implementation
{
    public class RevisionService : IRevisionService
    {
        private readonly IMapper _mapper;
        private readonly ILogger<RevisionService> _logger;
        private readonly IResourceRepository _resourceRepository;
        private readonly IMetadataService _metadataService;

        public RevisionService(
            IMapper mapper,
            ILogger<RevisionService> logger,
            IResourceRepository resourceRepository,
            IMetadataService metadataService)
        {
            _mapper = mapper;
            _logger = logger;
            _resourceRepository = resourceRepository;
            _metadataService = metadataService;
        }

        public async Task InitializeResourceInAdditionalsGraph(Resource ResourceToBeCreated, IList<MetadataProperty> allMetaData)
        {
            string revisionGraphPrefix = ResourceToBeCreated.Id + "Rev" + 1;
            string additionalGraphName = revisionGraphPrefix + "_added";
            _resourceRepository.CreateProperty(new Uri(ResourceToBeCreated.Id), new Uri(COLID.Graph.Metadata.Constants.Resource.HasRevision), revisionGraphPrefix, GetResourceInstanceGraph());
            _resourceRepository.Create(ResourceToBeCreated, allMetaData, new Uri(additionalGraphName));

        }

        public async Task<Resource> AddAdditionalsAndRemovals(Entity published, Entity draftToBePublished)
        {
            if (published.Properties[COLID.Graph.Metadata.Constants.EnterpriseCore.PidUri][0].Id != draftToBePublished.Properties[COLID.Graph.Metadata.Constants.EnterpriseCore.PidUri][0].Id)
            {
                throw new BusinessException("The resources to be compared do not have the same PidUri");
            }

            List<string> ignoredProperties = new List<string>();
            ignoredProperties.Add(COLID.Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus);
            ignoredProperties.Add(COLID.Graph.Metadata.Constants.Resource.HasRevision);
            ignoredProperties.Add(COLID.Graph.Metadata.Constants.EnterpriseCore.PidUri);
            ignoredProperties.Add(COLID.Graph.Metadata.Constants.Resource.HasSourceID);
            ignoredProperties.Add(ColidConstants.ContactValidityCheck.BrokenDataStewards);

            if (draftToBePublished.Properties.ContainsKey(COLID.Graph.Metadata.Constants.Resource.BaseUri) == published.Properties.ContainsKey(COLID.Graph.Metadata.Constants.Resource.BaseUri))
            ignoredProperties.Add(COLID.Graph.Metadata.Constants.Resource.BaseUri);
            ignoredProperties.AddRange(COLID.Graph.Metadata.Constants.Resource.LinkTypes.AllLinkTypes);

            var existingRevisions = published.Properties.TryGetValue(COLID.Graph.Metadata.Constants.Resource.HasRevision, out List<dynamic> revisionValues) ? revisionValues : new List<dynamic>();


            //IList <MetadataProperty> allMetaData = _metadataService.GetMetadataForEntityType(Published.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.RDF.Type, true));

            List<MetadataProperty> allMetaData = _metadataService.GetMetadataForEntityTypeInConfig(published.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.RDF.Type, true), published.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.Resource.MetadataGraphConfiguration, true));
            List<string> allMetaDataKeys = allMetaData.Select(x => x.Key).ToList();


            List<MetadataProperty> allMetaData2 = _metadataService.GetMetadataForEntityTypeInConfig(draftToBePublished.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.RDF.Type, true), draftToBePublished.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.Resource.MetadataGraphConfiguration, true));
            allMetaData.AddRange(allMetaData2.Where(x => !allMetaDataKeys.Contains(x.Key)).Select(y => y));


            Dictionary<string, List<dynamic>> additionals = new Dictionary<string, List<dynamic>>();
            Dictionary<string, List<dynamic>> removals = new Dictionary<string, List<dynamic>>();

            foreach (var metadata in allMetaData)
            {
                if (ignoredProperties.Contains(metadata.Key))
                {
                    continue;
                }

                if (published.Properties.TryGetValue(metadata.Key, out List<dynamic> firstValue) && draftToBePublished.Properties.TryGetValue(metadata.Key, out List<dynamic> secondValue))
                {
                    if (ResourceValueChanged(firstValue, secondValue))
                    {
                        additionals.Add(metadata.Key, secondValue);
                        removals.Add(metadata.Key, firstValue);
                    }
                }
                else if (published.Properties.TryGetValue(metadata.Key, out List<dynamic> OnlyfirstValue) && !draftToBePublished.Properties.TryGetValue(metadata.Key, out List<dynamic> NotsecondValue))
                {
                    removals.Add(metadata.Key, OnlyfirstValue);
                }
                else if (!published.Properties.TryGetValue(metadata.Key, out List<dynamic> NotfirstValue) && draftToBePublished.Properties.TryGetValue(metadata.Key, out List<dynamic> OnlysecondValue))
                {
                    additionals.Add(metadata.Key, OnlysecondValue);
                }
                else
                {
                    continue;
                }

            }

            Resource resource = UpdateResourceProperties(additionals, removals, _mapper.Map<Resource>(published));

            //if(additionals.Count==1 && removals.Count==1 && additionals.ContainsKey(COLID.Graph.Metadata.Constants.Resource.DateModified))
            
            string pattern = @"[^Rev]+$";
            Regex rg = new Regex(pattern);
            var revList = existingRevisions.Select(x => Int32.Parse(rg.Match(x).Value)).ToList();
            var max = revList.Max();
            
            string revisionGraphPrefix = published.Id + "Rev" + (max+1);
            _resourceRepository.CreateProperty(new Uri(published.Id), new Uri(COLID.Graph.Metadata.Constants.Resource.HasRevision), revisionGraphPrefix, GetResourceInstanceGraph());

            (additionals, removals) = GetFinalAdditionalsAndRemovals(additionals, removals);
            _resourceRepository.CreateAdditionalsAndRemovalsGraphs(additionals, removals, allMetaData, published.Id, revisionGraphPrefix);  //letzte revisionwert rausnehmen, counter erhöhen und damit dann die graphen erstellen


            return resource;
        }

        //if only specific metadata should be checked
        public async Task<Resource> AddAdditionalsAndRemovals(Entity Published, Entity draftToBePublished, IList<MetadataProperty> metaDataToCheck)
        {
            if (Published.Properties[COLID.Graph.Metadata.Constants.EnterpriseCore.PidUri][0].Id != draftToBePublished.Properties[COLID.Graph.Metadata.Constants.EnterpriseCore.PidUri][0].Id)
            {
                throw new BusinessException("The resources to be compared do not have the same PidUri");
            }

            List<string> ignoredProperties = new List<string>();
            ignoredProperties.Add(COLID.Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus);
            ignoredProperties.Add(COLID.Graph.Metadata.Constants.Resource.HasRevision);
            ignoredProperties.Add(COLID.Graph.Metadata.Constants.EnterpriseCore.PidUri);
            ignoredProperties.Add(COLID.Graph.Metadata.Constants.Resource.HasSourceID);

            if (draftToBePublished.Properties.ContainsKey(COLID.Graph.Metadata.Constants.Resource.BaseUri) == Published.Properties.ContainsKey(COLID.Graph.Metadata.Constants.Resource.BaseUri))
                ignoredProperties.Add(COLID.Graph.Metadata.Constants.Resource.BaseUri);
            ignoredProperties.AddRange(COLID.Graph.Metadata.Constants.Resource.LinkTypes.AllLinkTypes);

            var existingRevisions = Published.Properties.TryGetValue(COLID.Graph.Metadata.Constants.Resource.HasRevision, out List<dynamic> revisionValues) ? revisionValues : new List<dynamic>();


            //IList <MetadataProperty> allMetaData = _metadataService.GetMetadataForEntityType(Published.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.RDF.Type, true));


            Dictionary<string, List<dynamic>> additionals = new Dictionary<string, List<dynamic>>();
            Dictionary<string, List<dynamic>> removals = new Dictionary<string, List<dynamic>>();

            foreach (var metadata in metaDataToCheck)
            {
                if (ignoredProperties.Contains(metadata.Key))
                {
                    continue;
                }

                if (Published.Properties.TryGetValue(metadata.Key, out List<dynamic> firstValue) && draftToBePublished.Properties.TryGetValue(metadata.Key, out List<dynamic> secondValue))
                {
                    if (ResourceValueChanged(firstValue, secondValue))
                    {
                        additionals.Add(metadata.Key, secondValue);
                        removals.Add(metadata.Key, firstValue);
                    }
                }
                else if (Published.Properties.TryGetValue(metadata.Key, out List<dynamic> OnlyfirstValue) && !draftToBePublished.Properties.TryGetValue(metadata.Key, out List<dynamic> NotsecondValue))
                {
                    removals.Add(metadata.Key, OnlyfirstValue);
                }
                else if (!Published.Properties.TryGetValue(metadata.Key, out List<dynamic> NotfirstValue) && draftToBePublished.Properties.TryGetValue(metadata.Key, out List<dynamic> OnlysecondValue))
                {
                    additionals.Add(metadata.Key, OnlysecondValue);
                }
                else
                {
                    continue;
                }

            }

            Resource resource = UpdateResourceProperties(additionals, removals, _mapper.Map<Resource>(Published));

            //if(additionals.Count==1 && removals.Count==1 && additionals.ContainsKey(COLID.Graph.Metadata.Constants.Resource.DateModified))

            string pattern = @"[^Rev]+$";
            Regex rg = new Regex(pattern);
            var revList = existingRevisions.Select(x => Int32.Parse(rg.Match(x).Value)).ToList();
            var max = revList.Max();

            string revisionGraphPrefix = Published.Id + "Rev" + (max + 1);
            _resourceRepository.CreateProperty(new Uri(Published.Id), new Uri(COLID.Graph.Metadata.Constants.Resource.HasRevision), revisionGraphPrefix, GetResourceInstanceGraph());

            (additionals, removals) = GetFinalAdditionalsAndRemovals(additionals, removals);
            _resourceRepository.CreateAdditionalsAndRemovalsGraphs(additionals, removals, metaDataToCheck, Published.Id, revisionGraphPrefix);  //letzte revisionwert rausnehmen, counter erhöhen und damit dann die graphen erstellen


            return resource;
        }

        private static (Dictionary<string, List<dynamic>> additionals, Dictionary<string, List<dynamic>> removals) GetFinalAdditionalsAndRemovals(Dictionary<string, List<dynamic>> additionals, Dictionary<string, List<dynamic>> removals)
        {
            Dictionary<string, List<dynamic>> final_additionals = new Dictionary<string, List<dynamic>>();
            Dictionary<string, List<dynamic>> final_removals = new Dictionary<string, List<dynamic>>();
            List<string> joinedKeys = additionals.Keys.Where(x => removals.Keys.Contains(x)).ToList();

            final_additionals.AddRange(additionals.Where(y => !joinedKeys.Contains(y.Key)).ToList());
            final_removals.AddRange(removals.Where(y => !joinedKeys.Contains(y.Key)).ToList());

            foreach (var key in joinedKeys)
            {
                List<dynamic> AdditionalValues = additionals.GetValueOrDefault(key) != null ? additionals.GetValueOrDefault(key) : new List<dynamic>();
                List<dynamic> RemovalValues = removals.GetValueOrDefault(key) != null ? removals.GetValueOrDefault(key) : new List<dynamic>();

                List<dynamic> newAdditionalValues = new List<dynamic>();
                List<dynamic> newRemovalValues = new List<dynamic>();

                foreach (var value in AdditionalValues)
                {
                    if (value.GetType().Name != "Entity")
                    {
                        if (!RemovalValues.Contains(value))
                        {
                            newAdditionalValues.Add(value);
                        }
                    }
                    else
                    {
                        Entity entity = value;
                        Entity entity2 = RemovalValues.Where(x => x.Id == entity.Id).FirstOrDefault();
                        entity2 = entity2 != null ? entity2 : new Entity();
                        var entityProps = entity.Properties.Where(x => x.Key != COLID.Graph.Metadata.Constants.EnterpriseCore.PidUri).ToList().OrderBy(x => x.Key).ToDictionary(t => t.Key, t => t.Value);
                        var entityProps2 = entity2.Properties.Where(x => x.Key != COLID.Graph.Metadata.Constants.EnterpriseCore.PidUri).ToList().OrderBy(x => x.Key).ToDictionary(t => t.Key, t => t.Value);


                        var firstString = JsonConvert.SerializeObject(entityProps).ToString(); //entity.ToString();
                        var secondString = JsonConvert.SerializeObject(entityProps2).ToString(); //entity2.ToString();
                        using SHA256 sha256 = SHA256.Create();
                        var computedHash = HashGenerator.GetHash(sha256, firstString);
                        var computedHash2 = HashGenerator.GetHash(sha256, secondString);

                        if (computedHash != computedHash2)
                        {
                            newAdditionalValues.Add(value);
                        }
                    }
                }

                foreach (var value in RemovalValues)
                {
                    if (value.GetType().Name != "Entity")
                    {
                        if (!AdditionalValues.Contains(value))
                        {
                            newRemovalValues.Add(value);
                        }
                    }
                    else
                    {
                        Entity entity = value;
                        Entity entity2 = AdditionalValues.Where(x => x.Id == entity.Id).FirstOrDefault();
                        entity2 = entity2 != null ? entity2 : new Entity();

                        var entityProps = entity.Properties.Where(x => x.Key != COLID.Graph.Metadata.Constants.EnterpriseCore.PidUri).ToList().OrderBy(x => x.Key).ToDictionary(t => t.Key, t => t.Value);
                        var entityProps2 = entity2.Properties.Where(x => x.Key != COLID.Graph.Metadata.Constants.EnterpriseCore.PidUri).ToList().OrderBy(x => x.Key).ToDictionary(t => t.Key, t => t.Value);


                        var firstString = JsonConvert.SerializeObject(entityProps).ToString(); //entity.ToString();
                        var secondString = JsonConvert.SerializeObject(entityProps2).ToString(); //entity2.ToString();

                        using SHA256 sha256 = SHA256.Create();
                        var computedHash = HashGenerator.GetHash(sha256, firstString);
                        var computedHash2 = HashGenerator.GetHash(sha256, secondString);

                        if (computedHash != computedHash2)
                        {
                            newRemovalValues.Add(value);
                        }
                    }
                }

                final_additionals.Add(key, newAdditionalValues);
                

                

                final_removals.Add(key, newRemovalValues);
            }

            return (final_additionals, final_removals);
        }
        private static Resource UpdateResourceProperties(Dictionary<string, List<dynamic>> additionals, Dictionary<string, List<dynamic>> removals, Resource published)
        {

            var newAddedValues = additionals.Where(x => !removals.ContainsKey(x.Key)).ToList();
            var removedValues = removals.Where(x => !additionals.ContainsKey(x.Key)).ToList();
            var changedValues = additionals.Where(x => removals.ContainsKey(x.Key)).ToList();

            if (newAddedValues.Count != 0)
            {
                foreach (var entry in newAddedValues)
                {
                    published.Properties.Add(entry.Key, entry.Value);
                }
            }
            if (removedValues.Count != 0)
            {
                foreach (var entry in removedValues)
                {
                    published.Properties.Remove(entry.Key);
                }
            }
            if (changedValues.Count != 0)
            {
                foreach (var entry in changedValues)
                {
                    published.Properties.Remove(entry.Key);
                    published.Properties.Add(entry.Key, entry.Value);
                }
            }

            // remove broken link and invalid distribution endpoint contact on republishing
            if (published.Properties.ContainsKey(Graph.Metadata.Constants.Resource.Distribution))
            {
                var distributionEndpoints = published.Properties[Graph.Metadata.Constants.Resource.Distribution];
                foreach (var distributionEndpoint in distributionEndpoints)
                {
                    distributionEndpoint.Properties.Remove(ColidConstants.DistributionEndpoint.DistributionEndpointsTest.EndpointLifecycleStatus);
                    distributionEndpoint.Properties.Remove(ColidConstants.ContactValidityCheck.BrokenEndpointContacts);
                }
            }

            return published;
        }

        private static bool ResourceValueChanged(List<dynamic> firstValue, List<dynamic> secondValue)
        {
            if (firstValue.Count != secondValue.Count)
            {
                return true;
            }
            else
            {
                for (int i = 0; i < firstValue.Count; i++)
                {
                    firstValue.Sort();
                    secondValue.Sort();
                    string firstString;
                    string secondString;
                    if (!(firstValue[i] is string || secondValue[i] is string))
                    {
                        Entity entity = firstValue[i];
                        Entity entity2 = secondValue[i];
                        entity2.Properties = entity2.Properties.Where(x => x.Value.Count > 0).ToDictionary(x => x.Key, x => x.Value);

                        // removing the flags on the distribution endpoint should not be written to the history
                        var entityProps = entity.Properties
                            .Where(x => x.Key != EnterpriseCore.PidUri && x.Key != ColidConstants.DistributionEndpoint.DistributionEndpointsTest.EndpointLifecycleStatus && x.Key != ColidConstants.ContactValidityCheck.BrokenEndpointContacts)
                            .ToList()
                            .OrderBy(x => x.Key)
                            .ToDictionary(t => t.Key, t => t.Value);
                        var entityProps2 = entity2.Properties
                            .Where(x => x.Key != EnterpriseCore.PidUri && x.Key != ColidConstants.DistributionEndpoint.DistributionEndpointsTest.EndpointLifecycleStatus && x.Key != ColidConstants.ContactValidityCheck.BrokenEndpointContacts)
                            .ToList()
                            .OrderBy(x => x.Key)
                            .ToDictionary(t => t.Key, t => t.Value);

                        firstString = JsonConvert.SerializeObject(entityProps).ToString(); //entity.ToString();
                        secondString = JsonConvert.SerializeObject(entityProps2).ToString(); //entity2.ToString();
                    }
                    else
                    {
                        firstString = firstValue[i].ToString();
                        secondString = secondValue[i].ToString();
                    }
                    using SHA256 sha256 = SHA256.Create();
                    var computedHash = HashGenerator.GetHash(sha256, firstString);
                    var computedHash2 = HashGenerator.GetHash(sha256, secondString);

                    if (computedHash != computedHash2)
                    {
                        return true;
                    }
                }

            }

            return false;
        }

        public Uri GetResourceInstanceGraph()
        {
            return _metadataService.GetInstanceGraph(PIDO.PidConcept);
        }

    }
}
