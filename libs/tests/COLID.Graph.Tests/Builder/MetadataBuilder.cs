using System.Collections.Generic;
using System.Linq;
using COLID.Graph.Metadata.DataModels.Metadata;
using VDS.RDF.Parsing.Tokens;

namespace COLID.Graph.Tests.Builder
{
    public class MetadataBuilder
    {
        private IList<MetadataProperty> _prop = new List<MetadataProperty>();

        public IList<MetadataProperty> Build()
        {
            return _prop;
        }

        // TODO: Add all metadata properties
        public MetadataBuilder GenerateSampleResourceData(string entityType = "")
        {
            if (entityType == Graph.Metadata.Constants.Resource.Type.Ontology)
            {
                GenerateSampleBaseUri();
            }
            else if (entityType == Graph.Metadata.Constants.Resource.Type.MathematicalModel)
            {
                GenerateSampleHasMathematicalModelCategory();
            }

            GenerateSampleEntryLifceCycleStatus();
            GenerateSampleDateCreated();
            GenerateSampleDateModified();
            GenerateSamplePidUri();
            GenerateSampleAuthor();
            GenerateSampleLastChangeUser();
            GenerateSampleConsumerGroup();
            GenerateSampleKeyword();
            GenerateSampleIsCopyOfDataset();
            GenerateSampleHasInformationClassifikation();
            GenerateSampleType();

            return this;
        }

        public MetadataBuilder GenerateSampleEndpointData()
        {
            GenerateSamplePidUri();
            GenerateSampleType();
            GenerateSampleHasNetworkResourceLabel();
            GenerateSampleHasContactPerson();
            GenerateSampleHasNetworkAdress();

            return this;
        }

        public Graph.Metadata.DataModels.Metadata.Metadata BuildBrowsableResource()
        {
            return BuildEndpointMetadata("http://pid.bayer.com/kos/19014/BrowsableResource", "Browsable Resource", "A networked resource that can be opened in a web browser if access is granted e.g. a HTML documentation page of a resource.");
        }

        public Graph.Metadata.DataModels.Metadata.Metadata BuildQueryEndpoint()
        {
            return BuildEndpointMetadata("http://pid.bayer.com/kos/19014/QueryEndpoint", "Query Endpoint", "A networked resource that supports a query language e.g. SQL, SPARQL, CYPHER (might require valid credentials) based on a well-defined protocol.");
        }

        public Graph.Metadata.DataModels.Metadata.Metadata BuildEndpointMetadata(string key, string label, string description)
        {
            var metadata = new Graph.Metadata.DataModels.Metadata.Metadata(key, label, description, _prop);
            return metadata;
        }

        // Is also used for endpoints. Same data except label and comments
        public MetadataBuilder GenerateSampleType()
        {
            var dateCreatedProperty = new MetadataPropertyBuilder()
                .WithPidUri(Graph.Metadata.Constants.RDF.Type)
                .WithGroup(new MetadataPropertyGroupBuilder().GenerateSampleTechnicalInformationGroup())
                .WithPath(Graph.Metadata.Constants.RDF.Type)
                .WithNodekind(Graph.Metadata.Constants.Shacl.NodeKinds.IRI)
                .WithRange(Graph.Metadata.Constants.OWL.Class)
                .WithLabel("Resource Type")
                .WithName("Resource Type")
                .WithMaxCount("1")
                .WithMinCount("1")
                .WithOrder("50")
                .WithDomain(Graph.Metadata.Constants.PIDO.PidConcept)
                .Build();

            _prop.Add(dateCreatedProperty);

            return this;
        }

        public MetadataBuilder GenerateSampleEntryLifceCycleStatus()
        {
            var dateCreatedProperty = new MetadataPropertyBuilder()
                .WithPidUri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus)
                .WithGroup(new MetadataPropertyGroupBuilder().GenerateSampleTechnicalInformationGroup())
                .WithPath(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus)
                .WithType(Graph.Metadata.Constants.OWL.ObjectProperty)
                .WithNodekind(Graph.Metadata.Constants.Shacl.NodeKinds.IRI)
                .WithRange(Graph.Metadata.Constants.Resource.Type.PIDEntryLifecycleStatus)
                .WithLabel("hasEntryLifecycleStatus")
                .WithName("Entry Lifecycle Status")
                .WithMaxCount("1")
                .WithMinCount("1")
                .WithOrder("1")
                .Build();

