using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using COLID.Exception.Attributes;
using COLID.Exception.Models;

namespace COLID.Graph.HashGenerator.Exceptions
{
    [StatusCode(HttpStatusCode.BadRequest)]
    public class MissingPropertiesException : BusinessException
    {
        public MissingPropertiesException()
        {
        }

        public MissingPropertiesException(string message) : base(message)
        {
        }
    }
}
