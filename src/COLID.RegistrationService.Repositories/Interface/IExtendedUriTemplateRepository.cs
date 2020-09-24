using System.Collections.Generic;
using COLID.Graph.TripleStore.Repositories;
using COLID.RegistrationService.Common.DataModel.ExtendedUriTemplates;

namespace COLID.RegistrationService.Repositories.Interface
{
    /// <summary>
    /// Repository to handle all extended uri template related operations.
    /// </summary>
    public interface IExtendedUriTemplateRepository : IBaseRepository<ExtendedUriTemplate>
    {
        /// <summary>
        /// Get a map of orders and depending extended uri templates. The resulting map is ordered alphabetically by the order field.
        /// </summary>
        /// <returns>Dictionary containing orders (first generic) and extendedUriTemplate (second generic)</returns>
        IDictionary<string, string> GetExtendedUriTemplateOrders();
    }
}
