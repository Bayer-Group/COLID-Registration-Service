using System;
using COLID.Exception.Models;
using COLID.Graph.Metadata.Constants;
using Newtonsoft.Json;

namespace COLID.Graph.Triplestore.Exceptions
{
    public class GraphAlreadyExistsException : BusinessException
    {
        [JsonProperty]
        public virtual Uri Uri { get; }

        public GraphAlreadyExistsException(Uri existingGraphUri) : base(Messages.AWSNeptune.AlreadyExists)
        {
            Uri = existingGraphUri;
        }

        public GraphAlreadyExistsException(string message, Uri existingGraphUri) : base(message)
        {
            Uri = existingGraphUri;
        }


        public GraphAlreadyExistsException(string message, Uri existingGraphUri, System.Exception inner) : base(message, inner)
        {
            Uri = existingGraphUri;
        }
    }
}
