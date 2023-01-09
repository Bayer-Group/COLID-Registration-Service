using System.Text.Json.Serialization;
using Newtonsoft.Json.Converters;

namespace COLID.RegistrationService.Common.DataModels.TransferObjects
{
    /// <summary>
    /// Custom transfer object for user related messages, with minimized user information.
    /// </summary>
    public class MessageTemplateDto : DtoBase
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public string Type { get; set; }

        public string Subject { get; set; }

        public string Body { get; set; }
    }
}
