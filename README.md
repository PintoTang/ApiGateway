# 1. ApiGateway
 使用开源的Ocelot中间件实现的一款网关。
 Ocelot具有身份验证，监控，负载均衡，缓存，请求分片与管理，静态响应处理等功能。
 API网关方式的核心要点是，所有的客户端和消费端都通过统一的网关接入微服务，在网关层处理所有的非业务功能。
 ApiGateway项目下的configuration.json有比较完整的Ocelot配置，有详细的注释说明。

 1.1 Startup.cs
 public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(Microsoft.AspNetCore.Mvc.CompatibilityVersion.Version_2_1);
            //往容器添加认证服务，添加了两个Bearer认证方案
            services.AddAuthentication()
                .AddJwtBearer("Api_A", i =>
                {
                    i.Audience = "Api_A";
                    //IdentityServer4服务端地址 
                    i.Authority = "http://localhost:5003";
                    i.RequireHttpsMetadata = false;
                }).AddJwtBearer("Api_B", y =>
                {
                    y.Audience = "Api_B";
                    y.Authority = "http://localhost:5003";
                    y.RequireHttpsMetadata = false;
                });
            //往容器添加Ocelot服务，使用configuration.json里面的配置信息
            services.AddOcelot(new ConfigurationBuilder().AddJsonFile("configuration.json").Build());
        }
        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IHostingEnvironment env)
        {           
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseOcelot();
            app.UseAuthentication();
        }
    }
    
 1.2 configuration.json
    {
  "GlobalConfiguration": {
    //"BaseUrl": "http://127.0.0.1:9099", //对外暴露的网关地址
    "RateLimitOptions": {
      "DisableRateLimitHeaders": false,
      "QuotaExceededMessage": "接口限流!",
      "HttpStatusCode": 200,
      "ClientIdHeader": "ClientId"
    }
  },
  "ReRoutes": [
    {
      "UpstreamPathTemplate": "/Api_A/{controller}/{action}", //上游请求地址模板
      "UpstreamHttpMethod": [ //上游请求方式
        "Get"
      ],
      "DownstreamPathTemplate": "/api/{controller}/{action}", //下游跳转地址模板
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [ //如果使用LoadBalancer的话这里可以填多个节点
        {
          "Host": "localhost",
          "Port": 5001
        }
      ],
      "RequestIdKey": "",
      //Ocelot可以对下游请求结果进行缓存 ，目前缓存的功能还不是很强大。它主要是依赖于CacheManager 来实现的
      "FileCacheOptions": {
        "TtlSeconds": 0,
        "Region": "" //Region是对缓存进行的一个分区，我们可以调用Ocelot的 administration API来移除某个区下面的缓存
      },
      "ReRouteIsCaseSensitive": false,
      "ServiceName": "",
      //服务质量与熔断
      "QoSOptions": {
        "ExceptionsAllowedBeforeBreaking": 0, //允许多少个异常请求
        "DurationOfBreak": 0, // 熔断的时间，单位为秒
        "TimeoutValue": 0 //如果下游请求的处理时间超过多少秒则自如将请求设置为超时
      },
      // 将决定负载均衡的算法：LeastConnection–将请求发往最空闲的那个服务器；RoundRobin–轮流发送；NoLoadBalance–总是发往第一个请求或者是服务发现
      "LoadBalancer": "LeastConnection",
      //对请求进行限流可以防止下游服务器因为访问过载而崩溃
      "RateLimitOptions": {
        "ClientWhitelist": [], //白名单
        "EnableRateLimiting": false, //是否启用限流
        "Period": "5m", //1s, 5m, 1h, 1d
        "PeriodTimespan": 0, //多少秒之后客户端可以重试
        "Limit": 0, //在统计时间段内允许的最大请求数量
      },
      //鉴权认证
      "AuthenticationOptions": {
        "AuthenticationProviderKey": "Api_A",
        "AllowedScopes": [] //这里的Scopes将从当前 token 中的 claims中来获取，我们的鉴权服务将依靠于它来实现 。当前路由的下游API需要某个权限时，我们需要在这里声明 。和oAuth2中的scope意义一致。
      },
      ////我们通过认证中的AllowedScopes 拿到claims之后，如果要进行权限的鉴别需要添加以下配置
      //"RouteClaimsRequirement": {
      //  "UserType": "registered" //当前请求上下文的token中所带的claims如果没有 name=”UserType” 并且 value=”registered” 的话将无法访问下游服务。
      //},
      "HttpHandlerOptions": {
        "AllowAutoRedirect": true,
        "UseCookieContainer": true,
        "UseTracing": true
      },
      "UseServiceDiscovery": false,

      //在请求头转化这里Ocelot为我们提供了两个变量：BaseUrl和DownstreamBaseUrl。
      //BaseUrl就是我们在GlobalConfiguration里面配置的BaseUrl，
      //DownstreamBaseUrl是下游服务的Url
      //请求头转化;比如我们将客户端传过来的Header中的Location值value1改为BaseUrl后传给下游
      "UpstreamHeaderTransform": {
        "Location": "value1, {BaseUrl}"
      },
      //我们同样可以将下游Header中的Location再转为DownstreamBaseUrl之后再转给客户端。
      "DownstreamHeaderTransform": {
        "Location": "{DownstreamBaseUrl}, {BaseUrl}"
      }
    },
    {
      "UpstreamPathTemplate": "/Api_B/{controller}/{action}", //上游请求地址模板
      "UpstreamHttpMethod": [ //上游请求方式
        "Get"
      ],
      "DownstreamPathTemplate": "/api/{controller}/{action}", //下游跳转地址模板
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [ //如果使用LoadBalancer的话这里可以填多个节点
        {
          "Host": "localhost",
          "Port": 5002
        }
      ], //鉴权认证
      "AuthenticationOptions": {
        "AuthenticationProviderKey": "Api_B",
        "AllowedScopes": [] //这里的Scopes将从当前 token 中的 claims中来获取，我们的鉴权服务将依靠于它来实现 。当前路由的下游API需要某个权限时，我们需要在这里声明 。和oAuth2中的scope意义一致。
      }
    }
  ]
}


