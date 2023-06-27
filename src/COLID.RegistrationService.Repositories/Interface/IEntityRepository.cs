using System.Collections.Generic;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.Repositories;

namespace COLID.RegistrationService.Repositories.Interface
{
    /// <summary>
    /// Repository to handle all entity related operations.
    /// </summary>
    public interface IEntityRepository : IBaseRepository<Entity>
    {
        
    }
}
