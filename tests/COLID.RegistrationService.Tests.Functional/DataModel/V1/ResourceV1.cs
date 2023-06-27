using System;
using System.Collections.Generic;
using System.Linq;
using COLID.RegistrationService.Common.DataModel.Resources;
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.Metadata.Extensions;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.Metadata.DataModels.Resources;

namespace COLID.RegistrationService.Tests.Functional.DataModel.V1
{
    public class ResourceV1 : IComparable
    {
        public string Subject { get; set; }
        public IDictionary<string, List<dynamic>> Properties { get; set; }
        public Uri PidUri => GetIdentifier(Graph.Metadata.Constants.EnterpriseCore.PidUri);
        public Uri BaseUri => GetIdentifier(Graph.Metadata.Constants.Resource.BaseUri);
        public VersionOverviewCTO PreviousVersion => FindVersionInList(-1);
        public VersionOverviewCTO LaterVersion => FindVersionInList(1);
        public string PublishedVersion { get; set; }
        public IList<VersionOverviewCTO> Versions { get; set; }

        // TODO: PLEASE remove this getter function call stuff in an entity object ..
        private VersionOverviewCTO FindVersionInList(int index)
        {
            if (Versions == null)
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
            EntityV1 identifierEntity = Properties.GetValueOrNull(identifierType, true).ToObject<EntityV1>();

            if (identifierEntity == null || string.IsNullOrWhiteSpace(identifierEntity.Subject) || !Uri.IsWellFormedUriString(identifierEntity.Subject, UriKind.Absolute))
            {
                return null;
            }

            return new Uri(identifierEntity.Subject);
        }

        public int CompareTo(object obj)
        {
            if (obj == null)
            {
                return 1;
            }

            if (DynamicExtension.IsType(obj, out Entity otherEntity))
            {
                string type = Properties.GetValueOrNull(Graph.Metadata.Constants.RDF.Type, true);
                string otherType = otherEntity.Properties.GetValueOrNull(Graph.Metadata.Constants.RDF.Type, true);
                return type.CompareTo(otherType);
            }

            throw new ArgumentException($"Object is not type of class {typeof(Entity)}");
        }
    }
}
