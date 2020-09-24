using System.Collections.Generic;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.Transactions;
using VDS.RDF.Query;

namespace COLID.Graph.TripleStore.Repositories
{
    /// <summary>
    /// General repository with basic operations, required for all inheriting repositories.
    /// </summary>
    /// <typeparam name="T">the domain type the repository manages</typeparam>
    public interface IBaseRepository<T> where T : Entity, new()
    {
        /// <summary>
        /// Checks if an entity to a given id exists.
        /// The type is used to determine the correct entities by interiting repositories.
        /// <param name="id">the id to check</param>
        /// <param name="types">the type list to filter by</param>
        /// <returns>true if exists, otherwise false</returns>
        bool CheckIfEntityExists(string id, IList<string> types);

        /// <summary>
        /// Generates the generic entity, identified by the given id.
        /// </summary>
        /// <param name="id">the id to search for</param>
        /// <returns>the found entity, otherwise null</returns>
        T GetEntityById(string id);

        /// <summary>
        /// Creates a new type depending entity with given properties.
        /// </summary>
        /// <param name="newEntity">the new entity</param>
        /// <param name="metadataProperty">the new properties</param>
        void CreateEntity(T newEntity, IList<MetadataProperty> metadataProperty);

        /// <summary>
        /// Updates a present type depending entity with given properties.
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <param name="metadataProperties">the new metadata properties</param>
        void UpdateEntity(T entity, IList<MetadataProperty> metadataProperties);

        /// <summary>
        /// Delete an entity, identified by the given id.
        /// </summary>
        /// <param name="id">the entity to delete</param>
        void DeleteEntity(string id);

        /// <summary>
        /// Searches for entities filtered by given criteria parameters.
        /// </summary>
        /// <param name="search">Criteria to search for</param>
        /// <param name="types">the type list to filter by</param>
        /// <returns>List of entities matching the search criteria</returns>
        IList<T> GetEntities(EntitySearch search, IList<string> types);

        /// <summary>
        /// Creates a new transaction, used for transactional processing.
        /// </summary>
        /// <returns>new transaction object</returns>
        ITripleStoreTransaction CreateTransaction();

        /// <summary>
        /// Generates an insert query for validation purposes.
        /// </summary>
        /// <param name="entity">The entity to use</param>
        /// <param name="metadataProperties">properties to use</param>
        /// <param name="insertGraph">graph to use for insertion</param>
        /// <param name="queryGraph">graph to use for querying</param>
        /// <returns></returns>
        SparqlParameterizedString GenerateInsertQuery(T entity, IList<MetadataProperty> metadataProperties, string insertGraph, IEnumerable<string> queryGraph);
    }
}