# 2. ConsulCore
  简单地封装了服务的注册与注销功能（以后还得继续完善）。
  2.1 ConsulBuilderExtensions.cs
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
    
 # 3. Api_A
  主要是演示如何使用认证和服务注册
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvcCore().AddAuthorization().AddJsonFormatters()
                .SetCompatibilityVersion(Microsoft.AspNetCore.Mvc.CompatibilityVersion.Version_2_1);
            services.AddAuthentication("Bearer")
                 .AddJwtBearer("Bearer", options =>
                 {
                     //IdentityServer4 地址
                     options.Authority = "http://localhost:5003";
                     options.RequireHttpsMetadata = false;
                     options.Audience = "Api_A";
                 });

        }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        IConfiguration Configuration { get; }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime lifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseAuthentication();
            app.UseMvc(routes => {
                routes.MapRoute("areaRoute", "view/{area:exists}/{controller}/{action=Index}/{id?}");
                routes.MapRoute("default", "{controller=Home}/{action=Index}/{id?}");
                routes.MapSpaFallbackRoute("spa-fallback", new { controller = "Home", action = "Index" });
            });
            ConsulService consulService = new ConsulService()
            {
                IP = Configuration["Consul:IP"],
                Port = Convert.ToInt32(Configuration["Consul:Port"])
            };
            HealthService healthService = new HealthService()
            {
                IP = Configuration["Service:IP"],
                Port = Convert.ToInt32(Configuration["Service:Port"]),
                Name = Configuration["Service:Name"],
            };
            app.RegisterConsul(lifetime, healthService, consulService);
        }
    }
