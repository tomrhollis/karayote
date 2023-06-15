using KarafunAPI;
using Karayote;
using Botifex;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.WebHost.ConfigureAppConfiguration((ctx, cfg) =>
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
        services.AddBotifexClasses() // messaging bots
                .AddSingleton<IKarafun, Karafun>() // karafun API
                .AddHostedService<KarayoteBot>(); // the brains of the operation

        // .AddDbContext<KYContext>(options => options.UseSqlServer(ctx.Configuration.GetConnectionString("KYContext")));
        // .AddScoped(typeof(IRepository<>), typeof(Repository<>));
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

await app.RunAsync();