using System;
using System.Collections.Generic;
using COLID.Graph.TripleStore.DataModels.ConsumerGroups;
using COLID.Graph.TripleStore.Repositories;

namespace COLID.RegistrationService.Repositories.Interface
{
    /// <summary>
    /// Repository to handle all consumer group related operations.
    /// </summary>
    public interface IConsumerGroupRepository : IBaseRepository<ConsumerGroup>
    {
        /// <summary>
        /// Get the active directory role name for a consumer group. This group will be determined by the given id.
        /// </summary>
        /// <param name="id">the consumer group identifier</param>
        /// <returns>the active directory role as string</returns>
        string GetAdRoleForConsumerGroup(string id, Uri namedGraph);

        /// <summary>
        /// Get a list of consumer groups filtered by lifecycle status
        /// </summary>
        /// <param name="lifecycleStatus">the lifecycle status to be filtered</param>
        /// <returns>list of consumer groups filtered by lifecycle status</returns>
        IList<ConsumerGroup> GetConsumerGroupsByLifecycleStatus(string lifecycleStatus, Uri namedGraph);

        /// <summary>
        /// Checks if the consumer group was referenced by another colid entry
        /// </summary>
        /// <param name="id">Identifier of consumer group to check</param>
        /// <returns>true if reference exists, otherwise false</returns>
        bool CheckConsumerGroupHasColidEntryReference(string id, Uri consumerGroupNamedGraph, Uri resourceNamedGraph, Uri draftNamedGraph);

        public string GetContactPersonforConsumergroupe(Uri consumerGroupName, Uri resourceNamedGraph);
    }
}
