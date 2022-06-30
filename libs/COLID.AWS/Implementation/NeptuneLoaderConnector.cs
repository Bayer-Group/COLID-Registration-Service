using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using COLID.AWS.DataModels;
using COLID.AWS.Exceptions;
using COLID.AWS.Interface;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace COLID.AWS.Implementation
{
    public class NeptuneLoaderConnector : INeptuneLoaderConnector
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<NeptuneLoaderConnector> _logger;

        private readonly DefaultContractResolver _contractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };
        private readonly string _loaderUrl;
        private readonly string _iamRoleArn;
        private readonly string _region;

        public NeptuneLoaderConnector(IHttpClientFactory clientFactory, ILogger<NeptuneLoaderConnector> logger,
            IOptionsMonitor<ColidTripleStoreOptions> tripleStoreOptions, IOptionsMonitor<AmazonWebServicesOptions> awsOptions)
        {
            _clientFactory = clientFactory;
            _logger = logger;
            _loaderUrl = tripleStoreOptions.CurrentValue.LoaderUrl.OriginalString;
            _iamRoleArn = awsOptions.CurrentValue.S3AccessIamRoleArn;
            _region = awsOptions.CurrentValue.S3Region;
        }

        /// <summary>
        /// Import a graph with the given name into AWS Neptune. The ttl file has to get uploaded to S3 first in order
        /// to import it.
        /// </summary>
        /// <param name="s3Key">Path to ttl file in s3</param>
        /// <param name="graphName">the graph name to store as an Uri</param>
        /// <exception cref="NeptuneLoaderException">In case of errors</exception>
        public async Task<NeptuneLoaderResponse> LoadGraph(string s3Key, Uri graphName)
        {
            using var client = _clientFactory.CreateClient();
            var loaderRequest = new NeptuneLoaderRequest()
            {
                Source = s3Key,
                Format = "turtle",
                IamRoleArn = _iamRoleArn,
                Region = _region,
                FailOnError = "FALSE",
                Parallelism = "MEDIUM",
                ParserConfiguration = new Dictionary<string, string> { { "namedGraphUri", graphName.AbsoluteUri } }
            };
            var json = JsonConvert.SerializeObject(loaderRequest, new JsonSerializerSettings { ContractResolver = _contractResolver });
            HttpContent content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);

            var response = await client.PostAsync(_loaderUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var neptuneResponse = JsonConvert.DeserializeObject<NeptuneLoaderResponse>(responseContent);
                var loadId = neptuneResponse.payload["loadId"];
                var loaderResponse = new NeptuneLoaderResponse()
                {
                    status = "200 OK",
                    payload = new Dictionary<string, string>
                    {
                        {"loadId", loadId},
                        {"namedGraphName", graphName.AbsoluteUri}
                    }
                };

                return loaderResponse;
            }

            var neptuneError = JsonConvert.DeserializeObject<NeptuneLoaderErrorResponse>(responseContent);
            throw new NeptuneLoaderException(neptuneError);
        }

        /// <summary>
        /// Get the import-status for the given load id.
        /// </summary>
        /// <param name="loadId">the id to fetch the status for</param>
        public async Task<NeptuneLoaderStatusResponse> GetStatus(Guid loadId)
        {
            using var client = _clientFactory.CreateClient();
            var path = $"{_loaderUrl}/{loadId}";

            var response = await client.GetAsync(path);
            var responseContent = await response.Content.ReadAsStringAsync();
            var neptuneResponse = JsonConvert.DeserializeObject<JObject>(responseContent);

            var status = new NeptuneLoaderStatusResponse
            {
                Status = neptuneResponse["status"]?.ToString(),
                LoadStatus = neptuneResponse["payload"]?["overallStatus"]?["status"]?.ToString(),
                StartTime = neptuneResponse["payload"]?["overallStatus"]?["startTime"]?.ToString()
            };

            return status;
        }
    }
}
