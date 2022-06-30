using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using COLID.MessageQueue.Configuration;
using COLID.MessageQueue.Datamodel;
using CorrelationId;
using CorrelationId.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace COLID.MessageQueue.Services
{
    internal class MessageQueueService : IMessageQueueService
    {
        private readonly ICorrelationContextAccessor _correlationContext;
        private readonly IConnectionFactory _connectionFactory;
        private readonly IConnection _connection;
        private readonly ILogger _logger;
        private readonly IModel _channel;
        private readonly string _exchangeName;
        private readonly int _maxRetries = 3;

        private readonly IDictionary<string, Action<string>> _registeredTopics;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        public MessageQueueService(
            ICorrelationContextAccessor correlationContext,
            IOptionsMonitor<ColidMessageQueueOptions> messageQueueOptionsAccessor,
            IServiceProvider provider,
            ILogger<MessageQueueService> logger)
        {
            _correlationContext = correlationContext;
            _logger = logger;

            var options = messageQueueOptionsAccessor.CurrentValue;
            ConnectionFactory _connectionFactory;

            if (options.UseSsl)
            {
                X509Store store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadOnly);
                // default recovery time every 5 seconds
                _connectionFactory = new ConnectionFactory()
                {
                    HostName = options.HostName,
                    UserName = options.Username,
                    Password = options.Password,
                    Port = 5671,
                    Ssl = new SslOption()
                    {
                        ServerName = options.HostName,
                        Enabled = true,
                        Certs = store.Certificates
                    }
                };
            } 
            else
            {
                _connectionFactory = new ConnectionFactory()
                {
                    HostName = options.HostName,
                    UserName = options.Username,
                    Password = options.Password
                };
            }

            
            _exchangeName = options.ExchangeName;

            int retryCount = 0;
            do
            {
                try
                {
                    _connection = _connectionFactory.CreateConnection();
                }
                catch (BrokerUnreachableException exception)
                {
                    var delay = 5000 * (retryCount + 1);
                    Thread.Sleep(delay);

                    if (retryCount == _maxRetries - 1)
                    {
                        _logger.LogError("No connection to the message queue could be established.", exception);
                        throw;
                    }
                }
                retryCount++;
            } while (retryCount < _maxRetries);

            _channel = _connection.CreateModel();

            _registeredTopics = new Dictionary<string, Action<string>>();

            foreach (var mqPublisher in provider.GetServices<IMessageQueuePublisher>())
            {
                mqPublisher.PublishMessage = PublishMessage;
            }

            foreach (var mqReceiver in provider.GetServices<IMessageQueueReceiver>().ToList())
            {
                try
                {
                    foreach (var receiver in mqReceiver.OnTopicReceivers)
                    {
                        _registeredTopics.Add(receiver.Key, receiver.Value);
                        _logger.LogDebug($"Registered topic {receiver.Key} with method {receiver.Value.Method.Name}");
                    }
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    var message = "Something went wrong while registering the receiver.";
                    _logger.LogError(ex, message);
                }
            }
        }

        public void PublishMessage(string topic, string message, BasicProperty basicProperty = null)
        {
            if (!string.IsNullOrEmpty(message))
            {
                _logger.LogDebug("Publish Message" + topic);

                _logger.LogDebug("[Reindexing] Exchange declare" + topic);
                _channel.ExchangeDeclare(
                    exchange: _exchangeName,
                    type: "topic",
                    durable: false,
                    autoDelete: false,
                    arguments: null);

                var body = Encoding.UTF8.GetBytes(message);
                var basicProperties = CreateBasicProperties(basicProperty);
                _logger.LogDebug($"[Reindexing] Publish > CorrelationId = {basicProperties.CorrelationId}");

                lock (_channel)
                {
                    _channel.BasicPublish(
                    exchange: _exchangeName,
                    routingKey: topic,
                    basicProperties: basicProperties,
                    body: body);
                }


                _logger.LogDebug($"Published message to MQ topic {topic}");
            }
            
        }

        private IBasicProperties CreateBasicProperties(BasicProperty basicProperty)
        {
            var props = _channel.CreateBasicProperties();
            props.Priority = basicProperty?.Priority ?? 0;
            props.CorrelationId = _correlationContext.CorrelationContext.CorrelationId;

            return props;
        }

        public void Register()
        {
            _channel.ExchangeDeclare(exchange: _exchangeName,
                                    type: "topic",
                                    durable: false,
                                    autoDelete: false,
                                    arguments: null
            );

            var queueName = _channel.QueueDeclare().QueueName;

            foreach (var topic in _registeredTopics)
            {
                _channel.QueueBind(queue: queueName,
                                   exchange: _exchangeName,
                                   routingKey: topic.Key);
            }

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += (model, ea) =>
            {
                var routingKey = ea.RoutingKey;
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                var correlationContext = new CorrelationContext(ea.BasicProperties.CorrelationId, CorrelationIdOptions.DefaultHeader);
                _correlationContext.CorrelationContext = correlationContext;
                _logger.LogDebug($"CorrelationId(Received)={_correlationContext.CorrelationContext.CorrelationId}");

                _logger.LogDebug($"Received message on MQ topic {routingKey}");

                // Call the registered method
                if (_registeredTopics.TryGetValue(routingKey, out var registeredMethod))
                {
                    registeredMethod(message);
                }
            };

            _channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
        }

        public void Unregister()
        {
            _connection.Close();
        }

    }
}
