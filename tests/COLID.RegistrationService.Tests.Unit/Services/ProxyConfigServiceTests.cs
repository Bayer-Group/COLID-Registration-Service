using System;
using System.Collections.Generic;
using COLID.Graph.Metadata.Constants;
using COLID.Graph.Metadata.Services;
using COLID.RegistrationService.Common.DataModel.ExtendedUriTemplates;
using COLID.RegistrationService.Common.DataModel.Resources;
using COLID.RegistrationService.Repositories.Interface;
using COLID.RegistrationService.Services.Implementation;
using COLID.RegistrationService.Services.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace COLID.RegistrationService.Tests.Unit.Services
{
    public class ProxyConfigServiceTests
    {
        private readonly Mock<IConfiguration> _configuration;
        private readonly Mock<IExtendedUriTemplateService> _extendedUriTemplateService;
        private Mock<IResourceRepository> _resourceRepo;
        private readonly Mock<IMetadataService> _metadataService;

        private readonly IProxyConfigService _service;

        private readonly Uri _resourecGraph;

        public ProxyConfigServiceTests()
        {
            _resourecGraph = new Uri("https://pid.bayer.com/resource/2.0");

            _configuration = setupConfiguration();
            _metadataService = setupMetadataServiceMock();
            _extendedUriTemplateService = setupExtendedUriTemplateServiceMock();
            _resourceRepo = new Mock<IResourceRepository>();

            _service = new ProxyConfigService(_configuration.Object, _resourceRepo.Object, _extendedUriTemplateService.Object, _metadataService.Object, Mock.Of<ILogger<ProxyConfigService>>());
        }

        private Mock<IConfiguration> setupConfiguration()
        {
            var configuration = new Mock<IConfiguration>();

            var mockConfSection = new Mock<IConfigurationSection>();
            mockConfSection.SetupGet(m => m[It.Is<string>(s => s == "colidFrontEndUrl")]).Returns("https://frontend.colid.url/");
            mockConfSection.SetupGet(m => m[It.Is<string>(s => s == "colidDomain")]).Returns("pid.bayer.com");

            configuration.Setup(a => a.GetSection(It.Is<string>(s => s == "ConnectionStrings"))).Returns(mockConfSection.Object);



            return configuration;
        }

        private Mock<IMetadataService> setupMetadataServiceMock()
        {
            var metadataService = new Mock<IMetadataService>();
            metadataService.Setup(ms => ms.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type.FirstResouceType)).Returns(
                new List<string>()
                {
                    "http://pid.bayer.com/kos/19014/Ontology",
                    "https://pid.bayer.com/kos/19050/GenericDataset",
                    "https://pid.bayer.com/kos/19050/Mapping",
                    "https://pid.bayer.com/kos/19050/MathematicalModel",
                    "https://pid.bayer.com/kos/19050/RDFDatasetWithInstances"
                }
            );

            metadataService.Setup(mock => mock.GetInstanceGraph(PIDO.PidConcept))
                .Returns(_resourecGraph);

            return metadataService;
        }

        private Mock<IExtendedUriTemplateService> setupExtendedUriTemplateServiceMock()
        {
            var extendedUriTemplateService = new Mock<IExtendedUriTemplateService>();
            extendedUriTemplateService.Setup(eut => eut.GetEntities(null)).Returns(
                new List<ExtendedUriTemplateResultDTO>()
                {
                    new ExtendedUriTemplateResultDTO()
                    {
                        Properties = new Dictionary<string, List<dynamic>>()
                        {
                            { "https://pid.bayer.com/kos/19050#hasOrder",               new List<dynamic>() { "1" } },
                            { "https://pid.bayer.com/kos/19050#hasPidUriSearchRegex",   new List<dynamic>() { "^https://pid.bayer.com(.*)" } },
                            { "https://pid.bayer.com/kos/19050#hasReplacementString",   new List<dynamic>() { "{targetUri}?redirectFrom={encodedPidUri}$1" } },
                            { "https://pid.bayer.com/kos/19050#hasTargetUriMatchRegex", new List<dynamic>() { "^https://internal.example.com/" } },
                            { "https://pid.bayer.com/kos/19050#UseHttpScheme",          new List<dynamic>() { "true" } },
                        }
                    }
                }
            );

            return extendedUriTemplateService;
        }

        private string GetStandardHeader()
        {
            return @"server {
	listen 80;
	server_name pid.bayer.com www.pid.bayer.com;
	return 301 https://$server_name$request_uri;
}
server {
	listen 443 ssl http2;
	include snippets/self-signed.conf;
	include snippets/ssl-params.conf;
	server_name pid.bayer.com www.pid.bayer.com;
	proxy_set_header 'Access-Control-Allow-Origin' ""*"";
	proxy_set_header 'Access-Control-Allow-Credentials' 'true';
	proxy_set_header 'Access-Control-Allow-Methods' 'GET, POST, PUT, DELETE, OPTIONS';
	proxy_set_header 'Access-Control-Allow-Headers' 'Accept,Authorization,Cache-Control,Content-Type,DNT,If-Modified-Since,Keep-Alive,Origin,User-Agent,X-Requested-With';";
        }

        private string GetStandardLocation()
        {
            return @"	location / {
		rewrite ^.* ""https://frontend.colid.url/"";
	}
}";
        }

        [Fact]
        public void GetCurrentProxyConfiguration_StandardConfiguration_Successful()
        {
            _resourceRepo.Setup(ms => ms.GetResourcesForProxyConfiguration(It.IsAny<IList<string>>(), _resourecGraph, It.IsAny<Uri>())).Returns(
                new List<ResourceProxyDTO>()
            );

            var result = _service.GetCurrentProxyConfiguration();

            AssertContainsWithoutNewline(GetStandardHeader(), result);
            AssertContainsWithoutNewline(GetStandardLocation(), result);
        }

        [Fact]
        public void GetCurrentProxyConfiguration_SingleResource_RewriteToEditorFrontend_Successful()
        {
            _resourceRepo.Setup(ms => ms.GetResourcesForProxyConfiguration(It.IsAny<IList<string>>(), _resourecGraph, It.IsAny<Uri>())).Returns(
                new List<ResourceProxyDTO>()
                {
                    new ResourceProxyDTO()
                    {
                        PidUrl = "https://pid.bayer.com/URI5004",
                        TargetUrl = null,
                        ResourceVersion = null
                    }
                }
            );

            var result = _service.GetCurrentProxyConfiguration();

            AssertContainsWithoutNewline(GetStandardHeader(), result);
            AssertContainsWithoutNewline(GetStandardLocation(), result);
            AssertContainsWithoutNewline(
@"	location = /URI5004 {
		rewrite ^.* ""https://frontend.colid.url/resource?pidUri=https%3A%2F%2Fpid.bayer.com%2FURI5004"";
	}",
            result);
        }

        [Fact]
        public void GetCurrentProxyConfiguration_SingleResource_EmptyTargetUri_NoRewrite()
        {
            _resourceRepo.Setup(ms => ms.GetResourcesForProxyConfiguration(It.IsAny<IList<string>>(), _resourecGraph, It.IsAny<Uri>())).Returns(
                new List<ResourceProxyDTO>()
                {
                    new ResourceProxyDTO()
                    {
                        PidUrl = "https://pid.bayer.com/URI5004",
                        TargetUrl = "",
                        ResourceVersion = null
                    }
                }
            );

            var result = _service.GetCurrentProxyConfiguration();

            AssertContainsWithoutNewline(GetStandardHeader(), result);
            AssertContainsWithoutNewline(GetStandardLocation(), result);
            Assert.DoesNotContain(@"	location = /URI5004 {", result);
        }

        [Fact]
        public void GetCurrentProxyConfiguration_SingleResource_WrongTargetUri_NoRewrite()
        {
            _resourceRepo.Setup(ms => ms.GetResourcesForProxyConfiguration(It.IsAny<IList<string>>(), _resourecGraph, It.IsAny<Uri>())).Returns(
                new List<ResourceProxyDTO>()
                {
                    new ResourceProxyDTO()
                    {
                        PidUrl = "https://pid.bayer.com/URI5004",
                        TargetUrl = "ABCD.1234",
                        ResourceVersion = null
                    }
                }
            );

            var result = _service.GetCurrentProxyConfiguration();

            AssertContainsWithoutNewline(GetStandardHeader(), result);
            AssertContainsWithoutNewline(GetStandardLocation(), result);
            Assert.DoesNotContain(@"	location = /URI5004 {", result);
        }

        [Fact]
        public void GetCurrentProxyConfiguration_SingleResource_WrongTargetUriFormat_NoRewrite()
        {
            _resourceRepo.Setup(ms => ms.GetResourcesForProxyConfiguration(It.IsAny<IList<string>>(), _resourecGraph, It.IsAny<Uri>())).Returns(
                new List<ResourceProxyDTO>()
                {
                    new ResourceProxyDTO()
                    {
                        PidUrl = "https://pid.bayer.com/URI5004",
                        TargetUrl = "test.abc",
                        ResourceVersion = null
                    }
                }
            );

            var result = _service.GetCurrentProxyConfiguration();

            AssertContainsWithoutNewline(GetStandardHeader(), result);
            AssertContainsWithoutNewline(GetStandardLocation(), result);
            Assert.DoesNotContain(@"	location = /URI5004 {", result);
        }

        [Fact]
        public void GetCurrentProxyConfiguration_SingleResource_RewriteToExternalUrl_Successful()
        {
            _resourceRepo.Setup(ms => ms.GetResourcesForProxyConfiguration(It.IsAny<IList<string>>(), _resourecGraph, It.IsAny<Uri>())).Returns(
                new List<ResourceProxyDTO>()
                {
                    new ResourceProxyDTO()
                    {
                        PidUrl = "https://pid.bayer.com/URI5004",
                        TargetUrl = "https://external.foo.bar/",
                        ResourceVersion = null
                    }
                }
            );

            var result = _service.GetCurrentProxyConfiguration();

            AssertContainsWithoutNewline(GetStandardHeader(), result);
            AssertContainsWithoutNewline(GetStandardLocation(), result);
            AssertContainsWithoutNewline(
@"	location = /URI5004 {
		rewrite ^.* ""https://external.foo.bar/"";
	}",
            result);
        }

        [Fact]
        public void GetCurrentProxyConfiguration_SingleResource_RewriteToExternalUrl_SpecialCharacters_Successful()
        {
            _resourceRepo.Setup(ms => ms.GetResourcesForProxyConfiguration(It.IsAny<IList<string>>(), _resourecGraph, It.IsAny<Uri>())).Returns(
                new List<ResourceProxyDTO>()
                {
                    new ResourceProxyDTO()
                    {
                        PidUrl = "https://pid.bayer.com/URI5004",
                        TargetUrl = "http://external.url.com/sites/012345/ABCD_LEARN/%5BWording%204%5D%20Test%20%2B%20Special/Characters-Views%20;%20Some-More,Words/DSx%20H2x%20ODBC%20Special.pptx",
                        ResourceVersion = null
                    }
                }
            );

            var result = _service.GetCurrentProxyConfiguration();

            AssertContainsWithoutNewline(GetStandardHeader(), result);
            AssertContainsWithoutNewline(GetStandardLocation(), result);
            AssertContainsWithoutNewline(
@"	location = /URI5004 {
		rewrite ^.* ""http://external.url.com/sites/012345/ABCD_LEARN/%5BWording%204%5D%20Test%20%2B%20Special/Characters-Views%20;%20Some-More,Words/DSx%20H2x%20ODBC%20Special.pptx"";
	}",
            result);
        }

        [Fact]
        public void GetCurrentProxyConfiguration_SingleResource_DifferentProtocolTargetUriFormat_Successful()
        {
            _resourceRepo.Setup(ms => ms.GetResourcesForProxyConfiguration(It.IsAny<IList<string>>(), _resourecGraph, It.IsAny<Uri>())).Returns(
                new List<ResourceProxyDTO>()
                {
                    new ResourceProxyDTO()
                    {
                        PidUrl = "https://pid.bayer.com/URI5004",
                        TargetUrl = "ftp://test.abc",
                        ResourceVersion = null
                    }
                }
            );

            var result = _service.GetCurrentProxyConfiguration();

            AssertContainsWithoutNewline(GetStandardHeader(), result);
            AssertContainsWithoutNewline(GetStandardLocation(), result);
            AssertContainsWithoutNewline(
@"	location = /URI5004 {
		rewrite ^.* ""ftp://test.abc"";
	}",
            result);
        }

        [Fact]
        public void GetCurrentProxyConfiguration_SingleResource_RewriteWithExtendedUriTemplate__Successful()
        {
            _resourceRepo.Setup(ms => ms.GetResourcesForProxyConfiguration(It.IsAny<IList<string>>(), _resourecGraph, It.IsAny<Uri>())).Returns(
                new List<ResourceProxyDTO>()
                {
                    new ResourceProxyDTO()
                    {
                        PidUrl = "https://pid.bayer.com/URI5004",
                        TargetUrl = "https://internal.example.com/",
                        ResourceVersion = null
                    }
                }
            );

            var result = _service.GetCurrentProxyConfiguration();

            AssertContainsWithoutNewline(GetStandardHeader(), result);
            AssertContainsWithoutNewline(GetStandardLocation(), result);
            AssertContainsWithoutNewline(
@"	location = /URI5004 {
		rewrite ^.* ""https://internal.example.com/"";
	}",
            result);
            AssertContainsWithoutNewline(
@"	location ~ ^/URI5004(.*)$ {
		rewrite ^/URI5004(.*)$ ""https://internal.example.com/?redirectFrom=http%3A%2F%2Fpid.bayer.com%2FURI5004$1"";
	}",
            result);
        }

        [Fact]
        public void GetCurrentProxyConfiguration_SingleResources_NestedProxies_RewriteToEditorFrontend_Successful()
        {
            _resourceRepo.Setup(ms => ms.GetResourcesForProxyConfiguration(It.IsAny<IList<string>>(), _resourecGraph, It.IsAny<Uri>())).Returns(
                new List<ResourceProxyDTO>()
                {
                    new ResourceProxyDTO()
                    {
                        PidUrl = "https://pid.bayer.com/URI5004",
                        TargetUrl = null,
                        ResourceVersion = null,
                        NestedProxies = new List<ResourceProxyDTO>()
                        {
                            new ResourceProxyDTO()
                            {
                                PidUrl = "https://pid.bayer.com/URI5005",
                                TargetUrl = null,
                                ResourceVersion = null
                            }
                        }
                    }
                }
            );

            var result = _service.GetCurrentProxyConfiguration();

            AssertContainsWithoutNewline(GetStandardHeader(), result);
            AssertContainsWithoutNewline(GetStandardLocation(), result);
            AssertContainsWithoutNewline(
@"	location = /URI5004 {
		rewrite ^.* ""https://frontend.colid.url/resource?pidUri=https%3A%2F%2Fpid.bayer.com%2FURI5004"";
	}",
            result);
            AssertContainsWithoutNewline(
@"	location = /URI5005 {
		rewrite ^.* ""https://frontend.colid.url/resource?pidUri=https%3A%2F%2Fpid.bayer.com%2FURI5005"";
	}",
            result);
        }

        [Fact]
        public void GetCurrentProxyConfiguration_SingleResources_NestedProxies_RewriteToEditorFrontendAndExternal_Successful()
        {
            _resourceRepo.Setup(ms => ms.GetResourcesForProxyConfiguration(It.IsAny<IList<string>>(), _resourecGraph, It.IsAny<Uri>())).Returns(
                new List<ResourceProxyDTO>()
                {
                    new ResourceProxyDTO()
                    {
                        PidUrl = "https://pid.bayer.com/URI5004",
                        TargetUrl = "https://external.foo.bar/",
                        ResourceVersion = null,
                        NestedProxies = new List<ResourceProxyDTO>()
                        {
                            new ResourceProxyDTO()
                            {
                                PidUrl = "https://pid.bayer.com/URI5005",
                                TargetUrl = null,
                                ResourceVersion = null
                            }
                        }
                    }
                }
            );

            var result = _service.GetCurrentProxyConfiguration();

            AssertContainsWithoutNewline(GetStandardHeader(), result);
            AssertContainsWithoutNewline(GetStandardLocation(), result);
            AssertContainsWithoutNewline(
@"	location = /URI5004 {
		rewrite ^.* ""https://external.foo.bar/"";
	}",
            result);
            AssertContainsWithoutNewline(
@"	location = /URI5005 {
		rewrite ^.* ""https://frontend.colid.url/resource?pidUri=https%3A%2F%2Fpid.bayer.com%2FURI5005"";
	}",
            result);
        }

        [Fact]
        public void GetCurrentProxyConfiguration_SingleResources_NestedProxies_RewriteToExternalAndEditorFrontend_Successful()
        {
            _resourceRepo.Setup(ms => ms.GetResourcesForProxyConfiguration(It.IsAny<IList<string>>(), _resourecGraph, It.IsAny<Uri>())).Returns(
                new List<ResourceProxyDTO>()
                {
                    new ResourceProxyDTO()
                    {
                        PidUrl = "https://pid.bayer.com/URI5004",
                        TargetUrl = null,
                        ResourceVersion = null,
                        NestedProxies = new List<ResourceProxyDTO>()
                        {
                            new ResourceProxyDTO()
                            {
                                PidUrl = "https://pid.bayer.com/URI5005",
                                TargetUrl = "https://external.foo.bar/",
                                ResourceVersion = null
                            }
                        }
                    }
                }
            );

            var result = _service.GetCurrentProxyConfiguration();

            AssertContainsWithoutNewline(GetStandardHeader(), result);
            AssertContainsWithoutNewline(GetStandardLocation(), result);
            AssertContainsWithoutNewline(
@"	location = /URI5004 {
		rewrite ^.* ""https://frontend.colid.url/resource?pidUri=https%3A%2F%2Fpid.bayer.com%2FURI5004"";
	}",
            result);
            AssertContainsWithoutNewline(
@"	location = /URI5005 {
		rewrite ^.* ""https://external.foo.bar/"";
	}",
            result);
        }

        [Fact]
        public void GetCurrentProxyConfiguration_MultipleResources_RewriteToEditorFrontend_Successful()
        {
            _resourceRepo.Setup(ms => ms.GetResourcesForProxyConfiguration(It.IsAny<IList<string>>(), _resourecGraph, It.IsAny<Uri>())).Returns(
                new List<ResourceProxyDTO>()
                {
                    new ResourceProxyDTO()
                    {
                        PidUrl = "https://pid.bayer.com/URI5004",
                        TargetUrl = null,
                        ResourceVersion = null
                    },
                    new ResourceProxyDTO()
                    {
                        PidUrl = "https://pid.bayer.com/URI5005",
                        TargetUrl = null,
                        ResourceVersion = null
                    }
                }
            );

            var result = _service.GetCurrentProxyConfiguration();

            AssertContainsWithoutNewline(GetStandardHeader(), result);
            AssertContainsWithoutNewline(GetStandardLocation(), result);
            AssertContainsWithoutNewline(
@"	location = /URI5004 {
		rewrite ^.* ""https://frontend.colid.url/resource?pidUri=https%3A%2F%2Fpid.bayer.com%2FURI5004"";
	}",
            result);
            AssertContainsWithoutNewline(
@"	location = /URI5005 {
		rewrite ^.* ""https://frontend.colid.url/resource?pidUri=https%3A%2F%2Fpid.bayer.com%2FURI5005"";
	}",
            result);
        }

        private void AssertContainsWithoutNewline(string expectedSubstring, string actualString)
        {
            Assert.Contains(expectedSubstring.Replace("\r\n", "\n"), actualString.Replace("\r\n", "\n"));
        }
    }
}
