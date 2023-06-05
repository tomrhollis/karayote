using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Windows;
using KarafunAPI;
using Karayote.Views;
using Botifex;
using Microsoft.Extensions.DependencyInjection;
using Karayote.Models;

namespace Karayote
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IHost host;

        public App()
        {
            host = new HostBuilder()
                .ConfigureAppConfiguration((ctx, cfg) =>
                {
                    cfg.SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true) // non-sensitive settings
                        .AddJsonFile("botsettings.json", optional: true, reloadOnChange: true) // sensitive settings such as API keys
                        .Build();
                })
                .ConfigureLogging((ctx, logging) =>
                {
                    logging.ClearProviders().AddConsole();
                })
                .ConfigureServices((ctx, services) =>
                {
                    services.AddMyClasses()                             // messaging bots
                            .AddSingleton<IKarafun, Karafun>()          // karafun API
                            .AddSingleton<IKarayoteBot, KarayoteBot>()  // the controller
                            .AddSingleton<MainWindow>();                // WPF window

                            // .AddDbContext<KYContext>(options => options.UseSqlServer(ctx.Configuration.GetConnectionString("KYContext")));
                            // .AddScoped(typeof(IRepository<>), typeof(Repository<>));
                })
                .Build();
        }

        public async void Application_Startup(object sender, StartupEventArgs e)
        {
            await host.StartAsync();
            var mainWindow = host.Services.GetService<MainWindow>();
            mainWindow.Show();
        }

        public async void Application_Exit(object sender, EventArgs e)
        {
            await host.StopAsync(TimeSpan.FromSeconds(10));
        }
    }
}
