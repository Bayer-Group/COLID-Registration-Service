using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using COLID.Cache.Services;
using COLID.Common.Extensions;
using COLID.Common.Utilities;
using COLID.Graph.TripleStore.DataModels.Attributes;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.DataModels.ConsumerGroups;
using COLID.Graph.TripleStore.Services;
using COLID.RegistrationService.Services.Implementation;

namespace COLID.RegistrationService.Services.Extensions
{
    public static class CacheExtension
    {
        // required because of the entity service getById
        public static void DeleteRelatedCacheEntries<TService, TEntityType>(this ICacheService cache, string identifier) where TEntityType : EntityBase
        {
            Guard.ArgumentNotNullOrWhiteSpace(identifier, nameof(identifier));
            cache.Delete("*", $"*{identifier}*", false);

            cache.DeleteRelatedCacheEntries<TService, TEntityType>();
        }


        public static void DeleteRelatedCacheEntries<TService, TEntityType>(this ICacheService cache) where TEntityType : EntityBase
        {
            // delete cache for current class
            cache.Delete(nameof(TService), "*");

            // delete taxonomy and entity (entity service) cache entries
            string entityType = typeof(TEntityType).GetAttributeValue((TypeAttribute type) => type.Type);
            cache.Delete("*", $"*{entityType}*", false);
        }
    }
}
