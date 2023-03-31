// See https://aka.ms/new-console-template for more information


using Botifex;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

await Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((ctx, cfg) =>
    {
        cfg.SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("botsettings.json", optional: true, reloadOnChange: true)
            .Build();
    })
    .ConfigureLogging((ctx, logging) =>
    {
        logging.ClearProviders().AddConsole();
    })
    .ConfigureServices((ctx, services) =>
    {
        services.AddMyClasses()
                .AddHostedService<Karayote>();

             // .AddDbContext<KYContext>(options => options.UseSqlServer(ctx.Configuration.GetConnectionString("KYContext")));
             // .AddScoped(typeof(IRepository<>), typeof(Repository<>));
    })
    .UseConsoleLifetime()
    .RunConsoleAsync();
