using System.Collections.Generic;
using System.Threading.Tasks;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.TripleStore.DataModels.Base;

namespace COLID.Graph.Metadata.Services
{
    /// <summary>
    /// Service to handle all remote validation related operations.
    /// </summary>
    public interface IValidationService
    {
        /// <summary>
        /// Validates a given entity.
        /// </summary>
        /// <param name="entity">the entity to validate</param>
        /// <param name="metadataProperties">the properties to consider</param>
        /// <returns>the result of validation</returns>
        Task<ValidationResult> ValidateEntity(Entity entity, IList<MetadataProperty> metadataProperties);

        /// <summary>
        /// Checks whether the given entity, contains a property with its type and may be stored by the interface. Accordingly, an exception is thrown.
        /// </summary>
        /// <typeparam name="TEntity">TEntity is a type that implements the class entity</typeparam>
        /// <param name="entity">The entity to be checked</param>
        void CheckType<TEntity>(TEntity entity) where TEntity : EntityBase;

        /// <summary>
        /// Checks whether the given entity, contains a property with its type, is not abstract class and may be stored by the interface. Accordingly, an exception is thrown.
        /// </summary>
        /// <typeparam name="TEntity">TEntity is a type that implements the class entity</typeparam>
        /// <param name="entity">The entity to be checked</param>
        public void CheckInstantiableEntityType<TEntity>(TEntity entity) where TEntity : EntityBase;

        /// <summary>
        /// Checks whether the entity contains properties that are not defined in the ontology.
        /// </summary>
        /// <typeparam name="TEntity">TEntity is a type that implements the class entity</typeparam>
        /// <param name="entity">The entity to be checked</param>
        /// <returns>Returns a list of critical validation results properties. These show which properties are not allowed to be stored</returns>
        IList<ValidationResultProperty> CheckForbiddenProperties<TEntity>(TEntity entity) where TEntity : Entity;
    }
}
