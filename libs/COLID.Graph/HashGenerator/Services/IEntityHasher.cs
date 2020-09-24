using System;
using System.Collections.Generic;
using COLID.Graph.TripleStore.DataModels.Base;

namespace COLID.Graph.HashGenerator.Services
{
    /// <summary>
    /// This class represents the hashing of entities.
    /// </summary>
    public interface IEntityHasher
    {
        /// <summary>
        /// Hashes the given entity with SHA256 and returns the result as a string.
        /// Only properties will be considered and these will be sorted by key first, then by it's value.
        ///
        /// In addition to that, the following technical and invisible technical attributes will be removed:
        /// <list type="bullet">
        ///		<item>https://pid.bayer.com/kos/19050/hasHistoricVersion</item>
        ///		<item>https://pid.bayer.com/kos/19050/646465</item>
        ///		<item>https://pid.bayer.com/kos/19050/hasEntryLifecycleStatus</item>
        ///		<item>https://pid.bayer.com/kos/19050/hasLaterVersion</item>
        ///		<item>https://pid.bayer.com/kos/19050/lastChangeUser</item>
        ///		<item>https://pid.bayer.com/kos/19050/546454</item>
        ///		<item>https://pid.bayer.com/kos/19050/author</item>
        ///		<item>https://pid.bayer.com/kos/19050/lastChangeDateTime</item>
        ///		<item>https://pid.bayer.com/kos/19050/dateCreate</item>
        /// </list>
        /// </summary>
        /// <param name="entity">the entity to hash</param>
        /// <returns>sha256 hash of entity</returns>
        /// <exception cref="ArgumentNullException">if given entity is null</exception>
        string Hash(Entity entity);

        /// <summary>
        /// Hashes the given entity with SHA256 and returns the result as a string.
        /// Only properties will be considered and these will be sorted by key first, then by it's value.
        /// </summary>
        /// <param name="entity">the entity to hash</param>
        /// <param name="propertyKeysToIgnore">additional properties to ignore</param>
        /// <returns>sha256 hash of entity</returns>
        /// <exception cref="ArgumentNullException">if given entity is null</exception>
        string Hash(Entity entity, ISet<string> propertyKeysToIgnore);
    }
}
