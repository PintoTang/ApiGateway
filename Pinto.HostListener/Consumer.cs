using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Pinto.HostListener
{
    public class Consumer:BaseListener
    {
        private readonly ILogger<BaseListener> _logger;
        private readonly IServiceProvider _service;
        public Consumer(IServiceProvider service, IOptions<RabbitConfig> options, ILogger<BaseListener> logger) : base(options,logger)
        {
            base.RoutingKey = options.Value.RabbitExchange.RouteKey;//每个消费者的路由
            base.QueueName = options.Value.RabbitExchange.QueueName;
            base.Exchange = options.Value.RabbitExchange.Exchange;
            base.ExchangeType = options.Value.RabbitExchange.ExchangeType;
            _logger = logger;
            _service = service;
        }

        public override bool Process(string message)
        {
            var taskMessage = JToken.Parse(message);
            if (taskMessage == null)
            {
                return false;
            }
            try
            {
                using (var scope = _service.CreateScope())
                {
                    Console.WriteLine($"--------------I Get the Message:{taskMessage},{DateTime.Now.ToShortTimeString()}---------------");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Process fail,error:{ex.Message},stackTrace:{ex.StackTrace}");
                _logger.LogError(-1, ex, "Process fail");
                return false;
            }
        }
    }
}
