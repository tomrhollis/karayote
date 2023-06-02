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
    /// <summary>
    /// Hosted service to route information between the users on messaging apps and the different sources of karaoke songs
    /// </summary>
    internal class KarayoteBot : IHostedService
    {
        private ILogger<KarayoteBot> log;
        private IBotifex botifex;
        private IKarafun karafun;
        private YouTubeService youtube;
        internal Session currentSession = new Session();
        private List<KarayoteUser> knownUsers = new List<KarayoteUser>();
        private HashSet<Song> knownSongs = new HashSet<Song>();

        /// <summary>
        /// Constructor to create and initialize the <see cref="KarayoteBot"/> instance
        /// </summary>
        /// <param name="log">The injected <see cref="ILogger"/> for logging to the console</param>
        /// <param name="cfg">The injected <see cref="IConfiguration"/> holding the user defined settings and bot keys</param>
        /// <param name="karApi">The injected <see cref="IKarafun"/> to talk to a running Karafun server</param>
        /// <param name="botifex">The injected <see cref="IBotifex"/> library allowing input from and responses to multiple messaging apps</param>
        public KarayoteBot(ILogger<KarayoteBot> log, IConfiguration cfg, IKarafun karApi, Botifex.IBotifex botifex)
        {
            this.log = log;
            this.botifex = botifex;
            karafun = karApi;

            youtube = new YouTubeService(new BaseClientService.Initializer()
            {
                ApplicationName = cfg.GetSection("Youtube").GetValue<string>("GoogleAPIAppName"),
                ApiKey = cfg.GetSection("Youtube").GetValue<string>("YoutubeAPIKey")                
            });
            
            // Create all the commands the messaging apps need to interact with Karayote
            // See processing in ProcessCommand for more info on each
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
                botifex.AddCommand(new SlashCommand(adminOnly: true)
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
            botifex.AddCommand(new SlashCommand()
            {
                Name = "switchsongs",
                Description = "Switch priority of two songs",
                Options = new List<CommandField>
                {
                    new CommandField
                    {
                        Name = "song1",
                        Description = "the first song's number in your list",
                        Required = true
                    },
                    new CommandField
                    {
                        Name = "song2",
                        Description = "the second song's number in your list",
                        Required = true
                    }
                }
            });
            botifex.AddCommand(new SlashCommand()
            {
                Name = "help",
                Description = "Get some guidance on how to use this"
            });
            botifex.AddCommand(new SlashCommand(adminOnly: true)
            {
                Name = "startsession",
                Description = "Start the first song"
            });
            botifex.AddCommand(new SlashCommand(adminOnly: true)
            {
                Name = "nextsong",
                Description = "Move to the next song"
            });

            // register handlers for different messenger input types
            botifex.RegisterTextHandler(ProcessText);
            botifex.RegisterCommandHandler(ProcessCommand);
        }

        /// <summary>
        /// Perform tasks on startup. Automatically called by host on service start
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns><see cref="Task.CompletedTask"/></returns>
        public Task StartAsync(CancellationToken cancellationToken) // not actually async due to lack of need, but that's the interface signature
        {
            log.LogDebug("StartAsync has been called.");
            karafun.OnStatusUpdated += KarafunStatusUpdate;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Perform tasks before stopping. Automatically called by host when shutting down.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns><see cref="Task.CompletedTask"/></returns>
        public Task StopAsync(CancellationToken cancellationToken) // not actually async due to lack of need, but that's the interface signature
        {
            currentSession.End();
            log.LogDebug("StopAsync has been called.");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Receive commands from <see cref="Botifex.Botifex"/> and process them according to which command it is
        /// </summary>
        /// <param name="sender">The sender of the event, most likely <see cref="Botifex.Botifex"/></param>
        /// <param name="e"><see cref="InteractionReceivedEventArgs"/> containing the <see cref="Interaction"/> with the user</param>
        private async void ProcessCommand(object? sender, InteractionReceivedEventArgs e)
        {            
            KarayoteUser user = CreateOrFindUser(e.Interaction.User!); // make sure we keep track of which user this is
            ICommandInteraction interaction = (ICommandInteraction)e.Interaction;
#if DEBUG
            log.LogDebug($"[{DateTime.Now}] Karayote got {interaction.BotifexCommand.Name} from {sender?.GetType()}");
#endif
            switch (interaction.BotifexCommand.Name)
            {
                // searching Karafun for a specific song
                case "search":
                    if (currentSession.IsOpen)
                    {
                        await interaction.Reply($"Searching Karafun catalog for {interaction.CommandFields["terms"]}"); // let user know we're doing things
                        karafun.Search(new Action<List<Song>>(async (List<Song> foundSongs) =>
                        {
                            if (foundSongs is null || foundSongs.Count == 0)
                            {
                                await ((Interaction)interaction).Reply("No songs like that found in the Karafun catalog");
                                await interaction.End();
                                return;
                            }
                            // build a dictionary for the menu to work with: a pair of song ID and its description string
                            Dictionary<string, string> results = new Dictionary<string, string>();
                            for (int i = 0; i < foundSongs.Count; i++)
                            {
                                knownSongs.Add(foundSongs[i]);
                                results.Add($"{foundSongs[i].Id}", $"{foundSongs[i]}");
                            }
                            // make the menu and send it
                            ReplyMenu menu = new ReplyMenu("chosensong", results, ProcessMenuReply);
                            await ((Interaction)interaction).ReplyWithOptions(menu, "Pick a song to add yourself to the queue");

                        }), interaction.CommandFields["terms"]);
                    }
                    // notify if the session isn't open to searching and queueing yet
                    else
                    {
                        await NoSessionReply(interaction);
                        await interaction.End();
                    }                                       
                    break;

                // see the current song queue if the session is open
                case "seequeue":
                    if(currentSession.IsOpen)
                    {
                        await interaction.Reply(currentSession.SongQueue.ToString());
                    }
                    else
                        await NoSessionReply(interaction);
                    await interaction.End();

                    break;

                // get a link to the karafun website to search the catalog any time, even when the session is closed
                case "karafunlink":
                    await interaction.Reply("https://www.karafun.com/karaoke -- note that their site contains a few songs not licensed for use in Canada. If you can't find them through /search when the event starts, that's probably why");
                    await interaction.End();
                    break;

                // get the unique ID of the chat this command came from. Needed for setting up Telegram. Should be disabled in appsettings.json when not needed
                case "getid":
                    await interaction.Reply($"Chat ID: {((Interaction)interaction).Source.ChannelId}");
                    await interaction.End();
                    break;

                // open the session to searching and queueing
                case "opensession":
                    if(currentSession.IsOpen)
                    {
                        await interaction.Reply("The session is already open silly");
                    }
                    // karafun must be operational to open a session
                    else if (karafun.Status is null)
                    {
                        await interaction.Reply("Can't open the session, Karafun isn't speaking to us right now.");
                    }
                    // open the session
                    else
                    {
                        currentSession.Open();
                        await botifex.SendOneTimeStatusUpdate("The session is now open for searching and queueing! DM me to make your selections and get in line.", notification: true);
                        await interaction.Reply("The session is now open for searching and queueing.");
                        await KarayoteStatusUpdate(karafun.Status);
                    }
                    await interaction.End();
                    break;

                // select a youtube video as a song request
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
                        // see if it's a real address or not
                        Uri.TryCreate(interaction.CommandFields["video"], new UriCreationOptions(), out youtubeLink);

                        if (youtubeLink is not null)
                            song = new YoutubeSong(youtubeLink, user); // constructor for addresses
                        else
                            song = new YoutubeSong(interaction.CommandFields["video"], user); // constructor for presumed IDs

                        VideosResource.ListRequest listRequest = youtube.Videos.List("snippet,contentDetails");
                        listRequest.Id = song.Id;
                        VideoListResponse ytVideos = listRequest.Execute();
                        
                        // make sure it's a reasonable length
                        int durationMins = int.Parse(Regex.Match(ytVideos.Items[0].ContentDetails.Duration.Split("M")[0], "[\\d]{1,2}$").Value); // exception if less than a min
                        log.LogDebug(durationMins.ToString());
                        if (durationMins < 10 && durationMins > 0)
                        {
                            song.Video = ytVideos.Items[0];
                            log.LogDebug($"[{DateTime.Now}] Got request for video id {song.Id} from {user.Name} with id {user.Id}");

                            TryAddSong(song, ref response);
                            response += "\n\n" + GetMySongs(user);
                        }
                        else
                            response = $"No can do, a Youtube video has to be less than 10 minutes long.";
                    }
                    catch(FormatException)
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
                        response = "Couldn't find a YouTube video link or ID in what you entered. Make sure you copy links directly from the video, or if you're using an id that it's 11 characters long, no more no less";
                    }                    
                    await interaction.Reply(response);
                    await interaction.End();
                    break;

                // retrieve a list of a user's queued and reserved songs for this sesssion
                case "mysongs":
                    response = GetMySongs(user);

                    await interaction.Reply(response);
                    await interaction.End();
                    break;

                // delete a song at a user's request
                case "removesong":
                    try
                    {
                        int position = int.Parse(interaction.CommandFields["songnumber"]);

                        if (!currentSession.SongQueue.HasUser(user))
                            response = "You haven't selected any songs yet";

                        // send a confirmation menu if this is their last selected song and they'll lose their spot doing this
                        else if (position == 1 && user.ReservedSongCount == 0)
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
                            response = DeleteSong(user, position) + "\n\n" + GetMySongs(user);
                    }
                    catch(Exception ex) when (ex is ArgumentNullException or FormatException or OverflowException)
                    {
                        response = "Well I wasn't expecting that";
                    }
                    await interaction.Reply(response);
                    await interaction.End();
                    break;

                // switch the priority of two songs in a user's personal selections
                case "switchsongs":
                    response = "Couldn't switch songs at those positions. Make sure both numbers are between 1 and " + (KarayoteUser.MAX_RESERVED_SONGS + 1);
                    try
                    {
                        int position1 = int.Parse(interaction.CommandFields["song1"]);
                        int position2 = int.Parse(interaction.CommandFields["song2"]);

                        if (!currentSession.SongQueue.HasUser(user) || user.ReservedSongCount == 0)
                            response = "You haven't selected two songs yet to switch them";

                        else
                        {
                            bool success = currentSession.SwitchUserSongs(user, position1, position2);

                            if (success)
                            {
                                response = "Done!\n\n" + GetMySongs(user);
                                if(Math.Min(position1, position2) == 1) // trigger a status update if one of the queued songs changed
                                    await KarayoteStatusUpdate(karafun.Status);
                            }
                        }
                    }
                    catch (Exception ex) when (ex is ArgumentNullException or FormatException or OverflowException)
                    {
                        response = "One or both of those wasn't even close to being the number of a song!";
                    }

                    await interaction.Reply(response);
                    await interaction.End();
                    break;

                case "help":
                    response = "How to use this bot:\n\n" +
                               "Before the event, you can search the catalog using the link from /karafunlink, or search youtube for ideas\n\n" +
                               "A little while before singing starts, the bot will open for searching and getting in line. Then you can use /search to find and add Karafun songs to your selections, or /youtube to add a youtube song\n\n" +
                               "/mysongs will show you what you've selected and /seequeue will show you what the queue is looking like\n\n" +
                               "After you look at /mysongs for the order your songs are in, you can use /switchsongs to change their order or /removesong to get rid of one\n\n" +
                               "When the singing starts, you'll get a DM from the bot when the person before you starts singing. Don't miss your turn!";
                    await e.Interaction.Reply(response);
                    await interaction.End();
                    break;

                case "startsession":
                    response = "This session was already started";
                    if (!currentSession.IsStarted && !currentSession.IsOver) // make sure this hasn't already been done
                    {
                        currentSession.Start();
                        await botifex.SendOneTimeStatusUpdate("And the singing starts.... NOW!");
                        await SendSingerNotifications();
                        response = "The queue is now flowing!";                    
                    }
                    await e.Interaction.Reply(response);
                    await interaction.End();
                    break;

                case "nextsong":
                    response = "We're not singing right now";
                    if (!currentSession.IsStarted && !currentSession.IsOver) // only if the queue is moving right now
                    {
                        currentSession.NextSong();
                        await KarayoteStatusUpdate(karafun.Status);
                        await SendSingerNotifications();
                        response = "Done!";
                    }
                    await e.Interaction.Reply(response);
                    await interaction.End();
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Retrieves a <see cref="List"/> of <see cref="SelectedSong"/>s the user has selected in this session
        /// </summary>
        /// <param name="user">The <see cref="KarayoteUser"/> who is asking for their selected songs</param>
        /// <returns>A formatted <see cref="string"/> listing a <see cref="KarayoteUser"/>'s <see cref="SelectedSong"/>s for this session</returns>
        private string GetMySongs(KarayoteUser user)
        {
            string response = "You have no songs in the queue or reserve";
            Tuple<SelectedSong, int>? songAtPosition = currentSession.SongQueue.GetUserSongWithPosition(user);
            
            if (songAtPosition is not null)
            {
                // build the response list starting with the song in the queue
                response = $"Your Selected Songs:\n1) {songAtPosition.Item1.Title} at queue position {songAtPosition.Item2}";
                List<SelectedSong> reserveSongs = user.GetReservedSongs().ToList();

                // add reserved songs if they exist
                if (reserveSongs.Count > 0)
                {
                    for (int i = 0; i < reserveSongs.Count; i++)
                        response += $"\n{i + 2}) {reserveSongs[i].Title} [in reserve]";
                }
            }

            // tack on any previously sung songs in this session as a history
            List<SelectedSong>? history = currentSession.GetUserHistory(user);
            if(history is not null)
            {
                response += "\n\nPreviously sung today:";
                foreach (var item in history)
                {
                    response += "\n" + item.ToString();
                }
            }
            return response;
        }

        /// <summary>
        /// Handle any interaction where a <see cref="KarayoteUser"/> requests deletion of a <see cref="SelectedSong"/>. Attempts to delete it and responds with how it went.
        /// </summary>
        /// <param name="user">The <see cref="KarayoteUser"/> to delete a <see cref="SelectedSong"/> for</param>
        /// <param name="position">The position of the <see cref="SelectedSong"/> in their personal queue. 1 being in the main queue, 2+ being positions 0+ in their reserve</param>
        /// <returns>A <see cref="string"/> describing the result of the attempted deletion</returns>
        private async Task<string> DeleteSong(KarayoteUser user, int position)
        {
            string response = "Couldn't remove that song, not sure why";
            bool reservedSong = user.ReservedSongCount > 0;
            try
            {
                if (currentSession.RemoveSong(user, position))
                {
                    response = $"Removed your selected song #{position}.";
                    if (position == 1) // assure the user that they're still in the same spot in the queue if they had a reserved song
                    {
                        response += reservedSong ? " Your first reserved song has taken its place in the queue" : "";
                        await KarayoteStatusUpdate(karafun.Status);
                    }
                }
            }
            catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentNullException)
            {
                response = "That wasn't the number of one of the songs that can be removed";
            }
            return response;
        }

        /// <summary>
        /// Create the reply to any interaction that requires an open session when the session is still closed.
        /// </summary>
        /// <param name="interaction">The <see cref="ICommandInteraction"/> to interact with the user on the other end</param>
        /// <returns><see cref="Task.CompletedTask"/></returns>
        private async Task NoSessionReply(ICommandInteraction interaction)
        {
            // message for when it's not the scheduled start time yet
            if (currentSession.StartTime is null || currentSession.StartTime > DateTime.Now)
            {
                string nextSession = "Not sure when the next one is, but"; // eventually will check for future sessions when those exist
                await interaction.Reply($"There aren't any open sessions yet. {nextSession} hold your horses. Meanwhile you can search the catalog online at https://www.karafun.com/karaoke -- note that their site contains some songs not licensed for use in Canada. If you can't find them through /search when the event starts, that's probably why");
            }
            else // this needs to be fleshed out
            {
                await interaction.Reply($"The queue has closed for now, but I have not been programmed to know why in this case");
            }            
        }

        /// <summary>
        /// Notify the current and next singers
        /// </summary>
        /// <returns><see cref="Task.CompletedTask"/></returns>
        private async Task SendSingerNotifications()
        {
            if (currentSession.SongQueue.NowPlaying is null) return; // if there's no current song, there's nothing to do here
            await botifex.SendToUser(currentSession.SongQueue.NowPlaying.User.BotUser, $"It's now your turn to sing {currentSession.SongQueue.NowPlaying.Title}! Come on up to the stage!");
            
            if (currentSession.SongQueue.NextUp is null) return; // if there's no next song, there's nothing left to do here
            await botifex.SendToUser(currentSession.SongQueue.NextUp.User.BotUser, $"You'll be up next to sing {currentSession.SongQueue.NextUp.Title} after {currentSession.SongQueue.NowPlaying.User.Name} sings {currentSession.SongQueue.NowPlaying.Title}. Don't go too far!");
        }

        /// <summary>
        /// Handler for processing plain text messages. Currently no use case
        /// </summary>
        /// <param name="sender">The <see cref="Botifex.Botifex"/> instance sending the events</param>
        /// <param name="e">The <see cref="InteractionReceivedEventArgs"/> containing the <see cref="Interaction"/> needed to reply</param>
        private async void ProcessText(object? sender, InteractionReceivedEventArgs e)
        {
            // track the user
            KarayoteUser user = CreateOrFindUser(e.Interaction.User!);

            ITextInteraction interaction = (ITextInteraction)e.Interaction;
#if DEBUG
            log.LogDebug($"[{DateTime.Now}] Karayote got {interaction.Text} from {sender?.GetType()}");
#endif
            string reply = "";
            // if this is a /start command from interacting with the telegram bot for the first time
            if(interaction is TelegramTextInteraction && interaction.Text == "/start")
            {
                reply = "Hey there! Check out the menu button at the bottom to see your options, or type /help for more information";
            }
            // otherwise just tell them to use a command
            else
            {
                reply = "I see you! Please use a slash command to make a request.";
            }
            await interaction.Reply(reply);
            await interaction.End();
        }

        /// <summary>
        /// Handler for processing replies to previously sent menu options
        /// </summary>
        /// <param name="sender">A <see cref="ReplyMenu"/> that has generated this event</param>
        /// <param name="e">The <see cref="MenuReplyReceivedEventArgs"/> containing the user's selection</param>
        /// <exception cref="ArgumentException">Thrown when there is no sender, since we need the sender to know what to do with it</exception>
        private async void ProcessMenuReply(object? sender, MenuReplyReceivedEventArgs e)
        {
            if (sender is null) throw new ArgumentException(); // need a sender to know what kind of menu this is

            ReplyMenu menu = (ReplyMenu)sender!;
            KarayoteUser user = CreateOrFindUser(e.Interaction.User!); // should be finding not creating if we're at this point
            string response = "";

            switch (menu.Name)
            {
                // menu options for choosing a song that was returned from a karafun search
                case "chosensong":
                    Song chosenSong = knownSongs.First(s => s.Id == uint.Parse(e.Reply));
                    
                    KarafunSong karafunSong = new KarafunSong(chosenSong, user);
#if DEBUG
                    log.LogDebug($"[{DateTime.Now}] Got request for {karafunSong.Title} from {user.Name} with id {user.Id}");
#endif
                    TryAddSong(karafunSong, ref response);
                    response += "\n\n" + GetMySongs(user);
                    break;

                // menu options for confirming deletion of someone's last selected song (losing their spot in the queue)
                case "confirmdelete":
                    int position = int.Parse(e.Interaction.CommandFields["songnumber"]);
                    if (e.Reply == "✅")
                        response = DeleteSong(user, position) + "\n\n" + GetMySongs(user);
                    else
                        response = "OK, nevermind!";
                    break;

                default: break;
            }

            if(!String.IsNullOrEmpty(response)) await e.Interaction.Reply(response);
            await e.Interaction.End();
        }

        /// <summary>
        /// Try to add a <see cref="SelectedSong"/> to the current <see cref="Session"/>'s queue
        /// </summary>
        /// <param name="song">The <see cref="SelectedSong"/> to try to add to the queue</param>
        /// <param name="response">A <see cref="string"/> reponse to let the user know how that went</param>
        private void TryAddSong(SelectedSong song, ref string response)
        {
            switch (currentSession.GetInLine(song))
            {
                case Session.SongAddResult.SuccessInQueue:
                    response = $"Added {song.Title} to the queue at position {currentSession.SongQueue.Count}";
                    KarayoteStatusUpdate(karafun.Status).Wait();
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
                case Session.SongAddResult.QueueClosed:
                    response = $"Couldn't add {song.Title}, the queue is closed right now";
                    break;
                case Session.SongAddResult.UnknownFailure:
                    response = $"Couldn't add {song.Title}, but I'm not sure why it didn't work";
                    break;
                default:
                    response = $"Something so unexpected occurred that I have no idea if your song was added or not. Please check using /mysongs and speak with the hosts if it's not there";
                    break;                               
            }   
        }

        /// <summary>
        /// Event handler for when anything about the Karafun status changes
        /// </summary>
        /// <param name="sender">The <see cref="Karafun"/> instance whose <see cref="Status"/> has changed</param>
        /// <param name="e"><see cref="StatusUpdateEventArgs"/> containing the <see cref="Status"/> of <see cref="Karafun"/>'s queue</param>
        private async void KarafunStatusUpdate(object? sender, StatusUpdateEventArgs e)
        {
            await KarayoteStatusUpdate(e.Status); // pass status to karaoke update method
        }

        /// <summary>
        /// Update the messenger's status messages with new information
        /// </summary>
        /// <param name="status">The current <see cref="Status"/> of the <see cref="Karafun"/> player to add to Karayote's knowledge of the status situation</param>
        /// <returns><see cref="Task.CompletedTask"/></returns>
        private async Task KarayoteStatusUpdate(Status status)
        {
            log.LogDebug($"[{DateTime.Now}] karayote status update fired");
            if (currentSession is null || !currentSession.IsOpen) return;
            await botifex.SendStatusUpdate(currentSession.SongQueue.ToString() + "\n" + status.ToString());
        }

        /// <summary>
        /// Make sure any user Karayote needs to reference is matched to only one <see cref="BotifexUser"/>
        /// </summary>
        /// <param name="remoteUser">The <see cref="BotifexUser"/> who initiated an interaction</param>
        /// <returns>An existing or newly created <see cref="KarayoteUser"/></returns>
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

