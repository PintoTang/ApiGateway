using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pinto.HostListener
{
    public class Publisher
    {
        private readonly IModel _channel;
        private readonly ILogger _logger;
        private readonly IOptionsSnapshot<RabbitConfig> _options;

        public Publisher(IOptionsSnapshot<RabbitConfig> options, ILogger<Publisher> logger)//使用IOptionsSnapshot支持重新加载配置，不需要重启服务
        {
            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = options.Value.RabbitConnection.HostName,
                    UserName = options.Value.RabbitConnection.UserName,
                    Password = options.Value.RabbitConnection.Password,
                    Port = options.Value.RabbitConnection.Port,
                    AutomaticRecoveryEnabled=true,//断线自动恢复
                };
                var connection = factory.CreateConnection();
                _channel = connection.CreateModel();
                _logger = logger;
                _options = options;
            }
            catch (Exception ex)
            {
                _logger.LogError(-1, ex, "Publisher init fail");
            }
        }

        public virtual void PushMessage(string routingKey, object message)
        {
            _logger.LogInformation($"PushMessage,routingKey:{routingKey}");
            _channel.QueueDeclare(queue: _options.Value.RabbitExchange.QueueName, durable: false, exclusive: false, autoDelete: true, arguments: null);
            _channel.ConfirmSelect();//启动消息发送确认机制
            string msgJson = JsonConvert.SerializeObject(message);
            var body = Encoding.UTF8.GetBytes(msgJson);
            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;//消息持久化
            properties.MessageId = Guid.NewGuid().ToString("N");
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            _channel.BasicPublish(exchange: _options.Value.RabbitExchange.Exchange, routingKey: routingKey, basicProperties: properties, body: body);
            var isOk = _channel.WaitForConfirms();//确认RabbitMQ服务端收到消息
            if (!isOk)
            {
                _logger.LogWarning($"Push {properties.MessageId} Message not reached to the server!");
            }
        }
    }
}
