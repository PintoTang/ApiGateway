using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Consul;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Api_B
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        /// <summary>
        /// 业务实现
        /// </summary>
        private readonly IConfiguration _configuration;
        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="authService"></param>
        public ValuesController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("Discovery")]
        public async Task<string> Discovery()
        {
            var result = "";
            var url = _configuration["Consul:ConsulAddress"].ToString();
            using (var consulClient = new ConsulClient(a => a.Address = new Uri(url)))
            {
                //在全部的Consul服务中寻找ConsulApi_A服务
                var services = consulClient.Catalog.Service("ConsulApi_A").Result.Response;
                if (services != null && services.Any())
                {
                    //客户端负载均衡，随机选出一台服务，可以选择合适的负载均衡工具或框架
                    Random r = new Random();
                    int index = r.Next(services.Count());
                    var service = services.ElementAt(index);
                    using (HttpClient client = new HttpClient())
                    {
                        var response = await client.GetAsync($"http://{service.ServiceAddress}:{service.ServicePort}/api/values/print");
                        result = await response.Content.ReadAsStringAsync();
                    }
                }
            }
            return result;
        }

        [HttpGet("print")]
        public string Print()
        {
            return "BBB";
        }
    }
}
