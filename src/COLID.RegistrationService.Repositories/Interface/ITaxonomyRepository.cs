using System.Collections.Generic;
using COLID.Graph.TripleStore.DataModels.Taxonomies;
using COLID.Graph.TripleStore.Repositories;

namespace COLID.RegistrationService.Repositories.Interface
{
    /// <summary>
    /// Repository to handle all taxonomy related operations.
    /// </summary>
    public interface ITaxonomyRepository : IBaseRepository<Taxonomy>
    {
        /// <summary>
        /// Returns the taxonomies that begin with the taxonomy of the identifier
        /// </summary>
        /// <param name="identifier">The identifier of the taxonomy to be searched from</param>
        /// <returns>Plan list of taxonomies beginning with the identifier</returns>
        IList<Taxonomy> GetTaxonomiesByIdentifier(string identifier);

        /// <summary>
        /// Returns the taxonomies of the given type.
        /// </summary>
        /// <param name="type">The type of the taxonomy to be searched for</param>
        /// <returns>Plan list of taxonomies</returns>
        IList<Taxonomy> GetTaxonomies(string type);
    }
}
