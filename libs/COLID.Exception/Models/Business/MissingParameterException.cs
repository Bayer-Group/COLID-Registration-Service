using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace COLID.Exception.Models.Business
{
    public class MissingParameterException : BusinessException
    {
        [JsonProperty]
        public IList<string> Parameters { get; set; }

        public MissingParameterException(string message, IList<string> parameters) : base(message)
        {
            Parameters = parameters;
        }

        public MissingParameterException(string message, IList<string> parameters, System.Exception inner) : base(message, inner)
        {
            Parameters = parameters;
        }
    }
}
