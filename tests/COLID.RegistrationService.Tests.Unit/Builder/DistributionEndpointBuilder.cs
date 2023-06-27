using System;
using COLID.Graph.Metadata.Constants;
using COLID.RegistrationService.Common.Extensions;
using Entity = COLID.Graph.TripleStore.DataModels.Base.Entity;
using Type = COLID.RegistrationService.Common.Enums.DistributionEndpoint;

namespace COLID.RegistrationService.Tests.Common.Builder
{
    public class DistributionEndpointBuilder : AbstractEntityBuilder<Entity>
    {
        private Entity _de = new Entity();

        public override Entity Build()
        {
            _de.Properties = _prop;
            return _de;
        }

        // Generates a sample with the following structure:
        /*
        "https://pid.bayer.com/kos/19050/distribution": [
            {
                "properties": {
                    "http://pid.bayer.com/kos/19014/hasPID": [
                        {
                            "properties": {
                                "https://pid.bayer.com/kos/19050/hasUriTemplate": [
                                    "https://pid.bayer.com/kos/19050#00a3047f-699a-4867-b775-0b6a7c189ef4"
                                ],
                                "http://www.w3.org/1999/02/22-rdf-syntax-ns#type": [
                                    "http://pid.bayer.com/kos/19014/PermanentIdentifier"
                                ]
                            },
                            "id": null
                        }
                    ],
                    "http://www.w3.org/1999/02/22-rdf-syntax-ns#type": [
                        "http://pid.bayer.com/kos/19014/BrowsableResource"
                    ],
                    "https://pid.bayer.com/kos/19050/hasNetworkedResourceLabel": [
                        "DE-Google"
                    ],
                    "https://pid.bayer.com/kos/19050/hasContactPerson": [
                        "anonymous@anonymous.com"
                    ],
                    "https://pid.bayer.com/kos/19050/hasDistributionEndpointLifecycleStatus": [
                        "https://pid.bayer.com/kos/19050/active"
                    ],
                    "http://pid.bayer.com/kos/19014/hasNetworkAddress": [
                        "http://google.de"
                    ]
                },
                "id": "https://pid.bayer.com/kos/19050#223538e9-3cfb-c0d7-c712-46a7049ff6cf"
            }
        ],
        */

        public DistributionEndpointBuilder GenerateSampleData()
        {
            var random = new Random();
            WithPidUri($"https://pid.bayer.com/constraint/c{random.Next(0, 9999999)}");
            WithId($"https://pid.bayer.com/kos/19050#{Guid.NewGuid()}");
            WithType(Type.BrowsableResource);
            WithNetworkedResourceLabel("DE-Google");
            WithContactPerson("anonymous@anonymous.com");
            WithDistributionEndpointLifecycleStatus(LifecycleStatus.Active);
            WithNetworkAddress("http://google.de");

            return this;
        }

        public DistributionEndpointBuilder WithId(string id)
        {
            _de.Id = id;
            return this;
        }

        public DistributionEndpointBuilder WithType(Type type)
        {
            CreateOrOverwriteProperty(RDF.Type, EnumExtension.GetDescription(type));
            return this;
        }

        public DistributionEndpointBuilder WithNetworkedResourceLabel(string label)
        {
            CreateOrOverwriteProperty(Resource.DistributionEndpoints.HasNetworkedResourceLabel, label);
            return this;
        }

        public DistributionEndpointBuilder WithNetworkAddress(string networkAddress)
        {
            CreateOrOverwriteProperty(Resource.DistributionEndpoints.HasNetworkAddress, networkAddress);
            return this;
        }

        public DistributionEndpointBuilder WithContactPerson(string contactPerson)
        {
            CreateOrOverwriteProperty(Resource.DistributionEndpoints.HasContactPerson, contactPerson);
            return this;
        }

        public DistributionEndpointBuilder WithDistributionEndpointLifecycleStatus(LifecycleStatus status)
        {
            CreateOrOverwriteProperty(Resource.DistributionEndpoints.DistributionEndpointLifecycleStatus, EnumExtension.GetDescription(status));
            return this;
        }
    }
}
