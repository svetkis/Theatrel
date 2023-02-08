using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using theatrel.Lib;

namespace theatrel.Worker;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddTheatrelLib();
                services.AddHostedService<Worker>();
            });
}