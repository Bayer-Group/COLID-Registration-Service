using COLID.Graph.TripleStore.Services;
using COLID.RegistrationService.Common.DataModel.ExtendedUriTemplates;
using COLID.RegistrationService.Repositories.Interface;

namespace COLID.RegistrationService.Services.Interface
{
    /// <summary>
    /// Service to handle all extended uri template related operations.
    /// </summary>
    public interface IExtendedUriTemplateService : IBaseEntityService<ExtendedUriTemplate, ExtendedUriTemplateRequestDTO, ExtendedUriTemplateResultDTO, ExtendedUriTemplateWriteResultCTO, IExtendedUriTemplateRepository>
    {
    }
}
