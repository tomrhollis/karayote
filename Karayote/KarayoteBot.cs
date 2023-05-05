using Botifex;
using Botifex.Services;
using KarafunAPI;
using KarafunAPI.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Karayote.Models;
using Botifex.Models;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System.Text.RegularExpressions;

namespace Karayote
{
    internal class KarayoteBot : IHostedService
    {
        private ILogger<KarayoteBot> log;
        private IConfiguration cfg;
        private IBotifex botifex;
        private IKarafun karafun;
        private YouTubeService youtube;
        internal Session currentSession = new Session();
        private List<KarayoteUser> knownUsers = new List<KarayoteUser>();
        private HashSet<Song> knownSongs = new HashSet<Song>();

        public KarayoteBot(ILogger<KarayoteBot> log, IConfiguration cfg, IKarafun karApi, Botifex.IBotifex botifex)
        {
            this.log = log;
            this.cfg = cfg;
            this.botifex = botifex;
            karafun = karApi;

            youtube = new YouTubeService(new BaseClientService.Initializer()
            {
                ApplicationName = cfg.GetSection("Youtube").GetValue<string>("GoogleAPIAppName"),
                ApiKey = cfg.GetSection("Youtube").GetValue<string>("YoutubeAPIKey")                
            });
            
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
            botifex.AddCommand(new SlashCommand()
            {
                Name = "youtube",
                Description = "Add a YouTube karaoke video",
                Options = new List<CommandField>
                {
                    new CommandField()
                    {
                        Name = "video",
                        Description = "the YouTube link or 11-character video id",
                        Required = true
                    }
                }
            });
            botifex.AddCommand(new SlashCommand()
            {
                Name = "mysongs",
                Description = "See what songs you've selected to sing"
            });
            botifex.AddCommand(new SlashCommand()
            {
                Name = "removesong",
                Description = "Decide against singing a song",
                Options = new List<CommandField>
                {
                    new CommandField
                    {
                        Name = "songnumber",
                        Description = "the song's number in /mysongs",
                        Required = true
                    }
                }
            });

            botifex.RegisterTextHandler(ProcessText);
            botifex.RegisterCommandHandler(ProcessCommand);
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
                        await interaction.End();
                    }
                                       
                    break;

                case "seequeue":
                    if(currentSession.IsOpen)
                    {
                        await interaction.Reply(currentSession.SongQueue.ToString());
                    }
                    else
                        await NoSessionReply(interaction);
                    await interaction.End();

                    break;

                case "karafunlink":
                    await interaction.Reply("https://www.karafun.com/karaoke -- note that their site contains a few songs not licensed for use in Canada. If you can't find them through /search when the event starts, that's probably why");
                    await interaction.End();
                    break;

                case "getid":
                    await interaction.Reply($"Chat ID: {((Interaction)interaction).Source.MessageId}");
                    await interaction.End();
                    break;

                case "opensession":
                    if(currentSession.IsOpen)
                    {
                        await interaction.Reply("The session is already open silly");
                    }
                    else if (karafun.Status is not null)
                    {
                        currentSession.Open();
                        await botifex.SendOneTimeStatusUpdate("The session is now open for searching and queueing! DM me to make your selections and get in line.", notification: true);
                        await interaction.Reply("The session is now open for searching and queueing");
                        KarayoteStatusUpdate(null, new StatusUpdateEventArgs(karafun.Status));
                    }
                    else if (karafun.Status is null)
                        await interaction.Reply("Can't open the session, Karafun isn't speaking to us right now.");

                    await interaction.End();
                    break;

                case "youtube":
                    if (!currentSession.IsOpen)
                    {
                        await NoSessionReply(interaction);
                        break;
                    }
                 
                    Uri? youtubeLink = null;
                    YoutubeSong? song = null;
                    string response = "";
                    try // the parse or if-else might fail if they put in some crappy text
                    {
                        Uri.TryCreate(interaction.CommandFields["video"], new UriCreationOptions(), out youtubeLink);

                        if (youtubeLink is not null)
                            song = new YoutubeSong(youtubeLink, user);
                        else
                            song = new YoutubeSong(interaction.CommandFields["video"], user);

                        VideosResource.ListRequest listRequest = youtube.Videos.List("snippet,contentDetails");
                        listRequest.Id = song.Id;
                        VideoListResponse ytVideos = listRequest.Execute();
                        
                        int durationMins = int.Parse(Regex.Match(ytVideos.Items[0].ContentDetails.Duration.Split("M")[0], "[\\d]{1,2}$").Value);
                        log.LogDebug(durationMins.ToString());
                        if (durationMins < 10 && durationMins > 0)
                        {
                            song.Video = ytVideos.Items[0];
                            log.LogDebug($"[{DateTime.Now}] Got request for video id {song.Id} from {user.Name} with id {user.Id}");

                            TryAddSong(song, ref response);
                        }
                        else
                            response = $"No can do, a Youtube video has to be less than 10 minutes long.";
                    }
                    catch(FormatException fx)
                    {
                        response = $"That's either a stream or less than a minute long. Nice try!";
                    }
                    catch(ArgumentOutOfRangeException arx)
                    {
                        log.LogWarning($"[{DateTime.Now}] User {user.Name} caused exception while requesting a youtube video {interaction.CommandFields["video"]}: {arx.GetType()} - {arx.Message}");
                        response = "Couldn't find a YouTube video with that ID";
                    }
                    catch (ArgumentException ax)
                    {
                        log.LogWarning($"[{DateTime.Now}] User {user.Name} caused exception while requesting a youtube video {interaction.CommandFields["video"]}: {ax.GetType()} - {ax.Message}");
                        response = "Couldn't find a YouTube video in that. Make sure you copy links directly from the video, or if you're using an id that it's 11 characters long, no more no less";
                    }                    
                    await e.Interaction.Reply(response);
                    await e.Interaction.End();
                    break;

