namespace COLID.RegistrationService.Common.DataModel.ProxyConfiguration
{
    public class NginxAttribute
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
