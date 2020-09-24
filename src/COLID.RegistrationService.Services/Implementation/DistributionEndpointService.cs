using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using COLID.Graph.Metadata.Constants;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.Metadata.Exceptions;
using COLID.Graph.Metadata.Extensions;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.Extensions;
using COLID.RegistrationService.Common.DataModel.Resources;
using COLID.RegistrationService.Services.Interface;
using Entity = COLID.Graph.TripleStore.DataModels.Base.Entity;

namespace COLID.RegistrationService.Services.Implementation
{
    public class DistributionEndpointService : IDistributionEndpointService
    {
        private readonly IMapper _mapper;

        private readonly IResourceService _resourceService;

        public DistributionEndpointService(IMapper mapper, IResourceService resourceService)
        {
            _mapper = mapper;
            _resourceService = resourceService;
        }

        public async Task<ResourceWriteResultCTO> CreateDistributionEndpoint(Uri resourcePidUri, bool createAsMainDistributionEndpoint, BaseEntityRequestDTO requestDistributionEndpoint)
        {
            var newEndpoint = _mapper.Map<Entity>(requestDistributionEndpoint);

            var resource = _resourceService.GetByPidUri(resourcePidUri);

            var endpoints = new List<dynamic>();

            if (resource.Properties.TryGetValue(Graph.Metadata.Constants.Resource.Distribution, out var repoEndpoints))
            {
                endpoints = repoEndpoints;
            }

            var mainEndpoints = new List<dynamic>();

            if (createAsMainDistributionEndpoint)
            {
                if (resource.Properties.TryGetValue(Graph.Metadata.Constants.Resource.MainDistribution, out var repoMainEndpoints))
                {
                    mainEndpoints = repoMainEndpoints;
                }

                resource.Properties.AddOrUpdate(Graph.Metadata.Constants.Resource.MainDistribution, new List<dynamic> { newEndpoint });
            }
            else
            {
                endpoints.Add(newEndpoint);
            }

            endpoints.AddRange(mainEndpoints);

            resource.Properties.AddOrUpdate(Graph.Metadata.Constants.Resource.Distribution, endpoints);

            var requestResource = _mapper.Map<ResourceRequestDTO>(resource);

            return await _resourceService.EditResource(resourcePidUri, requestResource).ConfigureAwait(false);
        }

        public async void DeleteDistributionEndpoint(Uri distributionEndpointPidUri)
        {
            var resourcePidUri = _resourceService.GetPidUriByDistributionEndpointPidUri(distributionEndpointPidUri);

            var resource = _resourceService.GetByPidUri(resourcePidUri);

            RemoveEndpointFromProperties(resource, distributionEndpointPidUri);

            var requestResource = _mapper.Map<ResourceRequestDTO>(resource);

            try
            {
                await _resourceService.EditResource(resource.PidUri, requestResource);
            }
            catch (ValidationException ex)
            {
                // If critical validation result exists, then the deletion process was not successful and a draft was not created.
                if (ex.ValidationResult.Results.Any(r => r.ResultSeverity == ValidationResultSeverity.Violation))
                {
                    throw ex;
                }
            }
        }

        private void RemoveEndpointFromProperties(Entity resource, Uri distributionEndpointPidUri)
        {
            // Remove distribution endpoint from pid entry
            if (resource.Properties.TryGetValue(Graph.Metadata.Constants.Resource.Distribution, out List<dynamic> endpoints))
            {
                int indexToRemove = 0;
                bool indexSet = false;

                var index = 0;
                foreach (var endpoint in endpoints)
                {
                    if (DynamicExtension.IsType<Entity>(endpoint, out Entity distributionEndpoint))
                    {
                        if (distributionEndpoint.Properties.TryGetValue(EnterpriseCore.PidUri, out List<dynamic> pidUriValue))
                        {
                            Entity pidUriEntity = pidUriValue.FirstOrDefault();
                            if (pidUriEntity.Id == distributionEndpointPidUri.ToString())
                            {
                                indexToRemove = index;
                                indexSet = true;
                                break;
                            }
                        }
                    }

                    index++;
                }

                if (indexSet)
                {
                    endpoints.RemoveAt(indexToRemove);
                    if (!endpoints.Any())
                    {
                        resource.Properties.Remove(Graph.Metadata.Constants.Resource.Distribution);
                    }
                    else
                    {
                        resource.Properties[Graph.Metadata.Constants.Resource.Distribution] = endpoints;
                    }
                }
            }
        }

        public async Task<ResourceWriteResultCTO> EditDistributionEndpoint(Uri distributionEndpointPidUri, bool editAsMainDistributionEndpoint, BaseEntityRequestDTO requestDistributionEndpoint)
        {
            var resourcePidUri = _resourceService.GetPidUriByDistributionEndpointPidUri(distributionEndpointPidUri);

            var resource = _resourceService.GetByPidUri(resourcePidUri);

            var newEndpoint = _mapper.Map<Entity>(requestDistributionEndpoint);

            var mainEndpoints = new List<dynamic>();

            if (resource.Properties.TryGetValue(Graph.Metadata.Constants.Resource.MainDistribution, out var mainRepoEndpoints))
            {
                mainEndpoints = FilterEndpoints(mainRepoEndpoints, distributionEndpointPidUri.ToString());
            }

            var endpoints = new List<dynamic>();

            if (resource.Properties.TryGetValue(Graph.Metadata.Constants.Resource.Distribution, out var repoEndpoints))
            {
                endpoints = FilterEndpoints(repoEndpoints, distributionEndpointPidUri.ToString());
            }

            if (editAsMainDistributionEndpoint)
            {
                endpoints.AddRange(mainEndpoints);
                mainEndpoints = new List<dynamic>() { newEndpoint };
            }
            else
            {
                endpoints.Add(newEndpoint);
            }

            resource.Properties.AddOrUpdate(Graph.Metadata.Constants.Resource.MainDistribution, mainEndpoints);
            resource.Properties.AddOrUpdate(Graph.Metadata.Constants.Resource.Distribution, endpoints);

            var requestResource = _mapper.Map<ResourceRequestDTO>(resource);

            return await _resourceService.EditResource(resource.PidUri, requestResource).ConfigureAwait(false);
        }

        /// <summary>
        /// Filters a list of full endpoints based on the specified pid uri.
        /// </summary>
        /// <param name="editableEndpointUri">Pid uri of the endpoint to be filtered from the list.</param>
        /// <param name="endpoints">A list of endpoints</param>
        /// <returns></returns>
        private static List<dynamic> FilterEndpoints(List<dynamic> endpoints, string editableEndpointUri)
        {
            return endpoints.Where(endpointValue => DynamicExtension.IsType<Entity>(endpointValue, out Entity endpoint) && endpoint.Properties.GetValueOrNull(Graph.Metadata.Constants.EnterpriseCore.PidUri, true)?.Id == editableEndpointUri).ToList();
        }
    }
}
