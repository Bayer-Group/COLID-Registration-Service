using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.Services;
using COLID.RegistrationService.Repositories.Interface;

namespace COLID.RegistrationService.Services.Interface
{
    /// <summary>
    /// Service to handle all entity related operations.
    /// </summary>
    public interface IEntityService : IBaseEntityService<Entity, BaseEntityRequestDTO, BaseEntityResultDTO, BaseEntityResultCTO, IEntityRepository>
    {
    }
}