            _prop.Add(dateCreatedProperty);

            return this;
        }

        public MetadataBuilder GenerateSampleHasInformationClassifikation()
        {
            var dateCreatedProperty = new MetadataPropertyBuilder()
                .WithPidUri(Graph.Metadata.Constants.Resource.HasInformationClassification)
                .WithGroup(new MetadataPropertyGroupBuilder().GenerateSampleSecurityAccessInformationGroup())
                .WithPath(Graph.Metadata.Constants.Resource.HasInformationClassification)
                .WithType(Graph.Metadata.Constants.OWL.ObjectProperty)
                .WithNodekind(Graph.Metadata.Constants.Shacl.NodeKinds.IRI)
                .WithRange(Graph.Metadata.Constants.Resource.Type.InformationClassification)
                .WithLabel("has information classification")
                .WithName("Information Classification")
                .WithMaxCount("1")
                .WithMinCount("1")
                .WithOrder("4")
                .WithDomain(Graph.Metadata.Constants.PIDO.PidConcept)
                .Build();

            _prop.Add(dateCreatedProperty);

            return this;
        }

        public MetadataBuilder GenerateSampleMathematicalCategory()
        {
            var dateCreatedProperty = new MetadataPropertyBuilder()
                .WithPidUri(Graph.Metadata.Constants.Resource.MathematicalModelCategory)
                .WithGroup(new MetadataPropertyGroupBuilder().GenerateSampleUsageAndMaintenanceInformationGroup())
                .WithPath(Graph.Metadata.Constants.Resource.MathematicalModelCategory)
                .WithType(Graph.Metadata.Constants.OWL.ObjectProperty)
                .WithNodekind(Graph.Metadata.Constants.Shacl.NodeKinds.IRI)
                .WithRange(Graph.Metadata.Constants.Resource.Type.MathematicalModelCategory)
                .WithLabel("hasMathematicalModelCategory")
                .WithName("Mathematical Model Category")
                .WithMaxCount("7")
                .WithMinCount("1")
                .WithOrder("3")
                .WithDomain(Graph.Metadata.Constants.Resource.Type.MathematicalModel)
                .WithFieldType(Graph.Metadata.Constants.PIDO.Shacl.FieldTypes.Hierarchy)
                .Build();

            _prop.Add(dateCreatedProperty);

            return this;
        }

        public MetadataBuilder GenerateSampleDateCreated()
        {
            var dateCreatedProperty = new MetadataPropertyBuilder()
                .WithPidUri(Graph.Metadata.Constants.Resource.DateCreated)
                .WithGroup(new MetadataPropertyGroupBuilder().GenerateSampleTechnicalInformationGroup())
                .WithPath(Graph.Metadata.Constants.Resource.DateCreated)
                .WithType(Graph.Metadata.Constants.OWL.DatatypeProperty)
                .WithNodekind(Graph.Metadata.Constants.Shacl.NodeKinds.Literal)
                .WithRange(Graph.Metadata.Constants.DataTypes.DateTime)
                .WithDataType(Graph.Metadata.Constants.DataTypes.DateTime)
                .WithLabel("date created")
                .WithName("Date Created")
                .WithMaxCount("1")
                .WithMinCount("1")
                .WithOrder("7")
                .WithComment("<small class=\"form-text text-muted\">Creation date/time.</small>")
                .WithDomain(Graph.Metadata.Constants.PIDO.PidConcept)
                .WithDescription("Creation date/time.")
                .Build();

            _prop.Add(dateCreatedProperty);

            return this;
        }

