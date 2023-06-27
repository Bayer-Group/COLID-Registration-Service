using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.RegistrationService.Services.Interface
{
    public interface ICacheManagerService
    {
        /// <summary>
        /// Flush all cache
        /// </summary>
        /// <returns></returns>
        public void ClearCache();
    }
}
