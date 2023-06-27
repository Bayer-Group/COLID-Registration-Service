using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using COLID.Exception.Attributes;
using COLID.Exception.Models;
using RedLockNet;

namespace COLID.Cache.Exceptions
{
    [StatusCode(HttpStatusCode.NotFound)]
    public class RedisKeyNotFoundException : BusinessException
    {
        public RedisKeyNotFoundException()
        {
        }

        public RedisKeyNotFoundException(string message) : base(message)
        {
        }

        public RedisKeyNotFoundException(string message, System.Exception innerException) : base(message, innerException)
        {
        }

    }
}
