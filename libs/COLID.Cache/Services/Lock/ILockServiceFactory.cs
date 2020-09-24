using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace COLID.Cache.Services.Lock
{
    /// <summary>
    /// The creator class to initiate lock services using the factory's set of redis endpoints.
    /// </summary>
    public interface ILockServiceFactory
    {
        /// <summary>
        /// Gets a lock service using the factory's set of redis endpoints.
        /// </summary>
        /// <returns><see cref="ILockService"/></returns>
        public ILockService CreateLockService();
    }
}
