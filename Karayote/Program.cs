///
/// Karayote - Karaoke event management app
/// (c) 2023
///

using Botifex;
using KarafunAPI;
using Karayote;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

await Host.CreateDefaultBuilder(args)
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
        services.AddMyClasses() // messaging bots
                .AddSingleton<IKarafun, Karafun>() // karafun API
                .AddHostedService<KarayoteBot>(); // the controller

             // .AddDbContext<KYContext>(options => options.UseSqlServer(ctx.Configuration.GetConnectionString("KYContext")));
             // .AddScoped(typeof(IRepository<>), typeof(Repository<>));
    })
    .UseConsoleLifetime()
    .RunConsoleAsync();
