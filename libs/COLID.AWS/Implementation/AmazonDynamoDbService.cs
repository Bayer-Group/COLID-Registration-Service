using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.Internal.Util;
using Amazon.S3.Model;
using Amazon.DynamoDBv2;
using COLID.AWS.DataModels;
using COLID.AWS.Interface;
using COLID.Common.Utilities;
using COLID.Exception.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Amazon.DynamoDBv2.Model;
using System.Threading;

namespace COLID.AWS.Implementation
{
#pragma warning disable CA1063 // Implement IDisposable Correctly
    public class AmazonDynamoDbService : IAmazonDynamoDB
#pragma warning restore CA1063 // Implement IDisposable Correctly
    {
#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
        public IDynamoDBv2PaginatorFactory Paginators => throw new NotImplementedException();
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations
        private readonly AmazonWebServicesOptions _awsConfig;
        private readonly ILogger<AmazonS3Service> _logger;
        public AmazonDynamoDbService(IOptionsMonitor<AmazonWebServicesOptions> awsConfig, ILogger<AmazonS3Service> logger)
        {
            _awsConfig = awsConfig.CurrentValue;
            _logger = logger;
        }

        protected virtual AmazonDynamoDBClient GetAmazonDynamoDbClient()
        {
            var awsCredentials = GetECSCredentials();
            if (!_awsConfig.UseLocalCredentials)
            {
           
                return new AmazonDynamoDBClient(awsCredentials.AccessKeyId, awsCredentials.SecretAccessKey, awsCredentials.Token);
            }
            AmazonDynamoDBConfig clientConfig = new AmazonDynamoDBConfig();
            // Set the endpoint URL
            //clientConfig.RegionEndpoint = RegionEndpoint.GetBySystemName(_awsConfig.S3Region);            
            clientConfig.ServiceURL = _awsConfig.S3ServiceUrl;
            
            _logger.LogInformation("Service Url: " + _awsConfig.S3ServiceUrl);
            return new AmazonDynamoDBClient(awsCredentials.AccessKeyId, awsCredentials.SecretAccessKey, clientConfig);
        }

        private AmazonWebServicesSecurityCredentials GetECSCredentials()
        {
            try
            {
                string uri = System.Environment.GetEnvironmentVariable(ECSTaskCredentials.ContainerCredentialsURIEnvVariable);
                if (!string.IsNullOrEmpty(uri))
                {
                    IWebProxy webProxy = System.Net.WebRequest.GetSystemWebProxy();
                    using var ecsTaskCredentials = new ECSTaskCredentials(webProxy);
                    var credentials = ecsTaskCredentials.GetCredentials();

                    return new AmazonWebServicesSecurityCredentials()
                    {
                        AccessKeyId = credentials.AccessKey,
                        SecretAccessKey = credentials.SecretKey,
                        Token = credentials.Token
                    };
                }
            }
            catch (SecurityException e)
            {
                Logger.GetLogger(typeof(ECSTaskCredentials)).Error(e, "Failed to access environment variable {0}", ECSTaskCredentials.ContainerCredentialsURIEnvVariable);
            }

            return new AmazonWebServicesSecurityCredentials
            {
                Expiration = DateTime.Now.AddMonths(36).ToString(),
                AccessKeyId = _awsConfig.AccessKeyId,
                SecretAccessKey = _awsConfig.SecretAccessKey
            };
        }

#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
        public IClientConfig Config => throw new NotImplementedException();
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations

        public Task<BatchExecuteStatementResponse> BatchExecuteStatementAsync(BatchExecuteStatementRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<BatchGetItemResponse> BatchGetItemAsync(Dictionary<string, KeysAndAttributes> requestItems, ReturnConsumedCapacity returnConsumedCapacity, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<BatchGetItemResponse> BatchGetItemAsync(Dictionary<string, KeysAndAttributes> requestItems, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<BatchGetItemResponse> BatchGetItemAsync(BatchGetItemRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<BatchWriteItemResponse> BatchWriteItemAsync(Dictionary<string, List<WriteRequest>> requestItems, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<BatchWriteItemResponse> BatchWriteItemAsync(BatchWriteItemRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<CreateBackupResponse> CreateBackupAsync(CreateBackupRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<CreateGlobalTableResponse> CreateGlobalTableAsync(CreateGlobalTableRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<CreateTableResponse> CreateTableAsync(string tableName, List<KeySchemaElement> keySchema, List<AttributeDefinition> attributeDefinitions, ProvisionedThroughput provisionedThroughput, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<CreateTableResponse> CreateTableAsync(CreateTableRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DeleteBackupResponse> DeleteBackupAsync(DeleteBackupRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DeleteItemResponse> DeleteItemAsync(string tableName, Dictionary<string, AttributeValue> key, CancellationToken cancellationToken = default)
        {
            Guard.ArgumentNotNullOrWhiteSpace(tableName, "tableName can not be null");
            Guard.ArgumentNotNull(key, "key can not be null");
            try
            {
                AmazonDynamoDBClient client = GetAmazonDynamoDbClient();
                
                var result = client.DeleteItemAsync(tableName, key);
                return result;
            }

            catch (AmazonDynamoDBException ex)
            {
                throw ex;
            }
        }

        public Task<DeleteItemResponse> DeleteItemAsync(string tableName, Dictionary<string, AttributeValue> key, ReturnValue returnValues, CancellationToken cancellationToken = default)
        {
            Guard.ArgumentNotNullOrWhiteSpace(tableName, "tableName can not be null");
            Guard.ArgumentNotNull(key, "key can not be null");
            Guard.ArgumentNotNull(returnValues, "returnValues can not be null");
            try
            {
                AmazonDynamoDBClient client = GetAmazonDynamoDbClient();
                var result = client.DeleteItemAsync(tableName,key,returnValues);
                return result;
            }
            catch (AmazonDynamoDBException ex)
            {
                throw ex;
            }
        }

        public Task<DeleteItemResponse> DeleteItemAsync(DeleteItemRequest request, CancellationToken cancellationToken = default)
        {
            Guard.ArgumentNotNull(request, "request can not be null");
            try
            {
                AmazonDynamoDBClient client = GetAmazonDynamoDbClient();
                var result = client.DeleteItemAsync(request);
                return result;
            }
            catch (AmazonDynamoDBException ex)
            {
                throw ex;
            }
        }

        public Task<DeleteTableResponse> DeleteTableAsync(string tableName, CancellationToken cancellationToken = default)
        {
            Guard.ArgumentNotNullOrWhiteSpace(tableName, "tableName can not be null");
            try
            {
                AmazonDynamoDBClient client = GetAmazonDynamoDbClient();
                var result = client.DeleteTableAsync(tableName);
                return result;
            }
            catch (AmazonDynamoDBException ex)
            {
                throw ex;
            }
        }

        public Task<DeleteTableResponse> DeleteTableAsync(DeleteTableRequest request, CancellationToken cancellationToken = default)
        {
            Guard.ArgumentNotNull(request, "request can not be null");
            try
            {
                AmazonDynamoDBClient client = GetAmazonDynamoDbClient();
                var result = client.DeleteTableAsync(request);
                return result;
            }
            catch (AmazonDynamoDBException ex)
            {
                throw ex;
            }
        }

        public Task<DescribeBackupResponse> DescribeBackupAsync(DescribeBackupRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DescribeContinuousBackupsResponse> DescribeContinuousBackupsAsync(DescribeContinuousBackupsRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DescribeContributorInsightsResponse> DescribeContributorInsightsAsync(DescribeContributorInsightsRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DescribeEndpointsResponse> DescribeEndpointsAsync(DescribeEndpointsRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DescribeExportResponse> DescribeExportAsync(DescribeExportRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DescribeGlobalTableResponse> DescribeGlobalTableAsync(DescribeGlobalTableRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DescribeGlobalTableSettingsResponse> DescribeGlobalTableSettingsAsync(DescribeGlobalTableSettingsRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DescribeKinesisStreamingDestinationResponse> DescribeKinesisStreamingDestinationAsync(DescribeKinesisStreamingDestinationRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DescribeLimitsResponse> DescribeLimitsAsync(DescribeLimitsRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DescribeTableResponse> DescribeTableAsync(string tableName, CancellationToken cancellationToken = default)
        {
            Guard.ArgumentNotNullOrWhiteSpace(tableName, "tableName can not be null");
            try
            {
                AmazonDynamoDBClient client = GetAmazonDynamoDbClient();
                var result = client.DescribeTableAsync(tableName);
                return result;
            }

            catch (AmazonDynamoDBException ex)
            {
                throw ex;
            }
        }

        public Task<DescribeTableResponse> DescribeTableAsync(DescribeTableRequest request, CancellationToken cancellationToken = default)
        {
            Guard.ArgumentNotNull(request, "request can not be null");
            try
            {
                AmazonDynamoDBClient client = GetAmazonDynamoDbClient();
                var result = client.DescribeTableAsync(request);
                return result;
            }

            catch (AmazonDynamoDBException ex)
            {
                throw ex;
            }
        }

        public Task<DescribeTableReplicaAutoScalingResponse> DescribeTableReplicaAutoScalingAsync(DescribeTableReplicaAutoScalingRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DescribeTimeToLiveResponse> DescribeTimeToLiveAsync(string tableName, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DescribeTimeToLiveResponse> DescribeTimeToLiveAsync(DescribeTimeToLiveRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DisableKinesisStreamingDestinationResponse> DisableKinesisStreamingDestinationAsync(DisableKinesisStreamingDestinationRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

#pragma warning disable CA1063 // Implement IDisposable Correctly
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
        public void Dispose()
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
#pragma warning restore CA1063 // Implement IDisposable Correctly
        {
            try
            {
                //AmazonDynamoDBClient client = GetAmazonDynamoDbClient();

                //client.Dispose();
            }

            catch (AmazonDynamoDBException ex)
            {
#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
                throw ex;
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations
            }
        }

        public Task<EnableKinesisStreamingDestinationResponse> EnableKinesisStreamingDestinationAsync(EnableKinesisStreamingDestinationRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ExecuteStatementResponse> ExecuteStatementAsync(ExecuteStatementRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ExecuteTransactionResponse> ExecuteTransactionAsync(ExecuteTransactionRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ExportTableToPointInTimeResponse> ExportTableToPointInTimeAsync(ExportTableToPointInTimeRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<GetItemResponse> GetItemAsync(string tableName, Dictionary<string, AttributeValue> key, CancellationToken cancellationToken = default)
        {
            Guard.ArgumentNotNullOrWhiteSpace(tableName, "tableName can not be null");
            Guard.ArgumentNotNull(key, "key can not be null");
            try
            {
                AmazonDynamoDBClient client = GetAmazonDynamoDbClient();

                var result = client.GetItemAsync(tableName, key);
            return result;
            }
            
            catch (AmazonDynamoDBException ex)
            {
                throw ex;
            }
        }

        public Task<GetItemResponse> GetItemAsync(string tableName, Dictionary<string, AttributeValue> key, bool consistentRead, CancellationToken cancellationToken = default)
        {
            Guard.ArgumentNotNullOrWhiteSpace(tableName, "tableName can not be null");
            Guard.ArgumentNotNull(key, "key can not be null");
            Guard.ArgumentNotNull(consistentRead, "consistentRead can not be null");
            try
            {
                AmazonDynamoDBClient client = GetAmazonDynamoDbClient();

                var result = client.GetItemAsync(tableName, key, consistentRead);
                return result;
            }

            catch (AmazonDynamoDBException ex)
            {
                throw ex;
            }
        }

        public Task<GetItemResponse> GetItemAsync(GetItemRequest request, CancellationToken cancellationToken = default)
        {
            Guard.ArgumentNotNull(request, "consistentRead can not be null");
            try
            {
                AmazonDynamoDBClient client = GetAmazonDynamoDbClient();

                var result = client.GetItemAsync(request);
                return result;
            }

            catch (AmazonDynamoDBException ex)
            {
                throw ex;
            }
        }

        public Task<ListBackupsResponse> ListBackupsAsync(ListBackupsRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ListContributorInsightsResponse> ListContributorInsightsAsync(ListContributorInsightsRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ListExportsResponse> ListExportsAsync(ListExportsRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ListGlobalTablesResponse> ListGlobalTablesAsync(ListGlobalTablesRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ListTablesResponse> ListTablesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                AmazonDynamoDBClient client = GetAmazonDynamoDbClient();

                var result = client.ListTablesAsync();
                return result;
            }

            catch (AmazonDynamoDBException ex)
            {
                throw ex;
            }
        }

        public Task<ListTablesResponse> ListTablesAsync(string exclusiveStartTableName, CancellationToken cancellationToken = default)
        {
            Guard.ArgumentNotNull(exclusiveStartTableName, "limit can not be null");
            try
            {
                AmazonDynamoDBClient client = GetAmazonDynamoDbClient();

                var result = client.ListTablesAsync(exclusiveStartTableName);
                return result;
            }

            catch (AmazonDynamoDBException ex)
            {
                throw ex;
            }
        }

        public Task<ListTablesResponse> ListTablesAsync(string exclusiveStartTableName, int limit, CancellationToken cancellationToken = default)
        {
            Guard.ArgumentNotNullOrWhiteSpace(exclusiveStartTableName, "exclusiveStartTableName can not be null");
            Guard.ArgumentNotNull(limit, "limit can not be null");
            try
            {
                AmazonDynamoDBClient client = GetAmazonDynamoDbClient();

                var result = client.ListTablesAsync(exclusiveStartTableName, limit);
                return result;
            }

            catch (AmazonDynamoDBException ex)
            {
                throw ex;
            }
        }

        public Task<ListTablesResponse> ListTablesAsync(int limit, CancellationToken cancellationToken = default)
        {
            Guard.ArgumentNotNull(limit, "limit can not be null");
            try
            {
                AmazonDynamoDBClient client = GetAmazonDynamoDbClient();

                var result = client.ListTablesAsync(limit);
                return result;
            }

            catch (AmazonDynamoDBException ex)
            {
                throw ex;
            }

        }

        public Task<ListTablesResponse> ListTablesAsync(ListTablesRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ListTagsOfResourceResponse> ListTagsOfResourceAsync(ListTagsOfResourceRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<PutItemResponse> PutItemAsync(string tableName, Dictionary<string, AttributeValue> item, CancellationToken cancellationToken = default)
        {
            Guard.ArgumentNotNullOrWhiteSpace(tableName, "tableName can not be null");
            Guard.ArgumentNotNull(item, "item can not be null");
            AmazonDynamoDBClient client =  GetAmazonDynamoDbClient();
            try
            {
                PutItemRequest request = new PutItemRequest
                {
                    TableName = tableName,
                    Item = item
                };

                // Issue PutItem request
                var result = client.PutItemAsync(request);
                return result;
            }

            catch (AmazonDynamoDBException ex)
            {
                throw ex;
            }
        }

        public Task<PutItemResponse> PutItemAsync(string tableName, Dictionary<string, AttributeValue> item, ReturnValue returnValues, CancellationToken cancellationToken = default)
        {
            Guard.ArgumentNotNullOrWhiteSpace(tableName, "tableName can not be null");
            Guard.ArgumentNotNull(item, "item can not be null");
            Guard.ArgumentNotNull(returnValues, "returnValues can not be null");
            try
            {
                AmazonDynamoDBClient client = GetAmazonDynamoDbClient();
                var result = client.PutItemAsync(tableName, item, returnValues);
                return result;
            }
            catch (AmazonDynamoDBException ex)
            {
                throw ex;
            }
        }

        public Task<PutItemResponse> PutItemAsync(PutItemRequest request, CancellationToken cancellationToken = default)
        {
            Guard.ArgumentNotNull(request, "request can not be null");
            try
            {
                AmazonDynamoDBClient client = GetAmazonDynamoDbClient();
                var result = client.PutItemAsync(request);
                return result;
            }
            catch (AmazonDynamoDBException ex)
            {
                throw ex;
            }
        }

        public Task<QueryResponse> QueryAsync(QueryRequest request, CancellationToken cancellationToken = default)
        {
            Guard.ArgumentNotNull(request, "request can not be null");
            try
            {
                AmazonDynamoDBClient client = GetAmazonDynamoDbClient();
                var result = client.QueryAsync(request);
                return result;
            }
            catch (AmazonDynamoDBException ex)
            {
                throw ex;
            }
        }

        public Task<RestoreTableFromBackupResponse> RestoreTableFromBackupAsync(RestoreTableFromBackupRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<RestoreTableToPointInTimeResponse> RestoreTableToPointInTimeAsync(RestoreTableToPointInTimeRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ScanResponse> ScanAsync(string tableName, List<string> attributesToGet, CancellationToken cancellationToken = default)
        {
            Guard.ArgumentNotNullOrWhiteSpace(tableName, "tableName can not be null");
            Guard.ArgumentNotNull(attributesToGet, "attributesToGet can not be null");
            try
            {
                AmazonDynamoDBClient client = GetAmazonDynamoDbClient();
                var result = client.ScanAsync(tableName, attributesToGet);
                return result;
            }
            catch (AmazonDynamoDBException ex)
            {
                throw ex;
            }
        }

        public Task<ScanResponse> ScanAsync(string tableName, Dictionary<string, Condition> scanFilter, CancellationToken cancellationToken = default)
        {
            Guard.ArgumentNotNullOrWhiteSpace(tableName, "tableName can not be null");
            Guard.ArgumentNotNull(scanFilter, "scanFilter can not be null");
            try
            {
                AmazonDynamoDBClient client = GetAmazonDynamoDbClient();
                var result = client.ScanAsync(tableName, scanFilter);
                return result;
            }
            catch (AmazonDynamoDBException ex)
            {
                throw ex;
            }
        }

        public Task<ScanResponse> ScanAsync(string tableName, List<string> attributesToGet, Dictionary<string, Condition> scanFilter, CancellationToken cancellationToken = default)
        {
            Guard.ArgumentNotNullOrWhiteSpace(tableName, "tableName can not be null");
            Guard.ArgumentNotNull(attributesToGet, "attributesToGet can not be null");
            Guard.ArgumentNotNull(scanFilter, "scanFilter can not be null");
            try
            {
                AmazonDynamoDBClient client = GetAmazonDynamoDbClient();
                var result = client.ScanAsync(tableName, attributesToGet, scanFilter);
                return result;
            }
            catch (AmazonDynamoDBException ex)
            {
                throw ex;
            }
        }

        public Task<ScanResponse> ScanAsync(ScanRequest request, CancellationToken cancellationToken = default)
        {
            Guard.ArgumentNotNull(request, "scanFilter can not be null");
            try
            {
                AmazonDynamoDBClient client = GetAmazonDynamoDbClient();
                var result = client.ScanAsync(request);
                return result;
            }
            catch (AmazonDynamoDBException ex)
            {
                throw ex;
            }
        }

        public Task<TagResourceResponse> TagResourceAsync(TagResourceRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<TransactGetItemsResponse> TransactGetItemsAsync(TransactGetItemsRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<TransactWriteItemsResponse> TransactWriteItemsAsync(TransactWriteItemsRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UntagResourceResponse> UntagResourceAsync(UntagResourceRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UpdateContinuousBackupsResponse> UpdateContinuousBackupsAsync(UpdateContinuousBackupsRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UpdateContributorInsightsResponse> UpdateContributorInsightsAsync(UpdateContributorInsightsRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UpdateGlobalTableResponse> UpdateGlobalTableAsync(UpdateGlobalTableRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UpdateGlobalTableSettingsResponse> UpdateGlobalTableSettingsAsync(UpdateGlobalTableSettingsRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UpdateItemResponse> UpdateItemAsync(string tableName, Dictionary<string, AttributeValue> key, Dictionary<string, AttributeValueUpdate> attributeUpdates, CancellationToken cancellationToken = default)
        {
            Guard.ArgumentNotNullOrWhiteSpace(tableName, "tableName can not be null");
            Guard.ArgumentNotNull(key, "key can not be null");
            Guard.ArgumentNotNull(attributeUpdates, "attributeUpdates can not be null");
            try
            {
                AmazonDynamoDBClient client = GetAmazonDynamoDbClient();
                var result = client.UpdateItemAsync(tableName, key, attributeUpdates);
                return result;
            }
            catch (AmazonDynamoDBException ex)
            {
                throw ex;
            }
        }

        public Task<UpdateItemResponse> UpdateItemAsync(string tableName, Dictionary<string, AttributeValue> key, Dictionary<string, AttributeValueUpdate> attributeUpdates, ReturnValue returnValues, CancellationToken cancellationToken = default)
        {
            Guard.ArgumentNotNullOrWhiteSpace(tableName, "tableName can not be null");
            Guard.ArgumentNotNull(key, "key can not be null");
            Guard.ArgumentNotNull(attributeUpdates, "attributeUpdates can not be null");
            Guard.ArgumentNotNull(returnValues, "returnValues can not be null");
            try
            {
                AmazonDynamoDBClient client = GetAmazonDynamoDbClient();
                var result = client.UpdateItemAsync(tableName, key, attributeUpdates, returnValues);
                return result;
            }
            catch (AmazonDynamoDBException ex)
            {
                throw ex;
            }
        }

        public Task<UpdateItemResponse> UpdateItemAsync(UpdateItemRequest request, CancellationToken cancellationToken = default)
        {
            Guard.ArgumentNotNull(request, "returnValues can not be null");
            try
            {
                AmazonDynamoDBClient client = GetAmazonDynamoDbClient();
                var result = client.UpdateItemAsync(request);
                return result;
            }
            catch (AmazonDynamoDBException ex)
            {
                throw ex;
            }
        }

        public Task<UpdateTableResponse> UpdateTableAsync(string tableName, ProvisionedThroughput provisionedThroughput, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UpdateTableResponse> UpdateTableAsync(UpdateTableRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UpdateTableReplicaAutoScalingResponse> UpdateTableReplicaAutoScalingAsync(UpdateTableReplicaAutoScalingRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UpdateTimeToLiveResponse> UpdateTimeToLiveAsync(UpdateTimeToLiveRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DescribeImportResponse> DescribeImportAsync(DescribeImportRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ImportTableResponse> ImportTableAsync(ImportTableRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ListImportsResponse> ListImportsAsync(ListImportsRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
