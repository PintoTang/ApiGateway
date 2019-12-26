using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.AspNetCore.Authorization;

namespace ApiGateway
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(Microsoft.AspNetCore.Mvc.CompatibilityVersion.Version_2_1);
            services.AddAuthentication()
                .AddJwtBearer("Api_A", i =>
                {
                    i.Audience = "Api_A";
                    i.Authority = "http://localhost:5003";
                    i.RequireHttpsMetadata = false;
                }).AddJwtBearer("Api_B", y =>
                {
                    y.Audience = "Api_B";
                    y.Authority = "http://localhost:5003";
                    y.RequireHttpsMetadata = false;
                });
            services.AddOcelot(new ConfigurationBuilder().AddJsonFile("configuration.json").Build());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
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
}
