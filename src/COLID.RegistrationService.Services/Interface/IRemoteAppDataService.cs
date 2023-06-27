using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.RegistrationService.Common.DataModel.DistributionEndpoints;
using COLID.RegistrationService.Common.DataModels.Contacts;
using COLID.RegistrationService.Common.DataModels.TransferObjects;

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
        /// <param name="pidUri">Pid uri of colid entry</param>
        /// <param name="resource">The COLID entry</param>
        Task NotifyResourceDeleted(Uri pidUri, Entity resource);

        /// <summary>
        /// Checks if a person exists in the active directory
        /// </summary>
        /// <param name="id">The person id to check.</param>
        /// <returns>True if exists otherwise false</returns>
        bool CheckPerson(string id);

        /// <summary>
        /// Checks if a person exists in colid
        /// </summary>
        /// <returns>True if exists otherwise false</returns>
        Task<List<ColidUserDto>> GetAllColidUser();

        /// <summary>
        /// Gets all existing MessageTemplateDtos
        /// </summary>
        /// <returns>list of messagetemplates</returns>
        Task<List<MessageTemplateDto>> GetAllMessageTemplates();

        /// <summary>
        /// Notify user about invalid distribution endpoint
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task NotifyInvalidDistributionEndpoint(InvalidDistributionEndpointMessage message);

        /// <summary>
        /// Notify users about invalid data stewards and distribution endpoint contacts
        /// </summary>
        /// <param name="message">Containing the email to whom send the notification and list of the resources 
        /// containing invalid contacts</param>
        /// <returns></returns>
        Task NotifyInvalidContact(ColidEntryContactInvalidUsersDto message);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="checkSuccessful"></param>
        Task DeleteByAdditionalInfoAsync(IList<string> checkSuccessful);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="checkUnsuccessful"></param>
        /// <returns></returns>
        Task<List<(string pidUri, DateTime createdAt)>> GetByAdditionalInfoAsync(IList<string> checkUnsuccessful);

        /// <summary>
        /// sends generic message to colid user
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        Task SendGenericMessage(string subject, string body, string email);

        /// <summary>
        /// Check a given list of user emails for invalid contacts
        /// </summary>
        /// <param name="userEmails">list of user emails</param>
        /// <returns></returns>
        Task<IList<AdUserDto>> CheckUsersValidity(ISet<string> userEmails);

    }
}
