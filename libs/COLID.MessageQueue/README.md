# COLID Message Queue library

COLID.MessageQueue module uses RabbitMQ message queue for messaging.
It depends on configuration from appsettings.json as shown below.

## Preparation

In order to use this class libary in a .NET Core project, add the following parts to the project. Since this Message Queue package has a dependency on the external package [CorrelationId](https://github.com/stevejgordon/CorrelationId), the initialization of package CorrelationId must be done first.

In Setup.cs
```
public void ConfigureServices(IServiceCollection services)
{
    services.AddDefaultCorrelationId();
    services.AddMessageQueueModule(Configuration);
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseCorrelationId();
    app.UseMessageQueueModule(Configuration);
}
```

### AppSettings

```json
// Topics is a dynamic list which can be extended based on requirement.

"ColidMessageQueueOptions": {
    "Enabled": true,
    "HostName": "rabbitmq.cluster.local",
    "Username": "&lt;injected via env variables / user secrets&gt;",
    "Password": "&lt;injected via env variables / user secrets&gt;",
    "ExchangeName": "daaa.dev.events",
    "Topics": {
      "TopicResourcePublished": "daaa.dev.resources.published",
      "TopicResourceDeleted": "daaa.dev.resources.deleted"
    }
  }
```
