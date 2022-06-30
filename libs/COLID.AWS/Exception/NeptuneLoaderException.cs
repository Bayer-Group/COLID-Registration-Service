using System;
using COLID.AWS.Constants;
using COLID.AWS.DataModels;
using COLID.Exception.Models;
using Newtonsoft.Json;

namespace COLID.AWS.Exceptions
{
    public class NeptuneLoaderException : BusinessException
    {
        [JsonProperty]
        public virtual NeptuneLoaderErrorResponse NeptuneLoaderErrorResponse { get; }

        public NeptuneLoaderException(NeptuneLoaderErrorResponse errorResponse) : base(AWSConstants.ExceptionMessages.TurtleFileLoadingFailed)
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
