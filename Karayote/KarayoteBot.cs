using Botifex;
using Botifex.Services;
using KarafunAPI;
using KarafunAPI.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Karayote.Models;
using Telegram.Bot.Requests;
using Botifex.Models;

namespace Karayote
{
    internal class KarayoteBot : IHostedService
    {
        private ILogger<KarayoteBot> log;
        private IConfiguration cfg;
        private IBotifex botifex;
        private IKarafun karafun;
        internal Session currentSession = new Session();
        private List<KarayoteUser> knownUsers = new List<KarayoteUser>();
        private HashSet<Song> knownSongs = new HashSet<Song>();

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
                Description = "Search the Karafun catalog and pick a song",
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
                Name = "seequeue",
                Description = "See the current song queue"
            });
            botifex.AddCommand(new SlashCommand()
            {
                Name = "karafunlink",
                Description = "Get a link to the online karafun catalog anytime"
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
            KarayoteUser user = CreateOrFindUser(e.Interaction.User!);
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
                            if (foundSongs is null || foundSongs.Count == 0)
                            {
                                await ((Interaction)interaction).Reply("No songs like that found in the Karafun catalog");
                                interaction.End();
                                return;
                            }
                            Dictionary<string, string> results = new Dictionary<string, string>();
                            for (int i = 0; i < foundSongs.Count; i++)
                            {
                                knownSongs.Add(foundSongs[i]);
                                results.Add($"{foundSongs[i].Id}", $"{foundSongs[i]}");
                            }
                            ReplyMenu menu = new ReplyMenu("chosensong", results, ProcessMenuReply);

                            await ((Interaction)interaction).ReplyWithOptions(menu, "Pick a song to add yourself to the queue");

                        }), interaction.CommandFields["terms"]);
                    }
                    else
                    {
                        await NoSessionReply(interaction);
                        interaction.End();
                    }
                                       
                    break;

                case "seequeue":
                    if(currentSession.IsOpen)
                    {
                        await interaction.Reply(currentSession.SongQueue.ToString());
                    }
                    else
                        await NoSessionReply(interaction);
                    interaction.End();

                    break;

                case "karafunlink":
                    await interaction.Reply("https://www.karafun.com/karaoke -- note that their site contains a few songs not licensed for use in Canada. If you can't find them through /search when the event starts, that's probably why");
                    interaction.End();
                    break;

                case "getid":
                    await interaction.Reply($"Chat ID: {((Interaction)interaction).Source.MessageId}");
                    interaction.End();
                    break;

                case "opensession":
                    if(currentSession.IsOpen)
                    {
                        await interaction.Reply("The session is already open silly");
                    }
                    else if (karafun.Status is not null)
                    {
                        currentSession.Open();
                        await botifex.SendOneTimeStatusUpdate("The session is now open for searching and queueing! DM me @karayotebot to make your selections and get in line.", notification: true);
                        await interaction.Reply("The session is now open for searching and queueing");
                        KarayoteStatusUpdate(null, new StatusUpdateEventArgs(karafun.Status));
                    }
                    else if (karafun.Status is null)
                        await interaction.Reply("Can't open the session, Karafun isn't speaking to us right now.");
                    interaction.End();
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
            KarayoteUser user = CreateOrFindUser(e.Interaction.User!);
            ITextInteraction interaction = (ITextInteraction)e.Interaction;
            log.LogDebug($"[{DateTime.Now}] Karayote got {interaction.Text} from {sender?.GetType()}");
            await interaction.Reply("I see you! Please use a slash command to make a request.");
            interaction.End();
        }

        private async void ProcessMenuReply(object? sender, MenuReplyReceivedEventArgs e)
        {
            if (sender is null) throw new ArgumentException();

            ReplyMenu menu = (ReplyMenu)sender!;
            string response = "";

            switch (menu.Name)
            {
                case "chosensong":
                    Song chosenSong = knownSongs.First(s => s.Id == uint.Parse(e.Reply));
                    KarayoteUser user = CreateOrFindUser(e.Interaction.User!);
                    KarafunSong karafunSong = new KarafunSong(chosenSong, user);

                    log.LogDebug($"[{DateTime.Now}] Got request for {karafunSong.Title} from {user.Name} with id {user.Id}");

                    response = (currentSession.GetInLine(karafunSong)) ? $"Added {karafunSong.Title} to the queue at position {currentSession.SongQueue.Count}"
                                                                              : $"Couldn't add {karafunSong.Title}, you already have a song in the queue or someone else picked that";
                    break;
                default: break;
            }

            await e.Interaction.Reply(response);
            e.Interaction.End();
        }

        private async void KarayoteStatusUpdate(object? sender, StatusUpdateEventArgs e)
        {
            log.LogDebug($"[{DateTime.Now}] karayote status update fired");
            if (currentSession is null || !currentSession.IsOpen) return;
            await botifex.SendStatusUpdate(currentSession.SongQueue.ToString() + "\n" + e.Status.ToString());
        }

        private KarayoteUser CreateOrFindUser(BotifexUser remoteUser)
        {
            KarayoteUser? user = knownUsers.FirstOrDefault(u=>u.Id == remoteUser.Guid);
            if (user == null)
            {
                user = new KarayoteUser(remoteUser);
                knownUsers.Add(user);
            }
            return user;
        }
    }
}

