using System;
using System.Net;
using COLID.Exception.Attributes;
using Newtonsoft.Json;

namespace COLID.Exception.Models.Business
{
    [StatusCode(HttpStatusCode.NotFound)]
    public class GraphNotFoundException : BusinessException
    {
        [JsonProperty]
        public Uri Uri { get; set; }

        public GraphNotFoundException(string message, Uri uri) : base(message)
        {
            Uri = uri;
        }

        public GraphNotFoundException(string message, Uri uri, System.Exception inner) : base(message, inner)
        {
            Uri = uri;
        }

        public GraphNotFoundException()
        {
        }
    }
}
