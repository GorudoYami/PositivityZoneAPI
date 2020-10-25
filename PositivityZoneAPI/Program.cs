using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Security.Cryptography.X509Certificates;

namespace PositivityZoneAPI {
    public class Program {
        public static void Main(string[] args) {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => {
                    webBuilder.UseStartup<Startup>()
                    .UseUrls(urls: "https://10.0.0.1:5001")
                    .UseContentRoot("/srv/PositivityZoneAPI")
                    .ConfigureKestrel(serverOptions => {
                        serverOptions.ConfigureHttpsDefaults(configureOptions => {
                            configureOptions.ServerCertificate = new X509Certificate2("cert.pfx", "RemovedForSecurityReasons");
                        });
                    });
                });
    }
}