                case "mysongs":
                    Tuple<SelectedSong, int>? songAtPosition = currentSession.SongQueue.GetUserSong(user);                                        
                    response = "You have no songs in the queue or reserve";

                    if (songAtPosition is not null)
                    {
                        response = $"1) {songAtPosition.Item1.Title} at queue position {songAtPosition.Item2}";
                        List<SelectedSong> reserveSongs = user.GetReservedSongs().ToList();
                        
                        if (reserveSongs.Count > 0)
                        {
                            for (int i = 0;  i < reserveSongs.Count; i++)
                                response += $"\n{i+2}) {reserveSongs[i].Title} [in reserve]";
                        }
                    }

                    await e.Interaction.Reply(response);
                    await interaction.End();
                    break;

                case "removesong":
                    try
                    {
                        int position = int.Parse(interaction.CommandFields["songnumber"]);

                        if (!currentSession.SongQueue.HasUser(user))
                            response = "You haven't selected any songs yet";

                        else if (position == 1 && user.reservedSongs.Count == 0)
                        {
                            Dictionary<string, string> options = new Dictionary<string, string>
                        {
                            { "✅", "Yes, Delete" },
                            { "❌", "No, Nevermind" }
                        };
                            ReplyMenu menu = new ReplyMenu("confirmdelete", options, ProcessMenuReply);
                            menu.NumberedChoices = false;

                            await ((Interaction)interaction).ReplyWithOptions(menu, "That's your last selected song so you'll lose your place in the queue. Add another song first to keep your spot. Are you sure you want to delete this now?");
                            break;
                        }

                        else
                            response = DeleteSong(user, position);
                    }
                    catch(Exception ex) when (ex is ArgumentNullException or FormatException or OverflowException)
                    {
                        response = "Well I wasn't expecting that";
                    }

                    await e.Interaction.Reply(response);
                    await interaction.End();
                    break;

                default:
                    break;
                    
            }
        }

        private string DeleteSong(KarayoteUser user, int position)
        {
            string response = "Couldn't remove that song, not sure why";
            bool reservedSong = user.reservedSongs.Count > 0;
            try
            {
                if (currentSession.RemoveSong(user, position))
                {
                    response = $"Removed your selected song #{position}.";
                    if (position == 1)
                    {
                        response += reservedSong ? " Your first reserved song has taken its place in the queue" : "";
                        KarayoteStatusUpdate(null, new StatusUpdateEventArgs(karafun.Status));
                    }
                }
            }
            catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentNullException)
            {
                response = "That wasn't the number of one of the songs that can be removed";
            }
            return response;
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
            await interaction.End();
        }

        private async void ProcessMenuReply(object? sender, MenuReplyReceivedEventArgs e)
        {
            if (sender is null) throw new ArgumentException();

            ReplyMenu menu = (ReplyMenu)sender!;
            KarayoteUser user = CreateOrFindUser(e.Interaction.User!);
            string response = "";

            switch (menu.Name)
            {
                case "chosensong":
                    Song chosenSong = knownSongs.First(s => s.Id == uint.Parse(e.Reply));
                    
                    KarafunSong karafunSong = new KarafunSong(chosenSong, user);

                    log.LogDebug($"[{DateTime.Now}] Got request for {karafunSong.Title} from {user.Name} with id {user.Id}");

                    TryAddSong(karafunSong, ref response);                    
                    break;

                case "confirmdelete":
                    int position = int.Parse(e.Interaction.CommandFields["songnumber"]);
                    if (e.Reply == "✅")
                        response = DeleteSong(user, position);
                    else
                        response = "OK, nevermind!";
                    break;

                default: break;
            }

            if(!String.IsNullOrEmpty(response)) await e.Interaction.Reply(response);
            await e.Interaction.End();
        }

        private void TryAddSong(SelectedSong song, ref string response)
        {
            switch (currentSession.GetInLine(song))
            {
                case Session.SongAddResult.SuccessInQueue:
                    response = $"Added {song.Title} to the queue at position {currentSession.SongQueue.Count}";
                    KarayoteStatusUpdate(null, new StatusUpdateEventArgs(karafun.Status));
                    break;
                case Session.SongAddResult.SuccessInReserve:
                    response = $"Added {song.Title} to your reserved songs";
                    break;
                case Session.SongAddResult.UserReserveFull:
                    response = $"Couldn't add {song.Title}, you've already selected 3 songs. You can delete one or select a new one after you sing next";
                    break;
                case Session.SongAddResult.AlreadySelected:
                    response = $"Couldn't add {song.Title}, someone else already picked that today";
                    break;
                case Session.SongAddResult.UnknownFailure:
                    response = $"Couldn't add {song.Title}, but I'm not sure why it didn't work";
                    break;
                default:
                    break;                               
            }   
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

