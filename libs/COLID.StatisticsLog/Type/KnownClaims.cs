namespace COLID.StatisticsLog.Type
{
    public static class KnownClaims
    {
        public static ClaimMetadata UserId { get; private set; } = new ClaimMetadata("http://schemas.microsoft.com/identity/claims/objectidentifier", "UserId");
        public static ClaimMetadata FullName { get; private set; } = new ClaimMetadata("name", "Full Name");

        public static ClaimMetadata Email { get; private set; } = new ClaimMetadata("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn", "Email");
    }

    public class ClaimMetadata
    {
        public string ActualName { get; set; }
        public string ReadableName { get; set; }

        public ClaimMetadata(string actualName, string readableName)
        {
            ActualName = actualName;
            ReadableName = readableName;
        }
    }
}