        public MetadataBuilder GenerateSampleDateModified()
        {
            var dateCreatedProperty = new MetadataPropertyBuilder()
                .WithPidUri(Graph.Metadata.Constants.Resource.DateModified)
                .WithGroup(new MetadataPropertyGroupBuilder().GenerateSampleTechnicalInformationGroup())
                .WithPath(Graph.Metadata.Constants.Resource.DateModified)
                .WithType(Graph.Metadata.Constants.OWL.DatatypeProperty)
                .WithNodekind(Graph.Metadata.Constants.Shacl.NodeKinds.Literal)
                .WithRange(Graph.Metadata.Constants.DataTypes.DateTime)
                .WithDataType(Graph.Metadata.Constants.DataTypes.DateTime)
                .WithLabel("date modified")
                .WithName("Date Modified")
                .WithMaxCount("1")
                .WithMinCount("1")
                .WithOrder("7")
                .WithComment("Last change datetime")
                .WithDomain(Graph.Metadata.Constants.PIDO.PidConcept)
                .WithDescription("Last change datetime")
                .Build();

            _prop.Add(dateCreatedProperty);

            return this;
        }

        public MetadataBuilder GenerateSamplePidUri()
        {
            var pidUriProperty = new MetadataPropertyBuilder()
                .WithPidUri(Graph.Metadata.Constants.EnterpriseCore.PidUri)
                .WithGroup(new MetadataPropertyGroup()
                {
                    Key = "https://pid.bayer.com/kos/19050/PIDURI",
                    Label = "PIDURI",
                    Order = 1,
                    EditDescription = string.Empty,
                    ViewDescription = string.Empty
                })
                .WithPath(Graph.Metadata.Constants.EnterpriseCore.PidUri)
                .WithType(Graph.Metadata.Constants.OWL.ObjectProperty)
                .WithNodekind(Graph.Metadata.Constants.Shacl.NodeKinds.IRI)
                .WithRange(Graph.Metadata.Constants.Identifier.Type)
                .WithLabel("has PID")
                .WithName("PID URI")
                .WithMaxCount("1")
                .WithMinCount("1")
                .WithOrder("1")
                .WithComment("This property links a resource to its permanent identifier.")
                .WithDomain(Graph.Metadata.Constants.PIDO.Resource)
                .WithDescription("This property links a resource to its permanent identifier.")
                .Build();

            _prop.Add(pidUriProperty);

            return this;
        }

        public MetadataBuilder GenerateSampleBaseUri()
        {
            var pidUriProperty = new MetadataPropertyBuilder()
                .WithPidUri(Graph.Metadata.Constants.Resource.BaseUri)
                .WithGroup(new MetadataPropertyGroup() { Key = "https://pid.bayer.com/kos/19050/PIDURI", Label = "PIDURI", Order = 1, EditDescription = string.Empty, ViewDescription = "" })
                .WithPath(Graph.Metadata.Constants.Resource.BaseUri)
                .WithType(Graph.Metadata.Constants.OWL.ObjectProperty)
                .WithNodekind(Graph.Metadata.Constants.Shacl.NodeKinds.IRI)
                .WithRange(Graph.Metadata.Constants.Identifier.Type)
                .WithLabel("has base URI")
                .WithName("Base Uri")
                .WithMaxCount("1")
                .WithMinCount("1")
                .WithOrder("2")
                .WithComment("Any RDF based ontology needs to have a base URI which is used to create the URIs for the different concepts, properties and instances. The base URI of the PID application ontology is https://pid.bayer.com/kos/19050/.")
                .WithDomain(Graph.Metadata.Constants.PIDO.Resource)
                .Build();

            _prop.Add(pidUriProperty);

            return this;
        }

        public MetadataBuilder GenerateSampleAuthor()
        {
            var pidUriProperty = new MetadataPropertyBuilder()
                .WithPidUri(Graph.Metadata.Constants.Resource.Author)
                .WithGroup(new MetadataPropertyGroupBuilder().GenerateSampleTechnicalInformationGroup())
                .WithPath(Graph.Metadata.Constants.Resource.Author)
                .WithRange(Graph.Metadata.Constants.Person.Type)
                .WithType(Graph.Metadata.Constants.OWL.ObjectProperty)
                .WithNodekind(Graph.Metadata.Constants.Shacl.NodeKinds.Literal)
                .WithLabel("author")
                .WithName("Author")
                .WithMaxCount("1")
                .WithMinCount("1")
                .WithOrder("6")
                .WithComment("The person who registered the resource in PID (e.g. identified by the email address).")
                .WithDomain(Graph.Metadata.Constants.PIDO.PidConcept)
                .WithDescription("The person who registered the resource in PID (e.g. identified by the email address).")
                .Build();

            _prop.Add(pidUriProperty);

            return this;
        }

