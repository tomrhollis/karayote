﻿using Botifex;
using Botifex.Services;
using KarafunAPI;
using KarafunAPI.Models;
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

        botifex.AddCommand(new SlashCommand()
        {
            Name = "search",
            Description = "Search the Karafun song catalog",
            Options = new List<CommandField>
            { 
                new CommandField()
                {
                    Name = "terms",
                    Description = "the name and/or artist of the song to search for",
                    Required = true
                }
            }
        });
        botifex.AddCommand(new SlashCommand()
        {
            Name = "queue",
            Description = "See the current song queue"
        });
        botifex.AddCommand(new SlashCommand()
        {
            Name = "karafunlink",
            Description = "Get a link to use the online karafun catalog anytime"
        });
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        log.LogDebug("StartAsync has been called.");
        karafun.OnStatusUpdated += KarayoteStatusUpdate;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        log.LogDebug("StopAsync has been called.");
    }

    private async void ProcessCommand(object? sender, InteractionReceivedEventArgs e)
    {
        ICommandInteraction interaction = (ICommandInteraction)e.Interaction;
        log.LogDebug($"[{DateTime.Now}] Karayote got {interaction.BotifexCommand.Name} from {sender?.GetType()}");

        switch (interaction.BotifexCommand.Name)
        {
            case "search":
                await interaction.Reply($"Searching Karafun catalog for {interaction.CommandFields["terms"]}");
                karafun.Search(new Action<List<Song>>(async (List<Song> foundSongs) =>
                {
                    Dictionary<string,string> results = new Dictionary<string,string>();
                    for(int i=0; i<foundSongs.Count; i++)
                    {
                        results.Add($"{foundSongs[i].Id}", $"{foundSongs[i]}"); 
                    }

                    await ((Interaction)interaction).Reply("Pick a song to add yourself to the queue", results);

                }), interaction.CommandFields["terms"]);
                
                break;

            case "queue":
                string status = karafun.Status.ToString();
                await interaction.Reply("Status report in place of just queue for now:\n" + status);
                break;

            case "karafunlink":
                await interaction.Reply("https://www.karafun.com/karaoke -- note these results may contain songs not licensed for use in Canada. If you can't find them through /search when the event starts, that's probably why");
                break;

            default:
                break;
        }
    }

    private async void ProcessText(object? sender, InteractionReceivedEventArgs e)
    {
        ITextInteraction interaction = (ITextInteraction)e.Interaction;
        log.LogDebug($"[{DateTime.Now}] Karayote got {interaction.Text} from {sender?.GetType()}");
        await interaction.Reply("I see you! Please use a slash command to make a request.");
    }

    private async void KarayoteStatusUpdate(object? sender, StatusUpdateEventArgs e)
    {
        log.LogDebug($"[{DateTime.Now}] karayote status update fired");
        await botifex.SendStatusUpdate(e.Status.ToString());
    }

}