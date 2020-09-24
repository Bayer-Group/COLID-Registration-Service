# COLID Message Queue library

COLID.MessageQueue module uses RabbitMQ message queue for messaging.
It depends on configuration from appsettings.json as shown below.

## Preparation

In order to use this class libary in a .NET Core project, add the following parts to the project.

### AppSettings

```js
// Topics is a dynamic list which can be extended based on requirement.

"ColidMessageQueueOptions": {
    "Enabled": true,
    "HostName": "rabbitmq.cluster.local",
    "Username": "user",
    "Password": "&lt;injected via env variables / user secrets&gt;",
    "ExchangeName": "daaa.dev.events",
    "Topics": {
      "TopicResourcePublished": "daaa.dev.resources.published",
      "TopicResourceDeleted": "daaa.dev.resources.deleted"
    }
  }
```
