
using COLID.RegistrationService.Common.DataModel.Status;

namespace COLID.RegistrationService.Services.Interface
{
    public interface IStatusService
    {
        /// <summary>
        /// Determine and return build informations of the current COLID version.
        /// </summary>
        /// <returns>the current build information</returns>
        BuildInformationDTO GetBuildInformation();

    }
}
