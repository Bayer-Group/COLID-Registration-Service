using Amazon.SQS;
using COLID.Helper.SQS;
using COLID.SQS.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.SQS
{
    public static class AWSSQSModule
    {
        /// <summary>
        /// This will register all the supported functionality by AWS SQS module.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> object for registration.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> object for registration.</param>
        public static IServiceCollection AddAWSSQSModule(this IServiceCollection services, IConfiguration configuration)
        {
            if (configuration.GetSection("AWSSQSConfiguration") == null)
                throw new ArgumentNullException("AWSSQSConfiguration");

            //services.Configure<AWSSQSConfiguration>(configuration.GetSection("AWSSQSConfiguration"));
            //services.Configure<AWSSQSConfiguration>(options =>
            //{
            //    options.InputQueueUrl = configuration.GetSection("SQSConfiguration:InputQueueUrl").Value;
            //    options.OutputQueueUrl = configuration.GetSection("SQSConfiguration:OutputQueueUrl").Value;
            //});
            services.AddAWSService<IAmazonSQS>();
            services.AddSingleton<IAWSSQSHelper, AWSSQSHelper>();
            return services;
        }
    }
}
