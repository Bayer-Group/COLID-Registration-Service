using COLID.RegistrationService.Common.DataModel.Status;
using COLID.RegistrationService.Services.Interface;
using Microsoft.Extensions.Configuration;

namespace COLID.RegistrationService.Services.Implementation
{
    internal class StatusService : IStatusService
    {
        private readonly IConfiguration _configuration;

        public StatusService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public BuildInformationDTO GetBuildInformation()
        {
            return new BuildInformationDTO
            {
                VersionNumber = _configuration["Build:VersionNumber"],
                JobId = _configuration["Build:CiJobId"],
                PipelineId = _configuration["Build:CiPipelineId"],
                CiCommitSha = _configuration["Build:CiCommitSha"]
            };
        }
    }
}
