using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Windows;
using KarafunAPI;
using Karayote.Views;
using Botifex;
using Microsoft.Extensions.DependencyInjection;
using Karayote.Models;
using Karayote.ViewModels;
using System;
using Karayote.Database;
using Microsoft.EntityFrameworkCore;

namespace Karayote
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        IHost host;
        public App()
        {
            host = Host.CreateDefaultBuilder()
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
                    services.AddBotifexClasses()                    // messaging bots
                            .AddSingleton<KarayoteBot>()            // the brains of the operation
                            .AddHostedService(p => p.GetRequiredService<KarayoteBot>()) // run it constantly
                            .AddSingleton<IKarafun, Karafun>()      // karafun API
                            .AddSingleton<SongQueue>()              // karayote queue
                            .AddSingleton<Session>()                // karayote session info
                            .AddSingleton<MainWindowViewModel>()    // WPF view model
                            .AddSingleton<MainWindow>()            // WPF view                                                    
                            .AddDbContext<KYContext>(options => options.UseSqlServer(ctx.Configuration.GetConnectionString("KYContext")))
                            .AddScoped(typeof(IRepository<>), typeof(Repository<>));
                })
                //.UseConsoleLifetime()
                .Build();
        }
        
        public async void Application_Startup(object sender, StartupEventArgs e)
        {
            await host.StartAsync();
            var mainWindow = host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        public async void Application_Exit(object sender, EventArgs e)
        {
            await host.StopAsync(TimeSpan.FromSeconds(10));
            host.WaitForShutdown();
        }
    }
}
