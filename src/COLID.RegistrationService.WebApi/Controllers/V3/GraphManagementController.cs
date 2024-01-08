using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using System.Threading.Tasks;
using COLID.Common.DataModel.Attributes;
using COLID.Graph.Utils;
using COLID.Identity.Requirements;
using COLID.RegistrationService.Common.DataModels.Graph;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.WebApi.Filters;
using COLID.StatisticsLog.Type;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace COLID.RegistrationService.WebApi.Controllers.V3
{
    /// <summary>
    /// All named graph specific operations
    /// </summary>
    [ApiController]
    [Authorize(Policy = nameof(SuperadministratorRequirement))]
    [ApiVersion(Constants.API.Version.V3)]
    [Route("api/v{version:apiVersion}/graph")]
    [Produces(MediaTypeNames.Application.Json)]
    public class GraphManagementController : Controller
    {
        private readonly IGraphManagementService _graphService;
        private const string _mimetypeOctetStream = "application/octet-stream";

        /// <summary>
        /// API endpoint for graph management.
        /// </summary>
        /// <param name="graphService">The service for graph management</param>
        public GraphManagementController(IGraphManagementService graphService)
        {
            _graphService = graphService;
        }

        /// <summary>
        /// Returns all used named graphs of the triple store
        /// </summary>
        /// <returns>List of named graphs</returns>
        /// <response code="200">Returns all used named graphs</response>
        /// <response code="404">If no metadata graph configuration exists</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpGet]
        public IActionResult GetGraphs([NotRequiredAttribute] bool includeRevisionGraphs = false)
        {
         
            var graphs = _graphService.GetGraphs(includeRevisionGraphs);
            return Ok(graphs);
        }

        /// <summary>
        /// Deletes a graph unless it is used by the system.
        /// </summary>
        /// <response code="200">If the graph was successfully deleted.</response>
        /// <response code="400">If graph is used by the system or some other business exceptions occurs</response>
        /// <response code="404">If graph does not exist</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpDelete]
        [ValidateActionParameters]
        [Log(LogType.AuditTrail)]
        public IActionResult DeleteGraph([FromQuery] Uri graph)
        {
            _graphService.DeleteGraph(graph);
            return Ok();
        }

        /// <summary>
        /// Uploads a graph to database. This will trigger the the graph upload to AWS Neptune and returns a loadId (UUID) as well as the final graph uri.
        /// </summary>
        /// <param name="turtleFile">The file to upload</param>
        /// <param name="graphName">Name of the named graph</param>
        /// <param name="overwriteExisting">flag if existing graphs should be overwritten</param>
        /// <response code="200">If import succeeds</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPost, DisableRequestSizeLimit]
        [Log(LogType.AuditTrail)]
        public async Task<IActionResult> UploadGraph([Required] IFormFile turtleFile, Uri graphName, bool overwriteExisting = false)
        {
            var loadId = await _graphService.ImportGraph(turtleFile, graphName, overwriteExisting);
            return Ok(loadId);
        }

        /// <summary>
        /// Get the current status of graph upload for the given request id, which is returned from neptune's loader.
        /// see https://docs.aws.amazon.com/neptune/latest/userguide/load-api-reference-status-examples.html for more
        /// </summary>
        [HttpGet]
        [Route("{loadId}")]
        public async Task<IActionResult> GetGraphUploadStatus(Guid loadId)
        {
            var status = await _graphService.GetGraphImportStatus(loadId);
            return Ok(status);
        }

        /// <summary>
        /// Download the graph with the specified graph name in turtlefile format.
        /// </summary>
        [HttpGet("download")]
        public IActionResult DownloadGraph([FromQuery] Uri graph)
        {
            var file = File(_graphService.DownloadGraph(graph), _mimetypeOctetStream, GraphUtils.GetFileName(graph));            
            return file;
        }

        /// <summary>
        /// Returns the keyword graphs that are currently in use.
        /// </summary>
        /// <returns>The latest keyword graphs</returns>
        /// <response code="200">Returns the latest keyword graphs</response>
        /// <response code="404">If no keyword graph exists</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpGet]
        [Route("keywordGraphs")]
        public IActionResult GetAllKeywordGraphs()
        {
            return Ok(_graphService.GetAllKeywordGraphs());
        }

        /// <summary>
        /// Get the rdf type of instances in the graph, checks in the first triple only so multiple types will be ignored
        /// assuming one graph will hold only one type of instance
        /// </summary>
        /// <param name="graph"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("graphType")]
        public IActionResult GetGraphType(Uri graph)
        {
            return Ok(_graphService.GetGraphType(graph));
        }

        /// <summary>
        /// Gets all keywords and thier usage for the given graph.        
        /// </summary>
        /// <param name="graph">the graph to find keywords</param>
        /// <returns>keywords and thier usage</returns>
        [HttpGet]
        [Route("keywordUsageInGraph")]
        public IActionResult GetKeyWordUsageInGraph(Uri graph)
        {
            return Ok(_graphService.GetKeyWordUsageInGraph(graph));
        }

        /// <summary>
        /// Creates a new unreferenced graph with the changes
        /// </summary>
        /// <param name="changes">changes to be done in the Graph.</param>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPost]
        [Route("modifyKeyWordGraph")]
        public IActionResult ModifyKeyWordGraph(UpdateKeyWordGraph changes)
        {
            return Ok(_graphService.ModifyKeyWordGraph(changes));
        }
    }
}
