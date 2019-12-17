using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace ConsulCore
{
    public static class ConsulBuilderExtensions
    {
        public static IApplicationBuilder RegisterConsul(this IApplicationBuilder app, IApplicationLifetime lifetime, HealthService healthService, ConsulService consulService)
        {
            var consulClient = new ConsulClient(x => x.Address = new Uri($"http://{consulService.IP}:{consulService.Port}"));
            var httpCheck = new AgentServiceCheck()
            {
                DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(5),//服务启动多久后注册
                Interval = TimeSpan.FromSeconds(10),//健康检查时间间隔，或者称为心跳间隔
                HTTP = $"http://{healthService.IP}:{healthService.Port}/api/Health",
                Timeout = TimeSpan.FromSeconds(5)
            };
            var registration = new AgentServiceRegistration()
            {
                Checks = new[] { httpCheck },
                ID = healthService.Name + "_" + healthService.Port,
                Name = healthService.Name,
                Address = healthService.IP,
                Port = healthService.Port,
                Tags = new[] { $"urlprefix-/{healthService.Name}" }
            };
            consulClient.Agent.ServiceRegister(registration).Wait();//服务启动时注册，内部实现其实就是使用 Consul API 进行注册（HttpClient发起）
            lifetime.ApplicationStopping.Register(() =>
            {
                consulClient.Agent.ServiceDeregister(registration.ID).Wait();//服务停止时注销
            });
            return app;
        }
    }
}
