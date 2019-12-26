using ConsulCore;
using IdentityServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Api_A
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
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

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
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
}
