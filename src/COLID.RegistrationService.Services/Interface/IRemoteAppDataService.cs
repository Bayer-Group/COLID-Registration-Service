using System;
using System.Threading.Tasks;
using COLID.Graph.Metadata.DataModels.Resources;

namespace COLID.RegistrationService.Services.Interface
{
    /// <summary>
    /// Service to handle communication and authentication with the external AppDataService
    /// </summary>
    /// <list>
    public interface IRemoteAppDataService
    {
        /// <summary>
        /// Creates a new consumer group with given Id in AppDataService.
        /// </summary>
        /// <param name="consumerGroupId">The Id of a new consumer group</param>
        Task CreateConsumerGroup(Uri consumerGroupId);

        /// <summary>
        /// Creates a consumer group by given Id in AppDataService.
        /// </summary>
        /// <param name="consumerGroupId">The Id of a consumer group to delete</param>
        Task DeleteConsumerGroup(Uri consumerGroupId);

        /// <summary>
        /// Notifies the AppDataService, that a COLID entry with a PID URI has been published.
        /// </summary>
        /// <param name="resource">The COLID entry</param>
        Task NotifyResourcePublished(Resource resource);

        /// <summary>
        /// Notifies the AppDataService, that a COLID entry with a PID URI has been deleted.
        /// </summary>
        /// <param name="resource">The COLID entry</param>
        Task NotifyResourceDeleted(Resource resource);

        /// <summary>
        /// Checks if a person exists in the active directory
        /// </summary>
        /// <param name="id">The person id to check.</param>
        /// <returns>True if exists otherwise false</returns>
        Task<bool> CheckPerson(string id);
    }
}
