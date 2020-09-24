using System;

namespace COLID.RegistrationService.Services.DataModel
{
    /// <summary>
    /// Transfer Object for creating and deleting ConsumerGroups in AppDataService
    /// </summary>
    class ConsumerGroupDto
    {
        /// <summary>
        /// The Id of the ConsumerGroup
        /// </summary>
        public Uri Uri { get; set; }
    }
}
