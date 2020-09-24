using System;
using COLID.Graph.TripleStore.DataModels.Base;

namespace COLID.RegistrationService.Common.DataModel.Resources
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

        public bool HasDraft => null != Draft;

        public bool HasPublished => null != Published;

        public bool IsEmpty => !HasPublished && !HasDraft;

        public bool HasDraftAndNoPublished => HasDraft && !HasPublished;

        public bool HasPublishedAndNoDraft => HasPublished && !HasDraft;

        public bool HasPublishedAndDraft => HasPublished && HasDraft;

        public ResourcesCTO()
        {
        }

        public ResourcesCTO(Entity draftVersion, Entity publishedVersion)
        {
            Draft = draftVersion;
            Published = publishedVersion;
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
