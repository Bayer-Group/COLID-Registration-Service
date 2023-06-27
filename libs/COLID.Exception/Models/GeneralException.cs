using System.Net;
using System.Reflection;
using COLID.Exception.Attributes;
using Newtonsoft.Json;

namespace COLID.Exception.Models
{
    [StatusCode(HttpStatusCode.InternalServerError)]
    [JsonObject(MemberSerialization.OptIn)]
    public class GeneralException : System.Exception
    {
        [JsonProperty]
#pragma warning disable CA1721 // Property names should not match get methods
        public string Type => GetType().Name;
#pragma warning restore CA1721 // Property names should not match get methods

        [JsonProperty]
        public int Code => GetType().GetCustomAttribute<StatusCodeAttribute>(true).GetCode();

        [JsonProperty]
        public string RequestId { get; set; }

        [JsonProperty]
        public string ApplicationId { get; set; }

        [JsonProperty]
        public override string Message => base.Message;

        public GeneralException()
        {
        }

        public GeneralException(string message)
            : base(message)
        {
        }

        public GeneralException(string message, System.Exception inner)
            : base(message, inner)
        {
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
