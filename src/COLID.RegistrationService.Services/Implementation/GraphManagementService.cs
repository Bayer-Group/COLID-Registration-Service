using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using COLID.AWS.DataModels;
using COLID.AWS.Interface;
using COLID.Common.Extensions;
using COLID.Common.Utilities;
using COLID.Exception.Models;
using COLID.Exception.Models.Business;
using COLID.Graph.Metadata.Constants;
using COLID.Graph.Metadata.DataModels.MetadataGraphConfiguration;
using COLID.Graph.Metadata.Services;
using COLID.Graph.Triplestore.Exceptions;
using COLID.Graph.TripleStore.Repositories;
using COLID.RegistrationService.Common.DataModel.Graph;
using COLID.RegistrationService.Common.DataModels.Graph;
using COLID.RegistrationService.Repositories.Interface;
using COLID.RegistrationService.Services.Interface;
using COLID.StatisticsLog.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using VDS.RDF;
using VDS.RDF.Writing;

namespace COLID.RegistrationService.Services.Implementation
{
    public class GraphManagementService : IGraphManagementService
    {
        private readonly IGraphManagementRepository _graphManagementRepo;
        private readonly IGraphRepository _graphRepo;
        private readonly IMetadataGraphConfigurationService _graphConfigurationService;
        private readonly IAuditTrailLogService _auditTrailLogService;
        private readonly IAmazonS3Service _awsS3Service;
        private readonly INeptuneLoaderConnector _neptuneLoader;
        private readonly AmazonWebServicesOptions _awsConfig;
        private readonly IMetadataService _metadataService;

        public GraphManagementService(IGraphManagementRepository graphManagementRepository, IGraphRepository graphRepository,
            IMetadataGraphConfigurationService graphConfiguration, IAuditTrailLogService auditTrailLogService, IAmazonS3Service awsS3Service,
            INeptuneLoaderConnector neptuneLoader, IOptionsMonitor<AmazonWebServicesOptions> awsConfig, IMetadataService metadataService)
        {
            _graphManagementRepo = graphManagementRepository;
            _graphConfigurationService = graphConfiguration;
            _auditTrailLogService = auditTrailLogService;
            _graphRepo = graphRepository;
            _awsS3Service = awsS3Service;
            _neptuneLoader = neptuneLoader;
            _awsConfig = awsConfig.CurrentValue;
            _metadataService = metadataService;
        }

        public IList<GraphDto> GetGraphs(bool includeRevisionGraphs=false)
        {
            var graphNames = _graphManagementRepo.GetGraphs(includeRevisionGraphs);
            var graphConfigs = _graphConfigurationService.GetConfigurationOverview();

            var currentGraphConfig = graphConfigs.FirstOrDefault();
            var historicGraphConfigs = graphConfigs.Where(t => t != currentGraphConfig);

            var graphs = graphNames.Select(graph =>
            {
                var graphUri = new Uri(graph);

                if (IsActiveGraph(graph, currentGraphConfig))
                {
                    return new GraphDto(graphUri, Common.Enums.Graph.GraphStatus.Active, currentGraphConfig.StartDateTime);
                }

                if (historicGraphConfigs.TryGetFirstOrDefault(c => c.Graphs.Contains(graph), out var historicGraphConfig))
                {
                    return new GraphDto(graphUri, Common.Enums.Graph.GraphStatus.Historic, historicGraphConfig.StartDateTime);
                }

                return new GraphDto(graphUri, Common.Enums.Graph.GraphStatus.Unreferenced, string.Empty);
            }).ToList();

            return graphs;
        }

        public void DeleteGraph(Uri graph)
        {
            if (graph == null || !graph.IsAbsoluteUri)
            {
                throw new ArgumentException(Common.Constants.Messages.GraphMsg.InvalidFormat, nameof(graph));
            }

            var graphs = GetGraphs();
            var graphExists = graphs.TryGetFirstOrDefault(g => g.Name == graph, out var graphDto);

            if (!graphExists)
            {
                throw new GraphNotFoundException(Common.Constants.Messages.GraphMsg.NotExists, graph);
            }

            if (graphDto.Status != Common.Enums.Graph.GraphStatus.Unreferenced)
            {
                throw new ReferenceException(Common.Constants.Messages.GraphMsg.Referenced, graph.OriginalString);
            }

            _graphManagementRepo.DeleteGraph(graph);
            _auditTrailLogService.AuditTrail($"Graph in database with uri \"{graph}\" deleted.");
        }

        private static bool IsActiveGraph(string graph, MetadataGraphConfigurationOverviewDTO currentGraphConfig)
        {
            var isInCurrentConfig = null != currentGraphConfig && currentGraphConfig.Graphs.Contains(graph);
            var isMetadataConfigGraph = graph == COLID.Graph.Metadata.Constants.MetadataGraphConfiguration.Type;

            return isInCurrentConfig || isMetadataConfigGraph;
        }

