using System.Collections.Generic;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.Services;
using COLID.RegistrationService.Common.DataModel.PidUriTemplates;
using COLID.RegistrationService.Repositories.Interface;

namespace COLID.RegistrationService.Services.Interface
{
    /// <summary>
    /// Service to handle all pid uri template related operations.
    /// </summary>
    public interface IPidUriTemplateService : IBaseEntityService<PidUriTemplate, PidUriTemplateRequestDTO, PidUriTemplateResultDTO, PidUriTemplateWriteResultCTO, IPidUriTemplateRepository>
    {
        /// <summary>
        /// By a given id, the pid uri template will be deleted or set as deprecated.
        /// If a permanent identifier references the pid uri template, the status is set to deprecated,
        /// otherwise the pid uri template will be deleted.
        /// </summary>
        /// <param name="id">the pid uri template identifier</param>
        void DeleteOrDeprecatePidUriTemplate(string id);

        /// <summary>
        /// By a give id, the pid uri template will be reactivated.
        /// </summary>
        /// <param name="id">the uri template to reactivate</param>
        void ReactivateTemplate(string id);

        /// <summary>
        /// By a given id, the flat identifier templates will be determined and returned.
        /// </summary>
        /// <param name="id">the uri template to search for</param>
        /// <returns>the template if found. In case of an empty id, null will be returned</returns>
        PidUriTemplateFlattened GetFlatIdentifierTemplateById(string id);

        /// <summary>
        /// By a given entity, the flat identifier templates will be determined and returned.
        /// </summary>
        /// <param name="pidUriTemplate">the entity to search for</param>
        /// <returns>the template if found</returns>
        PidUriTemplateFlattened GetFlatPidUriTemplateByPidUriTemplate(Entity pidUriTemplate);

        /// <summary>
        /// Formats the name of a given template and returns it.
        /// </summary>
        /// <param name="pidUriTemplate">the name to format</param>
        /// <returns>a formatted name</returns>
        string FormatPidUriTemplateName(PidUriTemplateFlattened pidUriTemplate);

        /// <summary>
        /// Searches for flat identifier templates
        /// </summary>
        /// <param name="entitySearch">Criteria to search for</param>
        /// <returns>List of flat pid uri templates matching the search criteria</returns>
        IList<PidUriTemplateFlattened> GetFlatPidUriTemplates(EntitySearch entitySearch);
    }
}
