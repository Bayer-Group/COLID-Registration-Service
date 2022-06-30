using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using COLID.IronMountainService.Common.Models;

namespace COLID.RegistrationService.Repositories.Interface
{
    public interface IIronMountainRepository
    {
        /// <summary>
        /// Returns a list containing all record classes referenced in Iron Mountain
        /// </summary>
        /// <returns>A list of Iron Mountain Record Classes</returns>
        Task<IronMountainRentionScheduleDto> GetIronMountainData();
    }
}
