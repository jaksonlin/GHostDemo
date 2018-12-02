using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
namespace GHost
{
    class Program
    {
        /// <summary>
        /// #1. Create Host Configuration by calling ConfigureHostConfiguration; Host configuration automatically flows to app configuration (ConfigureAppConfiguration and the rest of the app).
        /// #2. The host configuration can carry the way down to the app/service, and you can access them in hostContext over load of ConfiguareAppConfiguration/ConfigureServices
        /// #3. The app settings can be read by the service via DI of IConfiguration, and further rebuilt into object to easliy access
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static async Task Main(string[] args)
        {
            var hostBaseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var host = new HostBuilder()
                .ConfigureHostConfiguration((IConfigurationBuilder configHost) => {
                    configHost.SetBasePath(hostBaseDir);
                    configHost.AddJsonFile("GHostConfig.json", false, true);//set the HostingEnvironment using JsonFile will read any value provided in json
                    //configHost.AddEnvironmentVariables(prefix: "GHost"); //set the HostingEnvironment using Env will have default to Production which can be dangerous. ANd the name is not good..AddEnvironmentVariables??huh?
                    configHost.AddCommandLine(args);
                })
                .ConfigureAppConfiguration((HostBuilderContext hostContext, IConfigurationBuilder configApp) =>{
                    configApp.SetBasePath(Path.Combine(hostBaseDir, "app1"));
                    configApp.AddJsonFile($@"app1.{hostContext.HostingEnvironment.EnvironmentName}.json".ToLower(), false, true);
                })
                .ConfigureServices((HostBuilderContext hostContext, IServiceCollection configSvc) => {
                    if (hostContext.HostingEnvironment.IsDevelopment())
                    {
                        Console.WriteLine("Development mode");
                    } else
                    {
                        Console.WriteLine("Production mode");
                    }
                    configSvc.AddHostedService<LifetimeEventsHostedService>();
                    configSvc.AddHostedService<TimedHostedService>();
                })
                .ConfigureLogging((HostBuilderContext hostContext, ILoggingBuilder configLogging)=> {
                    configLogging.AddConsole();
                    configLogging.AddDebug();
                })
                .UseConsoleLifetime()
                .Build();
            await host.RunAsync();
        }
    }
    internal class DevSettings
    {
        public int V1 { get; set; }
        public int V2 { get; set; }

        public DevSettings2 V3 { get; set; }
    }

    internal class DevSettings2
    {
        public int G1 { get; set; }
    }
    internal class LifetimeEventsHostedService : IHostedService
    {
        private readonly ILogger _logger;
        private readonly IApplicationLifetime _appLifetime;
        private readonly IConfiguration _configuration;
        private readonly IHostingEnvironment _environment;
        private readonly DevSettings devSettings;
        public LifetimeEventsHostedService(
            IConfiguration configuration,
            IHostingEnvironment environment,
            ILogger<LifetimeEventsHostedService> logger, 
            IApplicationLifetime appLifetime)
        {
            //use either model to access the settings or key configuration["Test1:V1"]
            devSettings = configuration.GetSection("Test1").Get<DevSettings>();
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
            _appLifetime = appLifetime;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _appLifetime.ApplicationStarted.Register(OnStarted);
            _appLifetime.ApplicationStopping.Register(OnStopping);
            _appLifetime.ApplicationStopped.Register(OnStopped);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void OnStarted()
        {
            _logger.LogInformation("OnStarted has been called.");

            // Perform post-startup activities here
        }

        private void OnStopping()
        {
            _logger.LogInformation("OnStopping has been called.");

            // Perform on-stopping activities here
        }

        private void OnStopped()
        {
            _logger.LogInformation("OnStopped has been called.");

            // Perform post-stopped activities here
        }
    }
    internal class TimedHostedService : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private Timer _timer;

        public TimedHostedService(ILogger<TimedHostedService> logger)
        {
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Background Service is starting.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(5));

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            _logger.LogInformation("Timed Background Service is working.");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Background Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
