using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using COLID.Common.Extensions;
using COLID.Common.Utilities;
using COLID.Graph.HashGenerator.Exceptions;
using COLID.Graph.Metadata.Extensions;
using COLID.Graph.TripleStore.DataModels.Base;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace COLID.Graph.HashGenerator.Services
{
    /// <summary>
    /// This class represents the hashing of entities.
    /// </summary>
    public class EntityHasher : IEntityHasher
    {
        private readonly JsonSerializerSettings _serializerSettings;

        public EntityHasher()
        {
            _serializerSettings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
        }

        /// <summary>
        /// Hashes the given entity with SHA256 and returns the result as a string.
        /// Only properties will be considered and all properties will be sorted by key first, and the by it's value.
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
        public string Hash(Entity entity)
        {
            Guard.ArgumentNotNull(entity, nameof(entity));
            var propertyKeysToIgnore = GetTechnicalKeys();
            return Hash(entity, propertyKeysToIgnore);
        }

        public string Hash(Entity entity, ISet<string> propertyKeysToIgnore)
        {
            Guard.ArgumentNotNull(entity, nameof(entity));
            Guard.ArgumentNotNull(propertyKeysToIgnore, nameof(propertyKeysToIgnore));

            if (entity.Properties.IsNullOrEmpty())
            {
                throw new MissingPropertiesException("The given entity does not contain any properties");
            }

            using SHA256 sha256 = SHA256.Create();

            var sortedEntity = SortEntity(entity);
            var cleanedProperties = sortedEntity.Properties
                .Where(x => !propertyKeysToIgnore.Any(y => y.Equals(x.Key.ToString())))
                .ToDictionary(t => t.Key, t => t.Value);

            if (cleanedProperties.IsNullOrEmpty())
            {
                throw new MissingPropertiesException(
                    "The given entity contains no properties. It is possible, that only technical ones were passed.");
            }

            string propertyJson = JsonConvert.SerializeObject(cleanedProperties, _serializerSettings);

            return GetHash(sha256, propertyJson);
        }

        /// <summary>
        /// Generate a hash by the given algorithm and input string. Originally taken from:
        /// https://docs.microsoft.com/de-de/dotnet/api/system.security.cryptography.hashalgorithm.computehash?view=netcore-3.1
        /// </summary>
        /// <param name="hashAlgorithm">The algorithm to use</param>
        /// <param name="input">the input string to hash</param>
        /// <returns>the hashed input string as hexadecimal</returns>
        private string GetHash(HashAlgorithm hashAlgorithm, string input)
        {
            // Convert the input string to a byte array and compute the hash.
            byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            var sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        /// <summary>
        /// Get a list of technical and invisible technical keys to ignore.
        /// </summary>
        private ISet<string> GetTechnicalKeys()
        {
            return new HashSet<string> {
                Metadata.Constants.Resource.HasHistoricVersion,
                Metadata.Constants.Resource.MetadataGraphConfiguration,
                Metadata.Constants.Resource.HasEntryLifecycleStatus,
                Metadata.Constants.Resource.HasLaterVersion,

                Metadata.Constants.Resource.ChangeRequester,
                Metadata.Constants.Resource.DateModified,
                Metadata.Constants.Resource.LastChangeUser,
                Metadata.Constants.Resource.Author,
                Metadata.Constants.Resource.DateCreated,
            };
        }

        /// <summary>
        /// Recursive function to sort the properties of an entity first by key, afterwards by value.
        /// </summary>
        /// <param name="entity">the entity to sort</param>
        /// <returns>a sorted entity</returns>
        private Entity SortEntity(Entity entity)
        {
            entity.Properties = entity.Properties
                .OrderBy(x => x.Key, StringComparer.Ordinal)
                .ThenBy(x => x.Value)
                .ToDictionary(x => x.Key, x => x.Value);

            foreach (var (_, value) in entity.Properties)
            {
                foreach (var listItem in value.Where(listItem => listItem != null))
                {
                    if (DynamicExtension.IsType<Entity>(listItem, out Entity mappedEntity))
                    {
                        SortEntity(mappedEntity);
                    }
                }
            }

            return entity;
        }
    }
}
