using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IdentityServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json", true, true).Build();
            WebHost.CreateDefaultBuilder(args)
            //.UseKestrel()
            .UseStartup<Startup>()
            .UseUrls(config.GetValue<string>("urls"))
            .Build()
            .Run();
        }
    }
}
