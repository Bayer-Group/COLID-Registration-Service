using System.Collections.Generic;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.TripleStore.DataModels.Resources;

namespace COLID.RegistrationService.Services.Validation.Models
{
    public class EntityValidationFacade
    {
        public ResourceCrudAction ResourceCrudAction { get; set; }
        public Resource RequestResource { get; set; }
        public ResourcesCTO ResourcesCTO { get; set; }
        public string PreviousVersion { get; set; }
        public IList<MetadataProperty> MetadataProperties { get; }
        public string ConsumerGroup { get; set; }
        public IList<ValidationResultProperty> ValidationResults { get; }

        public EntityValidationFacade(ResourceCrudAction resourceCrucAction,
            Resource requestResource,
            ResourcesCTO resourcesCTO,
            string previousVersion,
            IList<MetadataProperty> metadataProperties,
            string consumerGroup)
        {
            ResourceCrudAction = resourceCrucAction;
            RequestResource = requestResource;
            ResourcesCTO = resourcesCTO;
            PreviousVersion = previousVersion;
            MetadataProperties = metadataProperties;
            ConsumerGroup = consumerGroup;
            ValidationResults = new List<ValidationResultProperty>();
        }
    }
}
