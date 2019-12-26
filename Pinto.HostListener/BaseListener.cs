using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Collections.Generic;

namespace Pinto.HostListener
{
    public class BaseListener : IHostedService//.NET Core 2.0 引入了 IHostedService ，基于它可以很方便地执行后台任务，.NET Core 2.1 则锦上添花地提供了 IHostedService 的默认实现基类 BackgroundService
    {
        private readonly IConnection connection;
        private readonly IModel channel;
        private readonly ILogger _logger;

        public BaseListener(IOptions<RabbitConfig> options, ILogger logger)
        {
            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = options.Value.RabbitConnection.HostName,
                    UserName = options.Value.RabbitConnection.UserName,
                    Password = options.Value.RabbitConnection.Password,
                    Port = options.Value.RabbitConnection.Port,
                };
                this.connection = factory.CreateConnection();
                this.channel = connection.CreateModel();
                this._logger = logger;
                _retryTime = new List<int>
                {
                    1 * 1000,
                    10 * 1000,
                    30 * 1000
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"BaseListener init error,ex:{ex.Message}");
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Register();
            return Task.CompletedTask;
        }

        protected string RoutingKey;
        protected string QueueName;
        protected string Exchange;
        protected string ExchangeType;
        public virtual bool Process(string message)
        {
            throw new NotImplementedException();
        }

        public void Register()
        {
            Console.WriteLine($"BaseListener register,RoutingKey:{RoutingKey}");
            

            //设置死信交换机
            var retryDic = new Dictionary<string, object>
            {
                {"x-dead-letter-exchange", Exchange+"_Retry"},
                {"x-dead-letter-routing-key", RoutingKey+"_Retry"}
            };
            channel.ExchangeDeclare(exchange: Exchange, type: ExchangeType);
            channel.QueueDeclare(queue: QueueName, exclusive: false);
            channel.QueueBind(queue: QueueName, exchange: Exchange, routingKey: RoutingKey,retryDic);
            //channel.ExchangeDeclare(Exchange+"_Retry", "direct");
            //channel.QueueDeclare(QueueName+"_Retry", true, false, false, retryDic);
            //channel.QueueBind(QueueName + "_Retry", Exchange+"_Retry", RoutingKey+"_Retry");
            channel.BasicQos(0, 1, false);
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
              {
                  bool canAck;
                  var retryCount = 0;
                  if (ea.BasicProperties.Headers != null && ea.BasicProperties.Headers.ContainsKey("retryCount"))
                  {
                      retryCount = (int)ea.BasicProperties.Headers["retryCount"];
                      _logger.LogWarning($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]Message:{ea.BasicProperties.MessageId}, {++retryCount} retry started...");
                  }
                  var body = ea.Body;
                  var message = Encoding.UTF8.GetString(body);
                  //var result = Process(message);

                  try
                  {
                      Process(message);
                      canAck = true;
                  }
                  catch (Exception ex)
                  {
                      _logger.LogCritical(ex, "Error!");
                      if (CanRetry(retryCount))
                      {
                          SetupRetry(retryCount, "Exchange_Retry", "RouteB_Retry", ea);
                          canAck = true;
                      }
                      else
                      {
                          canAck = false;
                      }
                  }
                  try
                  {
                      if (canAck)
                      {
                          channel.BasicAck(ea.DeliveryTag, false);
                      }
                      else
                      {
                          channel.BasicNack(ea.DeliveryTag, false, false);
                      }
                  }
                  catch (AlreadyClosedException ex)
                  {
                      _logger.LogCritical(ex, "RabbitMQ is closed！");//设置了RabbitMQ的断线恢复机制，当RabbitMQ连接不可用时，与MQ通讯的操作会抛出AlreadyClosedException的异常，导致主线程退出，哪怕连接恢复了，程序也无法恢复，因此，需要捕获处理该异常。
                  }


              };
            channel.BasicConsume(queue: QueueName, consumer: consumer);
        }

        private void SetupRetry(int retryCount, string retryExchange, string retryRoute, BasicDeliverEventArgs ea)
        {
            var body = ea.Body;
            var properties = ea.BasicProperties;
            properties.Headers = properties.Headers ?? new Dictionary<string, object>();
            properties.Headers["retryCount"] = retryCount;
            properties.Expiration = _retryTime[retryCount].ToString();
            try
            {
                channel.BasicPublish(retryExchange, retryRoute, properties, body);
            }
            catch (AlreadyClosedException ex)
            {
                _logger.LogCritical(ex, "RabbitMQ is closed!");
            }
        }

        private List<int> _retryTime = null;


        private bool CanRetry(int retryCount)
        {
            return retryCount <= _retryTime.Count - 1;
        }

        public void DeRegister()
        {
            this.connection.Close();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this.connection.Close();
            return Task.CompletedTask;
        }
    }
}
