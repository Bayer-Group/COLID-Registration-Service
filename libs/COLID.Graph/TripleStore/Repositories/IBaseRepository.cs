using System;
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
        /// The type is used to determine the correct entities by inheriting repositories.
        /// </summary>
        /// <param name="id">the id to check</param>
        /// <param name="types">the type list to filter by</param>
        /// <param name="namedGraphs">Named graphs where the instances are stored</param>
        /// <returns>true if exists, otherwise false</returns>
        bool CheckIfEntityExists(string id, IList<string> types, ISet<Uri> namedGraphs);

        /// <summary>
        /// Generates the generic entity, identified by the given id.
        /// </summary>
        /// <param name="id">the id to search for</param>
        /// <param name="namedGraphs">Named graphs where the instances are stored</param>
        /// <returns>the found entity, otherwise null</returns>
        T GetEntityById(string id, ISet<Uri> namedGraphs);

        /// <summary>
        /// Creates a new type depending entity with given properties.
        /// </summary>
        /// <param name="newEntity">the new entity</param>
        /// <param name="metadataProperty">the new properties</param>
        /// <param name="namedGraph">Named graph where the instances are stored</param>
        void CreateEntity(T newEntity, IList<MetadataProperty> metadataProperty, Uri namedGraph);

        /// <summary>
        /// Updates a present type depending entity with given properties.
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <param name="metadataProperties">the new metadata properties</param>
        /// <param name="namedGraph">Named graph where the instances are stored</param>
        void UpdateEntity(T entity, IList<MetadataProperty> metadataProperties, Uri namedGraph);

        /// <summary>
        /// Delete an entity, identified by the given id.
        /// </summary>
        /// <param name="id">the entity to delete</param>
        /// <param name="namedGraph">Named graph where the instances are stored</param>
        void DeleteEntity(string id, Uri namedGraph);

        /// <summary>
        /// Searches for entities filtered by given criteria parameters.
        /// </summary>
        /// <param name="entitySearch">Criteria to search for</param>
        /// <param name="types">the type list to filter by</param>
        /// <param name="namedGraphs">Named graphs where the instances are stored</param>
        /// <returns>List of entities matching the search criteria</returns>
        IList<T> GetEntities(EntitySearch entitySearch, IList<string> types, ISet<Uri> namedGraphs);

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
        /// <param name="namedGraph">Named graph where the instances are stored</param>
        /// <returns></returns>
        SparqlParameterizedString GenerateInsertQuery(T entity, IList<MetadataProperty> metadataProperties, Uri namedGraph);

        /// <summary>
        /// Checks if an instance exists that has the given property.
        /// A string comparison is made for the object to exclude any differences in the data type. 
        /// </summary>
        /// <param name="predicate">Uri of the property to be checked</param>
        /// <param name="obj">String of the value (object) to be checked</param>
        /// <param name="entityType">Entity type of the instances to be checked</param>
        /// <param name="namedGraph">Graph to be checked</param>
        /// <param name="id">Output parameter is the id of the located instance.</param>
        /// <returns>true if entity exists, otherwise false</returns>
        bool CheckIfPropertyValueExists(Uri predicate, string obj, string entityType, Uri namedGraph, out string id);

        /// <summary>
        /// Searches for entities labels in the given system.
        /// </summary>
        /// <returns>List of entities matching their label</returns>
        IList<T> GetEntitiesLabels(ISet<Uri> namedGraphs);
    }
}
