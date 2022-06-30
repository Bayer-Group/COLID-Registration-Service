using System;
using System.Collections.Generic;
using COLID.Graph.HashGenerator.Exceptions;
using COLID.Graph.HashGenerator.Services;
using COLID.Graph.TripleStore.DataModels.Base;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace COLID.Graph.Tests.HashGenerator
{
    public class EntityHasherTests
    {
        private readonly IEntityHasher _hasher;
        private readonly ITestOutputHelper _output;

        public EntityHasherTests(ITestOutputHelper outputHelper)
        {
            var mockLogger = Mock.Of<ILogger<EntityHasher>>();
            _hasher = new EntityHasher(mockLogger);
            _output = outputHelper;
        }

        [Fact]
        public void HashEntity_Sample_Resource_Should_Result_In_Expected_Hash()
        {
            var entity = JsonConvert.DeserializeObject<Entity>(ResourceSample());

            const string expectedSha256Hash = "e0de7b31ccc6cdcf1480771d340e6b539f404fea5dbf17c636af5467711d4cbd";
            var resultHash = _hasher.Hash(entity);

            Assert.Equal(expectedSha256Hash, resultHash);
        }

        [Fact]
        public void HashEntity_Custom_Ignored_Property_Should_Be_Removed()
        {
            const string customProperty = Graph.Metadata.Constants.Resource.HasResourceDefintion;
            var entity = new Entity { Properties = new Dictionary<string, List<dynamic>> { { customProperty, new List<dynamic> { "<p>remove me</p>" } } } };
            ISet<string> ignoredKeys = new HashSet<string> { customProperty };

            Assert.Throws<MissingPropertiesException>(() => _hasher.Hash(entity, ignoredKeys));
        }

        [Fact]
        public void HashEntity_Default_Ignored_Property_Should_Be_Removed()
        {
            const string expectedSha256Hash = "f947e906b1b4d1831f2abc44351a05c481f2fd0d63a85f3f409043fa474461ec";

            var entity = new Entity
            {
                Properties = new Dictionary<string, List<dynamic>>
                {
                    {Graph.Metadata.Constants.Resource.HasResourceDefintion, new List<dynamic> {"<p>def</p>"}},
                    {
                        Graph.Metadata.Constants.Resource.LastChangeUser,
                        new List<dynamic> {"user.changed@bayer.com"}
                    },
                    {Graph.Metadata.Constants.Resource.Author, new List<dynamic> {"author@bayer.com"}}
                }
            };
            _output.WriteLine($"Entity to hash (properties only): {entity}");

            var resultHash = _hasher.Hash(entity);

            Assert.Equal(expectedSha256Hash, resultHash);
        }

        [Fact]
        public void HashEntity_Empty_And_Null_Properties_Should_Be_Removed()
        {
            var entity = new Entity
            {
                Id = "Will be ignored anyway",
                Properties = new Dictionary<string, List<dynamic>>
                {
                    {Graph.Metadata.Constants.Resource.HasResourceDefintion, new List<dynamic> {""}},
                    {Graph.Metadata.Constants.Resource.LastChangeUser, new List<dynamic>()},
                    {Graph.Metadata.Constants.Resource.Author, null}
                }
            };

            Assert.Throws<MissingPropertiesException>(() => _hasher.Hash(entity));
        }

        [Fact]
        public void HashEntity_With_Different_Arrangement_Should_Result_In_Same_Hash()
        {
            var sortedEntity = new Entity
            {
                Id = "Will be ignored anyway",
                Properties = new Dictionary<string, List<dynamic>> { { Graph.Metadata.Constants.Resource.HasDataSteward, new List<dynamic> { "first", "second", "third" } } }
            };
            _output.WriteLine($"Sorted entity to hash (properties only): {sortedEntity}");

            var unsortedEntity = new Entity
            {
                Id = "Will be ignored anyway",
                Properties = new Dictionary<string, List<dynamic>> { { Graph.Metadata.Constants.Resource.HasDataSteward, new List<dynamic> { "second", "third", "first" } } }
            };
            _output.WriteLine($"Unsorted entity to hash (properties only): {unsortedEntity}");

            var sortedHash = _hasher.Hash(sortedEntity);
            var unsortedHash = _hasher.Hash(unsortedEntity);

            Assert.Equal(sortedHash, unsortedHash);
        }

        [Fact]
        public void HashEntity_With_Different_Arrangement_Nested_Should_Result_In_Same_Hash()
        {
            Entity sortedEntity = new Entity
            {
                Properties = new Dictionary<string, List<dynamic>> {
                    { "https://pid.bayer.com/kos/19050/first", new List<dynamic>
                        {
                            "More corn",
                            "Some corn",
                            "THIS IS CORN"
                        }
                    },
                    { "https://pid.bayer.com/kos/19050/second", new List<dynamic> {
                            new Entity {
                                Properties = new Dictionary<string, List<dynamic>> {
                                    { "https://pid.bayer.com/kos/19050/first", new List<dynamic> {
                                            "first:first_item",
                                            "first:second_item"
                                        }
                                    },
                                    { "https://pid.bayer.com/kos/19050/second", new List<dynamic> {
                                            "second:first_item",
                                            "second:second_item"
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
            Entity unsortedEntity = new Entity
            {
                Properties = new Dictionary<string, List<dynamic>> {
                    { "https://pid.bayer.com/kos/19050/second", new List<dynamic> {
                            new Entity {
                                Properties = new Dictionary<string, List<dynamic>> {
                                    { "https://pid.bayer.com/kos/19050/second", new List<dynamic> {
                                            "second:second_item",
                                            "second:first_item"
                                        }
                                    },
                                    { "https://pid.bayer.com/kos/19050/first", new List<dynamic> {
                                            "first:second_item",
                                            "first:first_item"
                                        }
                                    }
                                }
                            }
                        }
                    },
                    { "https://pid.bayer.com/kos/19050/first", new List<dynamic>
                        {
                            "THIS IS CORN",
                            "Some corn",
                            "More corn"
                        }
                    }
                }
            };

            var sortedHash = _hasher.Hash(sortedEntity);
            var unsortedHash = _hasher.Hash(unsortedEntity);

            Assert.Equal(sortedHash, unsortedHash);
        }

        [Fact]
        public void HashEntity_With_One_Property_Should_Result_In_Same_Hash()
        {
            const string expectedSha256Hash = "f947e906b1b4d1831f2abc44351a05c481f2fd0d63a85f3f409043fa474461ec";
            var entity = new Entity
            { Properties = new Dictionary<string, List<dynamic>> { { Graph.Metadata.Constants.Resource.HasResourceDefintion, new List<dynamic> { "<p>def</p>" } } } };

            _output.WriteLine($"Entity to hash: {entity}");

            var resultHash = _hasher.Hash(entity);

            Assert.Equal(expectedSha256Hash, resultHash);
        }

        [Fact]
        public void HashEntity_With_Multiple_Allowed_Properties_Should_Result_In_Same_Hash()
        {
            const string expectedSha256Hash = "4b4a8245e659bd9f898715acc52174918b5e1207204f66d8a7c99ec4bef154d1";

            var entity = new Entity
            {
                Properties = new Dictionary<string, List<dynamic>>
                {
                    {Graph.Metadata.Constants.Resource.HasResourceDefintion, new List<dynamic> {"<p>def</p>"}},
                    {
                        Graph.Metadata.Constants.Resource.HasLabel,
                        new List<dynamic> {"Glorious resource entered the game"}
                    },
                    {Graph.Metadata.Constants.Resource.Keyword, new List<dynamic> {"THIS-IS-KEYWOOOOOORD"}},
                    {
                        Graph.Metadata.Constants.Resource.LifecycleStatus,
                        new List<dynamic> {"https://pid.bayer.com/kos/19050/released"}
                    },
                    {Graph.Metadata.Constants.Resource.Author, new List<dynamic> {"author@bayer.com"}}
                }
            };

            _output.WriteLine($"Entity to hash (properties only): {entity}");

            var resultHash = _hasher.Hash(entity);

            Assert.Equal(expectedSha256Hash, resultHash);
        }

        [Fact]
        public void HashEntity_Entity_Id_Should_Be_Ignored()
        {
            var entityWithId = new Entity
            {
                Id = "123",
                Properties = new Dictionary<string, List<dynamic>> { { Graph.Metadata.Constants.Resource.HasResourceDefintion, new List<dynamic> { "<p>abc</p>" } } }
            };
            var entityWithoutId = new Entity
            {
                Properties = new Dictionary<string, List<dynamic>> { { Graph.Metadata.Constants.Resource.HasResourceDefintion, new List<dynamic> { "<p>abc</p>" } } }
            };

            var entityWithIdHash = _hasher.Hash(entityWithId);
            var entityWithoutIdHash = _hasher.Hash(entityWithoutId);

            Assert.Equal(entityWithoutIdHash, entityWithIdHash);
        }

        [Fact]
        public void HashEntry_Throws_ArgumentNullException_If_ArgumentIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => _hasher.Hash(null));
        }

        [Fact]
        public void HashEntry_Throws_MissingPropertiesException_If_Entity_Has_No_Properties()
        {
            Assert.Throws<MissingPropertiesException>(() => _hasher.Hash(new Entity()));
        }

        private string ResourceSample()
        {
            return @"
{
    ""pidUri"": ""https://pid.bayer.com/11223344-5566-7788-9900-aabbccddeeff/"",
    ""baseUri"": null,
    ""previousVersion"": null,
    ""laterVersion"": null,
    ""publishedVersion"": null,
    ""versions"": [
      {
        ""id"": ""https://pid.bayer.com/kos/19050#bb51f5e9-827b-4019-b944-b7df5b20c89e"",
        ""version"": ""1"",
        ""pidUri"": ""https://pid.bayer.com/99887766-5544-3322-1100-ffeeddccbbaa/"",
        ""baseUri"": null,
        ""lifecycleStatus"": null,
        ""publishedVersion"": null
      }
    ],
    ""properties"": {
      ""http://www.w3.org/1999/02/22-rdf-syntax-ns#type"": [
        ""https://pid.bayer.com/kos/19050/GenericDataset""
      ],
      ""https://pid.bayer.com/kos/19050/646465"": [
        ""https://pid.bayer.com/kos/19050#11111111-2222-3333-4444-555555555555""
      ],
      ""https://pid.bayer.com/kos/19050/distribution"": [
        {
          ""properties"": {
            ""http://www.w3.org/1999/02/22-rdf-syntax-ns#type"": [
              ""http://pid.bayer.com/kos/19014/MaintenancePoint""
            ],
            ""https://pid.bayer.com/kos/19050/hasContactPerson"": [
              ""some.employee@bayer.com""
            ],
            ""https://pid.bayer.com/kos/19050/hasDistributionEndpointLifecycleStatus"": [
              ""https://pid.bayer.com/kos/19050/active""
            ],
            ""http://pid.bayer.com/kos/19014/hasNetworkAddress"": [
              ""https://www.google.de/""
            ],
            ""http://pid.bayer.com/kos/19014/hasPID"": [
              {
                ""id"": ""https://pid.bayer.com/55555555-4444-3333-2222-111111111111/"",
                ""properties"": {
                  ""http://www.w3.org/1999/02/22-rdf-syntax-ns#type"": [
                    ""http://pid.bayer.com/kos/19014/PermanentIdentifier""
                  ],
                  ""https://pid.bayer.com/kos/19050/hasUriTemplate"": [
                    ""https://pid.bayer.com/kos/19050#99999999-4444-6666-5555-222222222222""
                  ]
                }
              }
            ],
            ""https://pid.bayer.com/kos/19050/hasNetworkedResourceLabel"": [
              ""A very handsome network resource label""
            ]
          }
        },
        {
          ""properties"": {
            ""http://www.w3.org/1999/02/22-rdf-syntax-ns#type"": [
              ""http://pid.bayer.com/kos/19014/BrowsableResource""
            ],
            ""https://pid.bayer.com/kos/19050/hasContactPerson"": [
              ""some.employee@bayer.com""
            ],
            ""https://pid.bayer.com/kos/19050/hasDistributionEndpointLifecycleStatus"": [
              ""https://pid.bayer.com/kos/19050/active""
            ],
            ""http://pid.bayer.com/kos/19014/hasNetworkAddress"": [
              ""https://www.9gag.com""
            ],
            ""http://pid.bayer.com/kos/19014/hasPID"": [
              {
                ""id"": ""https://pid.bayer.com/09090909-0909-0909-0909-090909090909/"",
                ""properties"": {
                  ""http://www.w3.org/1999/02/22-rdf-syntax-ns#type"": [
                    ""http://pid.bayer.com/kos/19014/PermanentIdentifier""
                  ],
                  ""https://pid.bayer.com/kos/19050/hasUriTemplate"": [
                    ""https://pid.bayer.com/kos/19050#08080808-0404-0404-0404-121212121212""
                  ]
                }
              }
            ],
            ""https://pid.bayer.com/kos/19050/hasNetworkedResourceLabel"": [
              ""a funny collection of memes""
            ]
          }
        }
      ],
      ""https://pid.bayer.com/kos/19050/dateCreated"": [
        ""2020-01-01T07:00:00.000Z""
      ],
      ""https://pid.bayer.com/kos/19050/lastChangeDateTime"": [
        ""2020-01-01T08:00:00.000Z""
      ],
      ""https://pid.bayer.com/kos/19050/containsLicensedData"": [
        ""false""
      ],
      ""https://pid.bayer.com/kos/19050/isPersonalData"": [
        ""false""
      ],
      ""https://pid.bayer.com/kos/19050/hasInformationClassification"": [
        ""https://pid.bayer.com/kos/19050/Restricted""
      ],
      ""https://pid.bayer.com/kos/19050/author"": [
        ""some.employee@bayer.com""
      ],
      ""https://pid.bayer.com/kos/19050/hasLifecycleStatus"": [
        ""https://pid.bayer.com/kos/19050/released""
      ],
      ""https://pid.bayer.com/kos/19050/hasVersion"": [
        ""1""
      ],
      ""https://pid.bayer.com/kos/19050/hasDataSteward"": [
        ""some.employee@bayer.com"",
        ""example@bayer.com""
      ],
      ""https://pid.bayer.com/kos/19050/lastChangeUser"": [
        ""some.employee@bayer.com""
      ],
      ""https://pid.bayer.com/kos/19050/hasEntryLifecycleStatus"": [
        ""https://pid.bayer.com/kos/19050/published""
      ],
      ""https://pid.bayer.com/kos/19050#hasConsumerGroup"": [
        ""https://pid.bayer.com/kos/19050#87878787-4949-4949-4949-120012001200""
      ],
      ""http://pid.bayer.com/kos/19014/hasPID"": [
        {
          ""id"": ""https://pid.bayer.com/77777777-9999-5555-6666-112233445566/"",
          ""properties"": {
            ""http://www.w3.org/1999/02/22-rdf-syntax-ns#type"": [
              ""http://pid.bayer.com/kos/19014/PermanentIdentifier""
            ],
            ""https://pid.bayer.com/kos/19050/hasUriTemplate"": [
              ""https://pid.bayer.com/kos/19050#08080808-0404-0404-0404-121212121212""
            ]
          }
        }
      ],
      ""https://pid.bayer.com/kos/19050/hasLabel"": [
        ""Wow, such Resource""
      ],
      ""https://pid.bayer.com/kos/19050/hasResourceDefinition"": [
        ""This is a test-resource definition and should be hashed perfectly""
      ],
      ""https://pid.bayer.com/kos/19050/47119343"": [
        ""https://pid.bayer.com/kos/19050#45ba4ec5-25c2-4116-894b-807e7d7df250"",
        ""https://pid.bayer.com/kos/19050#dd99ecc0-a0ca-4b3b-a015-b102eecbbc64"",
        ""https://pid.bayer.com/kos/19050#32bccee8-03f5-4598-b501-6bec6fd1a19c""
      ],
      ""https://pid.bayer.com/kos/19050/hasPIDEditorialNote"": [
         ""Editorial note contains some special chars like html tags <li>Name</li>, quotes \""Key\"", \""Value\"" or empty and unicode chars Ã¢ÂÂ¬""
      ]
    }
  }
";
        }
    }
}
