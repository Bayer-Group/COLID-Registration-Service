using System;
using System.Collections.Generic;
using System.Linq;
using COLID.Graph.TripleStore.DataModels.Attributes;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.Extensions;
using COLID.RegistrationService.Common.DataModel.Resources;

namespace COLID.Graph.Metadata.DataModels.Resources
{
    [Type(Constants.TypeMap.FirstResouceType)]
    public class Resource : Entity
    {
        public Uri PidUri => GetIdentifier(Constants.EnterpriseCore.PidUri);
        public Uri BaseUri => GetIdentifier(Constants.Resource.BaseUri);
        public VersionOverviewCTO PreviousVersion => FindVersionInList(-1);
        public VersionOverviewCTO LaterVersion => FindVersionInList(1);
        public Dictionary<string, List<LinkingMapping>> Links { get; set; }
        public string PublishedVersion { get; set; }
        public IList<VersionOverviewCTO> Versions { get; set; }

        // TODO: PLEASE remove this getter function call stuff in an entity object ..
        private VersionOverviewCTO FindVersionInList(int index)
        {
            if (Versions == null || PidUri == null)
            {
                return null;
            }
            var actualVersion = Versions.FirstOrDefault(t => t.PidUri == PidUri.ToString());
            var indexActualVersion = Versions.IndexOf(actualVersion);

            try
            {
                return Versions[indexActualVersion + index];
            }
            catch
            {
                return null;
            }
        }

        private Uri GetIdentifier(string identifierType)
        {
            Entity identifierEntity = Properties.GetValueOrNull(identifierType, true);

            if (identifierEntity == null || string.IsNullOrWhiteSpace(identifierEntity.Id) || !Uri.IsWellFormedUriString(identifierEntity.Id, UriKind.Absolute))
            {
                return null;
            }

            return new Uri(identifierEntity.Id);
        }
    }
}
