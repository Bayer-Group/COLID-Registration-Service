using System;
using System.Collections.Generic;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.DataModels.Resources;
using COLID.Graph.TripleStore.Extensions;

namespace COLID.Graph.Metadata.DataModels.Resources
{
    public class ResourceIndexingDTO
    {
        public ResourceCrudAction Action { get; set; }
        public Entity Resource { get; set; }
        public Uri PidUri { get; set; }
        public ResourcesCTO RepoResources { get; set; }
        public IDictionary<string, List<dynamic>> InboundProperties => GetInboundProperties();
        public string CurrentLifecycleStatus => GetLifecycleStatus();

        public ResourceIndexingDTO(ResourceCrudAction action, Uri pidUri, Entity resource, ResourcesCTO repoResources)
        {
            Action = action;
            PidUri = pidUri;
            Resource = resource;
            RepoResources = repoResources ?? new ResourcesCTO();
        }

        private string GetLifecycleStatus()
        {
            return Resource.Properties.GetValueOrNull(
                COLID.Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus, true);
        }

        private IDictionary<string, List<dynamic>> GetInboundProperties()
        {
            /*
             if (RepoResources.HasPublishedOrDraft)
            {
                return RepoResources.GetDraftOrPublishedVersion().InboundProperties;
            }

            return new Dictionary<string, List<dynamic>>();
            */
            return Resource.InboundProperties;
        }
    }
}
