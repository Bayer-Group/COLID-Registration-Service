using COLID.Graph.Metadata.Constants;

namespace COLID.RegistrationService.Common.Constants
{
    public static class Regex
    {
        public static readonly string ResourceKey = @"^(?:" + Entity.IdPrefix + "){1}" + Guid;
#pragma warning disable CA1720 // Identifier contains type name
        public const string Guid = "[0-9A-Fa-f]{8}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{12}";
#pragma warning restore CA1720 // Identifier contains type name
        public const string PidUriTemplate = @"(\{\w+(:\d+)?\})";
        public const string Version = @"^(\d+\.)*\d+$";
        public const string Email = @"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$";
        public const string Pound = @"^((?!.*(#|%23).*).)*$";
        public const string APIVersionField = "^[vV]\\d{1,}$";
    }
}
