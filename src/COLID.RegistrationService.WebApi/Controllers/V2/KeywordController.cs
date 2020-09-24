using System;
using System.Net.Mime;
using System.Threading.Tasks;
using COLID.RegistrationService.Common.DataModel.Keywords;
using COLID.Identity.Requirements;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.WebApi.Controllers.V2.Filter;
using COLID.RegistrationService.WebApi.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace COLID.RegistrationService.WebApi.Controllers.V2
{
    /// <summary>
    /// API endpoint for keywords.
    /// </summary>
    [ApiController]
    [Authorize]
    [ApiVersion(Constants.API.Version.V1)]
    [ApiVersion(Constants.API.Version.V2)]
    [Route("api/v{version:apiVersion}")]
    [Produces(MediaTypeNames.Application.Json)]
    [TransformIdPropertyResponseFilter]
    [Obsolete]
    public class KeywordController : Controller
    {
        private readonly IKeywordService _keywordService;

        /// <summary>
        /// API endpoint for keywords.
        /// </summary>
        /// <param name="keywordService">The service for keywords</param>
        public KeywordController(IKeywordService keywordService)
        {
            _keywordService = keywordService;
        }

        /// <summary>
        /// Returns a list of all created Keywords.
        /// </summary>
        /// <returns>A list of all created keywords</returns>
        /// <response code="200">Returns the list of Keywords</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpGet]
        [Route("keywordList")]
        public IActionResult GetKeywords()
        {
            var keywords = _keywordService.GetEntities(null);

            return Ok(keywords);
        }

        /// <summary>
        /// Returns the keyword of the given subject.
        /// </summary>
        /// <param name="subject">The subject of a keyword.</param>
        /// <returns>A Keyword</returns>
        /// <response code="200">Returns the keyword of the given subject</response>
        /// <response code="404">If no keyword exists with the given subject</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpGet]
        [ValidateActionParameters]
        [Route("keyword")]
        public IActionResult GetKeywordById([FromQuery] string subject)
        {
            var keyword = _keywordService.GetEntity(subject);

            return Ok(keyword);
        }

        /// <summary>
        /// Deletes a keyword.
        /// </summary>
        /// <param name="subject">The subject of the keyword to delete.</param>
        /// <returns>A status code</returns>
        /// <response code="200">Returns a status code with a corresponding error message</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpDelete]
        [ValidateActionParameters]
        [Route("keyword")]
        [Authorize(Policy = nameof(SuperadministratorRequirement))]
        public IActionResult DeleteKeyword([FromQuery] string subject)
        {
            _keywordService.DeleteEntity(subject);

            return Ok();
        }

        /// <summary>
        /// Creates a keyword.
        /// </summary>
        /// <param name="keyword">The new keyword to create</param>
        /// <returns>A newly created keyword</returns>
        /// <response code="201">Returns the newly created keyword</response>
        /// <response code="400">If the keyword is invalid</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPost]
        [ValidateActionParameters]
        [Route("keyword")]
        public async Task<IActionResult> CreateKeyword([FromBody] KeywordRequestDTO keyword)
        {
            var newKeyword = await _keywordService.CreateEntity(keyword);

            return Created("/api/keyword/" + newKeyword.Entity.Id, newKeyword);
        }
    }
}
