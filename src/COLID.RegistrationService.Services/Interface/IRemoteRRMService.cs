using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using COLID.RegistrationService.Common.DataModels.RelationshipManager;
using COLID.RegistrationService.Common.DataModels.Search;

namespace COLID.RegistrationService.Services.Interface
{
    public interface IRemoteRRMService
    {
        /// <summary>
        /// Fetches all the RRM Maps from Resource relationship manager API
        /// </summary>
        /// <returns>List of maps in form of MapProxyDTO</returns>
        Task<List<MapProxyDTO>> GetAllRRMMaps();
    }
}
