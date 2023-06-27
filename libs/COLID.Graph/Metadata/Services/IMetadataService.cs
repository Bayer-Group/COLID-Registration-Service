using System;
using System.Collections.Generic;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.TripleStore.DataModels.Base;
using VDS.RDF;
using Newtonsoft.Json.Schema;
using COLID.Graph.Metadata.DataModels.Metadata.Comparison;
using COLID.Graph.Metadata.DataModels.FilterGroup;

namespace COLID.Graph.Metadata.Services
{
    /// <summary>
    /// Service to handle all metadata related operations.
    /// </summary>
    public interface IMetadataService
    {
        /// <summary>
        /// Based on a given entity type, all related metadata will be determined and stored in a list.
        /// </summary>
        /// <param name="entityType">the entity type to use</param>
        /// <returns>a List of properties, related to an entity type</returns>
        IList<MetadataProperty> GetMetadataForEntityType(string entityType);

        /// <summary>
        /// Based on a given entity type, all related metadata will be determined and stored in a list.
        /// </summary>
        /// <param name="entityType">the entity type to use</param>
        /// <param name="configIdentifier">the config identifier used to build the metadata</param>
        /// <returns>a List of properties, related to an entity type</returns>
        IList<MetadataProperty> GetMetadataForEntityTypeInConfig(string entityType, string configIdentifier);

        /// <summary>
        /// Based on given entity types, all related metadata will be determined and stored in a list.
        /// Metadata are grouped under the same key and metadata of different types are stored in a list by key
        /// </summary>
        /// <param name="metadataComparisonConfigTypes">combination of metadata configuration and entity types to use</param>
        /// <returns>A list of merged metadata properties, related to entity types</returns>
        IList<MetadataComparisonProperty> GetComparisonMetadata(IEnumerable<MetadataComparisonConfigTypesDto> metadataComparisonConfigTypes);

        /// <summary>
        /// Based on given entity types, all related metadata will be determined and stored in a list.
        /// Metadata are grouped under the same key and metadata of all types are stored distinct in a list
        /// </summary>
        /// <param name="entityTypes">the entity types to use</param>
        /// <param name="entityConfig">metadata config to use</param>
        /// <returns>A list of merged metadata properties, related to entity types</returns>
        IList<MetadataProperty> GetMergedMetadata(IEnumerable<string> entityTypes, string configIdentifier = null);

        /// <summary>
        /// Fetches the complete hierarchy to a given resource uri.
        /// </summary>
        /// <param name="firstEntityType">URI of a entity type to search for</param>
        /// <returns>a <see cref="EntityTypeDto"/> including the hierarchy</returns>
        EntityTypeDto GetResourceTypeHierarchy(string firstEntityType);

        /// <summary>
        /// Fetches the complete resource hierarchy in marketplace dto format.
        /// </summary>
        /// <param name="firstEntityType">URI of a entity type to search for</param>
        /// <returns>a <see cref="EntityTypeDto"/> including the hierarchy</returns>
        IList<ResourceHierarchyDTO> GetResourceTypeHierarchyDmp(string firstEntityType);

        /// <summary>
        /// Returns all the Link Types  present in metadata graphs.
        /// </summary>
        Dictionary<string, string> GetLinkTypes(); 

        /// <summary>
        /// Fetches the category filters in neptune raw format.
        /// </summary>
        IList<CategoryFilterDTO> GetCategoryFilter();

        /// <summary>
        /// Fetches a specific category filter in neptune raw format by given name.
        /// </summary>
        IList<CategoryFilterDTO> GetCategoryFilter(string categoryName);
        /// <summary>
        /// Fetches the category filters in marketplace dto format.
        /// </summary>
        IList<ResourceHierarchyDTO> GetCategoryFilterDmp();
        /// <summary>
        /// Creates or updates a category filter in neptune
        /// </summary>
        /// <param name="categoryFilter">category filter</param>
        void CreateOrUpdateCategoryFilter(CategoryFilterDTO categoryList);

