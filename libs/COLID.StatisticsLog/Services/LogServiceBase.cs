using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Security.Cryptography;
using System.Text;
using Amazon;
using Amazon.Runtime;
using COLID.StatisticsLog.Configuration;
using COLID.StatisticsLog.DataModel;
using COLID.StatisticsLog.LogTypes;
using Elasticsearch.Net;
using Elasticsearch.Net.Aws;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Elasticsearch;
using Serilog.Sinks.Elasticsearch;

namespace COLID.StatisticsLog.Services
{
    public abstract class LogServiceBase : IDisposable
    {
#pragma warning disable CA1051 // Do not declare visible instance fields
        protected readonly IHttpContextAccessor _httpContextAccessor;
        protected readonly ILogger _logger;
        protected readonly string _productName;
        protected readonly string _layerName;
        protected readonly string _anonymizerKey;
        protected readonly AwsHttpConnection _awsHttpConnection;
        protected readonly SHA256 _sha256;
#pragma warning restore CA1051 // Do not declare visible instance fields

        protected LogServiceBase(IOptionsMonitor<Configuration.ColidStatisticsLogOptions> optionsAccessor, IHttpContextAccessor httpContextAccessor)
        {
            Contract.Requires(optionsAccessor != null);

            _httpContextAccessor = httpContextAccessor;
            var options = optionsAccessor.CurrentValue;
            _productName = options.ProductName;
            _layerName = options.LayerName;
            _anonymizerKey = options.AnonymizerKey;
            _sha256 = SHA256.Create();

            if (options.Enabled)
            {
                // if accessKey and secretKey are provided via configuration, use them. otherwise try to
                // determine by default AWS credentials resolution process see https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/net-dg-config-creds.html#creds-assign
                if (!string.IsNullOrWhiteSpace(options.AccessKey) && !string.IsNullOrWhiteSpace(options.SecretKey))
                {
                    var creds = new BasicAWSCredentials(options.AccessKey, options.SecretKey);
                    var region = RegionEndpoint.GetBySystemName(options.AwsRegion);
                    _awsHttpConnection = new AwsHttpConnection(creds, region);
                }
                else
                {
                    _awsHttpConnection = new AwsHttpConnection(options.AwsRegion);
                }
            }

            _logger = GetLogger(options);
        }

        protected virtual LoggerConfiguration GetLoggerConfiguration(ColidStatisticsLogOptions options)
        {
            if (options.Enabled == false)
            {
                return new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .WriteTo.Console();
            }

            return new LoggerConfiguration()
              .MinimumLevel.Verbose()
              .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(options.BaseUri)
              {
                  NumberOfShards = 1,
                  ModifyConnectionSettings = conn =>
                  {
                      var httpConnection = _awsHttpConnection;
                      var pool = new SingleNodeConnectionPool(options.BaseUri);
                      return new ConnectionConfiguration(pool, httpConnection);
                  },
                  IndexFormat = IndexFormat(options),
                  FailureCallback = e =>
                  {
                      Console.WriteLine("Error: Unable to submit event " + e.MessageTemplate);
                      if (e.Exception != null)
                      {
                          Console.WriteLine(e.Exception.StackTrace);
                      }
                  },
                  EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog |
                                     EmitEventFailureHandling.WriteToFailureSink |
                                     EmitEventFailureHandling.RaiseCallback,
                  CustomFormatter = new ElasticsearchJsonFormatter(),
              });
        }

        protected string IndexFormat(ColidStatisticsLogOptions options)
        {
            return $"{options.DefaultIndex}-{Suffix()}-" + "{0:yyyy.MM}";
        }

        private ILogger GetLogger(ColidStatisticsLogOptions options)
        {
            return GetLoggerConfiguration(options).CreateLogger();
        }

        protected abstract string Suffix();

        protected LogEntry CreateLogEntry<T>(string message, Dictionary<string, dynamic> additionalInfo = null) where T : ILogType
        {
            var logEntry = new LogEntry
            {
                Message = message,
                Location = "",
                Layer = _layerName,
                Product = _productName,
                HostName = Environment.MachineName,
                AdditionalInfo = additionalInfo ?? new Dictionary<string, dynamic>(),
            };

            EnrichLogEntry<T>(logEntry);

            return logEntry;
        }

        protected void InternalLog(LogEntry logEntry, LogEventLevel logLevel)
        {
            switch (logLevel)
            {
                case LogEventLevel.Debug:
                    _logger.Debug("{@logEntry}", logEntry);
                    break;

                case LogEventLevel.Error:
                    _logger.Error("{@logEntry}", logEntry);
                    break;

                case LogEventLevel.Fatal:
                    _logger.Fatal("{@logEntry}", logEntry);
                    break;

                case LogEventLevel.Information:
                    _logger.Information("{@logEntry}", logEntry);
                    break;

                case LogEventLevel.Verbose:
                    _logger.Verbose("{@logEntry}", logEntry);
                    break;

                case LogEventLevel.Warning:
                    _logger.Warning("{@logEntry}", logEntry);
                    break;
            }
        }

        protected void EnrichLogEntry<T>(LogEntry logEntry) where T : ILogType
        {
            logEntry.UserId = GetAnonymizeUserId();
            logEntry.AppId = RetrieveAppId();
            logEntry.AddRequestDataToLog(_httpContextAccessor.HttpContext);
            var logType = typeof(T).Name;
            logEntry.LogType = logType;
        }

        protected string GetAnonymizeUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;  // ClaimsPrincipal.Current is not sufficient
            var oid = string.Empty;
            if (user?.Claims != null)
            {
                foreach (var claim in user.Claims)
                {
                    if (claim.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier")
                    {
                        oid = claim.Value;
                        break;
                    }
                }
            }

            return GetHashedUserId(oid);
        }

        protected string GetHashedUserId(string userId)
        {
            if (userId.EndsWith("anonymize", StringComparison.OrdinalIgnoreCase)) return userId;

            // Machine users do not have any user id set
            if (string.IsNullOrWhiteSpace(userId)) return userId;

            if (string.IsNullOrWhiteSpace(_anonymizerKey)) return userId;

            var hashedValue = _sha256.ComputeHash(Encoding.UTF8.GetBytes(userId + _anonymizerKey));
            return BitConverter.ToString(hashedValue).Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase) + "anonymize";
        }

        protected string RetrieveAppId()
        {
            var user = _httpContextAccessor.HttpContext?.User;  // ClaimsPrincipal.Current is not sufficient
            var appid = string.Empty;
            if (user?.Claims != null)
            {
                foreach (var claim in user.Claims)
                {
                    if (claim.Type == "appid")
                    {
                        appid = claim.Value;
                        break;
                    }
                }
            }

            return appid;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                ((IDisposable)_awsHttpConnection)?.Dispose();
                _sha256?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
