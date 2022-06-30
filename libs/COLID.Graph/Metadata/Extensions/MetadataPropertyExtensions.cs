using Newtonsoft.Json.Linq;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.TripleStore.Extensions;
using System.Collections.Generic;
using COLID.Graph.Metadata.Constants;

namespace COLID.Graph.Metadata.Extensions
{
    public static class MetadataPropertyExtensions
    {
        public static bool IsControlledVocabulary(this MetadataProperty metadataProperty, out string range)
        {
            range = metadataProperty.Properties.GetValueOrNull(Constants.Shacl.Range, true);

            if (string.IsNullOrWhiteSpace(range) || range == Constants.Identifier.Type)
            {
                return false;
            }

            string hasPid = metadataProperty.Properties.GetValueOrNull(Constants.EnterpriseCore.PidUri, true);

            if (hasPid == Constants.Resource.Attachment || 
                hasPid == Constants.Resource.MainDistribution ||
                hasPid == Constants.Resource.Distribution ||
                hasPid == Constants.Resource.BaseUri ||
                hasPid == Constants.RDF.Type ||
                //hasPid == Constants.Resource.HasHistoricVersion ||
                hasPid == Constants.Resource.HasLaterVersion ||
                hasPid == Constants.Resource.MetadataGraphConfiguration)
            {
                return false;
            }

            var groupKey = metadataProperty.GetMetadataPropertyGroup()?.Key;

            if (groupKey == Constants.Resource.Groups.LinkTypes)
            {
                return false;
            }

            return metadataProperty.Properties.GetValueOrNull(Constants.Shacl.NodeKind, true) == Constants.Shacl.NodeKinds.IRI;
        }

        public static bool IsMultipleValue(this MetadataProperty metadataProperty)
        {
            metadataProperty.Properties.TryGetValue("http://www.w3.org/ns/shacl#maxCount", out var maxCountString);

            if (string.IsNullOrWhiteSpace(maxCountString))
            {
                return true;
            }

            if (!(int.TryParse(maxCountString, out int maxCount) && maxCount <= 1))
            {
                return true;
            }

            return false;
        }

        public static MetadataPropertyGroup GetMetadataPropertyGroup(this MetadataProperty metadataProperty)
        {
            return GetMetadataPropertyGroup(metadataProperty?.Properties);
        }

        public static MetadataPropertyGroup GetMetadataPropertyGroup(this IDictionary<string, dynamic> metadataProperties)
        {
            MetadataPropertyGroup metadataGroup = null;
            if (metadataProperties != null && metadataProperties.TryGetValue(Shacl.Group, out var groupValue))
            {
                if (groupValue is JObject)
                {
                    var jsonObject = groupValue as JObject;
                    metadataGroup = jsonObject.ToObject<MetadataPropertyGroup>();
                }
                else if (groupValue is MetadataPropertyGroup)
                {
                    metadataGroup = groupValue;
                }
            }

            return metadataGroup;
        }

        public static bool IsTechnicalMetadataProperty(this MetadataProperty metadataProperty)
        {
            var technicalGroups = new List<string>() { Constants.Resource.Groups.TechnicalInformation, Constants.Resource.Groups.InvisibleTechnicalInformation };
            var group = metadataProperty.GetMetadataPropertyGroup();

            return group != null && technicalGroups.Contains(group.Key);
        }
    }
}
