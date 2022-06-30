using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using COLID.AWS;
using COLID.MessageQueue.Services;
using COLID.RegistrationService.Repositories;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.Tests.Functional.Setup;
using COLID.RegistrationService.WebApi;
using COLID.Graph.TripleStore.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using COLID.RegistrationService.Common.DataModel.Resources;
using Moq;
using COLID.RegistrationService.Services;
using COLID.Graph.Metadata.DataModels.Resources;
using Microsoft.Extensions.Hosting;

namespace COLID.RegistrationService.Tests.Functional
{
    public class FunctionTestsFixture : WebApplicationFactory<Startup>
    {
        protected override IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder().ConfigureWebHostDefaults(builder =>
            builder.UseStartup<Startup>());
        }
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((context, conf) =>
            {
                conf.AddJsonFile(AppDomain.CurrentDomain.BaseDirectory + "appsettings.Testing.json");
                conf.AddUserSecrets<Startup>();
            });

            builder.ConfigureTestServices((services) =>
            {
                var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
                services.RegisterDebugRepositoriesModule(configuration);
                services.AddDebugServicesModule(configuration);

                var testGraphsMapping = configuration.GetSection("FunctionalTests:Graphs").Get<Dictionary<string, string>>();
                var fakeRepo = new FakeTripleStoreRepository(testGraphsMapping);

                services.RemoveAll(typeof(ITripleStoreRepository));
                services.AddSingleton<ITripleStoreRepository>(provider =>
                {
                    return fakeRepo;
                });

                services.RemoveAll(typeof(IRemoteAppDataService));
                services.AddTransient(provider =>
                {
                    var remoteAppDataMock = new Mock<IRemoteAppDataService>();
                    var validPersons = new List<string> { "superadmin@bayer.com", "christian.kaubisch.ext@bayer.com", "anonymous@anonymous.com" };

                    remoteAppDataMock.Setup(mock => mock.CheckPerson(It.IsIn<string>(validPersons))).Returns(It.IsAny<bool>());
                    remoteAppDataMock.Setup(mock => mock.CheckPerson(It.IsNotIn<string>(validPersons))).Returns(It.IsAny<bool>());
                    remoteAppDataMock.Setup(t => t.CreateConsumerGroup(It.IsAny<Uri>()));
                    remoteAppDataMock.Setup(t => t.DeleteConsumerGroup(It.IsAny<Uri>()));
                    remoteAppDataMock.Setup(t => t.NotifyResourcePublished(It.IsAny<Resource>()));

                    return remoteAppDataMock.Object;
                });

                services.RemoveAll(typeof(IMessageQueueService));
                services.AddTransient(provider => new Mock<IMessageQueueService>().Object);

                services.RemoveAll(typeof(IReindexingService));
                services.AddTransient(provider => new Mock<IReindexingService>().Object);
            });
        }
    }
}
