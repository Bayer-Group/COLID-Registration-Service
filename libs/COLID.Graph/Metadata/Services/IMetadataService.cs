using System.Collections.Generic;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.TripleStore.DataModels.Base;
using VDS.RDF;
using Newtonsoft.Json.Schema;
using COLID.Graph.Metadata.DataModels.Metadata.Comparison;

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
        /// <returns>a List of properties, related to an entity type</returns>
        IList<MetadataProperty> GetMetadataForEntityTypeInConfig(string entityType, string entityConfig);

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
        /// <param name="firstEntityType">URI of a resource type to search for</param>
        /// <returns>a <see cref="EntityTypeDto"/> including the hierachry</returns>
        EntityTypeDto GetResourceTypeHierarchy(string firstEntityType);

        /// <summary>
        /// Determines all entity types of a given entity uri and stores them in a list.
        /// These entity types can be any possible type.
        /// </summary>
        /// <param name="firstEntityType">URI of a entity type to search for</param>
        /// <returns>a list of entity types</returns>
        IList<string> GetEntityTypes(string firstEntityType);

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
        /// Determine the valid validation schema for the given entity type.
        /// </summary>
        /// <param name="entityType">entity type to use</param>
        /// <returns>the validation schema</returns>
        JSchema GetValidationSchema(string entityType);
    }
}
