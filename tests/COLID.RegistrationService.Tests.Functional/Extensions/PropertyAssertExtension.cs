using System;
using System.Collections.Generic;
using System.Linq;
using COLID.Common.Extensions;
using COLID.RegistrationService.Common.Enums.ColidEntry;
using COLID.RegistrationService.Common.Extensions;
using Xunit;

namespace COLID.RegistrationService.Tests.Common.Extensions
{
    public static class PropertyAssertExtension
    {
        public static Dictionary<string, Func<string, bool>> GetContainsActions(
            this IDictionary<string, List<dynamic>> properties)
        {
            Dictionary<string, Func<string, bool>> containsActions = new Dictionary<string, Func<string, bool>>();

            containsActions.Add(Graph.Metadata.Constants.Resource.HasLabel, properties.ContainsLabel);
            containsActions.Add(Graph.Metadata.Constants.EnterpriseCore.PidUri, properties.ContainsPidUri);
            containsActions.Add(Graph.Metadata.Constants.Resource.HasResourceDefintion, properties.ContainsResourceDefinition);
            containsActions.Add(Graph.Metadata.Constants.Resource.LifecycleStatus, properties.ContainsLifecycleStatus);
            containsActions.Add(Graph.Metadata.Constants.Resource.HasInformationClassification, properties.ContainsInformationClassification);
            containsActions.Add(Graph.Metadata.Constants.Resource.LastChangeUser, properties.ContainsLastChangeUser);
            containsActions.Add(Graph.Metadata.Constants.Resource.Author, properties.ContainsAuthor);
            containsActions.Add("https://pid.bayer.com/kos/19050/containsLicensedData", properties.ContainsLicensedData);
            containsActions.Add(Graph.Metadata.Constants.Resource.HasVersion, properties.ContainsVersion);
            containsActions.Add(Graph.Metadata.Constants.RDF.Type, properties.ContainsType);
            containsActions.Add("https://pid.bayer.com/kos/19050/isDerivedFromDataset", properties.ContainsIsDerivedFromDataset);
            containsActions.Add("https://pid.bayer.com/kos/19050/isPersonalData", properties.ContainsIsPersonalData);
            containsActions.Add(Graph.Metadata.Constants.Resource.HasConsumerGroup, properties.ContainsHasConsumerGroup); 
            containsActions.Add(Graph.Metadata.Constants.Resource.HasHistoricVersion, properties.ContainsHasHistoricVersion); 
            containsActions.Add(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus, properties.ContainsEntryLifecycleStatus);
            containsActions.Add(Graph.Metadata.Constants.Resource.DateModified, properties.CheckDateModified);
            containsActions.Add(Graph.Metadata.Constants.Resource.DateCreated, properties.CheckDateCreated);

            return containsActions;
        }

        public static bool ContainsLabel(this IDictionary<string, List<dynamic>> property, string label)
        {
            return ContainsSingleValue(property, Graph.Metadata.Constants.Resource.HasLabel, label);
        }

        public static bool ContainsPidUri(this IDictionary<string, List<dynamic>> property, string pidUri)
        {
            
            return ContainsSingleValue(property, Graph.Metadata.Constants.EnterpriseCore.PidUri, pidUri);
        }

        public static bool ContainsResourceDefinition(this IDictionary<string, List<dynamic>> property, string resourceDefinition)
        {
            return ContainsSingleValue(property, Graph.Metadata.Constants.Resource.HasResourceDefintion, resourceDefinition);
        }

        public static bool ContainsLifecycleStatus(this IDictionary<string, List<dynamic>> property, string status)
        {
            return ContainsSingleValue(property, Graph.Metadata.Constants.Resource.LifecycleStatus, status);
        }
        public static bool ContainsLifecycleStatus(this IDictionary<string, List<dynamic>> property, LifecycleStatus status)
        {
            return ContainsSingleValue(property, Graph.Metadata.Constants.Resource.LifecycleStatus, status.GetDescription());
        }

        public static bool ContainsInformationClassification(this IDictionary<string, List<dynamic>> property, string informationClassification)
        {
            return ContainsSingleValue(property, Graph.Metadata.Constants.Resource.HasInformationClassification, informationClassification);
        }

