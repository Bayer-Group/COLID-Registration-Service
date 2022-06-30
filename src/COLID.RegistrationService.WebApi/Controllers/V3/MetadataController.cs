using System.Collections.Generic;
using System.Net.Mime;
using COLID.Common.DataModel.Attributes;
using COLID.Common.Extensions;
using COLID.Graph.Metadata.DataModels.Metadata.Comparison;
using COLID.Graph.Metadata.Services;
using COLID.RegistrationService.WebApi.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.Metadata.DataModels.Metadata;

namespace COLID.RegistrationService.WebApi.Controllers.V3
{
    /// <summary>
    /// API endpoint for metadata.
    /// </summary>
    [ApiController]
    [Authorize]
    [ApiVersion(Constants.API.Version.V3)]
    [Route("api/v{version:apiVersion}/metadata")]
    [Produces(MediaTypeNames.Application.Json)]
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
        ///    Get api/metadata?entityType=Ontology
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
        /// Returns a list of merged metadata properties for given entity types. 
        /// Metadata are grouped under the same key and metadata of different types are stored in a list.
        /// </summary>
        /// <remarks>
        ///    Get api/metadata/comparison?entityType=Ontology&entityType=Dataset
        /// </remarks>
        /// <param name="entityType">The name of the entity type</param>
        /// <param name="metadataConfig">(Optional) The identifier of the metadata config to use</param>
        /// <returns>A list of merged metadata properties of given types</returns>
        /// <response code="200">Returns a list of metadata properties of given types</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpGet("comparison")]
        [ValidateActionParameters]
        public IActionResult GetComparisonMetadata([FromBody] IEnumerable<MetadataComparisonConfigTypesDto> metadataComparisonConfigTypes)
        {
            return Ok(_metadataService.GetComparisonMetadata(metadataComparisonConfigTypes));
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
            var hierarchy = _metadataService.GetResourceTypeHierarchy(firstEntityType);
            return Ok(hierarchy);
        }

        /// <summary>
        /// Returns the resource type hierarchy in datamarketplace format starting at the given resource type name with all its subclasses. If no type is given, the first type will be pid concepts
        /// </summary>
        /// <param name="firstEntityType">The name of the first resource type to start with</param>
        /// <returns>The resource type hierarchy of the given resource type name</returns>
        /// <response code="200">Returns the resource type hierarchy of the given resource type name</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpGet]
        [Route("hierarchyDmp")]
        public IActionResult GetResourceTypeHierarchyDmp([FromQuery, NotRequired] string firstEntityType)
        {
            var hierarchy = _metadataService.GetResourceTypeHierarchyDmp(firstEntityType);
            return Ok(hierarchy);
        }

        /// <summary>
        /// Returns the resource type hierarchy starting at the given resource type name with all its subclasses. If no type is given, the first type will be pid concepts
        /// </summary>
        /// <param name="entityTypes">List of Entity Objects with Linked Data information</param>
        /// <returns>The Resource Types eligible for a given link of the given resource type name</returns>
        /// <response code="200">Returns the resource type hierarchy of the given resource type name</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPost]
        [Route("instantiableResources")]
        public IActionResult GetInstantiableResourceTypes(List<Entity> entityTypes)
        {
            var linkedEntityTypes = _metadataService.GetLinkedEntityTypes(entityTypes);
            return Ok(linkedEntityTypes);
        }

        /// <summary>
        /// Returns all the category filters including the resource types in a raw format as it is saved in neptune.
        /// </summary>
        /// <returns>The current category filter that is saved in neptune in a format that is usable in the marketplace</returns>
        /// <response code="200">Returns the current category filter that is saved in neptune in a format that is usable in the marketplace</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpGet]
        [Route("categoryFilter")]
        public IActionResult GetCategoryFilter()
        {
            var result = _metadataService.GetCategoryFilter();
            return Ok(result);
        }

        /// <summary>
        /// Returns a specific category filter including the resource types in a raw format as it is saved in neptune by given category name.
        /// </summary>
        /// <returns>The category filter with the given Name that is saved in a raw neptune format </returns>
        /// <response code="200">Returns the category filter with the specified name </response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpGet]
        [Route("categoryFilterByName")]
        public IActionResult GetCategoryFilter(string CategoryFilterName)
        {
            var result = _metadataService.GetCategoryFilter(CategoryFilterName);
            return Ok(result);
        }

        /// <summary>
        /// Returns the category filters including the resource types in datamarketplace format.
        /// </summary>
        /// <returns>The current category filters that are saved in neptune in a format that is usable in the marketplace</returns>
        /// <response code="200">Returns the current category filters that are saved in neptune in a format that is usable in the marketplace</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpGet]
        [Route("categoryFilterDmp")]
        public IActionResult GetCategoryFilterDmp()
        {
            var result = _metadataService.GetCategoryFilterDmp();
            return Ok(result);

        }

        /// <summary>
        /// Creates or updated a category filter in neptune
        /// </summary>
        /// <param name="category">the category filter including the resource types</param>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPost]
        [Route("categoryFilter")]
        public IActionResult CreateOrUpdateCategoryFilter(CategoryFilterDTO category)
        {
            _metadataService.CreateOrUpdateCategoryFilter(category);
            return Ok();
        }

        /// <summary>
        /// Deletes a specific category filter in neptune by given name
        /// </summary>
        /// <param name="CategoryFilterName">the name of the category filter to be deleted</param>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpDelete]
        [Route("categoryFilter")]
        public IActionResult DeleteCategoryFilter(string CategoryFilterName)
        {
            _metadataService.DeleteCategoryFilter(CategoryFilterName);
            return Ok();
        }

    }
}
