using Newtonsoft.Json;

namespace COLID.RegistrationService.Common.DataModel.Search
{
    public class HitDto
    {
        [JsonProperty("total")]
        public long Total { get; set; }

        [JsonProperty("max_score")]
        public double MaxScore { get; set; }

        [JsonProperty("hits")]
        public dynamic Hits { get; set; }
    }
}
