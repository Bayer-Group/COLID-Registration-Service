using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using Amazon.DynamoDBv2;
using COLID.AWS.DataModels;
using COLID.AWS.Interface;
using COLID.Graph.Metadata.Constants;
using COLID.Graph.Metadata.Services;
using COLID.RegistrationService.Common.DataModel.Search;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.Services.Authorization.UserInfo;
using COLID.RegistrationService.Common.DataModels.TransferObjects;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Reflection;
using COLID.Identity.Services;
using COLID.RegistrationService.Services.Configuration;
using Entity = COLID.Graph.TripleStore.DataModels.Base.Entity;
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.Metadata.DataModels.Metadata;
using Microsoft.AspNetCore.Hosting;
using System.Net;
using COLID.Graph.Metadata.Extensions;

namespace COLID.RegistrationService.Services.Implementation
{
    public class ExportService : IExportService
    {
        private readonly IConfiguration _configuration;
        private readonly IResourceService _resourceService;
        private readonly IUserInfoService _userInfoService;
        private readonly IAmazonS3Service _awsS3Service;
        //private readonly IAmazonDynamoDB _awsDynamoDbService;
        private readonly IMetadataService _metaDataService;
        private readonly ITaxonomyService _taxonomyService;
        private readonly AmazonWebServicesOptions _awsConfig;
        private readonly ILogger<ExportService> _logger;
        private readonly ITokenService<ColidSearchServiceTokenOptions> _searchTokenService;
        private readonly ITokenService<ColidAppDataServiceTokenOptions> _adsTokenService;
        private readonly string _searchServiceEndpoint;
        private readonly string _s3AccessLinkPrefix;
        private readonly string _appDataServiceEndpoint;
        private readonly IWebHostEnvironment _hostEnvironment;

        // Constants
        private readonly int _searchSizePerLoop = 20;
        private readonly string _uriAndMeta = "uriAndMeta";
        private readonly string _onlyUri = "onlyUri";
        private readonly string _readableExcel = "readableExcel";
        private readonly string _excelTemplate = "excelTemplate";
        private readonly string _clearText = "clearText";
        private readonly string _uris = "uris";
        private readonly string _exportWorksheetName = "Result";
        private readonly string _exportFileName = "export.xlsx";
        private readonly string _messageSubject = "Export finished";
        private readonly Func<string, string> _messageBody =
            (url) => string.Format("The exported file is available at <a href=\"{0}\">{0}</a href>", url);

        public ExportService(
            IConfiguration configuration,
            IOptionsMonitor<AmazonWebServicesOptions> awsOptionsMonitor,
            IResourceService resourceService,
            IUserInfoService userInfoService,
            IAmazonS3Service amazonS3Service,
            //IAmazonDynamoDB amazonDynamoDbService,
            IMetadataService metadataService,
            ITaxonomyService taxonomyService,
            ILogger<ExportService> logger,
            ITokenService<ColidSearchServiceTokenOptions> searchTokenService,
            ITokenService<ColidAppDataServiceTokenOptions> adsTokenService,
            IWebHostEnvironment hostEnvironment)
        {
            _configuration = configuration;
            _resourceService = resourceService;
            _userInfoService = userInfoService;
            _awsS3Service = amazonS3Service;
           // _awsDynamoDbService = amazonDynamoDbService;
            _awsConfig = awsOptionsMonitor.CurrentValue;
            _logger = logger;
            _searchServiceEndpoint = _configuration.GetConnectionString("searchServiceConnectionUrl");
            _s3AccessLinkPrefix = _configuration.GetConnectionString("s3AccessLinkPrefix");
            _appDataServiceEndpoint = _configuration.GetConnectionString("appDataServiceUrl") + "/api/Messages/sendGenericMessage";
            _searchTokenService = searchTokenService;
            _adsTokenService = adsTokenService;
            _metaDataService = metadataService;
            _taxonomyService = taxonomyService;
            _hostEnvironment = hostEnvironment;
        }

        /// <summary>
        /// Main function to export the resource meta data as per search parameter .
        /// </summary>
        /// <param name="exportRequest">Search request and additional export parameters</param>
        public async void Export(ExportRequestDto exportRequest)
        {
            try
            {               
                var resources = new List<Dictionary<string, List<dynamic>>>();
                //Check Whether to query by search criteria else look for resource for given list of pidUris
                if (exportRequest.pidUris.Count == 0)
                {
                    _logger.LogInformation("ExcelExport: Excel Export Started..");
                    // Loop over search results to export
                    var original_size = exportRequest.searchRequest.Size;
                    var original_from = exportRequest.searchRequest.From;

                    int size = _searchSizePerLoop;
                    int from = original_from;

                    
                    var resourcesWithLink = new List<Graph.Metadata.DataModels.Resources.Resource>();
                    List<Dictionary<string, List<dynamic>>> current_resources;
                    do
                    {
                        // Send search request to SearchService
                        exportRequest.searchRequest.Size = size;
                        exportRequest.searchRequest.From = from;
                        var response =  this.Search(exportRequest.searchRequest).Result;

                        // Get metadata for resulting resources
                        (List<Dictionary<string, List<dynamic>>> filteredList, List<Graph.Metadata.DataModels.Resources.Resource> resourceList) = this.GetDetails(
                            response,
                            exportRequest.exportSettings.exportContent,
                            exportRequest.exportSettings.exportFormat);

                        resources.AddRange(filteredList);
                        resourcesWithLink.AddRange(resourceList);
                        current_resources = filteredList;

                        from += filteredList.Count;
                    } while (current_resources.Any());

                    exportRequest.searchRequest.Size = original_size;
                    exportRequest.searchRequest.From = original_from;


                    //return this.generateExcelTemplate(resources, resourcesWithLink);
                    // Generate Excel file from resource metadata
                    var stream = this.exportToExcel(resources, exportRequest, resourcesWithLink);
                    //return stream;
                    //Upload generated Excel file to S3-Server
                    if (stream.Length > 0)
                    {
                        var uploadInfoDto = await this.uploadToS3(stream);
                        //Send message to user with URL to Excel file
                        this.SendNotification(uploadInfoDto);
                    }
                }
                else
                {
                    _logger.LogInformation("ExcelExport: Excel Export Started for list of piduri..");
                    // Construct a list of resources that were found
                    List<Graph.Metadata.DataModels.Resources.Resource> hits = new List<Graph.Metadata.DataModels.Resources.Resource>();
                    //exchange URIs with resource data
                    if (exportRequest.pidUris.Count > 0)
                    {
                        // Get Metadata of the resource from the ResourceService
                        hits.AddRange(_resourceService.GetByPidUris(exportRequest.pidUris));
                    }

                    //var to_return = new List<Dictionary<string, List<dynamic>>>();

                    foreach (var hit in hits)
                    {
                        var current = new Dictionary<string, List<dynamic>>()
                        {
                            ["Id"] = new List<dynamic>() { hit.Id }
                        };
                        foreach (var prop in hit.Properties)
                        {
                            current.Add(prop.Key, prop.Value);
                        }
                        resources.Add(current);
                    }


                    //combining resource Types and fetch their information
                    var resourceTypes = _metaDataService.GetInstantiableEntityTypes(Resource.Type.FirstResouceType).ToList();
                    List<MetadataProperty> metaDataEntityTypes = new List<MetadataProperty>();
                    resourceTypes.ForEach(x =>
                    {
                        metaDataEntityTypes.AddRange(
                            _metaDataService.GetMetadataForEntityType(x)
                                .Where(y => y.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.Shacl.Group, true)
                                .GetValue("key") == COLID.Graph.Metadata.Constants.Resource.Groups.LinkTypes)
                                );
                    });

                    ////get link information from database
                    //resourceTypes.ForEach(x =>
                    //{
                    //    metaDataEntityTypes.AddRange(
                    //        _metaDataService.GetMetadataForEntityType(x)
                    //            .Where(y => y.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.Shacl.Group, true)
                    //            .GetValue("key") == COLID.Graph.Metadata.Constants.Resource.Groups.LinkTypes)
                    //            );
                    //});

                    Uri publishedGraph = _metaDataService.GetInstanceGraph(PIDO.PidConcept);
                    _resourceService.GetLinksOfPublishedResources(hits, exportRequest.pidUris, publishedGraph, metaDataEntityTypes.Select(x => x.Key).ToHashSet());
                    //return this.generateExcelTemplate(to_return, hits);
                    //var stream = this.generateExcelTemplate(resources, hits);
                    var stream = this.exportToExcel(resources, exportRequest, hits);
                    //return stream;
                    //Upload generated Excel file to S3-Server
                    if (stream.Length > 0)
                    {
                        var uploadInfoDto = await this.uploadToS3(stream);
                        //Send message to user with URL to Excel file
                        this.SendNotification(uploadInfoDto);
                    }
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError("ExcelExport:- " + (ex.InnerException == null ? ex.Message : ex.InnerException.Message));                
            }
        }        

