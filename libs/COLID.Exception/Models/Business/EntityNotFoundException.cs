using System.Net;
using COLID.Exception.Attributes;
using Newtonsoft.Json;

namespace COLID.Exception.Models.Business
{
    [StatusCode(HttpStatusCode.NotFound)]
    public class EntityNotFoundException : BusinessException
    {
        private string _v;

        [JsonProperty]
        public string Id { get; set; }

        public EntityNotFoundException(string message, string id) : base(message)
        {
            Id = id;
        }

        public EntityNotFoundException(string message, string id, System.Exception inner) : base(message, inner)
        {
            Id = id;
        }

        public EntityNotFoundException(string v)
        {
            _v = v;
        }

        public EntityNotFoundException()
        {
        }
    }
}
