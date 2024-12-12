using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using COLID.AWS.DataModels;
using COLID.AWS.Implementation;
using COLID.AWS.Interface;
using Amazon.DynamoDBv2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Amazon.SQS;

namespace COLID.AWS
{
    public static class AmazonWebServiceModule
    {
        public static IServiceCollection AddAmazonWebServiceModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ColidTripleStoreOptions>(configuration.GetSection(nameof(ColidTripleStoreOptions)));
            services.Configure<AmazonWebServicesOptions>(configuration.GetSection(nameof(AmazonWebServicesOptions)));

            services.AddTransient<IEc2InstanceMetadataConnector, Ec2InstanceMetadataConnector>();
            services.AddTransient<INeptuneLoaderConnector, NeptuneLoaderConnector>();

            var awsOptions = new AmazonWebServicesOptions();
            configuration.Bind(nameof(AmazonWebServicesOptions), awsOptions);
            if (!awsOptions.S3UseMinIo)
            {
                services.AddTransient<IAmazonS3Service, AmazonS3Service>();
            }
            else
            {
                services.AddTransient<IAmazonS3Service, MinIoService>();
            }
            
            services.AddTransient<IAmazonDynamoDB, AmazonDynamoDbService>();
            //services.AddAWSService<IAmazonSQS>();
            services.AddSingleton<IAmazonSQSService, AmazonSQSService>();
            services.AddSingleton<IAmazonSQSExtendedService, AmazonSQSExtendedService>();
            return services;
        }

    }
}
