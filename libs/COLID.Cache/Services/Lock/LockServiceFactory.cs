using System.Threading.Tasks;
using RedLockNet;

namespace COLID.Cache.Services.Lock
{
    public class LockServiceFactory: ILockServiceFactory
    {
        private readonly IDistributedLockFactory _distributedLockFactory;

        public LockServiceFactory(IDistributedLockFactory distributedLockFactory)
        {
            _distributedLockFactory = distributedLockFactory;
        }

        public ILockService CreateLockService()
        {
            return new LockService(_distributedLockFactory);
        }
    }
}