        public async Task<NeptuneLoaderResponse> ImportGraph(IFormFile turtleFile, Uri graphName, bool overwriteExisting = false)
        {
            Guard.IsValidUri(graphName);
            CheckFileTypeForTtl(turtleFile);

            var fileUploadInfo = await _awsS3Service.UploadFileAsync(_awsConfig.S3BucketForGraphs, turtleFile);

            var graphExists = _graphRepo.CheckIfNamedGraphExists(graphName);
            if (graphExists && !overwriteExisting)
            {
                throw new GraphAlreadyExistsException(graphName);
            }

            var loaderResponse = await _neptuneLoader.LoadGraph(fileUploadInfo.S3KeyName, graphName);
            return loaderResponse;
        }

        public byte[] DownloadGraph(Uri graphName)
        {
            Guard.IsValidUri(graphName);

            var result = _graphManagementRepo.GetGraph(graphName);
            using (var memStream = new MemoryStream())
            using (var streamWriter = new StreamWriter(memStream))
            {
                CompressingTurtleWriter tw = new CompressingTurtleWriter();
                tw.Save(result, streamWriter, true);
                streamWriter.Flush();

                return memStream.ToArray();
            }
        }

        private static void CheckFileTypeForTtl(IFormFile turtleFile)
        {
            if (Path.GetExtension(turtleFile.FileName) != ".ttl" || !MediaTypeNames.Application.Octet.Equals(turtleFile.ContentType, StringComparison.Ordinal))
            {
                throw new BusinessException("The given file/content type is not valid, only .ttl-files are allowed.");
            }
        }

        public async Task<NeptuneLoaderStatusResponse> GetGraphImportStatus(Guid loadId)
        {
            var status = await _neptuneLoader.GetStatus(loadId);
            return status;
        }

        public IList<string> GetAllKeywordGraphs()
        {
            var allGraphs = GetGraphs();
            var curActiveKeyWordGraphs = _graphConfigurationService.GetAllKeywordGraphs();
            var unrefGraphs = allGraphs.Where(c=>c.Status == Common.Enums.Graph.GraphStatus.Unreferenced).ToList().Select(c => c.Name.ToString()).ToList();
            curActiveKeyWordGraphs.AddRange(unrefGraphs);
            return curActiveKeyWordGraphs.OrderBy(s => s).ToList();
        }

        
        public IList<Uri> GetGraphType(Uri graph)
        {
            return _graphManagementRepo.GetGraphType(graph);
        }

        public IList<GraphKeyWordUsage> GetKeyWordUsageInGraph(Uri graph)
        {
            return _graphManagementRepo.GetKeyWordUsageInGraph(graph, _metadataService.GetInstanceGraph(PIDO.PidConcept));
        }

        public Uri ModifyKeyWordGraph(UpdateKeyWordGraph changes)
        {
            //Validate whether Graph is already in use
            MetadataGraphConfigurationResultDTO allUsedGraphs = _graphConfigurationService.GetLatestConfiguration();
            if (allUsedGraphs.Properties.SelectMany(x => x.Value).Select(x => x).OfType<string>().Contains(changes.SaveAsGraph.ToString()))
            {
                throw new NotSupportedException(Common.Constants.Messages.GraphMsg.InUse);
            }
            
            //Do Modifications in an InMemory graph
            var curGraph = _graphManagementRepo.ModifyKeyWordGraph(changes);

            //Check and generate a new graph version name
            if (_graphRepo.CheckIfNamedGraphExists(changes.SaveAsGraph))
            {
                _graphManagementRepo.DeleteGraph(changes.SaveAsGraph);
            }

            //Generate ntriple from the InMemory graph
            using (System.IO.StringWriter sw = new System.IO.StringWriter())
            {
                NTriplesWriter writer = new NTriplesWriter();

                writer.Save(curGraph, sw);
                String nTriples = sw.ToString();

                //Save Graph
                _graphManagementRepo.InsertGraph(changes.SaveAsGraph, nTriples);
            }
            
            return changes.SaveAsGraph;
        }
        public IGraph GetGraph(Uri namedGraph)
        {
            return _graphManagementRepo.GetGraph(namedGraph);
        }

            ///// <summary>
            ///// Generate a new Version name of the graph
            ///// </summary>
            ///// <param name="curGraphName">current graph Name</param>
            ///// <param name="appendText">a text to append</param>
            ///// <param name="appendVersion">a version number to append</param>
            ///// <returns>New graph Name</returns>
            //private static Uri ConstructNewVersionGraphName(Uri curGraphName, string appendText, int appendVersion )
            //{
            //    string newGraphName = "";

            //    string[] splitGraphName = curGraphName.ToString().Split('/');

            //    if (splitGraphName.Length > 0)
            //    {
            //        if (splitGraphName[splitGraphName.Length - 2] == appendText)
            //        {
            //            splitGraphName[splitGraphName.Length - 1] = appendVersion.ToString();                    
            //            newGraphName = String.Join("/", splitGraphName);
            //        }
            //        else
            //        {
            //            newGraphName = curGraphName.ToString() + "/" + appendText + "/" + appendVersion;
            //        }
            //    }

            //    return new Uri(newGraphName);
            //}

        }
}
