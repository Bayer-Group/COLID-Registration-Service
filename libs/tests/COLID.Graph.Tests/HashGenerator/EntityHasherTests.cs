using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using COLID.Graph.HashGenerator.Exceptions;
using COLID.Graph.HashGenerator.Services;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.TripleStore.DataModels.Base;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace COLID.Graph.Tests.HashGenerator
{
    public class EntityHasherTests
    {
        private readonly IEntityHasher _hasher = new EntityHasher();
        private readonly ITestOutputHelper _output;

        public EntityHasherTests(ITestOutputHelper outputHelper)
        {
            _output = outputHelper;
        }

        [Fact]
        public void HashEntity_Error_CustomPropertyRemoved()
        {
            var entity = new Entity
            {
                Properties = new Dictionary<string, List<dynamic>>
                {
                    {Graph.Metadata.Constants.Resource.HasResourceDefintion, new List<dynamic> {"<p>def</p>"}},
                }
            };
            ISet<string> ignoredKeys = new HashSet<string> {Graph.Metadata.Constants.Resource.HasResourceDefintion};

            Assert.Throws<MissingPropertiesException>(() => _hasher.Hash(entity, ignoredKeys));
        }

        [Fact]
        public void HashEntity_Success_OneProperty()
        {
            const string expectedSha256Hash = "f947e906b1b4d1831f2abc44351a05c481f2fd0d63a85f3f409043fa474461ec";
            var entity = new Entity
            {
                Properties = new Dictionary<string, List<dynamic>>
                {
                    {Graph.Metadata.Constants.Resource.HasResourceDefintion, new List<dynamic> {"<p>def</p>"}},
                }
            };

            _output.WriteLine($"Entity to hash: {entity}");

            var resultHash = _hasher.Hash(entity);

            Assert.Equal(expectedSha256Hash, resultHash);
        }

        [Fact]
        public void HashEntity_Success_MultipleProperties()
        {
            const string expectedSha256Hash = "4b4a8245e659bd9f898715acc52174918b5e1207204f66d8a7c99ec4bef154d1";

            var entity = new Entity
            {
                Properties = new Dictionary<string, List<dynamic>>
                {
                    {Graph.Metadata.Constants.Resource.HasResourceDefintion, new List<dynamic> {"<p>def</p>"}},
                    {Graph.Metadata.Constants.Resource.HasLabel, new List<dynamic> {"Glorious resource entered the game"}},
                    {Graph.Metadata.Constants.Resource.Keyword, new List<dynamic> {"THIS-IS-KEYWOOOOOORD"}},
                    {Graph.Metadata.Constants.Resource.LifecycleStatus, new List<dynamic> {"https://pid.bayer.com/kos/19050/released"}},
                    {Graph.Metadata.Constants.Resource.Author, new List<dynamic> {"author@bayer.com"}}
                }
            };

            _output.WriteLine($"Entity to hash (properties only): {entity}");

            var resultHash = _hasher.Hash(entity);

            Assert.Equal(expectedSha256Hash, resultHash);
        }

        [Fact]
        public void HashEntry_Success_UnorderedProperties()
        {
            const string expectedSha256Hash = "720dd3d3f316e54483725923f4baa28c120e1372650e96b8c8e7dfaa630364c5";
            var entity = JsonConvert.DeserializeObject<Entity>(GetSampleResourceAsJson());
            _output.WriteLine($"Entity to hash (properties only): {entity}");

            var resultHash = _hasher.Hash(entity);

            Assert.Equal(expectedSha256Hash, resultHash);
        }

        [Fact]
        public void HashEntity_Success_RemovesIgnoredProperties()
        {
            const string expectedSha256Hash = "f947e906b1b4d1831f2abc44351a05c481f2fd0d63a85f3f409043fa474461ec";

            var entity = new Entity
            {
                Properties = new Dictionary<string, List<dynamic>>
                {
                    {Graph.Metadata.Constants.Resource.HasResourceDefintion, new List<dynamic> {"<p>def</p>"}},
                    {Graph.Metadata.Constants.Resource.LastChangeUser,new List<dynamic> {"user.changed@bayer.com"}},
                    {Graph.Metadata.Constants.Resource.Author, new List<dynamic> {"author@bayer.com"}}
                }
            };
            _output.WriteLine($"Entity to hash (properties only): {entity}");

            var resultHash = _hasher.Hash(entity);

            Assert.Equal(expectedSha256Hash, resultHash);
        }

        [Fact]
        public void HashEntry_Error_IfArgumentIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => _hasher.Hash(null));
        }

        [Fact]
        public void HashEntry_Error_IfEntityHasNoProperties()
        {
            Assert.Throws<MissingPropertiesException>(() => _hasher.Hash(new Entity()));
        }

        private string GetSampleResourceAsJson()
        {
            return @"
{
    ""properties"": {
        ""https://pid.bayer.com/kos/19050/hasResourceDefinition"": [
            ""<p>def</p>""
        ],
        ""https://pid.bayer.com/kos/19050/hasLabel"": [
            ""[PID-794] Seperate view  - Multi endpoints and links""
        ],
        ""http://pid.bayer.com/kos/19014/hasPID"": [
            {
                ""id"": ""https://dev-pid.bayer.com/data/k0000004"",
                ""properties"": {
                    ""https://pid.bayer.com/kos/19050/hasUriTemplate"": [
                        ""https://pid.bayer.com/kos/19050#10168d5b-9eb9-4767-90cb-d7e99a1660ac""
                    ],
                    ""http://www.w3.org/1999/02/22-rdf-syntax-ns#type"": [
                        ""http://pid.bayer.com/kos/19014/PermanentIdentifier""
                    ]
                }
            }
        ],
        ""https://pid.bayer.com/kos/19050/47119343"": [],
        ""https://pid.bayer.com/kos/19050/hasPIDEditorialNote"": [],
        ""https://pid.bayer.com/2ab6eaa7-d48e-453d-91f3-2db1cbd96b98/156621"": [],
        ""https://pid.bayer.com/kos/19050/hasVersion"": [
            ""1""
        ],
        ""https://pid.bayer.com/kos/19050/hasLifecycleStatus"": [],
        ""https://pid.bayer.com/kos/19050/containsLicensedData"": [],
        ""https://pid.bayer.com/kos/19050/isPersonalData"": [],
        ""https://pid.bayer.com/kos/19050/hasInformationClassification"": [],
        ""https://pid.bayer.com/kos/19050/hasDataSteward"": [],
        ""https://pid.bayer.com/kos/19050/isCopyOfDataset"": [
            ""https://dev-pid.bayer.com/sa-commercial-br-saleshierarchy-agroeste""
        ],
        ""https://pid.bayer.com/kos/19050/isDerivedFromDataset"": [
            ""https://dev-pid.bayer.com/openweathermap-current-temperature""
        ],
        ""https://pid.bayer.com/kos/19050/sameDatasetAs"": [],
        ""https://pid.bayer.com/kos/19050/isSubsetOfDataset"": [],
        ""https://pid.bayer.com/kos/19050/distribution"": [
            {
                ""properties"": {
                    ""http://pid.bayer.com/kos/19014/hasPID"": [
                        {
                            ""id"": ""https://dev-pid.bayer.com/data/k0000005"",
                            ""properties"": {
                                ""http://www.w3.org/1999/02/22-rdf-syntax-ns#type"": [
                                    ""http://pid.bayer.com/kos/19014/PermanentIdentifier""
                                ],
                                ""https://pid.bayer.com/kos/19050/hasUriTemplate"": [
                                    ""https://pid.bayer.com/kos/19050#10168d5b-9eb9-4767-90cb-d7e99a1660ac""
                                ]
                            }
                        }
                    ],
                    ""http://www.w3.org/1999/02/22-rdf-syntax-ns#type"": [
                        ""http://pid.bayer.com/kos/19014/BrowsableResource""
                    ],
                    ""https://pid.bayer.com/kos/19050/hasDistributionEndpointLifecycleStatus"": [
                        ""https://pid.bayer.com/kos/19050/active""
                    ],
                    ""https://pid.bayer.com/kos/19050/hasContactPerson"": [],
                    ""https://pid.bayer.com/kos/19050/hasNetworkedResourceLabel"": [
                        ""<p>Label Endpoint</p>""
                    ],
                    ""http://pid.bayer.com/kos/19014/hasNetworkAddress"": []
                },
                ""id"": ""https://pid.bayer.com/kos/19050#805f648f-0d3c-13cb-5bee-d73348bcecc4""
            },
            {
                ""properties"": {
                    ""http://pid.bayer.com/kos/19014/hasPID"": [
                        {
                            ""id"": ""https://dev-pid.bayer.com/data/k0000006"",
                            ""properties"": {
                                ""http://www.w3.org/1999/02/22-rdf-syntax-ns#type"": [
                                    ""http://pid.bayer.com/kos/19014/PermanentIdentifier""
                                ],
                                ""https://pid.bayer.com/kos/19050/hasUriTemplate"": [
                                    ""https://pid.bayer.com/kos/19050#10168d5b-9eb9-4767-90cb-d7e99a1660ac""
                                ]
                            }
                        }
                    ],
                    ""http://www.w3.org/1999/02/22-rdf-syntax-ns#type"": [
                        ""http://pid.bayer.com/kos/19014/QueryEndpoint""
                    ],
                    ""https://pid.bayer.com/kos/19050/hasDistributionEndpointLifecycleStatus"": [
                        ""https://pid.bayer.com/kos/19050/active""
                    ],
                    ""https://pid.bayer.com/kos/19050/hasContactPerson"": [],
                    ""https://pid.bayer.com/kos/19050/hasNetworkedResourceLabel"": [
                        ""<p>query endpoint</p>""
                    ],
                    ""http://pid.bayer.com/kos/19014/hasNetworkAddress"": []
                },
                ""id"": ""https://pid.bayer.com/kos/19050#9e29b2f8-5bc7-83ad-d8c0-c3229102c957""
            }
        ],
        ""https://pid.bayer.com/kos/19050/lastChangeUser"": [
            ""tim.odenthal.ext@bayer.com""
        ],
        ""https://pid.bayer.com/kos/19050/546454"": [],
        ""https://pid.bayer.com/kos/19050/author"": [
            ""tim.odenthal.ext@bayer.com""
        ],
        ""http://www.w3.org/1999/02/22-rdf-syntax-ns#type"": [
            ""https://pid.bayer.com/kos/19050/GenericDataset""
        ],
        ""https://pid.bayer.com/kos/19050#hasConsumerGroup"": [
            ""https://pid.bayer.com/kos/19050#bf2f8eeb-fdb9-4ee1-ad88-e8932fa8753c""
        ],
        ""https://pid.bayer.com/kos/19050/lastChangeDateTime"": [
            ""2020-08-27T08:30:08.543Z""
        ],
        ""https://pid.bayer.com/kos/19050/dateCreated"": [
            ""2020-08-27T08:29:38.269Z""
        ],
        ""https://pid.bayer.com/kos/19050/hasHistoricVersion"": [],
        ""https://pid.bayer.com/kos/19050/646465"": [
            ""https://pid.bayer.com/kos/19050#8bf66eae-b98f-431e-85de-c5fbd88b9e01""
        ],
        ""https://pid.bayer.com/kos/19050/hasEntryLifecycleStatus"": [
            ""https://pid.bayer.com/kos/19050/draft""
        ],
        ""https://pid.bayer.com/kos/19050/hasLaterVersion"": []
    }
}

";
        }
    }
}
