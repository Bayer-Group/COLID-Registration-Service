using System;
using System.Collections.Generic;
using COLID.Graph.HashGenerator.Exceptions;
using COLID.Graph.TripleStore.DataModels.Base;

namespace COLID.Graph.HashGenerator.Services
{
    /// <summary>
    /// This interface represents the hashing of entities.
    /// </summary>
    public interface IEntityHasher
    {
        /// <summary>
        /// This function creates a hash (SHA256) based on the given entity and returns it.
        /// On top level, only properties will be used and these will be sorted by key first, then by it's value.
        /// <br/><br/>
        /// For more details <seealso cref="IEntityHasher.Hash(Entity, ISet)"/>
        /// <br/><br/>
        /// In addition to that, the following technical and invisible technical attributes will be removed:
        /// <list type="bullet">
        ///     <item>https://pid.bayer.com/kos/19050/hasHistoricVersion</item>
        ///     <item>https://pid.bayer.com/kos/19050/646465</item>
        ///     <item>https://pid.bayer.com/kos/19050/hasEntryLifecycleStatus</item>
        ///     <item>https://pid.bayer.com/kos/19050/hasLaterVersion</item>
        ///     <item>https://pid.bayer.com/kos/19050/lastChangeUser</item>
        ///     <item>https://pid.bayer.com/kos/19050/546454</item>
        ///     <item>https://pid.bayer.com/kos/19050/author</item>
        ///     <item>https://pid.bayer.com/kos/19050/lastChangeDateTime</item>
        ///     <item>https://pid.bayer.com/kos/19050/dateCreate</item>
        /// </list>
        /// </summary>
        /// <param name="entity">the entity to hash</param>
        /// <returns>sha256 hash of entity</returns>
        /// <exception cref="ArgumentNullException">if given entity is null</exception>
        /// <exception cref="MissingPropertiesException">If the given entity doesn't contain properties (even after preparing)</exception>
        string Hash(Entity entity);

        /// <summary>
        /// In detail, this function also prepares the given entity for hashing, which performs various necessary operations to create a
        /// normalized entity. In general, only the reference is used, in order not to change the originally passed entity. But note, that
        /// the passed <b>original entity will be changed by LINQ</b>, which results in the order of the values of the sub entities.
        ///
        /// The hashing preparation will include several steps and recursions to normalize the given entity:
        /// <list type="bullet">
        ///     <item>Load the entity</item>
        ///     <item>Remove all empty values (null values, empty strings "" and empty lists)</item>
        ///     <item>Sort all properties by top level keys</item>
        ///     <item>Sort all properties by Values at the top level. If it is a value which has an entity, previous steps are recursive.</item>
        ///     <item>Remove id-fields from distribution, main-distribution and attachments</item>
        ///     <item>Remove all given property keys to be ignored</item>
        ///     <item>Creation of the hash value (hex digest)</item>
        /// </list>
        /// </summary>
        /// <param name="entity">the entity to hash</param>
        /// <param name="propertyKeysToIgnore">the property keys to ignore and remove from entity</param>
        /// <returns>sha256 hash of entity</returns>
        /// <exception cref="ArgumentNullException">if given entity is null</exception>
        /// <exception cref="MissingPropertiesException">If the given entity doesn't contain properties (even after preparing)</exception>
        string Hash(Entity entity, ISet<string> propertyKeysToIgnore);
    }
}
