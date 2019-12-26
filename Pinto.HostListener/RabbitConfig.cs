using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pinto.HostListener
{
    public class RabbitConfig
    {
        public RabbitConnectionOption RabbitConnection { get; set; }
        public RabbitExchangeOption RabbitExchange { get; set; }
       
    }

    public class RabbitConnectionOption
    {
        public string HostName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int Port { get; set; }
    }

    public class RabbitExchangeOption
    {
        public string Exchange { get; set; }
        public string QueueName { get; set; }
        public string ExchangeType { get; set; }
        public string RouteKey { get; set; }
    }
}
