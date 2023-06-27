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
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace COLID.Graph.HashGenerator.Services
{
    /// <summary>
    /// This class represents the hashing of entities.
    /// </summary>
    public class EntityHasher : IEntityHasher
    {
        /// <summary>
        /// Serializer setting for json.
        /// </summary>
        private readonly JsonSerializerSettings _serializerSettings;

        /// <summary>
        /// ILogger for debugging purposes.
        /// </summary>
        private readonly ILogger<EntityHasher> _logger;

        public EntityHasher(ILogger<EntityHasher> logger)
        {
            _serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            };

            IsoDateTimeConverter dateConverter = new IsoDateTimeConverter
            {
                DateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fff'Z'"
            };
            _serializerSettings.Converters.Add(dateConverter);

            _logger = logger;
        }

        /// <summary>
        /// <inheritdoc>
        ///     <cref>IEntityHasher.Hash(Entity)</cref>
        /// </inheritdoc>
        /// </summary>
        /// <param name="entity">the entity to hash</param>
        /// <returns>sha256 hash of entity</returns>
        /// <exception cref="ArgumentNullException">if given entity is null</exception>
        /// <exception cref="MissingPropertiesException">If the given entity doesn't contain properties (even after preparing)</exception>
        public string Hash(Entity entity)
        {
            Guard.ArgumentNotNull(entity, nameof(entity));

            var propertyKeysToIgnore = GetTechnicalKeys();

            return Hash(entity, propertyKeysToIgnore);
        }

        /// <summary>
        /// <inheritdoc>
        ///     <cref>IEntityHasher.Hash(Entity, ISet)</cref>
        /// </inheritdoc>
        /// </summary>
        /// <param name="entity">the entity to hash</param>
        /// <param name="propertyKeysToIgnore">the property keys to ignore and remove from entity</param>
        /// <returns>sha256 hash of entity</returns>
        /// <exception cref="ArgumentNullException">if given entity is null</exception>
        /// <exception cref="MissingPropertiesException">If the given entity doesn't contain properties (even after preparing)</exception>
        public string Hash(Entity entity, ISet<string> propertyKeysToIgnore)
        {
            Guard.ArgumentNotNull(entity, nameof(entity));
            Guard.ArgumentNotNull(propertyKeysToIgnore, nameof(propertyKeysToIgnore));
            if (entity.Properties.IsNullOrEmpty())
            {
                throw new MissingPropertiesException("The given entity does not contain any properties");
            }

            Entity hashableEntity = PrepareEntityForHashing(entity, propertyKeysToIgnore);
            if (hashableEntity.Properties.IsNullOrEmpty())
            {
                throw new MissingPropertiesException(
                    "The given entity contains no properties. It is possible, that only technical ones were passed.");
            }
            string entityJson = JsonConvert.SerializeObject(hashableEntity.Properties, _serializerSettings);

            using SHA256 sha256 = SHA256.Create();
            var computedHash = HashGenerator.GetHash(sha256, entityJson);

            var loggingInfos = new Dictionary<string, string>
            {
                {"Ignored_Properties", string.Join(',', propertyKeysToIgnore) },
                {"Algorithm", nameof(sha256)},
                {"Entity_Original", entity.ToString()},
                {"Entity_ToHash", hashableEntity.ToString()},
                {"Computed_Hash", computedHash}
            };
            _logger.LogDebug("Entity has been hashed", loggingInfos);

            return HashGenerator.GetHash(sha256, entityJson);
        }

        private Entity PrepareEntityForHashing(Entity entity, ISet<string> propertyKeysToIgnore, bool calledRecursively = false)
        {
            // Important: This reference will change the sub-property order in nested entities from the given original entity
            var ety = new Entity(entity.Id, entity.Properties);

            ety.Properties = RemoveEmptyProperties(ety.Properties, propertyKeysToIgnore);

            // Id of endpoints between resource versions are different
            ety.Properties = RemoveIdFromDistributionEndpoints(ety.Properties);

            ety.Properties = SortValuesByKeyAndValue(ety.Properties);

            // Inbound properties are not requiered to hash.
            ety.InboundProperties = null;

            return ety;
        }

        private IDictionary<string, List<dynamic>> RemoveEmptyProperties(IDictionary<string, List<dynamic>> properties, ISet<string> propertyKeysToIgnore)
        {
            properties = properties
                .Where(propertyValues => propertyValues.Value != null) // if the user inserts null values instead of lists 
                .Select(propertyValues =>
                {
                    var newValues = propertyValues.Value
                    .Where(v => v != null) // Filter empty properties
                    .Where(v => !string.IsNullOrWhiteSpace(v.ToString().Trim()))
                    .Select(value =>
                    {
                            // Check if value is entity and remove nested empty values
                            if (DynamicExtension.IsType<Entity>(value, out Entity entity))
                        {
                            entity.Properties = RemoveEmptyProperties(entity.Properties, propertyKeysToIgnore);
                            entity.InboundProperties = null;
                            return entity as dynamic;
                        }

                        return value;
                    });

                    return new KeyValuePair<string, List<dynamic>>(propertyValues.Key, newValues.ToList());
                })
                .Where(propertyValues => !(propertyValues.Value.IsNullOrEmpty() || propertyKeysToIgnore.Contains(propertyValues.Key))) // Remove empty lists and property keys to be ignored
                .ToDictionary(x => x.Key, x => x.Value);

            return properties;
        }

        /// <summary>
        /// Removes the field ID recursively from the given entity, if the key is distribution endpoint
        /// - Distribution
        /// - Main Distribution
        /// </summary>
        /// <param name="entity">the entity to use</param>
        private static IDictionary<string, List<dynamic>> RemoveIdFromDistributionEndpoints(IDictionary<string, List<dynamic>> properties)
        {
            return properties
                .Select(propertyValues =>
                {
                    var newValues = propertyValues.Value;

                    if (propertyValues.Key == Metadata.Constants.Resource.Distribution || propertyValues.Key == Metadata.Constants.Resource.MainDistribution)
                    {
                        newValues = propertyValues.Value.Select(v =>
                        {
                            if (DynamicExtension.IsType<Entity>(v, out Entity mappedEntity))
                            {
                                mappedEntity.Id = null;
                                return mappedEntity as dynamic;
                            }
                            return v;
                        }).ToList();

                    }

                    return new KeyValuePair<string, List<dynamic>>(propertyValues.Key, newValues);
                })
                .ToDictionary(x => x.Key, x => x.Value);
        }

        private IDictionary<string, List<dynamic>> SortValuesByKeyAndValue(IDictionary<string, List<dynamic>> properties)
        {
            var sortedProperties = properties
                .Select(propertyValues =>
                {
                    var newValues = propertyValues.Value
                    .Select(value =>
                    {
                        // Check if value is entity and remove nested empty values
                        if (DynamicExtension.IsType<Entity>(value, out Entity entity))
                        {
                            entity.Properties = SortValuesByKeyAndValue(entity.Properties);
                            return entity as dynamic;
                        }

                        return value;
                    })
                    .OrderBy(t => GetKeyToOrderBy(t));

                    return new KeyValuePair<string, List<dynamic>>(propertyValues.Key, newValues.ToList());
                })
                .OrderBy(x => x.Key, StringComparer.Ordinal) // sort by key
                .ToDictionary(x => x.Key, x => x.Value);

            return sortedProperties;
        }

        private static string GetKeyToOrderBy(dynamic value)
        {
            if (DynamicExtension.IsType<Entity>(value, out Entity entity))
            {
                if (entity.Properties.TryGetValue(COLID.Graph.Metadata.Constants.EnterpriseCore.PidUri, out var pidUriList))
                {
                    foreach(var pidUriEntity in pidUriList)
                    {
                        if (DynamicExtension.IsType<Entity>(pidUriEntity, out Entity pidUri))
                        {
                            return pidUri.Id;
                        }
                    }
                    
                }
                return entity.Id;
            }
            return value.ToString();
        }

        
        /// <summary>
        /// Get a list of technical and invisible technical keys to ignore.
        /// </summary>
        private static ISet<string> GetTechnicalKeys()
        {
            return new HashSet<string>
            {
                //Metadata.Constants.Resource.HasHistoricVersion,
                Metadata.Constants.Resource.MetadataGraphConfiguration,
                Metadata.Constants.Resource.HasEntryLifecycleStatus,
                Metadata.Constants.Resource.HasLaterVersion,
                Metadata.Constants.Resource.LastChangeUser,
                Metadata.Constants.Resource.ChangeRequester,
                Metadata.Constants.Resource.Author,
                Metadata.Constants.Resource.DateModified,
                Metadata.Constants.Resource.DateCreated,
                Metadata.Constants.Resource.HasPidEntryDraft,
                Metadata.Constants.Resource.HasEntryLifecycleStatus
            };
        }
    }
}
