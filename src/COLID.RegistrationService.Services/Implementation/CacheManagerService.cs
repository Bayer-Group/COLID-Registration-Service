using System;
using System.Collections.Generic;
using System.Text;
using COLID.Cache.Services;
using COLID.RegistrationService.Services.Interface;

namespace COLID.RegistrationService.Services.Implementation
{
    public class CacheManagerService: ICacheManagerService
    {
        private readonly ICacheService _cacheService;

        public CacheManagerService(ICacheService cacheService)
        {
            _cacheService = cacheService;
        }

        public void ClearCache()
        {
            _cacheService.Clear();
        }
    }
}
