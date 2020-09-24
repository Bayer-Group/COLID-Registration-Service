using System;
using Newtonsoft.Json;

namespace COLID.Exception.Models.Business
{
    public class InvalidFormatException : BusinessException
    {
        [JsonProperty]
        public string Id { get; set; }

        public InvalidFormatException(string message) : base(message)
        {
        }

        public InvalidFormatException(string message, string id) : base(message)
        {
            Id = id;
        }

        public InvalidFormatException(string message, Uri id) : base(message)
        {
            Id = id.ToString();
        }

        public InvalidFormatException(string message, string id, System.Exception innerException) : base(message, innerException)
        {
            Id = id;
        }

        public InvalidFormatException(string message, Uri id, System.Exception innerException) : base(message, innerException)
        {
            Id = id.ToString();
        }
    }
}
