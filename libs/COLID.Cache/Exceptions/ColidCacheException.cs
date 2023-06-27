using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using COLID.Exception.Attributes;
using COLID.Exception.Models;
using RedLockNet;

namespace COLID.Cache.Exceptions
{
        [StatusCode(HttpStatusCode.InternalServerError)]
        public class ColidCacheException : TechnicalException
        {
            public ColidCacheException()
            {
            }

            public ColidCacheException(string message) : base(message)
            {
            }

            public ColidCacheException(string message, System.Exception innerException) : base(message, innerException)
            {
            }
            
        }
    }
