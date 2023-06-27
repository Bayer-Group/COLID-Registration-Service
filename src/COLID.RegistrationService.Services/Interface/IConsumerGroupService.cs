using System.Collections.Generic;
using COLID.Graph.TripleStore.DataModels.ConsumerGroups;
using COLID.Graph.TripleStore.Services;
using COLID.RegistrationService.Repositories.Interface;

namespace COLID.RegistrationService.Services.Interface
{
    /// <summary>
    /// Service to handle all consumer group related operations.
    /// </summary>
    public interface IConsumerGroupService : IBaseEntityService<ConsumerGroup, ConsumerGroupRequestDTO, ConsumerGroupResultDTO, ConsumerGroupWriteResultCTO, IConsumerGroupRepository>
    {
        /// <summary>
        /// Returns a list of all active consumer groups.
        /// Active means that the group has the lifecycle status active and matches the ad rights of the user.
        /// </summary>
        /// <returns>List of active consumer groups</returns>
        IList<ConsumerGroupResultDTO> GetActiveEntities();

        /// <summary>
        /// Get the active directory role name for a consumer group. This group will be determined by the given id.
        /// </summary>
        /// <param name="id">the consumer group identifier</param>
        /// <returns>the active directory role as string</returns>
        string GetAdRoleForConsumerGroup(string id);

        /// <summary>
        /// By a given id, the consumer group will be deleted or set as deprecated.
        /// If a colid entry references the consumer group, the status is set to deprecated,
        /// otherwise the consumer group will be deleted.
        /// </summary>
        /// <param name="id">the consumer group identifier</param>
        void DeleteOrDeprecateConsumerGroup(string id);

        /// <summary>
        /// By a give id, the consumer group will be reactivated.
        /// </summary>
        /// <param name="id">the consumer group id to reactivate</param>
        void ReactivateConsumerGroup(string id);
    }
}
