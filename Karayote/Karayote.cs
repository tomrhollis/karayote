using Botifex;
using KarafunAPI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

internal class Karayote : IHostedService
{
    private ILogger<Karayote> log;
    private IConfiguration cfg;
    private IBotifex botifex;
    private IKarafun karafun;

    public Karayote(ILogger<Karayote> log, IConfiguration cfg, IKarafun karApi, Botifex.IBotifex botifex)
    {
        this.log = log;
        this.cfg = cfg;
        this.botifex = botifex;
        karafun = karApi;

        botifex.RegisterTextHandler(ProcessText);
        botifex.RegisterCommandHandler(ProcessCommand);        
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        log.LogDebug("StartAsync has been called.");
        karafun.OnStatusUpdated += HandleStatusUpdate;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        log.LogDebug("StopAsync has been called.");
    }

    private async void ProcessCommand(object? sender, CommandReceivedEventArgs e)
    {
        string optionsText = "no options";
        if (!String.IsNullOrEmpty(e.Options)) optionsText = "options " + e.Options;
        log.LogDebug($"Tester got {e.Command.Name} with {optionsText} from {sender.GetType()}");
    }

    private async void ProcessText(object? sender, MessageReceivedEventArgs e)
    {
        log.LogDebug($"Tester got {e.Message} from {sender.GetType()}");
    }

    private async void HandleStatusUpdate(object? sender, StatusUpdateEventArgs e)
    {
        log.LogDebug($"Karafun status update: \n{e.Status}");
    }

}