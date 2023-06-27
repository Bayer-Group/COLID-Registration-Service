using System;
using System.IO;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;
using COLID.Exception.Models;
using COLID.Exception.Models.Technical;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace COLID.Exception
{
    /// <summary>
    /// Central exception handler Middleware
    /// </summary>
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly string _applicationId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next.</param>
        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _jsonSerializerSettings = new JsonSerializerSettings();
            _jsonSerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            _jsonSerializerSettings.Formatting = Formatting.Indented;
            _applicationId = configuration["AzureAd:ClientId"];
        }

        /// <summary>
        /// Invokes the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (GeneralException exception)
            {
                _logger.LogError(exception, exception.Message);
                await HandleExceptionAsync(httpContext, exception);
            }
            catch (System.Exception exception) when (IsBusinessException(exception))
            {
                _logger.LogError(exception, exception.Message);
                var businessException = new BusinessException(exception.Message, exception);
                await HandleExceptionAsync(httpContext, businessException);
            }
            catch (System.Exception exception)
            {
                _logger.LogError(exception, exception.Message);
                var generalException = new GeneralException($"An unhandled exception has occurred: {exception.Message}", exception);
                await HandleExceptionAsync(httpContext, generalException);
            }
        }

        /// <summary>
        /// Checks if a System.Exception is of a specific type to transform it to a BusinessException.
        /// </summary>
        /// <param name="exception">The exception to check</param>
        /// <returns>True, if this exceptions is of the</returns>
        private static bool IsBusinessException(System.Exception exception)
        {
            return exception is ArgumentException || exception is FormatException || exception is JsonReaderException ||
                   exception is InvalidCastException  || exception is FileNotFoundException;
        }

        /// <summary>
        /// Handles all exceptions that are thrown by COLID and could not be treated.
        /// </summary>
        /// <param name="httpContext">the context of request.</param>
        /// <param name="generalException">New COLID exception that is passed on to the user.</param>
        /// <param name="exception">The untreated expcetion.</param>
        /// <returns></returns>
        private async Task HandleExceptionAsync(HttpContext httpContext, GeneralException generalException)
        {
            httpContext.Response.ContentType = MediaTypeNames.Application.Json;
            httpContext.Response.StatusCode = generalException.Code;
            generalException.RequestId = httpContext.TraceIdentifier;
            generalException.ApplicationId = _applicationId;

            await httpContext.Response.WriteAsync(JsonConvert.SerializeObject(generalException, _jsonSerializerSettings));
        }
    }
}
