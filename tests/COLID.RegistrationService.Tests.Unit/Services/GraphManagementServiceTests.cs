﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using AngleSharp.Common;
using AutoMapper;
using COLID.AWS.DataModels;
using COLID.AWS.Interface;
using COLID.Exception.Models.Business;
using COLID.Graph.Metadata.DataModels.MetadataGraphConfiguration;
using COLID.Graph.Metadata.Services;
using COLID.Graph.Triplestore.Exceptions;
using COLID.Graph.TripleStore.Repositories;
using COLID.RegistrationService.Repositories.Interface;
using COLID.RegistrationService.Services.Implementation;
using COLID.RegistrationService.Services.Interface;
using COLID.StatisticsLog.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace COLID.RegistrationService.Tests.Unit.Services
{
    [ExcludeFromCodeCoverage]
    public class GraphManagementServiceTests
    {
        private readonly Mock<IGraphManagementRepository> _mockGraphMgmtRepo;
        private readonly Mock<IGraphRepository> _mockGraphRepository;
        private readonly Mock<IMetadataGraphConfigurationService> _mockMetadataGraphConfigService;
        private readonly Mock<IAuditTrailLogService> _mockAuditTrailLogService;
        private readonly Mock<IAmazonS3Service> _mockAwsS3Service;
        private readonly Mock<INeptuneLoaderConnector> _mockNeptuneLoaderConnector;

        private readonly IGraphManagementService _service;

        public GraphManagementServiceTests()
        {
            _mockGraphMgmtRepo = new Mock<IGraphManagementRepository>();
            _mockGraphRepository = new Mock<IGraphRepository>();
            _mockMetadataGraphConfigService = new Mock<IMetadataGraphConfigurationService>();
            _mockAuditTrailLogService = new Mock<IAuditTrailLogService>();
            _mockAwsS3Service = new Mock<IAmazonS3Service>();
            _mockNeptuneLoaderConnector = new Mock<INeptuneLoaderConnector>();
            SetupMockGraphMgmt();

            AmazonWebServicesOptions awsOptions = new AmazonWebServicesOptions();
            var awsOptionsMonitor = Mock.Of<IOptionsMonitor<AmazonWebServicesOptions>>(_ => _.CurrentValue == awsOptions);

            _service = new GraphManagementService(_mockGraphMgmtRepo.Object, _mockGraphRepository.Object,
                _mockMetadataGraphConfigService.Object, _mockAuditTrailLogService.Object, _mockAwsS3Service.Object,
                _mockNeptuneLoaderConnector.Object, awsOptionsMonitor);
        }

        private void SetupMockGraphMgmt()
        {
            var usedGraphs = new List<string>() { "https://pid.bayer.com/resource/1.0", "https://pid.bayer.com/resource/2.0/Draft", "https://pid.bayer.com/resource/historic", "https://pid.bayer.com/linkhistory" };
            var historicGraphs = new List<string>() { "https://pid.bayer.com/resource/2.0" };
            var unusedGraph = new List<string>() { "https://pid.bayer.com/colid/test/graph", COLID.Graph.Metadata.Constants.MetadataGraphConfiguration.Type };
            var graphs = usedGraphs.Concat(unusedGraph).Concat(historicGraphs);

            var historicGraphConfig = new MetadataGraphConfigurationOverviewDTO() { Graphs = historicGraphs, StartDateTime = DateTime.UtcNow.AddYears(-1).ToString("o") };
            var currentGraphConfig = new MetadataGraphConfigurationOverviewDTO() { Graphs = usedGraphs, StartDateTime = DateTime.UtcNow.ToString("o") };
            var graphConfig = new List<MetadataGraphConfigurationOverviewDTO>() { currentGraphConfig, historicGraphConfig };

            _mockGraphMgmtRepo.Setup(s => s.GetGraphs(false)).Returns(graphs);

            var g = new VDS.RDF.Graph(true)
            {
                BaseUri = new Uri("https://pid.bayer.com/resource/1.0")
            };

            _mockGraphMgmtRepo.Setup(s => s.GetGraph(new Uri("https://pid.bayer.com/resource/1.0"))).Returns(g);

            _mockMetadataGraphConfigService.Setup(s => s.GetConfigurationOverview()).Returns(graphConfig);
        }

        #region Get Graph

        [Fact]
        public void GetGraphs_Success()
        {
            // Act
            var resultGraph = _service.GetGraphs(false);

            // Assert
            Assert.NotNull(resultGraph);
            Assert.Equal(7, resultGraph.Count);
            Assert.Single(resultGraph, g => g.Status == RegistrationService.Common.Enums.Graph.GraphStatus.Unreferenced && string.IsNullOrWhiteSpace(g.StartTime));
            Assert.Single(resultGraph, g => g.Status == RegistrationService.Common.Enums.Graph.GraphStatus.Historic && !string.IsNullOrWhiteSpace(g.StartTime));
        }

        #endregion Get Graph

        #region Delete Region

        [Fact]
        public void DeleteGraph_Success()
        {
            var uri = new Uri("https://pid.bayer.com/colid/test/graph");
            _service.DeleteGraph(uri);
            _mockGraphMgmtRepo.Verify(g => g.DeleteGraph(uri));
        }

        [Fact]
        public void DeleteGraph_ThrowsException_IfUriIsNull()
        {
            // Act
            Assert.Throws<ArgumentException>(() => _service.DeleteGraph(null));
        }

        [Fact]
        public void DeleteGraph_ThrowsException_IfUriIsNotAbsoluteUri()
        {
            var uri = new Uri("/pid.bayer.com", UriKind.Relative);
            Assert.Throws<ArgumentException>(() => _service.DeleteGraph(uri));
        }

        [Fact]
        public void DeleteGraph_ThrowsException_IfGraphNotExists()
        {
            var uri = new Uri("https://pid.bayer.com/not-exists");
            Assert.Throws<GraphNotFoundException>(() => _service.DeleteGraph(uri));
        }

        [Theory]
        [InlineData("https://pid.bayer.com/resource/1.0")]
        [InlineData("https://pid.bayer.com/resource/2.0")]
        [InlineData(COLID.Graph.Metadata.Constants.MetadataGraphConfiguration.Type)]
        public void DeleteGraph_ThrowsException_IfGraphIsReferenced(string urlString)
        {
            var uri = new Uri(urlString);
            Assert.Throws<ReferenceException>(() => _service.DeleteGraph(uri));
        }

        #endregion Delete Region

        [Fact]
        public async Task DownloadGraph_Returns_Stream()
        {
            // Act
            var graph = new Uri("https://pid.bayer.com/resource/1.0");
            var result = await _service.DownloadGraph(graph);

            //Assert
            Assert.NotNull(result);
            Assert.True(result.Position == 0);
            Assert.True(result.Length > 0);
        }

        [Fact]
        public async Task DownloadGraph_Returns_GraphNotFoundExceptionWhenGraphNotExist()
        {
            // Act
            var graph = new Uri("https://pid.bayer.com/resource/xx");
            Assert.ThrowsAsync<GraphNotFoundException>(() => _service.DownloadGraph(graph));
        }

        [Fact]
        public async Task DownloadGraph_Returns_UriFormatExceptionWhenUriIsNull()
        {
            Assert.ThrowsAsync<ArgumentNullException>(() => _service.DownloadGraph(null));
        }

        [Fact]
        public async Task ImportGraph_Success()
        {
            // Arrange
            var s3FileInfo = new AmazonS3FileUploadInfoDto { FileName = "", FileSize = 0, FileType = "", S3KeyName = "s3://some.fancy.kid.said/i.like.turtles", S3ObjectUrl = "" };
            var loadId = Guid.NewGuid().ToString();
            var formFile = GenerateTtlFormFile();
            var graphName = new Uri("https://www.speedofart.com/glorious/painting/1.0");
            var expectedResponse = new NeptuneLoaderResponse
            {
                status = "200 OK",
                payload = new Dictionary<string, string> { { "loadId", loadId }, { "namedGraphName", graphName.AbsoluteUri } }
            };

            _mockAwsS3Service.Setup(x => x.UploadFileAsync(It.IsAny<string>(), It.IsAny<IFormFile>())).ReturnsAsync(s3FileInfo);
            _mockGraphRepository.Setup(x => x.CheckIfNamedGraphExists(It.IsAny<Uri>())).Returns(false);
            _mockNeptuneLoaderConnector.Setup(x => x.LoadGraph(s3FileInfo.S3KeyName, graphName)).ReturnsAsync(expectedResponse);

            // Act
            var result = await _service.ImportGraph(formFile, graphName, false);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResponse.status, result.status);
            Assert.True(result.payload.TryGetValue("loadId", out var resultLoadIdValue));
            Assert.Equal(loadId, resultLoadIdValue);
            Assert.True(result.payload.TryGetValue("namedGraphName", out var resultGraphName));
            Assert.Equal(graphName.AbsoluteUri, resultGraphName);
        }

        [Fact]
        public async Task ImportGraph_ThrowsException_IfGraphExistsAndOverwriteFalse()
        {
            var s3FileInfo = new AmazonS3FileUploadInfoDto { FileName = "", FileSize = 0, FileType = "", S3KeyName = "s3://some.fancy.kid.said/i.like.turtles", S3ObjectUrl = "" };
            var formFile = GenerateTtlFormFile();
            var graphName = new Uri("https://www.speedofart.com/glorious/painting/1.0");
            _mockAwsS3Service.Setup(x => x.UploadFileAsync(It.IsAny<string>(), It.IsAny<IFormFile>())).ReturnsAsync(s3FileInfo);
            _mockGraphRepository.Setup(x => x.CheckIfNamedGraphExists(It.IsAny<Uri>())).Returns(true);

            await Assert.ThrowsAsync<GraphAlreadyExistsException>(() =>
                 _service.ImportGraph(formFile, graphName, false));
        }

        [Fact]
        public async Task GetGraphImportStatus_Success()
        {
            // Arrange
            var expectedResponse = new NeptuneLoaderStatusResponse { Status = "200 OK", LoadStatus = "LOAD_COMPLETED", StartTime = "1598252845" };
            _mockNeptuneLoaderConnector.Setup(x => x.GetStatus(It.IsAny<Guid>())).ReturnsAsync(expectedResponse);

            // Act
            var result = await _service.GetGraphImportStatus(It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResponse.Status, result.Status);
            Assert.Equal(expectedResponse.LoadStatus, result.LoadStatus);
            Assert.Equal(expectedResponse.StartTime, result.StartTime);
        }

        private IFormFile GenerateTtlFormFile()
        {
            return new FormFile(Stream.Null, 0, 0, "test", "FancyFileName.ttl")
            {
                Headers = new HeaderDictionary(),
                ContentType = MediaTypeNames.Application.Octet
            };
        }
    }
}
