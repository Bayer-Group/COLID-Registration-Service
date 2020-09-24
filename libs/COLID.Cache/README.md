# COLID Distributed Cache library

This is a class library for the different COLID projects to support Caching with Distributed Cache.
The library is based on open source caching named Redis. 

## Features

In addition to the distribution cache, a memory variation is also implemented, so that a local implementation of the cache is also available for development purposes.

## Preparation

In order to use this class libary in a .NET Core project, add the following parts to the project.

### AppSettings

Add the following part to the appsettings JSON files for each environment separately:

```js
 "ColidCacheOptions": {
    "Enabled": true,
    "UseInMemory": true,
    "EndpointUrls": [ "e.g redis-master:6379", "e.g redis-slave:6379" ],
    "Password": "<injected via env variables / user secrets>",
    "AbsoluteExpirationRelativeToNow": e.g 300,
    "SyncTimeout": 5000
  }
```

#### Startup.cs > ConfigureServices

```csharp
var serializerSettings = new JsonSerializerCacheSettings
{
    Converters = new List<JsonConverter>(),
    ContractResolver = new DefaultContractResolver
    {
        NamingStrategy = new CamelCaseNamingStrategy()
    },
    Formatting = Formatting.Indented
};
            
services.AddCacheModule(Configuration, serializerSettings);
```

## Usage

Use the cache via Dependency Injection by adding `ICacheService cacheService` to the constructor of a service class, so the cache can be used with the following methods.

```csharp
public T GetOrAdd<T>(string key, Func<T> addEntry);

public T Update<T>(string key, Func<T> updateEntry);

public void Delete(string key, Action method);
```

### Examples

The following are two general examples of how to read data from the cache within the `GetEntity` service methods and write data within the `UpdateEntity`. If the cache does not contain the data, the repository is queried to retrieve the data from the database. The cache method `GetOrAdd` then automatically sets the entity retrieved from the database by the repository. 

```csharp
private Entity GetEntity(int entityId)
{
    return this.cacheService.GetOrAdd(entityId, () => this.repository.GetOne(entityId));
}
```

The same approach is used to update an entity. After the entity has been updated in the repository, it is automatically set in the cache using the following process.

```csharp
private Entity UpdateEntity(int entityId, entityDto entityDto) 
{
    var dbEntity = GetEntity(entityId);
    var updatedEntity = this.mapper.Map<Entity>(entityDto);
    updatedEntity.Id = dbEntity;

    return this.cacheService.Update(entityId, () => this.repository.Update(updatedEntity));
}
```
