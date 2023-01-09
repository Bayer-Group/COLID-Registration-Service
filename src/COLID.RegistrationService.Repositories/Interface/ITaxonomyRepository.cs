using System;
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
        /// <param name="metadataNamedGraphs">All metadata graphs to be searched in</param>
        /// <returns>Plan list of taxonomies beginning with the identifier</returns>
        IList<Taxonomy> GetTaxonomiesByIdentifier(string identifier, ISet<Uri> metadataNamedGraphs);

        /// <summary>
        /// Returns the taxonomies of the given type.
        /// </summary>
        /// <param name="type">The type of the taxonomy to be searched for</param>
        /// <param name="metadataNamedGraphs">All metadata graphs to be searched in</param>
        /// <returns>Plan list of taxonomies</returns>
        IList<Taxonomy> GetTaxonomies(string type, ISet<Uri> metadataNamedGraphs);

        /// <summary>
        /// Get all taxonomies to resolve labels in excel export
        /// </summary>
        /// <param name="metadataNamedGraphs"></param>
        /// <returns></returns>
        IList<Taxonomy> GetTaxonomies(ISet<Uri> metadataNamedGraphs);

        /// <summary>
        /// Generates a taxonomy dynamically based upon top and broader concepts
        /// </summary>
        /// <param name="type">The type of the taxonomy to be searched for</param>
        /// <param name="metadataNamedGraphs">All metadata graphs to be searched in< </param>
        /// <returns>Plain list of taxonomy</returns>
        IList<Taxonomy> BuildTaxonomy(string type, ISet<Uri> metadataNamedGraphs);

        /// <summary>
        /// Get all taxonomy labels for excel export
        /// </summary>
        /// <param name="metadataNamedGraphs"></param>
        /// <returns></returns>
        IList<TaxonomyLabel> GetTaxonomyLabels(ISet<Uri> metadataNamedGraphs);
    }
}