        /// <summary>
        /// Send search request to SearchService API, return the HTTP Response
        /// </summary>
        /// <param name="searchRequest"></param>
        /// <returns></returns>
        private async Task<HttpResponseMessage> Search(SearchRequestDto searchRequest)
        {
            //var handler = new HttpClientHandler();
            //handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
            HttpClient client = new HttpClient();
            try
            {

                // Encode the searchRequest into a JSON object for sending
                string jsonobject = JsonConvert.SerializeObject(searchRequest);
                StringContent content = new StringContent(jsonobject, Encoding.UTF8, "application/json");

                //Fetch token for search service
                var accessToken = await _searchTokenService.GetAccessTokenForWebApiAsync();                  
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                // Post the JSON object to the SearchService endpoint
                HttpResponseMessage response = await client.PostAsync(_searchServiceEndpoint, content);
                response.EnsureSuccessStatusCode();
                return response;
            }
            catch (System.Exception ex)
            {
                _logger.LogError("ExcelExport: An error occurred while passing the search request to the search service.", ex.Message);
                throw ex;
            }
        }

        private (List<Dictionary<string, List<dynamic>>> filteredList, List<Graph.Metadata.DataModels.Resources.Resource> resourceList) GetDetails(
            HttpResponseMessage httpResponseMessage,
            string exportContent,
            string exportFormat)
        {
            // Convert the returned JSON object to a SearchResultDto
            var result = httpResponseMessage.Content.ReadAsStringAsync().Result;

            var s = JsonConvert.DeserializeObject<SearchResultDto>(result);

            // Construct a list of resources that were found
            List<Graph.Metadata.DataModels.Resources.Resource> hits = new List<Graph.Metadata.DataModels.Resources.Resource>();
            List<Uri> uris = new List<Uri>();
            //get the URIs from hits
            foreach (var hit in s.Hits.Hits)
            {
                uris.Add(new Uri(Uri.UnescapeDataString(hit.id.Value)));
            }

            //exchange URIs with resource data
            if (s.Hits.Hits.Count > 0 && (exportContent == _uriAndMeta || exportFormat == _excelTemplate))
            {
                // Get resource from the piduris
                hits.AddRange(_resourceService.GetByPidUris(uris));
            }

            var to_return = new List<Dictionary<string, List<dynamic>>>();

            // If "_uriAndMeta" or "_excelTemplate" is selected, include the metadata as well
            if (exportContent == _uriAndMeta || exportFormat == _excelTemplate)
            {
                foreach (var hit in hits)
                {
                    var current = new Dictionary<string, List<dynamic>>()
                    {
                        ["Id"] = new List<dynamic>() { hit.Id }
                    };
                    foreach (var prop in hit.Properties)
                    {
                        current.Add(prop.Key, prop.Value);
                    }
                    to_return.Add(current);
                }
            }
            // If "_onlyUri" is selected, only include the URIs
            else if (exportContent == _onlyUri)
            {
                foreach (var uri in uris)
                {
                    var current = new Dictionary<string, List<dynamic>>()
                    {
                        [EnterpriseCore.PidUri] = new List<dynamic>() { uri }
                    };
                    to_return.Add(current);
                }
            }
            else
            {
                throw new ArgumentException(string.Format("Unexpected value for exportContent. Accepted values: uriAndMeta, onlyUri. Given value: {0}.", exportContent));
            }

            //combining resource Types and fetch their information
            var resourceTypes = _metaDataService.GetInstantiableEntityTypes(Resource.Type.FirstResouceType).ToList();
            List<MetadataProperty> metaDataEntityTypes = new List<MetadataProperty>();
            resourceTypes.ForEach(x =>
            {
                metaDataEntityTypes.AddRange(
                    _metaDataService.GetMetadataForEntityType(x)
                        .Where(y => y.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.Shacl.Group, true)
                        .GetValue("key") == COLID.Graph.Metadata.Constants.Resource.Groups.LinkTypes)
                        );
            });

            ////get link information from database
            //resourceTypes.ForEach(x =>
            //{
            //    metaDataEntityTypes.AddRange(
            //        _metaDataService.GetMetadataForEntityType(x)
            //            .Where(y => y.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.Shacl.Group, true)
            //            .GetValue("key") == COLID.Graph.Metadata.Constants.Resource.Groups.LinkTypes)
            //            );
            //});

            Uri publishedGraph = _metaDataService.GetInstanceGraph(PIDO.PidConcept);
            _resourceService.GetLinksOfPublishedResources(hits, uris, publishedGraph, metaDataEntityTypes.Select(x => x.Key).ToHashSet());

            return (filteredList: to_return, resourceList: hits) ;
        }

        /// <summary>
        /// Generate Excel file based on export options
        /// </summary>
        /// <param name="resources">List of resource</param>
        /// <param name="exportRequestDto"></param>
        /// <returns></returns>
        private MemoryStream exportToExcel(List<Dictionary<string, List<dynamic>>> resources, ExportRequestDto exportRequestDto, List<Graph.Metadata.DataModels.Resources.Resource> resourcesWithLink)
        {
            // Generation depends on exportFormat
            if (exportRequestDto.exportSettings.exportFormat == _readableExcel)
            {
                return this.generateReadableExcel(
                    resources,
                    exportRequestDto.exportSettings.includeHeader,
                    exportRequestDto.exportSettings.readableValues,
                    exportRequestDto.searchRequest);
            }
            else if (exportRequestDto.exportSettings.exportFormat == _excelTemplate)
            {
                return this.generateExcelTemplate(resources, resourcesWithLink);
            }
            else
            {
                throw new ArgumentException(string.Format("Unexpected value for exportFormat. Accepted values: {0}, {1}. Given value: {2}.", _readableExcel, _excelTemplate, exportRequestDto.exportSettings.exportFormat));
            }
        }

