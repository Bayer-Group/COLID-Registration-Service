using COLID.IronMountainService.Common.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace COLID.RegistrationService.Services.Interface
{
    public interface IIronMountainApiService
    {
        /// <summary>
        /// Returns a list containing all record classes referenced in Iron Mountain
        /// </summary>
        /// <returns>A list of Iron Mountain Record Classes</returns>
        Task<IronMountainRentionScheduleDto> GetAllRecordClasses();

        /// <summary>
        /// Retrieves PID Uris and their data categories
        /// and returns the list of relevant policies from Iron Mountain 
        /// </summary>
        /// <param name="policyRequestValues"></param>
        /// <returns>List of Resources PID URIs and its Iron Mountain Policies</returns>
        Task<List<IronMountainResponseDto>> GetResourcePolicies(ISet<IronMountainRequestDto> policyRequestValues);

    }
}