        public static bool ContainsLastChangeUser(this IDictionary<string, List<dynamic>> property, string lastChangeUser)
        {
            return ContainsSingleValue(property, Graph.Metadata.Constants.Resource.LastChangeUser, lastChangeUser);
        }

        public static bool ContainsAuthor(this IDictionary<string, List<dynamic>> property, string author)
        {
            return ContainsSingleValue(property, Graph.Metadata.Constants.Resource.Author, author);
        }

        public static bool ContainsLicensedData(this IDictionary<string, List<dynamic>> property, string licenseData)
        {
            return ContainsSingleValue(property, "https://pid.bayer.com/kos/19050/containsLicensedData", licenseData);
        }

        public static bool ContainsVersion(this IDictionary<string, List<dynamic>> property, string version)
        {
            return ContainsSingleValue(property, Graph.Metadata.Constants.Resource.HasVersion, version);
        }

        public static bool ContainsType(this IDictionary<string, List<dynamic>> property, string type)
        {
            return ContainsSingleValue(property, Graph.Metadata.Constants.RDF.Type, type);
        }

        public static bool ContainsIsDerivedFromDataset(this IDictionary<string, List<dynamic>> property, string derivedFromDataset)
        {
            return ContainsSingleValue(property, "https://pid.bayer.com/kos/19050/isDerivedFromDataset", derivedFromDataset);
        }

        public static bool ContainsIsPersonalData(this IDictionary<string, List<dynamic>> property, string personalData)
        {
            return ContainsSingleValue(property, "https://pid.bayer.com/kos/19050/isPersonalData", personalData);
        }

        public static bool ContainsHasConsumerGroup(this IDictionary<string, List<dynamic>> property, string consumerGroup)
        {
            return ContainsSingleValue(property, Graph.Metadata.Constants.Resource.HasConsumerGroup, consumerGroup);
        }

        public static bool ContainsHasHistoricVersion(this IDictionary<string, List<dynamic>> property, string historicVersion)
        {
            return ContainsSingleValue(property, Graph.Metadata.Constants.Resource.HasHistoricVersion, historicVersion);
        }

        public static bool ContainsEntryLifecycleStatus(this IDictionary<string, List<dynamic>> property, string entryLifecycleStatus)
        {
            return ContainsSingleValue(property, Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus, entryLifecycleStatus);
        }
        
        public static bool ContainsEntryLifecycleStatus(this IDictionary<string, List<dynamic>> property, ColidEntryLifecycleStatus entryLifecycleStatus)
        {
            return ContainsSingleValue(property, Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus, entryLifecycleStatus.GetDescription());
        }

        public static bool ContainsChangeRequester(this IDictionary<string, List<dynamic>> property, string changeRequester)
        {
            return ContainsSingleValue(property, Graph.Metadata.Constants.Resource.ChangeRequester, changeRequester);
        }

        public static bool CheckDateCreated(this IDictionary<string, List<dynamic>> property, string datetime)
        {
            return DatetimeIsBetweenActualAndExpectedValue(property, Graph.Metadata.Constants.Resource.DateCreated, datetime);
        }

        public static bool CheckDateModified(this IDictionary<string, List<dynamic>> property, string datetime)
        {
            return DatetimeIsBetweenActualAndExpectedValue(property, Graph.Metadata.Constants.Resource.DateModified, datetime);
        }

        // TODO: Add datetime check
        public static bool DatetimeIsBetweenActualAndExpectedValue(this IDictionary<string, List<dynamic>> property, string constant, dynamic valueToCheck)
        {
            if (property.TryGetValue(constant, out var outVal))
            {
                Assert.Single(outVal);
                var firstOutVal = outVal.First();
                return true;
                //return Convert.ToDateTime(valueToCheck) >= Convert.ToDateTime(firstOutVal) &&
                //       Convert.ToDateTime(valueToCheck) <= Convert.ToDateTime(new DateTime().ToString("o"));
            }
            return false;
        }

        private static bool ContainsSingleValue(this IDictionary<string, List<dynamic>> property, string constant, string valueToCheck)
        {
            if (property.TryGetValue(constant, out var outVal))
            {
                Assert.Single(outVal);
                return outVal.First().ToString().Equals(valueToCheck);
            }
            return false;
        }
    }
}
