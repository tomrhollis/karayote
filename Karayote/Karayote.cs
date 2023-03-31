using Botifex;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

internal class Karayote : IHostedService
{
    private ILogger<Karayote> log;
    private IConfiguration cfg;
    private IHostApplicationLifetime appLifetime;
    private IBotifex botifex;

    public Karayote(ILogger<Karayote> log, IConfiguration cfg, IHostApplicationLifetime appLifetime, /*IKarafun karApi,*/ Botifex.IBotifex botifex)
    {
        this.log = log;
        this.cfg = cfg;
        this.appLifetime = appLifetime;
        this.botifex = botifex;

        appLifetime.ApplicationStarted.Register(OnStarted);
        appLifetime.ApplicationStopping.Register(OnStopping);
        appLifetime.ApplicationStopped.Register(OnStopped);

        botifex.RegisterTextHandler(ProcessText);
        botifex.RegisterCommandHandler(ProcessCommand);
        
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        log.LogInformation("StartAsync has been called.");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        log.LogInformation("StopAsync has been called.");
    }

    private void OnStarted()
    {
        log.LogInformation("OnStarted has been called.");
    }

    private void OnStopping()
    {
        log.LogInformation("OnStopping has been called.");
    }

    private void OnStopped()
    {
        log.LogDebug("OnStopped has been called.");
    }

    private async void ProcessCommand(object? sender, CommandReceivedEventArgs e)
    {
        string optionsText = "no options";
        if (!String.IsNullOrEmpty(e.Options)) optionsText = "options " + e.Options;
        log.LogDebug($"Tester got {e.Command} with {optionsText} from {sender.GetType()}");
    }

    private async void ProcessText(object? sender, MessageReceivedEventArgs e)
    {
        log.LogDebug($"Tester got {e.Message} from {sender.GetType()}");
    }

}