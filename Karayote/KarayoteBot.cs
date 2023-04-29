using Botifex;
using Botifex.Services;
using KarafunAPI;
using KarafunAPI.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Karayote
{
    internal class KarayoteBot : IHostedService
    {
        private ILogger<KarayoteBot> log;
        private IConfiguration cfg;
        private IBotifex botifex;
        private IKarafun karafun;
        internal Session currentSession = new Session();

        public KarayoteBot(ILogger<KarayoteBot> log, IConfiguration cfg, IKarafun karApi, Botifex.IBotifex botifex)
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
            botifex.AddCommand(new SlashCommand(adminOnly: true) 
            {
                Name="opensession",
                Description = "Open the session for searching and queueing"
            });
            if(cfg.GetValue<bool>("AllowGetID"))
                botifex.AddCommand(new SlashCommand()
                {
                    Name="getid",
                    Description = "Show the id digits for this channel"
                });

        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            log.LogDebug("StartAsync has been called.");
            karafun.OnStatusUpdated += KarayoteStatusUpdate;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            currentSession.End();
            log.LogDebug("StopAsync has been called.");
        }

        private async void ProcessCommand(object? sender, InteractionReceivedEventArgs e)
        {
            ICommandInteraction interaction = (ICommandInteraction)e.Interaction;
            log.LogDebug($"[{DateTime.Now}] Karayote got {interaction.BotifexCommand.Name} from {sender?.GetType()}");

            switch (interaction.BotifexCommand.Name)
            {
                case "search":
                    if (currentSession.IsOpen)
                    {
                        await interaction.Reply($"Searching Karafun catalog for {interaction.CommandFields["terms"]}");
                        karafun.Search(new Action<List<Song>>(async (List<Song> foundSongs) =>
                        {
                            Dictionary<string, string> results = new Dictionary<string, string>();
                            for (int i = 0; i < foundSongs.Count; i++)
                            {
                                results.Add($"{foundSongs[i].Id}", $"{foundSongs[i]}");
                            }

                            await ((Interaction)interaction).Reply("Pick a song to add yourself to the queue", results);

                        }), interaction.CommandFields["terms"]);
                    }
                    else
                        await NoSessionReply(interaction);                  
                    break;

                case "queue":
                    if(currentSession.IsOpen)
                    {
                        string status = karafun.Status.ToString();
                        await interaction.Reply("Status report in place of just queue for now:\n" + status);
                    }
                    else
                        await NoSessionReply(interaction);

                    break;

                case "karafunlink":
                    await interaction.Reply("https://www.karafun.com/karaoke -- note that their site contains a few songs not licensed for use in Canada. If you can't find them through /search when the event starts, that's probably why");
                    break;

                case "getid":
                    await interaction.Reply($"Chat ID: {((Interaction)interaction).Source.MessageId}");
                    break;

                case "opensession":
                    if (karafun.Status is not null)
                    {
                        currentSession.Open();
                        await botifex.SendOneTimeStatusUpdate("The session is now open for searching and queueing! DM me @karayotebot to make your selections and get in line.", notification: true);
                        await interaction.Reply("The session is now open for searching and queueing");
                    }
                    else
                        await interaction.Reply("Can't open the session, Karafun isn't speaking to us right now.");                   
                    break;

                default:
                    break;
            }
        }

        private async Task NoSessionReply(ICommandInteraction interaction)
        {
            string nextSession = "Not sure when the next one is, but";
            await interaction.Reply($"There aren't any open sessions yet. {nextSession} hold your horses. Meanwhile you can search the catalog online at https://www.karafun.com/karaoke -- note that their site contains some songs not licensed for use in Canada. If you can't find them through /search when the event starts, that's probably why");
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
}