        public MetadataBuilder GenerateSampleLastChangeUser()
        {
            var pidUriProperty = new MetadataPropertyBuilder()
                .WithPidUri(Graph.Metadata.Constants.Resource.LastChangeUser)
                .WithGroup(new MetadataPropertyGroupBuilder().GenerateSampleTechnicalInformationGroup())
                .WithPath(Graph.Metadata.Constants.Resource.LastChangeUser)
                .WithRange(Graph.Metadata.Constants.Person.Type)
                .WithType(Graph.Metadata.Constants.OWL.ObjectProperty)
                .WithNodekind(Graph.Metadata.Constants.Shacl.NodeKinds.Literal)
                .WithLabel("last change user")
                .WithName("Last Change User")
                .WithMaxCount("1")
                .WithMinCount("1")
                .WithOrder("20")
                .WithComment("This is the relation to the user who made the last change to the PID entry (e.g. identified by the email address).")
                .WithDomain(Graph.Metadata.Constants.PIDO.PidConcept)
                .WithDescription("This is the relation to the user who made the last change to the PID entry (e.g. identified by the email address).")
                .Build();

            _prop.Add(pidUriProperty);

            return this;
        }

        public MetadataBuilder GenerateSampleConsumerGroup()
        {
            var pidUriProperty = new MetadataPropertyBuilder()
                .WithPidUri(Graph.Metadata.Constants.Resource.HasConsumerGroup)
                .WithGroup(new MetadataPropertyGroupBuilder().GenerateSampleTechnicalInformationGroup())
                .WithPath(Graph.Metadata.Constants.Resource.HasConsumerGroup)
                .WithType(Graph.Metadata.Constants.OWL.ObjectProperty)
                .WithNodekind(Graph.Metadata.Constants.Shacl.NodeKinds.IRI)
                .WithLabel("hasConsumerGroup")
                .WithName("Consumer Group")
                .WithMaxCount("1")
                .WithMinCount("1")
                .WithOrder("3")
                .WithComment("The consumer group related to the resource.")
                .WithDomain(Graph.Metadata.Constants.PIDO.PidConcept)
                .WithDescription("The consumer group related to the resource.")
                .WithRange(Graph.Metadata.Constants.ConsumerGroup.Type)
                .Build();

            _prop.Add(pidUriProperty);

            return this;
        }

        public MetadataBuilder GenerateSampleKeyword()
        {
            var pidUriProperty = new MetadataPropertyBuilder()
                .WithPidUri(Graph.Metadata.Constants.Resource.Keyword)
                .WithGroup(new MetadataPropertyGroupBuilder().GenerateSampleUsageAndMaintenanceInformationGroup())
                .WithPath(Graph.Metadata.Constants.Resource.Keyword)
                .WithType(Graph.Metadata.Constants.OWL.ObjectProperty)
                .WithNodekind(Graph.Metadata.Constants.Shacl.NodeKinds.IRI)
                .WithLabel("keywords")
                .WithName("Keywords")
                .WithMinCount("0")
                .WithOrder("1")
                .WithComment("This property is used to assign a list of keywords to a resource like free tagging fields.")
                .WithDomain(Graph.Metadata.Constants.PIDO.PidConcept)
                .WithDescription("This property is used to assign a list of keywords to a resource like free tagging fields.")
                .WithRange(Graph.Metadata.Constants.Keyword.Type)
                .WithFieldType(Graph.Metadata.Constants.PIDO.Shacl.FieldTypes.ExtendableList)
                .Build();

            _prop.Add(pidUriProperty);

            return this;
        }

