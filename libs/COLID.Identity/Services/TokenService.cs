using System.Security.Authentication;
using System.Threading.Tasks;
using COLID.Common.Utilities;
using COLID.Identity.Configuration;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace COLID.Identity.Services
{
    internal class TokenService<TTokenServiceSettings> : ITokenService<TTokenServiceSettings> where TTokenServiceSettings : BaseServiceTokenOptions
    {
        private IConfidentialClientApplication _app;
        private readonly string[] _scopes;
        private readonly bool serviceEnabled;

        public TokenService(
            IOptionsMonitor<AzureADOptions> azureAdOptions,
            IOptionsMonitor<TTokenServiceSettings> tokenServiceSettings)
        {
            Guard.ArgumentNotNull(azureAdOptions, nameof(azureAdOptions));
            Guard.ArgumentNotNull(tokenServiceSettings, nameof(tokenServiceSettings));

            var currentAzureAdOptions = azureAdOptions.CurrentValue;
            var currentTokenServiceSettings = tokenServiceSettings.CurrentValue;
            serviceEnabled = currentTokenServiceSettings.Enabled;
            
            if(serviceEnabled) {
                _app = ConfidentialClientApplicationBuilder.Create(currentAzureAdOptions.ClientId)
                    .WithAuthority(AzureCloudInstance.AzurePublic, currentAzureAdOptions.TenantId)
                    .WithClientSecret(currentTokenServiceSettings.ClientSecret)
                    .Build();
            }

            _scopes = new string[] { currentTokenServiceSettings.ServiceId + "/.default" };

        }

        public async Task<string> GetAccessTokenForWebApiAsync()
        {
            if (!serviceEnabled)//why is serviceEnabled false?
            {
                return null;
            }

            AuthenticationResult result;

            try
            {
                result = await _app.AcquireTokenForClient(_scopes).ExecuteAsync();
            }
            catch (MsalServiceException ex)
            {
                throw new AuthenticationException($"AcquireTokenForClient failed", ex);
            }

            if (result == null)
            {
                return null;
            }

            return result.AccessToken;
        }
    }
}
