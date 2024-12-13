using System.Collections.Generic;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.Services;
using COLID.RegistrationService.Common.DataModel.ResourceTemplates;
using COLID.RegistrationService.Repositories.Interface;

namespace COLID.RegistrationService.Services.Interface
{
    /// <summary>
    /// Service to handle all resource template related operations.
    /// </summary>
    public interface IResourceTemplateService : IBaseEntityService<ResourceTemplate, ResourceTemplateRequestDTO, ResourceTemplateResultDTO, ResourceTemplateWriteResultCTO, IResourceTemplateRepository>
    {
        /// <summary>
        /// By a given id, the resource template will be deleted or set as deprecated.
        /// If a consumer group references the resource template, it can't be deleted,
        /// </summary>
        /// <param name="id">The Id of the resource Template to be deleted.</param>
        /// <returns>A status code</returns>
        void DeleteResourceTemplate(string id);

    }
}
