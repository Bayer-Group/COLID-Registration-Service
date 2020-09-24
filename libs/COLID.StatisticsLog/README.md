# COLID Logging library

This is a class library for the different COLID projects to support logging with ElasticSearch.
The library is based on Serilog and its ElasticSearch sink, but adds additional features such as automatic performance tracking on API calls, general exception handling, and a structured output for easy access of logging information in Kibana.

## Features

**LogLevels:** It supports six standard loglevel i.e. {Verbose, Debug, Information, Warning, Error, Fatal}.
**LogType:** It supports three levels i.e. {General, Performance, AuditTrail}

- ​AuditTrail: This type of log contains personal information so shall be used only for critical operations. This will log user's full name, email address and user id in plain text format.
- ​Performance: This is used by performance related logging, it logs all the http operation with duration.
- ​General: This is used for all other case of log.

## Configuration

Within this library several configurations can be done.

### Elasticsearch
Elasticsearch specific configurations can be done on in the `LogServiceBase.cs`.
- **Index Naming**  All indices of the different LogTypes will follow the naming convention `{DefaultIndex}-{LogType}-{yyyy.MM} `. Which results into a name like `dev-pid-log-general-2020.08`.
- **Number of shards** As standard for all log indices the value of the index setting `Number_of_Shards` is set to one. The reason for this default value is to minimize the number of all shards in elasticsearch cluster.


## Preparation

In order to use this class libary in a .NET Core project, add the following parts to the project.

### deployment.yaml (Kubernetes)

Add the IAM role of elastic search to the deployment.yaml file of Kubernetes configuration. This needs to be added to spec > template > metadata > annotations:

```yaml
      annotations:
        iam.amazonaws.com/role: "es_dmp-prod_writer20190221175900005400000003"
```

### AppSettings

Add the following part to the appsettings JSON files for each environment separately:

```js
 "ColidStatisticsLogOptions": {
    "Enabled": true,
    "BaseUri": "http://elasticseacrhurl",
    "DefaultIndex": "elasticsearch indexname",
    "AwsRegion": "AWS region name e.g. us-east-1",
    "ProductName": "e.g. COLID",
    "LayerName": "e.g. registration-service",
    "AccessKey": "Optional AWSAccessKey",
    "SecretKey": "Optional AWSSecretKey",
    "AnonymizerKey": "<injected via env variables / user secrets>"
  }
```

### Startup.cs

```C#
using COLID.StatisticsLog;
```

#### Startup.cs > ConfigureServices

```C#
services.AddLoggingModule(Configuration);
```

#### Startup.cs > Configure

```C#

var logService = app.ApplicationServices.GetService<ILogService>();

app.UseExceptionHandler(eApp =>
{
    eApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var errorCtx = context?.Features?.Get<IExceptionHandlerFeature>();
        if (errorCtx != null)
        {
            var ex = errorCtx.Error;
            logService.error(ex, "Exception");

            var errorId = Activity.Current?.Id ?? context.TraceIdentifier;
            var jsonResponse = JsonConvert.SerializeObject(new CustomErrorResponse
            {
                ErrorId = errorId,
                Message = "Error in PID API."
            });
            await context.Response.WriteAsync(jsonResponse, Encoding.UTF8);
        }
    });
});

```

## Usage

Use the logger via Dependency Injection by adding `ILogService logService` to the constructor of a class.
Afterwards, the logger can be used as the following examples.

Logging with additional information.

```C#
logger.info("New PID entry has been published", new Dictionary<string, dynamic> {
    { "subject", parentResource.Subject },
    { Constants.Metadata.PidUri, resourcePublish.GetPropertyValue(Constants.Metadata.PidUri)},
    { Constants.Metadata.DateCreated, resourcePublish.GetPropertyValue(Constants.Metadata.DateCreated)},
    { Constants.Metadata.DateModified, resourcePublish.GetPropertyValue(Constants.Metadata.DateModified)}
});
```

Logging expcetions.

```C#
_logger.error(ex, $"Failed: {ex.Message}");
```

Audit trail 
Using service

```C#
var messge = $"Pid with subject {subject} deleted.";
_logService.AuditTrail(messge);
```

Using Attribute

```C#
  [Log(LogType.AuditTrail)]
  public IActionResult DeleteConsumerGroup([FromQuery] string subject)

  // One can add additional claims
  [Log(LogType.AuditTrail, new ClaimMetaData("http://contact", "Contact"), new ClaimMetaData("http://additional1", "Additional 1"))]
  public IActionResult DeleteConsumerGroup([FromQuery] string subject)

  [Log(LogType.Performance, new ClaimMetadata("http://apikey","ExternalAPI"))]
  public IActionResult GetConsumerGroups([FromQuery] bool filterByUser = false)
```
There is a conveince class viz. KnownClaims which facilitates Email, UserId, and FullName. This is also used internally by AuditTrail.