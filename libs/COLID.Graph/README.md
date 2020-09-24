# COLID TripleStore Library

COLID library to access the Triple Store with transactions.

## Preparation

In order to use this class libary in a .NET Core project, add the following parts to the project.

### AppSettings

Add the following part to the appsettings JSON files for each environment separately:

```js
 "ColidTripleStoreOptions": {
    "ReadUrl": "http://localhost:3030/colid-dataset/query",
    "UpdateUrl": "http://localhost:3030/colid-dataset/update",
    "LoaderUrl": "",
    "Username": "fuseki-user",
    "Password": "super-secret-password",
  }
```
