namespace COLID.RegistrationService.Common.DataModel.ProxyConfiguration
{
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
    public class NginxAttribute
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
    {
        public string Key { get; set; }
        public string Value { get; set; }

        public NginxAttribute()
        {
        }

        public NginxAttribute(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }
}
