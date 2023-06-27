using System;
using System.Collections.Generic;
using System.Linq;
using COLID.Common.Extensions;
using COLID.RegistrationService.Common.DataModel.Resources;
using COLID.RegistrationService.Common.Enums.ColidEntry;
using COLID.RegistrationService.Common.Extensions;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.Metadata.DataModels.Resources;

namespace COLID.RegistrationService.Tests.Common.Builder
{
    public class ResourceBuilder : AbstractEntityBuilder<Resource>
    {
        private Resource _res = new Resource();

        private string hasLifecycleStatus = Graph.Metadata.Constants.Resource.LifecycleStatus;
        private string hasDataSteward = Graph.Metadata.Constants.Resource.HasDataSteward;

        public override Resource Build()
        {
            _res.Properties = _prop;
            return _res;
        }

        public ResourceRequestDTO BuildRequestDto()
        {
            return new ResourceRequestDTO()
            {
                Properties = _prop
            };
        }

        public ResourceRequestDTO BuildRequestDto(string pidUriString, string uriTemplate)
        {
            WithPidUri(pidUriString, uriTemplate);
            return new ResourceRequestDTO()
            {
                Properties = _prop
            };
        }

        /// <summary>
        /// <b>Caution</b>: may override existing content! To use it right:
        /// <code>new ResourceBuilder().GenerateSampleData().With ... .Build()</code>
        /// </summary>
        public ResourceBuilder GenerateSampleData()
        {
            AddSampleData();
            return this;
        }

        public ResourceBuilder GenerateSampleData(string pidUriString, string uriTemplate)
        {
            AddSampleData();
            WithPidUri(pidUriString, uriTemplate);
            return this;
        }

        private void AddSampleData()
        {
            WithId(Graph.Metadata.Constants.Entity.IdPrefix + Guid.NewGuid());

            // Properties:
            WithLabel("Some fancy resource label");
            WithEntryLifecycleStatus(ColidEntryLifecycleStatus.Draft);
            WithLifecycleStatus(LifecycleStatus.Released);
            WithAuthor("superadmin@bayer.com");
            WithVersion("4");
            WithKeyword("https://pid.bayer.com/kos/19050#a27e2b58-9c10-4128-8d68-3b4b05811da1");
            WithInformationClassification("https://pid.bayer.com/kos/19050/Restricted");
            WithLastChangeUser("superadmin@bayer.com");
            WithConsumerGroup($"{Graph.Metadata.Constants.Entity.IdPrefix}bf2f8eeb-fdb9-4ee1-ad88-e8932fa8753c");
            WithPidUri($"https://pid.bayer.com/pid/{Guid.NewGuid()}");
            WithLastChangeDateTime(DateTime.UtcNow.ToString("o"));
            WithDateCreated(DateTime.UtcNow.ToString("o"));
            WithType("https://pid.bayer.com/kos/19050/GenericDataset");
            WithResourceDefinition("version 4");
            HasPersonalData(true);
            HasLicensedData(false);
        }

        public ResourceBuilder WithDistributionEndpoint(Entity de)
        {
            CreateOrOverwriteProperty(Graph.Metadata.Constants.Resource.Distribution, de);
            return this;
        }

        public ResourceBuilder WithDistributionEndpoint(IList<Entity> des)
        {
            CreateOrOverwriteMultiProperty(Graph.Metadata.Constants.Resource.Distribution, des.Cast<dynamic>().ToList());
            return this;
        }

        public ResourceBuilder WithMainDistributionEndpoint(Entity mainDe)
        {
            CreateOrOverwriteProperty(Graph.Metadata.Constants.Resource.MainDistribution, mainDe);
            return this;
        }

        public ResourceBuilder WithId(string id)
        {
            _res.Id = id;
            return this;
        }

        public ResourceBuilder WithLabel(string label)
        {
            CreateOrOverwriteProperty(Graph.Metadata.Constants.Resource.HasLabel, label);
            return this;
        }

        public ResourceBuilder WithLabel(params string[] label)
        {
            CreateOrOverwriteMultiProperty(Graph.Metadata.Constants.Resource.HasLabel, label.Cast<dynamic>().ToList());
            return this;
        }

        public ResourceBuilder WithEntryLifecycleStatus(ColidEntryLifecycleStatus status)
        {
            CreateOrOverwriteProperty(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus, status.GetDescription());
            return this;
        }

        public ResourceBuilder WithLifecycleStatus(LifecycleStatus status)
        {
            CreateOrOverwriteProperty(hasLifecycleStatus, status.GetDescription());
            return this;
        }

        public ResourceBuilder WithAuthor(string authorEmail)
        {
            CreateOrOverwriteProperty(Graph.Metadata.Constants.Resource.Author, authorEmail);
            return this;
        }

        public ResourceBuilder WithDataSteward(string dataStewardEmail)
        {
            CreateOrOverwriteProperty(hasDataSteward, dataStewardEmail);
            return this;
        }

        public ResourceBuilder WithVersion(string version)
        {
            CreateOrOverwriteProperty(Graph.Metadata.Constants.Resource.HasVersion, version);
            return this;
        }

        public ResourceBuilder WithVersion(params string[] version)
        {
            CreateOrOverwriteMultiProperty(Graph.Metadata.Constants.Resource.HasVersion, version.Cast<dynamic>().ToList());
            return this;
        }

