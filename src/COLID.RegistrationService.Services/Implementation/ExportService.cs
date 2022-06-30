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

namespace COLID.RegistrationService.Services.Implementation
{
    public class ExportService : IExportService
    {
        private readonly IConfiguration _configuration;
        private readonly IResourceService _resourceService;
        private readonly IUserInfoService _userInfoService;
        private readonly IAmazonS3Service _awsS3Service;
        private readonly IAmazonDynamoDB _awsDynamoDbService;
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
            IAmazonDynamoDB amazonDynamoDbService,
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
            _awsDynamoDbService = amazonDynamoDbService;
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
        /// Main function to export the resource meta data.
        /// </summary>
        /// <param name="exportRequest">Search request and additional export parameters</param>
        public async void Export(ExportRequestDto exportRequest)
        {
            // Loop over search results to export
            var original_size = exportRequest.searchRequest.Size;
            var original_from = exportRequest.searchRequest.From;

            int size = _searchSizePerLoop;
            int from = original_from;

            var resources = new List<Dictionary<string, List<dynamic>>>();
            List<Dictionary<string, List<dynamic>>> current_resources;
            do
            {
                // Send search request to SearchService
                exportRequest.searchRequest.Size = size;
                exportRequest.searchRequest.From = from;
                var response = await this.Search(exportRequest.searchRequest);

                // Get metadata for resulting resources
                current_resources = this.GetDetails(
                    response,
                    exportRequest.exportSettings.exportContent,
                    exportRequest.exportSettings.exportFormat);
                resources.AddRange(current_resources);

                from += current_resources.Count;
            } while (current_resources.Any());

            exportRequest.searchRequest.Size = original_size;
            exportRequest.searchRequest.From = original_from;

            // Generate Excel file from resource metadata
            var stream = this.exportToExcel(resources, exportRequest);

            //Upload generated Excel file to S3-Server
            if (stream.Length > 0)
            {
                var uploadInfoDto = await this.uploadToS3(stream);
                //Send message to user with URL to Excel file
                this.SendNotification(uploadInfoDto);
            }
        }

        /// <summary>
        /// Send search request to SearchService API, return the HTTP Response
        /// </summary>
        /// <param name="searchRequest"></param>
        /// <returns></returns>
        private async Task<HttpResponseMessage> Search(SearchRequestDto searchRequest)
        {
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
            catch (HttpRequestException ex)
            {
                _logger.LogError("An error occurred while passing the search request to the search service.", ex);
                throw ex;
            }
        }

        private List<Dictionary<string, List<dynamic>>> GetDetails(
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
                // Get Metadata of the resource from the ResourceService
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

            return to_return;
        }

        /// <summary>
        /// Generate Excel file based on export options
        /// </summary>
        /// <param name="resources">List of resource</param>
        /// <param name="exportRequestDto"></param>
        /// <returns></returns>
        private MemoryStream exportToExcel(List<Dictionary<string, List<dynamic>>> resources, ExportRequestDto exportRequestDto)
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
                return this.generateExcelTemplate(resources);
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
            bottom_row.Append(new Cell() { CellValue = new CellValue(searchRequestDto.SearchTerm), DataType = CellValues.String });

            top_row.Append(new Cell() { CellValue = new CellValue("From"), DataType = CellValues.String });
            bottom_row.Append(new Cell() { CellValue = new CellValue(searchRequestDto.From), DataType = CellValues.Number });

            top_row.Append(new Cell() { CellValue = new CellValue("Size"), DataType = CellValues.String });
            bottom_row.Append(new Cell() { CellValue = new CellValue(searchRequestDto.Size), DataType = CellValues.Number });

            var aggregation_filters = string.Join(", ",
                searchRequestDto.AggregationFilters.Select(x =>
                    string.Format("{0}: [", x.Key)
                    + string.Join(", ", x.Value)
                    + "]"));
            top_row.Append(new Cell() { CellValue = new CellValue("Aggregation Filters"), DataType = CellValues.String });
            bottom_row.Append(new Cell() { CellValue = new CellValue(aggregation_filters), DataType = CellValues.String });

