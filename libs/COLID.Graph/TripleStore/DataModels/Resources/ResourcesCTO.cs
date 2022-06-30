using System;
using System.Collections.Generic;
using COLID.Graph.TripleStore.DataModels.Base;

namespace COLID.Graph.Metadata.DataModels.Resources
{
    /// <summary>
    /// This transport object is used to store two different lifecycle statuses of a resource.
    /// </summary>
    public class ResourcesCTO
    {
        // The resource with lifecycle status Draft.
        public Entity Draft { get; set; }

        // The resource with lifecycle version Published.
        public Entity Published { get; set; }

        public IList<VersionOverviewCTO> Versions { get; set; }

        public bool HasDraft => null != Draft;

        public bool HasPublished => null != Published;

        public bool IsEmpty => !HasPublished && !HasDraft;

        public bool HasDraftAndNoPublished => HasDraft && !HasPublished;

        public bool HasPublishedAndNoDraft => HasPublished && !HasDraft;

        public bool HasPublishedAndDraft => HasPublished && HasDraft;

        public bool HasPublishedOrDraft => HasPublished || HasDraft;

        public ResourcesCTO()
        {
        }

        public ResourcesCTO(Entity draftVersion, Entity publishedVersion, IList<VersionOverviewCTO> versions)
        {
            Draft = draftVersion;
            Published = publishedVersion;
            Versions = versions;
        }

        public Entity GetDraftOrPublishedVersion()
        {
            CheckIfDraftOrPublishedExists();

            if (HasDraft)
            {
                return Draft;
            }
            return Published;
        }

        public Entity GetPublishedOrDraftVersion()
        {
            CheckIfDraftOrPublishedExists();

            if (HasPublished)
            {
                return Published;
            }
            return Draft;
        }

        private void CheckIfDraftOrPublishedExists()
        {
            if (!HasDraft && !HasPublished)
            {
                throw new InvalidOperationException("Neither draft, nor published was set");
            }
        }
    }
}
