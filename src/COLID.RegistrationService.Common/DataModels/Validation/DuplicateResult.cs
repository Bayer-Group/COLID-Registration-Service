namespace COLID.RegistrationService.Common.DataModel.Validation
{
    public class DuplicateResult
    {
        public string Published { get; set; }

        public string Draft { get; set; }

        public string Type { get; set; }

        public string IdentifierType { get; set; }

        public DuplicateResult(string published, string draft, string type, string identifierType)
        {
            Published = published;
            Draft = draft;
            Type = type;
            IdentifierType = identifierType;
        }
    }
}
