using System.Net;
using COLID.Exception.Attributes;
using COLID.Exception.Models;
using RedLockNet;

namespace COLID.Cache.Exceptions
{
    [StatusCode(HttpStatusCode.Locked)]
    public class ResourceLockedException: BusinessException
    {
        public IRedLock RedLock { get; set; }

        public ResourceLockedException()
        {
        }

        public ResourceLockedException(string message) : base(message)
        {
        }

        public ResourceLockedException(string message, IRedLock redLock) : base(message)
        {
            RedLock = redLock;
        }

        public ResourceLockedException(string message, System.Exception innerException) : base(message, innerException)
        {
        }

        public ResourceLockedException(string message, System.Exception innerException, IRedLock redLock) : base(message, innerException)
        {
            RedLock = redLock;
        }

        
    }
}
