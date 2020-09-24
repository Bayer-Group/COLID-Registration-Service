
using COLID.Graph.Metadata.DataModels.Metadata;

namespace COLID.Graph.Tests.Builder
{
    public class MetadataPropertyGroupBuilder
    {
        private MetadataPropertyGroup _prop = new MetadataPropertyGroup();

        public MetadataPropertyGroup Build()
        {
            return _prop;
        }

        public MetadataPropertyGroup GenerateSampleTechnicalInformationGroup()
        {
            return new MetadataPropertyGroup()
            {
                Key = Graph.Metadata.Constants.Resource.Groups.TechnicalInformation,
                Label = "Technical Information",
                Order = 997,
                EditDescription = "Grouping all not editable technical Information",
                ViewDescription = "Grouping all not editable technical Information"
            };
        }

        public MetadataPropertyGroup GenerateSampleSecurityAccessInformationGroup()
        {
            return new MetadataPropertyGroup()
            {
                Key = Graph.Metadata.Constants.Resource.Groups.SecurityAccess,
                Label = "Security and Access",
                Order = 7,
                EditDescription = "Grouping all Information for security and Access",
                ViewDescription = "Grouping all Information for security and Access"
            };
        }

        public MetadataPropertyGroup GenerateSampleUsageAndMaintenanceInformationGroup()
        {
            return new MetadataPropertyGroup()
            {
                Key = Graph.Metadata.Constants.Resource.Groups.UsageAndMaintenance,
                Label = "Usage and Maintenance",
                Order = 6,
                EditDescription = "Grouping all Information for Usage and Maintenance",
                ViewDescription = "Grouping all Information for Usage and Maintenance"
            };
        }

        public MetadataPropertyGroup GenerateSampleLinkTypesGroup()
        {
            return new MetadataPropertyGroup()
            {
                Key = Graph.Metadata.Constants.Resource.Groups.LinkTypes,
                Label = "Linked Resources",
                Order = 9,
                EditDescription = "A group for all link types between resources",
                ViewDescription = "Grouping all link types"
            };
        }

        public MetadataPropertyGroup GenerateSampleDistributionEndpointGroup()
        {
            return new MetadataPropertyGroup()
            {
                Key = Graph.Metadata.Constants.Resource.Groups.DistributionEndpoints,
                Label = "Distribution Endpoints",
                Order = 10,
                EditDescription = "Grouping all information for distribution endpoints",
                ViewDescription = "Grouping all information for distribution endpoints"
            };
        }

        public MetadataPropertyGroupBuilder WithKey(string key)
        {
            _prop.Key = key;
            return this;
        }

        public MetadataPropertyGroupBuilder WithLabel(string label)
        {
            _prop.Label = label;
            return this;
        }

        public MetadataPropertyGroupBuilder WithOrder(decimal order)
        {
            _prop.Order = order;
            return this;
        }

        public MetadataPropertyGroupBuilder WithEditDescription(string editDescription)
        {
            _prop.EditDescription = editDescription;
            return this;
        }

        public MetadataPropertyGroupBuilder WithViewDescription(string viewDescription)
        {
            _prop.ViewDescription = viewDescription;
            return this;
        }
    }
}
