using System;
using System.Collections.Generic;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.Metadata.DataModels.Metadata;
using VDS.RDF;

namespace COLID.Graph.Metadata.Repositories
{
    /// <summary>
    /// Repository to handle all metadata related operations.
    /// Compared to all other repositories, no entities will be queried here.
    /// <para><b>Note:</b> a caching mechanism is included here to improve performance.</para>
    /// </summary>
    public interface IMetadataRepository
    {
        /// <summary>
        /// Fetch the entity class to a given entity type uri.
        /// </summary>
        /// <param name="entityType">URI of a entity type to search for</param>
        EntityTypeDto GetEntityType(Uri entityType);

        /// <summary>
        /// Fetches the complete hierarchy to a given resource uri as plain list.
        /// </summary>
        /// <param name="firstEntityType">URI of a resource type to search for</param>
        /// <returns>a <see cref="EntityTypeDto"/> including the hierachry</returns>
        EntityTypeDto GetEntityTypes(string firstEntityType);

        /// <summary>
        /// Determines all entity types of a given entity uri and stores them in a list.
        /// These entity types can be any possible type.
        /// </summary>
        /// <param name="firstEntityType">URI of a entity type to search for</param>
        /// <returns>a list of entity types</returns>
        IList<string> GetParentEntityTypes(string firstEntityType);

        /// <summary>
        /// Determines all leaf entity types of a given entity uri and stores them in a list.
        /// These entity types can be any possible type.
        /// </summary>
        /// <param name="firstEntityType">URI of a entity type to search for</param>
        /// <returns>a list of entity types</returns>
        IList<string> GetLeafEntityTypes(string firstEntityType);

        /// <summary>
        /// Determines all instantiable entity types of a given entity uri and stores them in a list.
        /// This function is only allowed for instances in the colid editor.
        /// 
        /// Instantiatable classes are those classes that have the property "abstract" explicitly set to false in ontology. 
        /// A distinction is made here between classes of the programming language itself and the classes from the ontology. 
        /// </summary>
        /// <param name="firstEntityType">URI of a resource type to search for</param>
        /// <returns>a list of entity types</returns>
        IList<EntityTypeDto> GetInstantiableEntityTypes(string firstEntityType);

        /// <summary>
        /// Fetches all Shacl and stores them in a graph.
        /// </summary>
        /// <returns>all shacles as a graph</returns>
        IGraph GetAllShaclAsGraph();

        /// <summary>
        /// Returns the label of an entity of the given id
        /// </summary>
        /// <param name="id">URI of the entity to search for</param>
        /// <returns>the entity label</returns>
        string GetEntityLabelById(string id);

        /// <summary>
        /// Returns the metadata properties of a specific metadata
        /// </summary>
        /// <param name="id">subject id of the metadata</param>
        /// <returns>metadata properties of the given metadata</returns>
        Dictionary<string, string> GetMetadatapropertyValuesById(string id);

        /// <summary>
        /// Based on a given entity type, all related metadata will be determined and stored in a list.
        /// </summary>
        /// <param name="entityType">the entity type to use</param>
        /// <returns>a List of properties, related to an entity type</returns>
        IList<MetadataProperty> GetMetadataForEntityTypeInConfig(string entityType, string configIdentifier = null);
        List<CategoryFilterDTO> GetCategoryFilter();
        List<CategoryFilterDTO> GetCategoryFilter(string categoryFilterName);
        void AddCategoryFilter(CategoryFilterDTO categoryFilterDto);
        void DeleteCategoryFilter(string categoryFilterName);
    }
}
