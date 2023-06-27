using COLID.Graph.Metadata.Constants;
using COLID.RegistrationService.Services.Implementation;
using COLID.RegistrationService.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading.Tasks;
using COLID.RegistrationService.Common.DataModel.DistributionEndpoints;
using Microsoft.AspNetCore.Http;
using COLID.RegistrationService.Common.DataModels.Contacts;

namespace COLID.RegistrationService.WebApi.Controllers.V3
{
    [ApiController]
    [Authorize]
    [ApiVersion(Constants.API.Version.V3)]
    [Route("api/v{version:apiVersion}/resourceDataValidityCheck")]
    [Produces(MediaTypeNames.Application.Json)]
    public class ResourceDataValidityCheckController : Controller
    {
        private readonly IResourceDataValidityCheckService _resourceDataValidityCheckService;
        private readonly IRemoteAppDataService _remoteAppDataService;

        public ResourceDataValidityCheckController(IResourceDataValidityCheckService resourceDataValidityCheckService, IRemoteAppDataService remoteAppDataService)
        {
            _resourceDataValidityCheckService = resourceDataValidityCheckService;
            _remoteAppDataService = remoteAppDataService;
        }

        /// <summary>
        /// Checks that data stewards and distribution endpoint contacts are still valid and sends notifications 
        /// for invalid contacts
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("checkDataStewardsAndDistributionEndpointContacts")]
        public IActionResult CheckDataStewardsAndDistributionEndpointContactsAndNotifyUsers()
        {
            _resourceDataValidityCheckService.PushContactsToCheckInQueue();
            return Ok();
        }

        /// <summary>
        /// Test the endpoints and notify the users
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("checkDistributionEndpoints")]
        public IActionResult TestDistributionEndpoints()
        {
            _resourceDataValidityCheckService.PushEndpointsInQueue();
            return Ok();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="distributionPidUri"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("checkSingleDistributionEndpoint")]
        public IActionResult TestSingleDistributionEndpoint(Uri distributionPidUri)
        {
            _resourceDataValidityCheckService.PushSingleEndpointInQueue(distributionPidUri);
            return Ok();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("flaggedResources")]
        [ProducesResponseType(typeof(IList<DistributionEndpointsTest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public IList<DistributionEndpointsTest> GetFlaggedResources()
        {
            return _resourceDataValidityCheckService.GetBrokenEndpoints();
        }

        [HttpGet]
        [Route("setInvalidResourceDataInElastic")]
        public void SetInvalidResourceDataInElastic()
        {
            _resourceDataValidityCheckService.PushDataFlaggingInQueue();
        }

        [HttpGet]
        [Route("getPidUrisForInvalidDataResources")]
        public IList<Uri> GetPidUrisForInvalidDataResources()
        {
            return _resourceDataValidityCheckService.GetPidUrisForInvalidDataResources();
        }

        [HttpPost]
        [Route("testUsersValidity")]
        public async Task<IList<AdUserDto>> TestUsersValidityEnpoint([FromBody] ISet<string> users)
        {
            return await _remoteAppDataService.CheckUsersValidity(users);
        }
    }
}