        public MetadataBuilder GenerateSampleIsCopyOfDataset()
        {
            var pidUriProperty = new MetadataPropertyBuilder()
                .WithPidUri(Graph.Metadata.Constants.Resource.IsCopyOfDataset)
                .WithGroup(new MetadataPropertyGroupBuilder().GenerateSampleLinkTypesGroup())
                .WithPath(Graph.Metadata.Constants.Resource.IsCopyOfDataset)
                .WithType(Graph.Metadata.Constants.OWL.ObjectProperty)
                .WithNodekind(Graph.Metadata.Constants.Shacl.NodeKinds.IRI)
                .WithLabel("is copy of dataset")
                .WithName("Is Copy Of Dataset")
                .WithMinCount("0")
                .WithComment("This property links a copy of a dataset with the original dataset. Copy in this context means a complete and identical replication. This property is an asymmetric property, which means that is must point from the copy to the original.")
                .WithDomain(Graph.Metadata.Constants.PIDO.NonRDFDataset)
                .WithDescription("This property links a copy of a dataset with the original dataset. Copy in this context means a complete and identiical replication. This property is an asymmetric property, which means that is must point from the copy to the original.")
                .WithRange(Graph.Metadata.Constants.PIDO.NonRDFDataset)
                .Build();

            _prop.Add(pidUriProperty);

            return this;
        }

        public MetadataBuilder GenerateSampleMainDistributionEndpoint()
        {
            var pidUriProperty = new MetadataPropertyBuilder()
                .WithPidUri(Graph.Metadata.Constants.Resource.MainDistribution)
                .WithGroup(new MetadataPropertyGroupBuilder().GenerateSampleDistributionEndpointGroup())
                .WithPath(Graph.Metadata.Constants.Resource.MainDistribution)
                .WithType(Graph.Metadata.Constants.OWL.ObjectProperty)
                .WithNodekind(Graph.Metadata.Constants.Shacl.NodeKinds.IRI)
                .WithLabel("main distribution")
                .WithName("Main Distribution Endpoint")
                .WithMaxCount("1")
                .WithMinCount("0")
                .WithComment("This property connects an instance of the concept ontology with the distribution endpoint to which the base uri has to resolve to.")
                .WithDomain(Graph.Metadata.Constants.PIDO.RDFDataset)
                .WithSubPropertyOf(Graph.Metadata.Constants.Resource.Distribution)
                .WithDescription(string.Empty)
                .WithRange(Graph.Metadata.Constants.EnterpriseCore.NetworkedResource)
                .Build();

            _prop.Add(pidUriProperty);

            return this;
        }

        public MetadataBuilder GenerateSampleDistributionEndpoint(params Graph.Metadata.DataModels.Metadata.Metadata[] nestedMetadata)
        {
            var pidUriProperty = new MetadataPropertyBuilder()
                .WithPidUri(Graph.Metadata.Constants.Resource.Distribution)
                .WithGroup(new MetadataPropertyGroupBuilder().GenerateSampleDistributionEndpointGroup())
                .WithPath(Graph.Metadata.Constants.Resource.Distribution)
                .WithType(Graph.Metadata.Constants.OWL.ObjectProperty)
                .WithNodekind(Graph.Metadata.Constants.Shacl.NodeKinds.IRI)
                .WithLabel("distribution")
                .WithName("Distribution Endpoint")
                .WithMaxCount("1")
                .WithMinCount("0")
                .WithComment("This property was derived from dcat:distribution and it links a PID Resource with its different distribution endpoints.")
                .WithDomain(Graph.Metadata.Constants.PIDO.RDFDataset)
                .WithDescription(string.Empty)
                .WithRange(Graph.Metadata.Constants.EnterpriseCore.NetworkedResource)
                .WithNestedMetadata(nestedMetadata.ToList())
                .Build();

            _prop.Add(pidUriProperty);

            return this;
        }