        /// <summary>
        /// If selected, generate the two rows for a header in the generated Excel file
        /// </summary>
        /// <param name="searchRequestDto"></param>
        /// <param name="number_of_hits"></param>
        /// <returns></returns>
        private List<Row> generateHeader(SearchRequestDto searchRequestDto, int number_of_hits)
        {
            var top_row = new Row();
            var bottom_row = new Row();

            // Add user email to header
            string user = _userInfoService.GetEmail();
            top_row.Append(new Cell() { CellValue = new CellValue("User"), DataType = CellValues.String });
            bottom_row.Append(new Cell() { CellValue = new CellValue(user), DataType = CellValues.String });

            // Add date and time, timezone to header
            DateTime now = DateTime.Now;
            top_row.Append(new Cell() { CellValue = new CellValue("Date"), DataType = CellValues.String });
            bottom_row.Append(new Cell() { CellValue = new CellValue(now.ToString("dd/MM/yyyy")), DataType = CellValues.String });

            top_row.Append(new Cell() { CellValue = new CellValue("Time"), DataType = CellValues.String });
            bottom_row.Append(new Cell() { CellValue = new CellValue(now.ToString("HH:mm:ss")), DataType = CellValues.String });

            string timezone = TimeZoneInfo.Local.DisplayName;
            top_row.Append(new Cell() { CellValue = new CellValue("Timezone"), DataType = CellValues.String });
            bottom_row.Append(new Cell() { CellValue = new CellValue(timezone), DataType = CellValues.String });

            // Add SearchRequest parameters to header
            top_row.Append(new Cell() { CellValue = new CellValue("Search term"), DataType = CellValues.String });
            bottom_row.Append(new Cell() { CellValue = new CellValue((searchRequestDto == null ? "Selected resources" : searchRequestDto.SearchTerm)), DataType = CellValues.String });

            top_row.Append(new Cell() { CellValue = new CellValue("From"), DataType = CellValues.String });
            bottom_row.Append(new Cell() { CellValue = new CellValue((searchRequestDto == null ? 0 : searchRequestDto.From)), DataType = CellValues.Number });

            top_row.Append(new Cell() { CellValue = new CellValue("Size"), DataType = CellValues.String });
            bottom_row.Append(new Cell() { CellValue = new CellValue((searchRequestDto == null ? 0 : searchRequestDto.Size)), DataType = CellValues.Number });

            var aggregation_filters = "";
            if (searchRequestDto != null)
            {
                aggregation_filters = string.Join(", ",
                    searchRequestDto.AggregationFilters.Select(x =>
                        string.Format("{0}: [", x.Key)
                        + string.Join(", ", x.Value)
                        + "]"));
            }
            top_row.Append(new Cell() { CellValue = new CellValue("Aggregation Filters"), DataType = CellValues.String });
            bottom_row.Append(new Cell() { CellValue = new CellValue(aggregation_filters), DataType = CellValues.String });

            var range_filters = "";
            if (searchRequestDto != null)
            {
                range_filters = string.Join(", ",
                searchRequestDto.RangeFilters.Select(x =>
                    string.Format("{0}: ", x.Key)
                    + "{"
                    + string.Join(", ", x.Value.Select(y =>
                        string.Format("{0}: {1}", y.Key, y.Value)))
                    + "}"));
            }
            top_row.Append(new Cell() { CellValue = new CellValue("Range Filters"), DataType = CellValues.String });
            bottom_row.Append(new Cell() { CellValue = new CellValue(range_filters), DataType = CellValues.String });

            top_row.Append(new Cell() { CellValue = new CellValue("No Auto Correct"), DataType = CellValues.String });
            bottom_row.Append(new Cell() { CellValue = new CellValue((searchRequestDto == null ? "" :searchRequestDto.NoAutoCorrect.ToString())), DataType = CellValues.String });

            top_row.Append(new Cell() { CellValue = new CellValue("Enable Highlighting"), DataType = CellValues.String });
            bottom_row.Append(new Cell() { CellValue = new CellValue((searchRequestDto == null ? "" : searchRequestDto.EnableHighlighting.ToString())), DataType = CellValues.String });

            top_row.Append(new Cell() { CellValue = new CellValue("Api call time"), DataType = CellValues.String });
            bottom_row.Append(new Cell() { CellValue = new CellValue((searchRequestDto == null ? "" : searchRequestDto.ApiCallTime)), DataType = CellValues.String });

            string searchIndex = "";
            if (searchRequestDto != null)
            {
                searchIndex = typeof(SearchIndex).GetMember(searchRequestDto.SearchIndex.ToString())[0].GetCustomAttributes(false).OfType<EnumMemberAttribute>().FirstOrDefault()?.Value;
            }
            top_row.Append(new Cell() { CellValue = new CellValue("Search Index"), DataType = CellValues.String });
            bottom_row.Append(new Cell() { CellValue = new CellValue(searchIndex), DataType = CellValues.String });

            top_row.Append(new Cell() { CellValue = new CellValue("Enable Aggregation"), DataType = CellValues.String });
            bottom_row.Append(new Cell() { CellValue = new CellValue((searchRequestDto == null ? "" : searchRequestDto.EnableAggregation.ToString())), DataType = CellValues.String });

            top_row.Append(new Cell() { CellValue = new CellValue("Enable Suggest"), DataType = CellValues.String });
            bottom_row.Append(new Cell() { CellValue = new CellValue((searchRequestDto == null ? "" : searchRequestDto.EnableSuggest.ToString())), DataType = CellValues.String });

            string searchOrder = "";
            if (searchRequestDto != null)
            {
                searchOrder = typeof(SearchOrder).GetMember(searchRequestDto.Order.ToString())[0].GetCustomAttributes(false).OfType<EnumMemberAttribute>().FirstOrDefault()?.Value;
            }
            top_row.Append(new Cell() { CellValue = new CellValue("Search Order"), DataType = CellValues.String });
            bottom_row.Append(new Cell() { CellValue = new CellValue(searchOrder), DataType = CellValues.String });

            top_row.Append(new Cell() { CellValue = new CellValue("Order Field"), DataType = CellValues.String });
            bottom_row.Append(new Cell() { CellValue = new CellValue((searchRequestDto == null ? "" : searchRequestDto.OrderField)), DataType = CellValues.String });

            string fieldsToReturn = "";
            if (searchRequestDto != null)
            {
                fieldsToReturn = searchRequestDto.FieldsToReturn == null ? null : string.Join(", ", searchRequestDto.FieldsToReturn);
            }
            top_row.Append(new Cell() { CellValue = new CellValue("Fields to return"), DataType = CellValues.String });
            bottom_row.Append(new Cell() { CellValue = new CellValue(fieldsToReturn), DataType = CellValues.String });

            // Add number of exported resources to header
            top_row.Append(new Cell() { CellValue = new CellValue("Number of exported resources"), DataType = CellValues.String });
            bottom_row.Append(new Cell() { CellValue = new CellValue(number_of_hits), DataType = CellValues.Number });

            // Return both generated rows
            return new List<Row>() { top_row, bottom_row };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="props"></param>
        /// <returns></returns>
        private List<string> removeUnwantedColumnsAndOrder(List<string> props)
        {
            //list of unwanted properties
            List<string> _unwantedPops = new List<string>()
            {
                "Id",
                COLID.Graph.Metadata.Constants.Resource.MetadataGraphConfiguration,
                COLID.Graph.Metadata.Constants.Resource.HasHistoricVersion,
                COLID.Graph.Metadata.Constants.Resource.DateModified,
                COLID.Graph.Metadata.Constants.Resource.hasUserAuthorization,
                COLID.Graph.Metadata.Constants.Resource.hasLicenseTraining,
                COLID.Graph.Metadata.Constants.Resource.hasDataCategory,
            };

            List<string> _order = new List<string>()
            {
                COLID.Graph.Metadata.Constants.Resource.HasLabel,
                COLID.Graph.Metadata.Constants.Resource.HasResourceDefintion,
                COLID.Graph.Metadata.Constants.Resource.hasPID,
                COLID.Graph.Metadata.Constants.Resource.LifecycleStatus,
                COLID.Graph.Metadata.Constants.Resource.HasVersion,
                COLID.Graph.Metadata.Constants.Resource.EditorialNote,
                COLID.Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus,
                COLID.Graph.Metadata.Constants.Resource.HasInformationClassification,
                COLID.Graph.Metadata.Constants.Resource.HasConsumerGroup,
                COLID.Graph.Metadata.Constants.Resource.LastChangeUser,
                COLID.Graph.Metadata.Constants.Resource.Author,
                COLID.Graph.Metadata.Constants.Resource.isSchemaOfDataset,
                COLID.Graph.Metadata.Constants.Resource.Keyword,
                COLID.Graph.Metadata.Constants.Resource.HasDataSteward,
                COLID.Graph.Metadata.Constants.Resource.MainDistribution,
                COLID.Graph.Metadata.Constants.Resource.ChangeRequester,
                COLID.Graph.Metadata.Constants.Resource.hasCompetencyQuestion,
                COLID.Graph.Metadata.Constants.Resource.Distribution,
                COLID.Graph.Metadata.Constants.Resource.DistributionEndpoints.DistributionEndpointLifecycleStatus,
                COLID.Graph.Metadata.Constants.Resource.DistributionEndpoints.HasNetworkAddress,
                COLID.Graph.Metadata.Constants.Resource.DistributionEndpoints.HasNetworkedResourceLabel,
                COLID.Graph.Metadata.Constants.Resource.DistributionEndpoints.HasContactPerson,
            };

            props.RemoveAll(x => _unwantedPops.Contains(x));
            return props.OrderBy(x => _order.IndexOf(x).Equals(-1) ? 9999 : _order.IndexOf(x)).ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="resources"></param>
        /// <returns></returns>
        private List<(string key, string value)> loadLabels(List<Dictionary<string, List<dynamic>>> resources)
        {
            //Final list of URI and Labels
            List<(string key, string value)> _labelList = new List<(string key, string value)>();
            List<string> _resourceTypes = new List<string>();

            // Taxonomiesto resolve URI's in data
            var taxonomies = _taxonomyService.GetTaxonomyLabels();
                //.Where(x => x.Properties.ContainsKey(COLID.Graph.Metadata.Constants.RDFS.Label))
                //.Select(x =>
                //(
                //    x.Id,
                //    (string)x.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.RDFS.Label, true)
                //));

            Func<string, bool> validateUriFormat = (uri) => uri.StartsWith("http");

            //Labels from resource metadata
            var labels = new List<(string, string)>();

            foreach (var resource in resources)
            {
                //for key(COLUMN) labels
                var resourceType = resource.GetValueOrNull(COLID.Graph.Metadata.Constants.RDF.Type, true);

                if (!_resourceTypes.Contains(resourceType))
                {
                    List<MetadataProperty> _metaDataMap = _metaDataService.GetMetadataForEntityType(resourceType);
                    labels.AddRange(_metaDataMap
                        .Where(x => !labels.Any(y => y.Item1 == x.Key))
                        .Select(x =>
                            (
                                x.Key,
                                (string)x.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.Shacl.Name, true)
                            )));

                    _resourceTypes.Add(resourceType);
                }
            }

            _labelList.AddRange(
                resources.SelectMany(field => field.Keys)
                .Where(key => key.IsValidBaseUri() && !_labelList.Any(y => y.key == key))
                .Where(key => labels.Any(x => x.Item1.Equals(key)))
                .Select(key =>
                    (
                        key,
                        labels.FirstOrDefault(taxonomy => taxonomy.Item1.Equals(key)).Item2 ?? key
                    )
                ));

            _labelList.AddRange(
                 resources.SelectMany(field => field.Values)
                 .Select(value => string.Join(",", value))
                 .Where(value => validateUriFormat(value) && !_labelList.Any(y => y.key == value))
                 .Select(values =>
                    (
                        values,
                        string.Join(",", values.Split(",").Select(value => taxonomies.FirstOrDefault(taxonomy => taxonomy.Id.Equals(value)) == null? value : taxonomies.FirstOrDefault(taxonomy => taxonomy.Id.Equals(value)).Label))
                    )));

            _labelList.AddRange(
                resources.SelectMany(field => field.Keys)
                .Where(key => key.IsValidBaseUri() && !_labelList.Any(y => y.key == key))
                .Where(key => taxonomies.Any(x => x.Id.Equals(key)))
                .Select(key =>
                (
                        key,
                        taxonomies.FirstOrDefault(taxonomy => taxonomy.Id.Equals(key)) == null? key : taxonomies.FirstOrDefault(taxonomy => taxonomy.Id.Equals(key)).Label
                )));

            return _labelList;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="labelList"></param>
        /// <returns></returns>
        private string resolveUri(string uri, List<(string key, string value)> labelList)
        {
            string label = labelList.FirstOrDefault(x => x.key == uri).value ?? uri;

            if (label.IsValidBaseUri() && label.Split("/").Last().StartsWith("has", StringComparison.InvariantCultureIgnoreCase))
            {
                label = label.Split("/").Last();
            }

            if (!label.IsValidBaseUri())
            {
                label = System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(label);
                return char.ToUpper(label[0]) + label.Substring(1);
            }
            return label;
        }

        /// <summary>.0
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="includeHeader"></param>
        /// <param name="readableValues"></param>
        /// <param name="searchRequest"></param>
        /// <returns></returns>
        private MemoryStream generateReadableExcel(
            List<Dictionary<string, List<dynamic>>> resources,
            bool includeHeader,
            string readableValues,
            SearchRequestDto searchRequest)
        {
            // Stream that will be returned
            MemoryStream memoryStream = new MemoryStream();

            // Create new Excel document in memory
            SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);

            WorkbookPart workbookpart = document.AddWorkbookPart();

            workbookpart.Workbook = new Workbook();

            WorksheetPart worksheetPart = workbookpart.AddNewPart<WorksheetPart>();

            worksheetPart.Worksheet = new Worksheet();

            // data contains the rows of metadata
            SheetData data = new SheetData();
            worksheetPart.Worksheet.AppendChild(data);

            // If includeHeader was selected, it is generated and appended with an empty row following it
            if (includeHeader)
            {
                data.Append(generateHeader(searchRequest, resources.Count));
                data.AppendChild(new Row());
            }

            // Generate one row per resource
            List<Row> rows = new List<Row>();

            // Save all previously seen properties in a list
            List<string> properties = removeUnwantedColumnsAndOrder(resources.SelectMany(x => x.Keys).Distinct().ToList());

            getAdditionalInfo(ref resources);

            List<(string key, string value)> _labelList = loadLabels(resources);

            for (int i = 0; i < resources.Count; i++)
            {
                var resource = resources[i];

                // Check for all known properties, add existing values
                var current_row = new Row();

                foreach (var prop in properties)
                {
                    Cell cell = new Cell() { DataType = CellValues.String };
                    if (resource.TryGetValue(prop, out List<dynamic> value))
                    {
                        string _value = string.Join(",", value);
                        _value = _value.FormatDate(prop).HtmlEncode(); //format date string to specific format; Encode html string to normal string

                        if (readableValues == _clearText)
                        {
                            _value = resolveUri(_value, _labelList);
                        }
                        cell.CellValue = new CellValue(_value);

                        if (_value.IsJson())
                        {
                            if (_value.Contains(COLID.Graph.Metadata.Constants.AttachmentConstants.Type)) //Attachment
                            {
                                var _attachments = JsonConvert.DeserializeObject<List<Entity>>($"[{_value}]");
                                var _images = _attachments.Select(x => new { Id = x.Id, Label = x.Properties.GetValueOrNull(RDFS.Label, true) });
                                _value = string.Join(", ", _images.Select(x => x.Label));

                                if (_images.First().Id.IsHyperLink())
                                {
                                    cell = createHyperlink(cell, $"=HYPERLINK(\"{_images.First().Id}\", \"{_value}\")", _value);
                                }
                            }
                            if (_value.Contains(COLID.Graph.Metadata.Constants.Identifier.Type)) //BaseUri
                            {
                                _value = JsonConvert.DeserializeObject<Entity>(_value)?.Id;
                                _value = resolveUri(_value, _labelList);
                                cell.CellValue = new CellValue(_value);
                            }
                        }
                    }
                    current_row.Append(cell);
                }

                rows.Add(current_row);
            }

            // Prepend another row containing the property names
            Row headerRow = new Row();

            foreach (var prop in properties)
            {
                string prop_value = prop;

                if (prop != "Id" && readableValues == _clearText)
                {
                    prop_value = resolveUri(prop, _labelList);
                }
                else if (readableValues != _clearText && readableValues != _uris)
                {
                    throw new ArgumentException(string.Format("Unexpected value for readableValues. Accepted values: {0}, {1}. Given value: {2}.", _clearText, _uris, readableValues));
                }
                Cell cell = new Cell()
                {
                    DataType = CellValues.String,
                    CellValue = new CellValue(prop_value)
                };
                headerRow.Append(cell);
            }

            rows.Insert(0, headerRow);

            // Add rows to data object
            data.Append(rows);

            worksheetPart.Worksheet.Save();

            // Create Worksheet
            Sheets sheets = new Sheets();

            sheets.AppendChild(new Sheet()
            {
                Id = workbookpart.GetIdOfPart(worksheetPart),
                SheetId = (uint)1,
                // Name of the final worksheet
                Name = _exportWorksheetName
            });

            workbookpart.Workbook.AppendChild(sheets);

            workbookpart.Workbook.Save();

            // Close the document
            document.Close();

            // Reset pointer in stream
            memoryStream.Seek(0, SeekOrigin.Begin);

            // Return Excel file in stream
            return memoryStream;
        }

        private Cell createHyperlink(Cell oldCell, string link, string value)
        {
            if (link.Length >= 255)
            {
                oldCell.CellValue = new CellValue(value);
                return oldCell;
            }

            Cell cell = new Cell()
            {
                DataType = new EnumValue<CellValues>(CellValues.String),
                CellFormula = new CellFormula(link),
                CellValue = new CellValue(value)
            };
            return cell;
        }

        private void getAdditionalInfo(ref List<Dictionary<string, List<dynamic>>> resources)
        {
            foreach (var resource in resources)
            {
                //fetch distribution endpoint info
                if (resource.TryGetValue(Graph.Metadata.Constants.Resource.Distribution, out List<dynamic> value))
                {
                    if (value.First() is Graph.TripleStore.DataModels.Base.Entity)
                    {
                        var distributionEndpointProperties = ((COLID.Graph.TripleStore.DataModels.Base.Entity)value[0]).Properties;
                        if (distributionEndpointProperties.TryGetValue(EnterpriseCore.PidUri, out List<dynamic> pidUriDEValue))
                        {
                            if (pidUriDEValue.First() is Graph.TripleStore.DataModels.Base.Entity)
                            {
                                var distributionEndpointPidUri = ((COLID.Graph.TripleStore.DataModels.Base.Entity)pidUriDEValue[0]).Id;
                                resource.Remove(Graph.Metadata.Constants.Resource.Distribution);
                                resource.Add(Graph.Metadata.Constants.Resource.Distribution, new List<dynamic>() { distributionEndpointPidUri });
                                distributionEndpointProperties.Remove(RDF.Type);
                                distributionEndpointProperties.Remove(EnterpriseCore.PidUri);
                                foreach (KeyValuePair<string, List<dynamic>> distributionProperty in distributionEndpointProperties)
                                {
                                    if (!resource.ContainsKey(distributionProperty.Key))
                                        resource.Add(distributionProperty.Key, distributionProperty.Value);
                                }
                            }
                        }
                    }
                }

                if (resource.TryGetValue(EnterpriseCore.PidUri, out List<dynamic> pidUriValue))
                {
                    if (pidUriValue.First() is Graph.TripleStore.DataModels.Base.Entity)
                    {
                        //The URI is a pid uri, now we need to extract it
                        var pidUri = ((COLID.Graph.TripleStore.DataModels.Base.Entity)pidUriValue[0]).Id;
                        resource[EnterpriseCore.PidUri] = new List<dynamic> { pidUri };

                        if (pidUriValue.First() is Graph.TripleStore.DataModels.Base.Entity)
                        {
                            var properties = ((COLID.Graph.TripleStore.DataModels.Base.Entity)pidUriValue[0]).Properties;
                            if (properties.TryGetValue(Identifier.HasUriTemplate, out List<dynamic> hasUriTemplate))
                            {
                                var uriTemplate = string.Join(", ", hasUriTemplate);
                                resource.Add(Identifier.HasUriTemplate, new List<dynamic> { uriTemplate });
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// If selected, generate Excel from bulk template and return it in a stream
        /// </summary>
        /// <param name="resources"></param>
        /// <returns></returns>
        private MemoryStream generateExcelTemplate(List<Dictionary<string, List<dynamic>>> resources, List<Graph.Metadata.DataModels.Resources.Resource> resourcelinks = null)
        {
            //Prepare to load the excel template from file
            var currentFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var excelFilePath = currentFolder + "/App_Data/excel_template.xlsx";

            if (!File.Exists(excelFilePath))
            {
                _logger.LogError("ExcelExport: Template could not be loaded" + excelFilePath);
                return new MemoryStream();
            }
            
            //Get list of Properties
            var allProperties = new List<MetadataProperty>();
            Dictionary<string, List<MetadataProperty>> typeListWithProperties = new Dictionary<string, List<MetadataProperty>>();
            
            var resourceTypes = _metaDataService.GetInstantiableEntity(Resource.Type.FirstResouceType);
            var distributionEndpointTypes = _metaDataService.GetDistributionEndpointTypes();

            foreach (var resType in resourceTypes)
            {
                var resMetadataProp = _metaDataService.GetMetadataForEntityType(resType.Id);
                typeListWithProperties.Add(resType.Id, resMetadataProp.ToList());
                
                foreach (var prop in resMetadataProp)
                {
                    if (prop.Properties.ContainsKey(Shacl.Order) && prop.Properties[Shacl.Order] != null)
                    {
                        if (allProperties.Find(x => x.Key == prop.Properties[Shacl.Path]) == null)
                        {
                            allProperties.Add(prop);
                        }
                    }
                }
            }
            
            //Filter out Properties used for Links and NonLinks
            var propertiesWithoutLinks = new List<MetadataProperty>();
            var propertiesForLinks = new List<MetadataProperty>();
            
            foreach (var prop in allProperties)
            {               
                if (prop.Properties.ContainsKey(Shacl.Group))
                {                                        
                    MetadataPropertyGroup propGrp = prop.GetMetadataPropertyGroup();                    
                    if (propGrp.Key == Resource.Groups.LinkTypes)
                    {
                        propertiesForLinks.Add(prop);
                    }
                    else
                    {
                        propertiesWithoutLinks.Add(prop);
                    }
                }
                else
                {
                    propertiesWithoutLinks.Add(prop);
                }
            }

            // Stream that will be returned
            MemoryStream memoryStream = new MemoryStream();
            using (SpreadsheetDocument doc = (SpreadsheetDocument)SpreadsheetDocument.Open(excelFilePath, true).Clone(memoryStream))
            {
                //extract the important inner parts of the worksheet
                WorkbookPart workbookPart = doc.WorkbookPart;
                Sheets sheets = workbookPart.Workbook.GetFirstChild<Sheets>();
                Sheet dataSheet = sheets.Elements<Sheet>().Where(s => s.Name == "Data").FirstOrDefault(); //Get data sheet
                Sheet linkDataSheet = sheets.Elements<Sheet>().Where(s => s.Name == "Links").FirstOrDefault();
                Sheet linkTypesDataSheet = sheets.Elements<Sheet>().Where(s => s.Name == "LinkTypes").FirstOrDefault();
                Sheet resTypesDataSheet = sheets.Elements<Sheet>().Where(s => s.Name == "ResourceTypes").FirstOrDefault();
                Sheet resTypePropertiesDataSheet = sheets.Elements<Sheet>().Where(s => s.Name == "ResourceTypeWithProperties").FirstOrDefault();
                Sheet distributionEndpointTypesDataSheet = sheets.Elements<Sheet>().Where(s => s.Name == "DistributionEndpointTypes").FirstOrDefault();
                Sheet techinicalInfoDataSheet = sheets.Elements<Sheet>().Where(s => s.Name == "TechnicalInfo").FirstOrDefault(); 

                //Get worksheet of "Data" 
                Worksheet worksheet = ((WorksheetPart)workbookPart.GetPartById(dataSheet.Id)).Worksheet;
                SheetData rowContainer = (SheetData)worksheet.GetFirstChild<SheetData>();

                //Get worksheet of "Link" 
                Worksheet linkWorksheet = ((WorksheetPart)workbookPart.GetPartById(linkDataSheet.Id)).Worksheet;
                SheetData linkRowContainer = (SheetData)linkWorksheet.GetFirstChild<SheetData>();

                //Get worksheet of "LinkTypes" 
                Worksheet linkTypesWorksheet = ((WorksheetPart)workbookPart.GetPartById(linkTypesDataSheet.Id)).Worksheet;
                SheetData linkTypesRowContainer = (SheetData)linkTypesWorksheet.GetFirstChild<SheetData>();

                //Get worksheet of "ResourceTypes" 
                Worksheet resTypesWorksheet = ((WorksheetPart)workbookPart.GetPartById(resTypesDataSheet.Id)).Worksheet;
                SheetData resTypesRowContainer = (SheetData)resTypesWorksheet.GetFirstChild<SheetData>();

                //Get worksheet of "ResourceTypeProperties" 
                Worksheet resTypePropertiesWorksheet = ((WorksheetPart)workbookPart.GetPartById(resTypePropertiesDataSheet.Id)).Worksheet;
                SheetData resTypePropertiesRowContainer = (SheetData)resTypePropertiesWorksheet.GetFirstChild<SheetData>();

                //Get worksheet of "DistributionEndpointTypes" 
                Worksheet distributionEndpointTypesWorksheet = ((WorksheetPart)workbookPart.GetPartById(distributionEndpointTypesDataSheet.Id)).Worksheet;
                SheetData distributionEndpointTypesRowContainer = (SheetData)distributionEndpointTypesWorksheet.GetFirstChild<SheetData>();

                //Get worksheet of "TechnicalInfo" 
                Worksheet technicalInfoWorksheet = ((WorksheetPart)workbookPart.GetPartById(techinicalInfoDataSheet.Id)).Worksheet;
                SheetData technicalInfoRowContainer = (SheetData)technicalInfoWorksheet.GetFirstChild<SheetData>();
                //Update donloaded by user
                Row docDownloaddByUser = technicalInfoRowContainer.Elements<Row>().ElementAt(0);
                Cell docDownloadByUserCell = docDownloaddByUser.Elements<Cell>().Where(c => string.Compare
               (c.CellReference.Value, "B" + docDownloaddByUser.RowIndex.Value, true) == 0).First();

                docDownloadByUserCell.CellValue = new CellValue(_userInfoService.GetEmail());
                docDownloadByUserCell.DataType = CellValues.String;
                //Update Download date
                Row docDownloaddDate = technicalInfoRowContainer.Elements<Row>().ElementAt(1);
                Cell docDownloadDateCell = docDownloaddDate.Elements<Cell>().Where(c => string.Compare
               (c.CellReference.Value, "B" + docDownloaddDate.RowIndex.Value, true) == 0).First();

                docDownloadDateCell.CellValue = new CellValue(DateTime.UtcNow.ToString());
                docDownloadDateCell.DataType = CellValues.String;

                //Populate Resource Types
                var resTypesStartingIndex = 1;
                foreach (var resTyp in resourceTypes)
                {
                    Row resTypeRow = new Row();
                    //Add LinkType information columns                        
                    resTypeRow.Append(new Cell { DataType = CellValues.String, CellValue = new CellValue(resTyp.Label) });
                    resTypeRow.Append(new Cell { DataType = CellValues.String, CellValue = new CellValue(resTyp.Id) });

                    resTypesRowContainer.InsertAt<Row>(resTypeRow, resTypesStartingIndex);
                    resTypesStartingIndex++;
                }

                //Populate Resource Type Properties
                var resTypePropertiesStartingIndex = 1;
                foreach (var resTyp in typeListWithProperties)
                {
                    foreach (var prop in resTyp.Value)
                    {
                        Row resTypePropertiesRow = new Row();
                        //Add columns
                        resTypePropertiesRow.Append(new Cell { DataType = CellValues.String, CellValue = new CellValue(resTyp.Key) });
                        resTypePropertiesRow.Append(new Cell { DataType = CellValues.String, CellValue = new CellValue(prop.Properties[Shacl.Path]) });
                        resTypePropertiesRow.Append(new Cell { DataType = CellValues.String, CellValue = new CellValue(prop.Properties[Shacl.Name]) });
                       
                        int chkMaxCount = 0;
                        int chkMinCount = 0;                        
                        if (prop.Properties.ContainsKey(Shacl.MinCount))
                            chkMinCount = int.Parse(prop.Properties[Shacl.MinCount]);
                        if (prop.Properties.ContainsKey(Shacl.MaxCount))
                            chkMaxCount = int.Parse(prop.Properties[Shacl.MaxCount]);
                        
                        resTypePropertiesRow.Append(new Cell { DataType = CellValues.String, CellValue = new CellValue(chkMaxCount > 1 ? bool.TrueString : bool.FalseString) });
                        resTypePropertiesRow.Append(new Cell { DataType = CellValues.String, CellValue = new CellValue(chkMinCount >= 1 ? bool.TrueString : bool.FalseString) });

                        resTypePropertiesRowContainer.InsertAt<Row>(resTypePropertiesRow, resTypePropertiesStartingIndex);
                        resTypePropertiesStartingIndex++;
                    }
                }                

                //Populate LinkTypes
                var linkTypesStartingIndex = 1;
                foreach (var linkType in propertiesForLinks)
                {
                    Row linkTypeRow = new Row();
                    //Add LinkType information columns                        
                    linkTypeRow.Append(new Cell { DataType = CellValues.String, CellValue = new CellValue(linkType.Properties[Shacl.Name]) });
                    linkTypeRow.Append(new Cell { DataType = CellValues.String, CellValue = new CellValue(linkType.Key) });

                    linkTypesRowContainer.InsertAt<Row>(linkTypeRow, linkTypesStartingIndex);
                    linkTypesStartingIndex++;
                }

                //Populate distribution Endpoint Types
                var distEndPointTypesStartingIndex = 1;
                foreach (var distType in distributionEndpointTypes)
                {
                    Row distTypeRow = new Row();
                    //Add LinkType information columns                        
                    distTypeRow.Append(new Cell { DataType = CellValues.String, CellValue = new CellValue(distType.Value) });
                    distTypeRow.Append(new Cell { DataType = CellValues.String, CellValue = new CellValue(distType.Key) });

                    distributionEndpointTypesRowContainer.InsertAt<Row>(distTypeRow, distEndPointTypesStartingIndex);
                    distEndPointTypesStartingIndex++;
                }
                
                //get the third row of the template (which should include the PID URIs)
                //and transform them into a list for later usage
                Row pidUriRow = rowContainer.Elements<Row>().ElementAt(3);
                Row pidUriTypeRow = rowContainer.Elements<Row>().ElementAt(2);
                Row uriHeaderRow = rowContainer.Elements<Row>().ElementAt(1);

                List<string> pidUris = this.getRowValues(pidUriRow, doc);
                List<string> pidUriTypes = this.getRowValues(pidUriTypeRow, doc);
                List<string> uriHeader = this.getRowValues(uriHeaderRow, doc);
                uriHeader.RemoveRange(0, Math.Min(3, uriHeader.Count)); //remove first three items in this list, because they have no PID URI. This way, the indizes from the two lists here match

                //Check Whether all Metadata Properties are there in the Excel Template if not Add
                foreach(var prop in propertiesWithoutLinks)
                {
                    if (pidUris.Contains(prop.Key) == false)
                    {
                        //Add Header Label
                        uriHeader.Add(prop.Properties[Shacl.Name]);
                        Cell labelCell = new Cell() { DataType = CellValues.String };
                        labelCell.CellValue = new CellValue(prop.Properties[Shacl.Name]);
                        uriHeaderRow.Append(labelCell);
                        
                        //Add Header Key
                        pidUris.Add(prop.Properties[Shacl.Path]);
                        Cell valueCell = new Cell() { DataType = CellValues.String };
                        valueCell.CellValue = new CellValue(prop.Properties[Shacl.Path]);
                        pidUriRow.Append(valueCell);

                        //Add field Type
                        string fieldType = "";
                        int maxCount = 0;
                        if (prop.Properties.ContainsKey(COLID.Graph.Metadata.Constants.RDF.Type))
                        {
                            fieldType = prop.Properties[COLID.Graph.Metadata.Constants.RDF.Type];
                            fieldType = fieldType.Split("#")[1];
                        }
                            
                        if (prop.Properties.ContainsKey(Shacl.MaxCount))
                            maxCount = int.Parse(prop.Properties[Shacl.MaxCount]);

                        pidUriTypes.Add(maxCount > 1 ? "comma separated " + fieldType : fieldType);
                        pidUriTypeRow.Append(new Cell { DataType = CellValues.String, CellValue = new CellValue(maxCount > 1 ? "comma separated " + fieldType : fieldType) });
                    }
                       
                }
                
                //Loop through the resources and create rows
                List<Row> rows = new List<Row>();
                List<Row> linkRows = new List<Row>();
                List<string> displayedColumns = new List<string>();

                int _index = 1; //row index

                foreach (var resource in resources)
                {
                    // Convert Distribution Endpoint piduris to indices
                    if (resource.TryGetValue(Graph.Metadata.Constants.Resource.Distribution, out List<dynamic> distributionEndpoints))
                    {
                        int dEIndex = _index + 1;
                        var distributionEndpointsIndices = new List<dynamic>();
                        foreach (var item in distributionEndpoints)
                        {
                            distributionEndpointsIndices.Add(dEIndex++.ToString());
                        }

                        //replace distribution endpoints (json string) with distribution endpoint ids for human readability
                        resource.Remove(Graph.Metadata.Constants.Resource.Distribution);
                        resource.Add(Graph.Metadata.Constants.Resource.Distribution, distributionEndpointsIndices);
                    }

                    // Convert MainDistribution Endpoint piduris to indices
                    if (resource.TryGetValue(Graph.Metadata.Constants.Resource.MainDistribution, out List<dynamic> mainDistributionEndpoints))
                    {
                        int mdEIndex = _index + 1 + (distributionEndpoints == null? 0: distributionEndpoints.Count);
                        var mainDistributionEndpointsIndices = new List<dynamic>();
                        foreach (var item in mainDistributionEndpoints)
                        {
                            mainDistributionEndpointsIndices.Add(mdEIndex++.ToString());
                        }

                        //replace main distribution endpoints (json string) with distribution endpoint ids for human readability
                        resource.Remove(Graph.Metadata.Constants.Resource.MainDistribution);
                        resource.Add(Graph.Metadata.Constants.Resource.MainDistribution, mainDistributionEndpointsIndices);
                    }

                    // add resouce details in a excel row
                    rows.Add(generateRow(_index, resource, pidUris, uriHeader, displayedColumns));
                    _index++;

                    ////find out whether resource contains distribution endpoint and add then in a seperate row of excel
                    if (distributionEndpoints != null)
                    {
                        foreach (var item in distributionEndpoints)
                        {
                            var distributionEndpointProperties = ((COLID.Graph.TripleStore.DataModels.Base.Entity)item).Properties;
                            distributionEndpointProperties.Add("Id", new List<dynamic> { ((COLID.Graph.TripleStore.DataModels.Base.Entity)item).Id });
                            //Type "DE" is explicitely defined for distribution endpoint, for resource its default "RE" provided
                            rows.Add(generateRow(_index, distributionEndpointProperties, pidUris, uriHeader, displayedColumns, "DE"));
                            _index++;
                        }
                    }
                    ////find out whether resource contains main distribution endpoint and add them in a seperate row of excel
                    if (mainDistributionEndpoints != null)
                    {
                        foreach (var item in mainDistributionEndpoints)
                        {
                            var mainDistributionEndpointProperties = ((COLID.Graph.TripleStore.DataModels.Base.Entity)item).Properties;
                            mainDistributionEndpointProperties.Add("Id", new List<dynamic> { ((COLID.Graph.TripleStore.DataModels.Base.Entity)item).Id });
                            //Type "DE" is explicitely defined for distribution endpoint, for resource its default "RE" provided
                            rows.Add(generateRow(_index, mainDistributionEndpointProperties, pidUris, uriHeader, displayedColumns, "DE"));
                            _index++;
                        }
                    }

                    //Populate Links
                    if (resourcelinks != null)
                    {                        
                        var curReslink = resourcelinks.Where(s => s.Id == resource.GetValueOrNull("Id", true)).FirstOrDefault();
                        if (curReslink != null && curReslink.Links.Count > 0)
                        {
                            foreach (var link in curReslink.Links)
                            {
                                foreach(var linkItem in link.Value)
                                {
                                    if (linkItem.LinkType == Common.DataModel.Resources.LinkType.inbound)
                                    {
                                        linkRows.Add(generateLinkRow(linkItem.PidUri, resource.GetValueOrNull(Graph.Metadata.Constants.Resource.hasPID, true).Id, link.Key));
                                    }
                                    else
                                    {
                                        linkRows.Add(generateLinkRow(resource.GetValueOrNull(Graph.Metadata.Constants.Resource.hasPID, true).Id, linkItem.PidUri, link.Key));                                        
                                    }
                                }
                            }
                        }
                    }                    
                }

                //insert rows into sheet data
                var startingIndex = 4;
                rows.ForEach(r =>
                {
                    rowContainer.InsertAt<Row>(r, startingIndex);
                    startingIndex++;
                });

                //insert rows into sheet link
                var linkStartingIndex = 2;
                linkRows.ForEach(r =>
                {
                    linkRowContainer.InsertAt<Row>(r, linkStartingIndex);
                    linkStartingIndex++;
                });
                
                //remove empty columns from sheet
                //List<string> empty_columns = uriHeader.Except(displayedColumns).ToList();
                //foreach (var empty_column in empty_columns)
                //{
                //    deleteBlankColumn(empty_column, rowContainer.Elements<Row>());
                //}

                workbookPart.Workbook.Save();

                // Close the document
                doc.Close();

                // Reset pointer in stream
                memoryStream.Seek(0, SeekOrigin.Begin);

            }

            // Return Excel file in stream
            return memoryStream;
        }

        private List<string> getRowValues(Row valueRow, SpreadsheetDocument doc)
        {
            List<string> pidUris = new List<string>();
            foreach (Cell cell in valueRow)
            {
                //Loop through the cells and extract the PID URIs.
                //The following complicated looking snipped is used to get a proper string from the cells and unfortunately required
                if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
                {
                    SharedStringTablePart sharedStringTablePart = doc.WorkbookPart.GetPartsOfType<SharedStringTablePart>().First();
                    SharedStringItem[] items = sharedStringTablePart.SharedStringTable.Elements<SharedStringItem>().ToArray();
                    pidUris.Add(items[int.Parse(cell.CellValue.Text)].InnerText);
                }
                else
                {
                    pidUris.Add(cell.CellValue == null? cell.InnerText : cell.CellValue.Text);
                }
            }
            return pidUris;
        }

        private Row generateRow(int index, IDictionary<string, List<dynamic>> pResource, List<string> templateProperties, List<string> templatePropertyLabels,
            List<string> displayedColumns, string type = "RE")
        {
            //prepare resource
            var resource = pResource;

            Row currentResource = new Row();
            //Add First 5 blank Columns 1st col for Action Response 2nd,3rd 4th, 5th for marking Action with "x" (Create/Update/Delete/Type Change)
            currentResource.Append(new Cell { DataType = CellValues.String, CellValue = new CellValue("") });
            currentResource.Append(new Cell { DataType = CellValues.String, CellValue = new CellValue("") });
            currentResource.Append(new Cell { DataType = CellValues.String, CellValue = new CellValue("") });
            currentResource.Append(new Cell { DataType = CellValues.String, CellValue = new CellValue("") });
            currentResource.Append(new Cell { DataType = CellValues.String, CellValue = new CellValue("") });

            //Add Type, Index, Published Draft and Internal ID columns
            currentResource.Append(new Cell { DataType = CellValues.String, CellValue = new CellValue("published") });
            currentResource.Append(new Cell { DataType = CellValues.String, CellValue = new CellValue(type) });
            currentResource.Append(new Cell { DataType = CellValues.Number, CellValue = new CellValue(index) });
            
            //if (resource.TryGetValue("Id", out List<dynamic> idValue))
            //{
            //    currentResource.Append(new Cell { DataType = CellValues.String, CellValue = new CellValue(idValue[0]) });
            //}
            
            //else
            //{
            //    currentResource.Append(new Cell { DataType = CellValues.String, CellValue = new CellValue("") });
            //}
            

            for (int i = 0; i < templateProperties.Count; i++)
            {
                var templateProperty = templateProperties[i];
                var templatePropertyLabel = templatePropertyLabels[i + 5];

                Cell valueCell = new Cell() { DataType = CellValues.String };
                
                string cellValue = "";
                string actualProperty = templateProperty;
                if (templateProperty == Identifier.HasUriTemplate)
                {
                    actualProperty = EnterpriseCore.PidUri;
                }
                if (resource.TryGetValue(actualProperty, out List<dynamic> propertyValue))
                {
                    if (templateProperty == EnterpriseCore.PidUri)
                    {
                        //The URI is a pid uri, now we need to extract it
                        cellValue = ((COLID.Graph.TripleStore.DataModels.Base.Entity)propertyValue[0]).Id;
                    }
                    else if (templateProperty == Graph.Metadata.Constants.Resource.BaseUri)
                    {
                        //The URI is a base uri, now we need to extract it
                        cellValue = ((COLID.Graph.TripleStore.DataModels.Base.Entity)propertyValue[0]).Id;
                    }
                    else if (templateProperty == Identifier.HasUriTemplate)
                    {
                        
                        ////it is an uri template, now we have to check if it is from BaseUri or PidUri
                        switch (templatePropertyLabel.ToLower())
                        {
                            case "pid uri template":
                                //now fetch the pid uri from the hasPid properties
                                if (resource.TryGetValue(EnterpriseCore.PidUri, out List<dynamic> pidUriValue))
                                {
                                    var properties = ((COLID.Graph.TripleStore.DataModels.Base.Entity)pidUriValue[0]).Properties;
                                    if (properties.TryGetValue(Identifier.HasUriTemplate, out List<dynamic> hasUriTemplate))
                                    {
                                        cellValue = hasUriTemplate[0];
                                    }
                                }
                                break;
                            case "base uri template":
                                //now fetch the pid uri from the hasPid properties
                                if (resource.TryGetValue(Graph.Metadata.Constants.Resource.BaseUri, out List<dynamic> baseUriValue))
                                {
                                    var properties = ((COLID.Graph.TripleStore.DataModels.Base.Entity)baseUriValue[0]).Properties;
                                    if (properties.TryGetValue(Identifier.HasUriTemplate, out List<dynamic> hasUriTemplate))
                                    {
                                        cellValue = hasUriTemplate[0];
                                    }
                                }
                                break;
                        }
                    }
                    else
                    {
                        cellValue = string.Join(",", CheckJson(propertyValue));
                    }

                    //maintain list of properties which has some values, later we use this list to remove empty columns
                    if (!displayedColumns.Any(column => column == templatePropertyLabel))
                        displayedColumns.Add(templatePropertyLabel);
                }
                valueCell.CellValue = new CellValue(cellValue);
                currentResource.Append(valueCell);

            }

            //templateProperties.ForEach(templateProperty =>
            //{
            //    Cell valueCell = new Cell() { DataType = CellValues.String };
            //    if (resource.TryGetValue(templateProperty, out List<dynamic> value))
            //    {
            //        valueCell.CellValue = new CellValue(string.Join(", ", value));
            //    }
            //    else
            //    {
            //        valueCell.CellValue = new CellValue("");
            //    }
            //    currentResource.Append(valueCell);
            //});

            return currentResource;
        }
        private Row generateLinkRow (string sourcePidUri, string targetPidUri, string linkType)
        {
            Row linkRow = new Row();
            //Add First 3 blank Columns for technical response & marking Action with "x" (Create/Delete)
            linkRow.Append(new Cell { DataType = CellValues.String, CellValue = new CellValue("") });
            linkRow.Append(new Cell { DataType = CellValues.String, CellValue = new CellValue("") });
            linkRow.Append(new Cell { DataType = CellValues.String, CellValue = new CellValue("") });            

            //Add Link information columns            
            linkRow.Append(new Cell { DataType = CellValues.String, CellValue = new CellValue(sourcePidUri) });
            linkRow.Append(new Cell { DataType = CellValues.String, CellValue = new CellValue(targetPidUri) });
            linkRow.Append(new Cell { DataType = CellValues.String, CellValue = new CellValue(linkType) });

            return linkRow;
        }

        public List<dynamic> CheckJson(List<dynamic> props)
        {
            List<dynamic> output = new List<dynamic>();
            foreach (var item in props)
            {
                string value = string.Join(",", new List<dynamic>() { item });
                if (value.IsJson())
                {
                    if (value.Contains(COLID.Graph.Metadata.Constants.AttachmentConstants.Type)) //Attachment
                        output.Add(((COLID.Graph.TripleStore.DataModels.Base.Entity)item).Id);
                    else
                    {
                        var properties = ((COLID.Graph.TripleStore.DataModels.Base.Entity)item).Properties;
                        if (properties.TryGetValue(EnterpriseCore.PidUri, out List<dynamic> pidUriDEValue))
                        {
                            output.Add(((COLID.Graph.TripleStore.DataModels.Base.Entity)pidUriDEValue[0]).Id);
                        }
                    }
                }
                else
                {
                    return props;
                }
            }
            return output;
        }
        public static void deleteBlankColumn(string columnName, IEnumerable<Row> rows)
        {
            // Ensure that there are actually rows in the workbook
            if (rows.Count() > 0)
            {
                // Select all the cells from each row where the column letter is equal to index
                foreach (Row row in rows)
                {
                    try
                    {
                        var cellsToRemove = row.Elements<Cell>().Where(x => new String(x.CellReference.Value.Where(Char.IsLetter).ToArray()) == columnName);

                        foreach (var cell in cellsToRemove)
                            cell.Remove();
                    }
                    catch (System.Exception ex)
                    {
                        break;
                    }
                }
            }
        }
        
        /// <summary>
        /// Upload a given stream as a file to the S3-Server
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        private async Task<AmazonS3FileUploadInfoDto> uploadToS3(MemoryStream stream)
        {
            // Create formfile from stream
            var formFile = new FormFile(stream, 0, stream.Length, "file", _exportFileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };

            // Upload the formfile using awsS3Service
            var s3FileInfo = await _awsS3Service.UploadFileAsync(_awsConfig.S3BucketForFiles, Guid.NewGuid().ToString(), formFile, true);

            // Close the stream
            stream.Close();

            // Return info about uploaded file, contains URL to download
            return s3FileInfo;
        }

        /// <summary>
        /// Send a notification via AppDataService to the user informing 
        /// about successful export and download link
        /// </summary>
        /// <param name="uploadInfoDto"></param>
        private async void SendNotification(AmazonS3FileUploadInfoDto uploadInfoDto)
        {
            HttpClient client = new HttpClient();
            try
            {
                // Get user info
                string user = _userInfoService.GetEmail();

                var fileLink = this._s3AccessLinkPrefix + uploadInfoDto.FileKey;
                // Generate generic message
                var message = new MessageUserDto()
                {
                    Subject = _messageSubject,
                    Body = _messageBody(fileLink),
                    UserEmail = user
                };

                // Convert Message to JSON-Object
                string jsonobject = JsonConvert.SerializeObject(message);
                StringContent content = new StringContent(jsonobject, Encoding.UTF8, "application/json");

                //Set AAD token
                var accessToken = await _adsTokenService.GetAccessTokenForWebApiAsync();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                // Send JSON-Object to AppDataService endpoint
                HttpResponseMessage notification_response = await client.PutAsync(_appDataServiceEndpoint, content);
                notification_response.EnsureSuccessStatusCode();
                return;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError("ExcelExport: An error occurred while passing the notification to the AppData service.", ex.Message);
                throw ex;
            }
        }
    }

    public static class ExportStringExtentions
    {
        public static bool IsJson(this String str)
        {
            if (string.IsNullOrWhiteSpace(str)) return false;

            str = str.Trim();
            if ((str.StartsWith("{") && str.EndsWith("}")) || //For object
                (str.StartsWith("[") && str.EndsWith("]"))) //For array
            {
                try
                {
                    var obj = JsonConvert.DeserializeObject($"[{str}]");
                    return true;
                }
                catch // not valid
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        public static bool IsHyperLink(this String uri)
        {
            return Uri.TryCreate(uri, UriKind.Absolute, out Uri uriResult)
                 && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)
                 && uri.StartsWith("http")
                 && uri.Split(",")?.Count() == 1
                 && uri.Length < 255; //Excel has limit to 255 character
        }
        public static string HtmlEncode(this string _htmlCode)
        {
            string _decodedHtml = _htmlCode;
            if (!_htmlCode.IsJson() && !_htmlCode.IsHyperLink())
                _decodedHtml = Regex.Replace(_htmlCode, "<[a-zA-Z/].*?>", String.Empty);

            return _decodedHtml;
        }
        public static string FormatDate(this string _date, string prop)
        {
            string _formattedDate = _date;
            if (prop.Contains("date", StringComparison.OrdinalIgnoreCase)
                && DateTime.TryParse(_date, out DateTime result))
            {
                _formattedDate = result.ToString("dd/MM/yyyy HH:mm:ss");
            }
            return _formattedDate;
        }


    }

}