        /// <summary>
        /// Deletes a category filter in neptune by given Name
        /// </summary>
        /// <param name="categoryFilter">category filter</param>
        void DeleteCategoryFilter(string categoryName);





        /// <summary>
        /// Determines all entity types of a given entity uri and stores them in a list.
        /// These entity types can be any possible type.
        /// </summary>
        /// <param name="entityType">URI of a entity type to search for</param>
        /// <returns>a list of entity types</returns>
        IList<string> GetEntityTypes(string entityType);

        /// <summary>
        /// Returns the metadata properties of a specific metadata
        /// </summary>
        /// <param name="id">subject id of the metadata</param>
        /// <returns>metadata properties of the given metadata</returns>
        Dictionary<string, string> GetMetadatapropertyValuesById(string id);

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
        /// </summary>
        /// <param name="firstEntityType">URI of a resource type to search for</param>
        /// <returns>a list of entity types</returns>
        IList<string> GetInstantiableEntityTypes(string firstEntityType);

        /// <summary>
        /// Determines all instantiable entity types of a given entity uri and stores them in a list.
        /// This function is only allowed for instances in the colid editor.
        /// </summary>
        /// <param name="firstEntityType">URI of a resource type to search for</param>
        /// <returns>a list of entity type DTOs</returns>
        IList<EntityTypeDto> GetInstantiableEntity(string firstEntityType);

        /// <summary>
        /// Fetches all Shacl and stores them in a graph.
        /// </summary>
        /// <returns>all shacles as a graph</returns>
        IGraph GetAllShaclAsGraph();

        /// <summary>
        /// Determine a taxonomy by a given id and returns its label.
        /// </summary>
        /// <param name="id">The Id of the taxonomy to search for</param>
        /// <returns>the matched taxonomy</returns>
        string GetPrefLabelForEntity(string id);

        /// <summary>
        /// Returns the instance graph for the given entity type.
        /// It is checked in the metadata or the corresponding config if a graph is stored.
        /// </summary>
        /// <param name="entityType">Type to retrieve the instance graph.</param>
        /// <returns>Graph where the instances of the type is stored.</returns>
        Uri GetInstanceGraph(string entityType);

        /// <summary>
        /// Returns all graphs where instance of the given type might be stored.
        ///
        /// Reason:
        /// For controlled vocabulary it is not possible to specify exactly in which graph the necessary data is stored.
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        ISet<Uri> GetMultiInstanceGraph(string entityType);

        /// <summary>
        /// Returns the instance graph for historic resource
        /// </summary>
        /// <returns>Graph for historic resources</returns>
        Uri GetHistoricInstanceGraph();

        /// <summary>
        /// Returns all graphs that are stored in the metadata configuration. 
        /// </summary>
        /// <returns>List of all graphs</returns>
        ISet<Uri> GetAllGraph();

        /// <summary>
        /// Returns all graphs that are stored in the metadata configuration which is applicable to the published resources
        /// </summary>
        /// <returns></returns>
        ISet<Uri> GetGraphForPublishedResource();

        ISet<Uri> GetMetadataGraphs();

        /// <summary>
        /// Determine the valid validation schema for the given entity type.
        /// </summary>
        /// <param name="entityType">entity type to use</param>
        /// <returns>the validation schema</returns>
        JSchema GetValidationSchema(string entityType);

        /// <summary>
        /// Determine the valid resource types for given links.
        /// </summary>
        /// <param name="entityType">entity type to use</param>
        /// <returns>the validation schema</returns>
        IList<Entity> GetLinkedEntityTypes (IList<Entity> entityType);

        /// <summary>
        /// Get all distribution endpoint types
        /// </summary>
        /// <returns></returns>
        Dictionary<string, string> GetDistributionEndpointTypes();

        /// <summary>
        /// Get Filter Group and Properties
        /// </summary>
        /// <returns></returns>
        IList<FilterGroup> GetFilterGroupAndProperties();
    }
}