        public MetadataBuilder GenerateSampleHasMathematicalModelCategory()
        {
            var pidUriProperty = new MetadataPropertyBuilder()
                .WithPidUri(Graph.Metadata.Constants.Resource.MathematicalModelCategory)
                .WithPath(Graph.Metadata.Constants.Resource.MathematicalModelCategory)
                .WithType(Graph.Metadata.Constants.OWL.ObjectProperty)
                .WithNodekind(Graph.Metadata.Constants.Shacl.NodeKinds.IRI)
                .WithLabel("hasMathematicalModelCategory")
                .WithName("Mathematical Model Category")
                .WithMaxCount("7")
                .WithMinCount("0")
                .WithComment("This property links a resource that is a mathematical model with a mathematical model category.")
                .WithDomain(Graph.Metadata.Constants.Resource.Type.MathematicalModel)
                .WithRange(Graph.Metadata.Constants.Resource.MathematicalModelCategory)
                .Build();

            _prop.Add(pidUriProperty);

            return this;
        }

        #region Distributed Endpoint 

        public MetadataBuilder GenerateSampleHasNetworkResourceLabel()
        {
            var pidUriProperty = new MetadataPropertyBuilder()
                .WithPidUri(Graph.Metadata.Constants.Resource.DistributionEndpoints.HasNetworkedResourceLabel)
                .WithType(Graph.Metadata.Constants.OWL.DatatypeProperty)
                .WithDomain(Graph.Metadata.Constants.EnterpriseCore.NetworkedResource)
                .WithLabel("hasNetworkedResourceLabel")
                .WithRange(Graph.Metadata.Constants.RDF.HTML)
                .WithSubPropertyOf(Graph.Metadata.Constants.SKOS.PrefLabel)
                .WithDataType(Graph.Metadata.Constants.RDF.HTML)
                .WithMaxCount("1")
                .WithMaxCount("1")
                .WithName("Label")
                .WithNodekind(Graph.Metadata.Constants.Shacl.NodeKinds.Literal)
                .WithOrder("1")
                .WithPath(Graph.Metadata.Constants.Resource.DistributionEndpoints.HasNetworkedResourceLabel)
                .Build();

            _prop.Add(pidUriProperty);

            return this;
        }

        public MetadataBuilder GenerateSampleHasContactPerson()
        {
            var pidUriProperty = new MetadataPropertyBuilder()
                .WithPidUri(Graph.Metadata.Constants.Resource.DistributionEndpoints.HasContactPerson)
                .WithType(Graph.Metadata.Constants.OWL.ObjectProperty)
                .WithComment("This property relates an instance of a network resource to the contact person for this instance.")
                .WithDomain(Graph.Metadata.Constants.EnterpriseCore.NetworkedResource)
                .WithLabel("has contact person")
                .WithRange(Graph.Metadata.Constants.EnterpriseCore.Person)
                .WithClass(Graph.Metadata.Constants.EnterpriseCore.Person)
                .WithMaxCount("1")
                .WithMaxCount("0")
                .WithName("Contact Person")
                .WithNodekind(Graph.Metadata.Constants.Shacl.NodeKinds.Literal)
                .WithOrder("2")
                .WithPath(Graph.Metadata.Constants.Resource.DistributionEndpoints.HasContactPerson)
                .Build();

            _prop.Add(pidUriProperty);

            return this;
        }


        public MetadataBuilder GenerateSampleHasNetworkAdress()
        {
            var pidUriProperty = new MetadataPropertyBuilder()
                .WithPidUri(Graph.Metadata.Constants.Resource.DistributionEndpoints.HasNetworkAddress)
                .WithPath(Graph.Metadata.Constants.Resource.DistributionEndpoints.HasNetworkAddress)
                .WithType(Graph.Metadata.Constants.OWL.DatatypeProperty)
                .WithNodekind(Graph.Metadata.Constants.Shacl.NodeKinds.Literal)
                .WithLabel("network address")
                .WithName("Target URI")
                .WithMaxCount("1")
                .WithMinCount("0")
                .WithComment("This is the IP or the URL where the networked resource is accessible.")
                .WithDomain(Graph.Metadata.Constants.EnterpriseCore.NetworkedResource)
                .WithRange(Graph.Metadata.Constants.DataTypes.AnyUri)
                .Build();

            _prop.Add(pidUriProperty);

            return this;
        }

        #endregion
    }
}