            var range_filters = string.Join(", ",
                searchRequestDto.RangeFilters.Select(x =>
                    string.Format("{0}: ", x.Key)
                    + "{"
                    + string.Join(", ", x.Value.Select(y =>
                        string.Format("{0}: {1}", y.Key, y.Value)))
                    + "}"));
            top_row.Append(new Cell() { CellValue = new CellValue("Range Filters"), DataType = CellValues.String });
            bottom_row.Append(new Cell() { CellValue = new CellValue(range_filters), DataType = CellValues.String });

            top_row.Append(new Cell() { CellValue = new CellValue("No Auto Correct"), DataType = CellValues.String });
            bottom_row.Append(new Cell() { CellValue = new CellValue(searchRequestDto.NoAutoCorrect.ToString()), DataType = CellValues.String });

            top_row.Append(new Cell() { CellValue = new CellValue("Enable Highlighting"), DataType = CellValues.String });
            bottom_row.Append(new Cell() { CellValue = new CellValue(searchRequestDto.EnableHighlighting.ToString()), DataType = CellValues.String });

            top_row.Append(new Cell() { CellValue = new CellValue("Api call time"), DataType = CellValues.String });
            bottom_row.Append(new Cell() { CellValue = new CellValue(searchRequestDto.ApiCallTime), DataType = CellValues.String });

            string searchIndex = typeof(SearchIndex).GetMember(searchRequestDto.SearchIndex.ToString())[0].GetCustomAttributes(false).OfType<EnumMemberAttribute>().FirstOrDefault()?.Value;
            top_row.Append(new Cell() { CellValue = new CellValue("Search Index"), DataType = CellValues.String });
            bottom_row.Append(new Cell() { CellValue = new CellValue(searchIndex), DataType = CellValues.String });

            top_row.Append(new Cell() { CellValue = new CellValue("Enable Aggregation"), DataType = CellValues.String });
            bottom_row.Append(new Cell() { CellValue = new CellValue(searchRequestDto.EnableAggregation.ToString()), DataType = CellValues.String });

            top_row.Append(new Cell() { CellValue = new CellValue("Enable Suggest"), DataType = CellValues.String });
            bottom_row.Append(new Cell() { CellValue = new CellValue(searchRequestDto.EnableSuggest.ToString()), DataType = CellValues.String });

            string searchOrder = typeof(SearchOrder).GetMember(searchRequestDto.Order.ToString())[0].GetCustomAttributes(false).OfType<EnumMemberAttribute>().FirstOrDefault()?.Value;
            top_row.Append(new Cell() { CellValue = new CellValue("Search Order"), DataType = CellValues.String });
            bottom_row.Append(new Cell() { CellValue = new CellValue(searchOrder), DataType = CellValues.String });

            top_row.Append(new Cell() { CellValue = new CellValue("Order Field"), DataType = CellValues.String });
            bottom_row.Append(new Cell() { CellValue = new CellValue(searchRequestDto.OrderField), DataType = CellValues.String });

