using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.Repositories;

namespace COLID.Graph.TripleStore.Services
{
    /// <summary>
    /// General service with basic operations, required for all inheriting services.
    /// </summary>
    /// <list>
    /// <typeparam name="TEntity">the entity class</typeparam>
    /// <typeparam name="TEntityRequest">the entity request class</typeparam>
    /// <typeparam name="TEntityResult">the entity result class</typeparam>
    /// <typeparam name="TEntityWriteResult">the entity write result class</typeparam>
    /// <typeparam name="TRepository">the repository class</typeparam>
#pragma warning disable CA1005 // Avoid excessive parameters on generic types
    public interface IBaseEntityService<TEntity, TEntityRequest, TEntityResult, TEntityWriteResult, TRepository>
#pragma warning restore CA1005 // Avoid excessive parameters on generic types
        where TEntity : Entity, new()
        where TEntityRequest : BaseEntityRequestDTO
        where TEntityResult : BaseEntityResultDTO
        where TEntityWriteResult : BaseEntityResultCTO, new()
        where TRepository : IBaseRepository<TEntity>
    {
        /// <summary>
        /// Searches for entities filtered by given criteria parameters.
        /// </summary>
        /// <param name="search">Criteria to search for</param>
        /// <returns>List of entities matching the search criteria</returns>
        IList<TEntityResult> GetEntities(EntitySearch search);

        /// <summary>
        /// Generates the generic entity, identified by the given id.
        /// </summary>
        /// <param name="id">the Id to search for</param>
        /// <returns>the found entity, otherwise null</returns>
        TEntityResult GetEntity(string id);

        /// <summary>
        /// Creates a new type depending entity with given properties.
        /// </summary>
        /// <param name="baseEntityRequest">the entity</param>
        Task<TEntityWriteResult> CreateEntity(TEntityRequest baseEntityRequest);

        /// <summary>
        /// Edits a present entity.
        /// </summary>
        /// <param name="identifier">the id to search for</param>
        /// <param name="baseEntityRequest">The entity to edit</param>
        TEntityWriteResult EditEntity(string identifier, TEntityRequest baseEntityRequest);

        /// <summary>
        /// Delete an entity, identified by the given id.
        /// </summary>
        /// <param name="id">the entity to delete</param>
        void DeleteEntity(string id);

        /// <summary>
        /// Checks if an instance exists that has the given property.
        /// A string comparison is made for the object to exclude any differences in the data type. 
        /// </summary>
        /// <param name="predicate">Uri of the property to be checked</param>
        /// <param name="obj">String of the value (object) to be checked</param>
        /// <param name="entityType">Entity type of the instances to be checked</param>
        /// <param name="id">Output parameter is the id of the located instance.</param>
        /// <returns>true if entity exists, otherwise false</returns>
        bool CheckIfPropertyValueExists(Uri predicate, string obj, string entityType, out string id);
    }
}
