using System;
using COLID.Exception.Models;
using COLID.Graph.Metadata.Constants;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.TripleStore.AWS;
using Newtonsoft.Json;

namespace COLID.Graph.Triplestore.Exceptions
{
    public class NeptuneLoaderException : BusinessException
    {
        [JsonProperty]
        public virtual NeptuneLoaderErrorResponse NeptuneLoaderErrorResponse { get; }

        public NeptuneLoaderException(NeptuneLoaderErrorResponse errorResponse) : base(Messages.AWSNeptune.Failed)
        {
            NeptuneLoaderErrorResponse = errorResponse;
        }

        public NeptuneLoaderException(string message, NeptuneLoaderErrorResponse errorResponse)
            : base(message)
        {
            NeptuneLoaderErrorResponse = errorResponse;
        }


        public NeptuneLoaderException(string message, NeptuneLoaderErrorResponse errorResponse, System.Exception inner)
            : base(message, inner)
        {
            NeptuneLoaderErrorResponse = errorResponse;
        }
    }
}
