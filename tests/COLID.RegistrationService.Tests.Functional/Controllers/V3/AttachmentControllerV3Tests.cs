using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.DataModels.ConsumerGroups;
using COLID.RegistrationService.Tests.Common.Builder;
using Newtonsoft.Json;
using Xunit;

namespace COLID.RegistrationService.Tests.Functional.Controllers.V3
{
    public class AttachmentControllerV3Test : IClassFixture<FunctionTestsFixture>
    {
        private readonly HttpClient _client;
        private readonly FunctionTestsFixture _factory;

        private readonly string _apiPath = "api/v3/attachments";

        public AttachmentControllerV3Test(FunctionTestsFixture factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }
    }
}
