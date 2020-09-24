using System;

namespace COLID.RegistrationService.Services.DataModel
{
    /// <summary>
    /// Transfer Object for notifiying AppDataService that a COLID entry has been published
    /// </summary>
    public class ColidEntryCto
    {
        public Uri ColidPidUri { get; set; }
        public string Label { get; set; }
    }
}
