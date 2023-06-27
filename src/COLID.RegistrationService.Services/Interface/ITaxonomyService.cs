using System.Collections.Generic;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.Services;
using COLID.Graph.TripleStore.DataModels.Taxonomies;
using COLID.RegistrationService.Repositories.Interface;

namespace COLID.RegistrationService.Services.Interface
{
    public interface ITaxonomyService : IBaseEntityService<Taxonomy, TaxonomyRequestDTO, TaxonomyResultDTO, BaseEntityResultCTO, ITaxonomyRepository>
    {
        /// <summary>
        /// Returns the taxonomies of the given type.
        /// </summary>
        /// <param name="taxonomyType">The type of the taxonomy to be searched for</param>
        /// <returns>List of taxonomies</returns>
        IList<TaxonomyResultDTO> GetTaxonomies(string taxonomyType);

        /// <summary>
        /// Returns all taxonomies as a plain list of the given type.
        /// </summary>
        /// <param name="taxonomyType">The type of the taxonomy to be searched for</param>
        /// <returns>List of taxonomies</returns>
        IList<TaxonomyResultDTO> GetTaxonomiesAsPlainList(string taxonomyType);

        /// <summary>
        /// Get all taxonomies to resolve labels in excel export
        /// </summary>
        /// <returns></returns>
        IList<TaxonomyLabel> GetTaxonomyLabels();
    }
}