            string fieldsToReturn = searchRequestDto.FieldsToReturn == null ? null : string.Join(", ", searchRequestDto.FieldsToReturn);
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
            var taxonomies = _taxonomyService.GetAllTaxonomies()
                .Where(x => x.Properties.ContainsKey(COLID.Graph.Metadata.Constants.RDFS.Label))
                .Select(x =>
                (
                    x.Id,
                    (string)x.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.RDFS.Label, true)
                ));

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
                        string.Join(",", values.Split(",").Select(value => taxonomies.FirstOrDefault(taxonomy => taxonomy.Id.Equals(value)).Item2 ?? value))
                    )));

            _labelList.AddRange(
                resources.SelectMany(field => field.Keys)
                .Where(key => key.IsValidBaseUri() && !_labelList.Any(y => y.key == key))
                .Where(key => taxonomies.Any(x => x.Id.Equals(key)))
                .Select(key =>
                (
                        key,
                        taxonomies.FirstOrDefault(taxonomy => taxonomy.Id.Equals(key)).Item2 ?? key
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
        public MemoryStream generateExcelTemplate(List<Dictionary<string, List<dynamic>>> resources)
        {
            //Prepare to load the excel template from file
            var currentFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var excelFilePath = currentFolder + "/App_Data/excel_template.xlsx";

            if (!File.Exists(excelFilePath))
            {
                _logger.LogError("Excel template could not be loaded" + excelFilePath);
                return new MemoryStream();
            }

            // Stream that will be returned
            MemoryStream memoryStream = new MemoryStream();

            using (SpreadsheetDocument doc = (SpreadsheetDocument)SpreadsheetDocument.Open(excelFilePath, true).Clone(memoryStream))
            {
                //extract the important inner parts of the worksheet
                WorkbookPart workbookPart = doc.WorkbookPart;
                Sheets sheets = workbookPart.Workbook.GetFirstChild<Sheets>();
                Sheet dataSheet = sheets.Elements<Sheet>().Where(s => s.Name == "Data").FirstOrDefault(); //Get data sheet

                //Get worksheet of "Data" sheet
                Worksheet worksheet = ((WorksheetPart)workbookPart.GetPartById(dataSheet.Id)).Worksheet;
                SheetData rowContainer = (SheetData)worksheet.GetFirstChild<SheetData>();

                //get the third row of the template (which should include the PID URIs)
                //and transform them into a list for later usage
                Row pidUriRow = rowContainer.Elements<Row>().ElementAt(3);
                Row uriHeaderRow = rowContainer.Elements<Row>().ElementAt(1);

                List<string> pidUris = this.getRowValues(pidUriRow, doc);
                List<string> uriHeader = this.getRowValues(uriHeaderRow, doc);
                uriHeader.RemoveRange(0, Math.Min(3, uriHeader.Count)); //remove first three items in this list, because they have no PID URI. This way, the indizes from the two lists here match

                //Loop through the resources and create rows
                List<Row> rows = new List<Row>();

                List<string> displayedColumns = new List<string>();

                int _index = 1; //row index

                foreach (var resource in resources)
                {
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

                    // add resouce details in a excel row
                    rows.Add(generateRow(_index, resource, pidUris, uriHeader, displayedColumns));
                    _index++;

                    ////find out whether resource contains distribution endpoint and add then in a seperate row of excel
                    if (distributionEndpoints != null)
                    {
                        foreach (var item in distributionEndpoints)
                        {
                            var distributionEndpointProperties = ((COLID.Graph.TripleStore.DataModels.Base.Entity)item).Properties;
                            //Type "DE" is explicitely defined for distribution endpoint, for resource its default "RE" provided
                            rows.Add(generateRow(_index, distributionEndpointProperties, pidUris, uriHeader, displayedColumns, "DE"));
                            _index++;
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
                    pidUris.Add(cell.CellValue.Text);

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
            Cell resourceType = new Cell()
            {
                DataType = CellValues.String,
                CellValue = new CellValue(type)
            };
            Cell resourceId = new Cell()
            {
                DataType = CellValues.Number,
                CellValue = new CellValue(index)
            };
            Cell resourceStatus = new Cell()
            {
                DataType = CellValues.String,
                CellValue = new CellValue("published")
            };

            currentResource.Append(resourceType);
            currentResource.Append(resourceId);
            currentResource.Append(resourceStatus);

            for (int i = 0; i < templateProperties.Count; i++)
            {
                var templateProperty = templateProperties[i];
                var templatePropertyLabel = templatePropertyLabels[i];

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
                    else if (templateProperty == Identifier.HasUriTemplate)
                    {
                        //it is an uri template, now we have to check if it is from BaseUri or PidUri
                        switch (templatePropertyLabel.ToLower())
                        {
                            case "pid uri template":
                                //now fetch the pid uri from the hasPid properties
                                if (resource.TryGetValue(EnterpriseCore.PidUri, out List<dynamic> pidUriValue))
                                {
                                    var properties = ((COLID.Graph.TripleStore.DataModels.Base.Entity)pidUriValue[0]).Properties;
                                    if (properties.TryGetValue(Identifier.HasUriTemplate, out List<dynamic> hasUriTemplate))
                                    {
                                        cellValue = string.Join(", ", hasUriTemplate);
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
                _logger.LogError("An error occurred while passing the notification to the AppData service.", ex);
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