        public ResourceBuilder WithInformationClassification(string informationClassification)
        {
            CreateOrOverwriteProperty(Graph.Metadata.Constants.Resource.HasInformationClassification, informationClassification);
            return this;
        }

        public ResourceBuilder WithLastChangeUser(string userEmail)
        {
            CreateOrOverwriteProperty(Graph.Metadata.Constants.Resource.LastChangeUser, userEmail);
            return this;
        }

        public ResourceBuilder WithConsumerGroup(string consumerGroup)
        {
            CreateOrOverwriteProperty(Graph.Metadata.Constants.Resource.HasConsumerGroup, consumerGroup);
            return this;
        }

        public ResourceBuilder WithLastChangeDateTime(string dateTime)
        {
            CreateOrOverwriteProperty(Graph.Metadata.Constants.Resource.DateModified, dateTime);
            return this;
        }

        public ResourceBuilder WithDateCreated(string dateCreated)
        {
            CreateOrOverwriteProperty(Graph.Metadata.Constants.Resource.DateCreated, dateCreated);
            return this;
        }

        public ResourceBuilder WithKeyword(string keyword)
        {
            CreateOrOverwriteProperty(Graph.Metadata.Constants.Resource.Keyword, keyword);
            return this;
        }

        public ResourceBuilder WithType(string resourceType)
        {
            CreateOrOverwriteProperty(Graph.Metadata.Constants.RDF.Type, resourceType);
            return this;
        }

        public ResourceBuilder WithLaterVersion(string laterVersionUri)
        {
            CreateOrOverwriteProperty(Graph.Metadata.Constants.Resource.HasLaterVersion, laterVersionUri);
            return this;
        }

        public ResourceBuilder WithResourceDefinition(string resourceDefinition)
        {
            CreateOrOverwriteProperty(Graph.Metadata.Constants.Resource.HasResourceDefintion, resourceDefinition);
            return this;
        }

        public ResourceBuilder WithResourceDefinition(params string[] resourceDefinition)
        {
            CreateOrOverwriteMultiProperty(Graph.Metadata.Constants.Resource.HasResourceDefintion, resourceDefinition.Cast<dynamic>().ToList());
            return this;
        }

        public ResourceBuilder WithCopyOfDataset(string linkedPidUri)
        {
            CreateOrOverwriteProperty(Graph.Metadata.Constants.Resource.IsCopyOfDataset, linkedPidUri);
            return this;
        }

        public ResourceBuilder WithCopyOfDataset(params string[] linkedPidUris)
        {
            CreateOrOverwriteMultiProperty(Graph.Metadata.Constants.Resource.IsCopyOfDataset, linkedPidUris.Cast<dynamic>().ToList());
            return this;
        }

        public ResourceBuilder HasPersonalData(bool val)
        {
            CreateOrOverwriteProperty("https://pid.bayer.com/kos/19050/isPersonalData", val.ToString().ToLower());
            return this;
        }

        public ResourceBuilder HasLicensedData(bool val)
        {
            CreateOrOverwriteProperty("https://pid.bayer.com/kos/19050/containsLicensedData", val.ToString().ToLower());
            return this;
        }

       /* public ResourceBuilder WithHistoricVersion(string pidUri)
        {
            CreateOrOverwriteProperty(Graph.Metadata.Constants.Resource.HasHistoricVersion, pidUri);
            return this;
        }*/

        // === TODO === //

        public new ResourceBuilder WithPidUri(string pidUriString, string uriTemplate = "https://pid.bayer.com/kos/19050#14d9eeb8-d85d-446d-9703-3a0f43482f5a")
        {
            // Create properties for Pid Uri
            return WithPermanentIdentifier(pidUriString, Graph.Metadata.Constants.EnterpriseCore.PidUri, uriTemplate);
        }

        public ResourceBuilder WithBaseUri(string baseUriString, string uriTemplate = "https://pid.bayer.com/kos/19050#14d9eeb8-d85d-446d-9703-3a0f43482f5a")
        {
            // Create properties for Base Uri
            return WithPermanentIdentifier(baseUriString, Graph.Metadata.Constants.Resource.BaseUri, uriTemplate);
        }

        public ResourceBuilder WithMetadataGraphConfiguration(string config)
        {
            // Create properties for metadata graph configuration
            CreateOrOverwriteProperty(Graph.Metadata.Constants.Resource.MetadataGraphConfiguration, config);
            return this;
        }

        private ResourceBuilder WithPermanentIdentifier(string pidUriString, string identifierType, string uriTemplate)
        {
            // Create properties for Pid Uri
            IDictionary<string, List<dynamic>> pidUriProp = new Dictionary<string, List<dynamic>>();
            pidUriProp.Add(Graph.Metadata.Constants.RDF.Type, new List<dynamic>() { Graph.Metadata.Constants.Identifier.Type });

            if (!string.IsNullOrWhiteSpace(uriTemplate))
            {
                pidUriProp.Add(Graph.Metadata.Constants.Identifier.HasUriTemplate, new List<dynamic>() { uriTemplate });
            }

            // Create Entity and assign properties to PID Uri
            Entity pidUri = new Entity(pidUriString, pidUriProp);

            // Create properties for resource
            CreateOrOverwriteProperty(identifierType, pidUri);

            return this;
        }
    }
}
