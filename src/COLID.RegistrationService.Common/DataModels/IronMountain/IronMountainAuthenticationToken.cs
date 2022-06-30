using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace COLID.RegistrationService.Common.DataModels.IronMountain
{
    public class IronMountainAuthenticationToken
    {
        public IronMountainAuthenticationToken()
        {
            Issued = DateTime.Now;
        }

        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("as:client_id")]
        public string ClientId { get; set; }

        [JsonProperty("userName")]
        public string UserName { get; set; }

        [JsonProperty("as:region")]
        public string Region { get; set; }

        [JsonProperty(".issued")]
        public DateTime Issued { get; set; }

        [JsonProperty(".expires")]
        public DateTime Expires
        {
            get { return Issued.AddMilliseconds(ExpiresIn); }
        }

        [JsonProperty("bearer")]
        public string Bearer { get; set; }
    }
}
