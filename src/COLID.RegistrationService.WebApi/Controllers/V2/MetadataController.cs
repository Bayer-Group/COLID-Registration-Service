using System;
using System.Net.Mime;
using COLID.Common.DataModel.Attributes;
using COLID.Common.Extensions;
using COLID.Graph.Metadata.Constants;
using COLID.Graph.Metadata.Services;
using COLID.RegistrationService.WebApi.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace COLID.RegistrationService.WebApi.Controllers.V2
{
    /// <summary>
    /// API endpoint for metadata.
    /// </summary>
    [ApiController]
    [Authorize]
    [ApiVersion(Constants.API.Version.V1)]
    [ApiVersion(Constants.API.Version.V2)]
    [Route("api/v{version:apiVersion}/metadata")]
    [Produces(MediaTypeNames.Application.Json)]
    [Obsolete]
    public class MetadataController : Controller
    {
        private readonly IMetadataService _metadataService;

        /// <summary>
        /// API endpoint for metadata.
        /// </summary>
        /// <param name="metadataService">The service for metedata</param>
        public MetadataController(IMetadataService metadataService)
        {
            _metadataService = metadataService;
        }

        /// <summary>
        /// Returns a list of the metadata properties of the given resource type. If no type can be found, an empty list is returned.
        /// </summary>
        /// <remarks>
        ///    Get api/graph?entityType=Ontology
        /// </remarks>
        /// <param name="entityType">The name of the entity type</param>
        /// <param name="metadataConfig">(Optional) The identifier of the metadata config to use</param>
        /// <returns>A list of metadata properties of the given type of the resource</returns>
        /// <response code="200">Returns a list of metadata properties of the given type of the resource</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpGet]
        [ValidateActionParameters]
        public IActionResult GetMetadataPropertiesForEntityType([FromQuery] string entityType, [FromQuery, NotRequired] string metadataConfig)
        {
            if (!metadataConfig.IsNullOrEmpty())
            {
                return Ok(_metadataService.GetMetadataForEntityTypeInConfig(entityType, metadataConfig));
            }
            return Ok(_metadataService.GetMetadataForEntityType(entityType));
        }

        /// <summary>
        /// Returns the resource type hierarchy starting at the given resource type name with all its subclasses. If no type is given, the first type will be pid concepts
        /// </summary>
        /// <param name="firstEntityType">The name of the first resource type to start with</param>
        /// <returns>The resource type hierarchy of the given resource type name</returns>
        /// <response code="200">Returns the resource type hierarchy of the given resource type name</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpGet]
        [Route("hierarchy")]
        public IActionResult GetResourceTypeHierarchy([FromQuery, NotRequired]string firstEntityType)
        {
            var defaultResourceType = Resource.Type.FirstResouceType;

            return Ok(_metadataService.GetResourceTypeHierarchy(string.IsNullOrWhiteSpace(firstEntityType) ? defaultResourceType : firstEntityType));
        }
    }
}
