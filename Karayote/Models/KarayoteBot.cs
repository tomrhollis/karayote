using Botifex;
using Botifex.Services;
using Botifex.Services.TelegramBot;
using Botifex.Services.Discord;
using KarafunAPI;
using KarafunAPI.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Botifex.Models;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Karayote.Models
{
    /// <summary>
    /// Hosted service to route information between the users on messaging apps and the different sources of karaoke songs
    /// </summary>
    public class KarayoteBot : IHostedService, IKarayoteBot
    {
        private ILogger<KarayoteBot> log;
        private IBotifex botifex;
        private IKarafun karafun;
        private YouTubeService youtube;
        internal Session currentSession;
        private List<KarayoteUser> knownUsers = new List<KarayoteUser>();
        private HashSet<Song> knownSongs = new HashSet<Song>();

        /// <summary>
        /// Constructor to create and initialize the <see cref="KarayoteBot"/> instance
        /// </summary>
        /// <param name="log">The injected <see cref="ILogger"/> for logging to the console</param>
        /// <param name="cfg">The injected <see cref="IConfiguration"/> holding the user defined settings and bot keys</param>
        /// <param name="karApi">The injected <see cref="IKarafun"/> to talk to a running Karafun server</param>
        /// <param name="botifex">The injected <see cref="IBotifex"/> library allowing input from and responses to multiple messaging apps</param>
        /// <param name="host">The injected <see cref="IHost"/> interface for interacting with the host</param>
        public KarayoteBot(ILogger<KarayoteBot> log, IConfiguration cfg, IKarafun karApi, IBotifex botifex, IHost host)
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
                Name = "openqueue",
                Description = "Open the session for searching and queueing"
            });
            if (cfg.GetValue<bool>("AllowGetID"))
                botifex.AddCommand(new SlashCommand(adminOnly: true)
                {
                    Name = "getid",
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
            botifex.AddCommand(new SlashCommand(adminOnly: true)
            {
                Name = "closequeue",
                Description = "Disallow any more queueing"
            });
            botifex.AddCommand(new SlashCommand(adminOnly: true)
            {
                Name = "endsession",
                Description = "Shut down the singing completely"
            });
            botifex.AddCommand(new SlashCommand(adminOnly: true)
            {
                Name = "usewaitinglist",
                Description = "Add a song from the waiting list"
            });

            // register handlers for different messenger input types
            botifex.RegisterTextHandler(ProcessText);
            botifex.RegisterCommandHandler(ProcessCommand);

            log.LogDebug("Karayote Constructor ran");
            currentSession = host.Services.GetRequiredService<Session>();
            /*window = host.Services.GetRequiredService<MainWindow>();
            window.Show();*/
        }

        /// <summary>
        /// Perform tasks on startup. Automatically called by host on service start
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns><see cref="Task.CompletedTask"/></returns>
        public Task StartAsync(CancellationToken cancellationToken) // not actually async due to lack of need, but that's the interface signature
        {
            log.LogDebug("StartAsync has been called.");
            //karafun.OnStatusUpdated += KarafunStatusUpdate;
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
                        karafun.Search(new Action<List<Song>>(async (foundSongs) =>
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
                    if (currentSession.IsOpen || currentSession.IsStarted)
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
                case "openqueue":
                    if (currentSession.IsOpen || currentSession.IsStarted) // remove isstarted when all the proper checks are in place for reopening
                    {
                        await interaction.Reply("You already did that silly");
                    }
                    // karafun must be operational to open a session
                    else if (karafun.Status is null)
                    {
                        await interaction.Reply("Can't open the session, Karafun isn't speaking to us right now.");
                    }
                    // reopen the session 
                    // not available until all the proper checks are in place for restarting from a closed state
                    /*else if (currentSession.IsStarted)
                    {
                        currentSession.Reopen();
                        await interaction.Reply("Reopened the queue to submissions");
                    }*/
                    // open the session
                    else
                    {
                        currentSession.Open();
                        await botifex.SendOneTimeStatusUpdate("The session is now open for searching and queueing! DM me to make your selections and get in line.", notification: true);
                        await interaction.Reply("The session is now open for searching and queueing.");
                        await KarayoteStatusUpdate();
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

                            response = await TryAddSong(song);
                            response += "\n\n" + GetMySongs(user);
                        }
                        else
                            response = $"No can do, a Youtube video has to be less than 10 minutes long.";
                    }
                    catch (FormatException)
                    {
                        response = $"That's either a stream or less than a minute long. Nice try!";
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        response = "Couldn't find a YouTube video with that ID";
                    }
                    catch (ArgumentException)
                    {
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
                    catch (Exception ex) when (ex is ArgumentNullException or FormatException or OverflowException)
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
                                if (Math.Min(position1, position2) == 1) // trigger a status update if one of the queued songs changed
                                    await KarayoteStatusUpdate();
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
                    await interaction.Reply(response);
                    await interaction.End();
                    break;

                case "startsession":
                    response = "This session was already started";
                    if (!currentSession.IsStarted && !currentSession.IsOver) // make sure this hasn't already been done
                    {
                        currentSession.Start();
                        await botifex.ReplaceStatusMessage("And the singing starts.... NOW!");
                        await SendSingerNotifications();
                        response = "The queue is now flowing!";
                    }
                    await interaction.Reply(response);
                    await interaction.End();
                    break;

                case "nextsong":
                    response = "We're not singing right now";
                    if (currentSession.IsStarted && !currentSession.IsOver && currentSession.SongQueue.Count > 0) // only if the queue is moving right now
                    {
                        await AdvanceQueue(true);
                        response = "Done!";
                    }
                    await interaction.Reply(response);
                    await interaction.End();
                    break;

                case "closequeue":
                    currentSession.Close();
                    await interaction.Reply("Queue closed to new additions");
                    await interaction.End();
                    break;

                case "endsession":
                    currentSession.End();
                    await interaction.Reply("Session closed. Have a good night!");
                    await botifex.SendOneTimeStatusUpdate("Karayote is done for the night. Thanks for coming!\n\n");
                    await interaction.End();
                    break;

                case "usewaitinglist":
                    response = "There isn't a waiting list right now";/*
                    if (!currentSession.IsOpen && currentSession.HasWaitingList) // check if this command is even relevant. If the queue is still open or the list is empty, nothing to do.
                    { 
                        KarayoteUser? addedUser = currentSession.AddFromWaitingList();
                        
                        // keep grabbing from the waiting list until one of them still has a song in reserve                        
                        while (addedUser is null && currentSession.HasWaitingList)
                            addedUser = currentSession.AddFromWaitingList();

                        // this could be null in the rare possibility that everyone on the waiting list deletes their reserved songs
                        if (addedUser is not null)
                        {
                            SelectedSong song = user.GetSelectedSong(0);
                            response = "";
                        }                        
                    }*/
                    await interaction.Reply(response);
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
            if (history is not null)
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
                        await KarayoteStatusUpdate();
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
            if (!(currentSession.SongQueue.NowPlaying is null) && !(currentSession.SongQueue.NowPlaying.User.BotUser is null))
                await botifex.SendToUser(currentSession.SongQueue.NowPlaying.User.BotUser!, $"It's now your turn to sing {currentSession.SongQueue.NowPlaying.Title}! Come on up to the stage!");

            if (!(currentSession.SongQueue.NextUp is null) && !(currentSession.SongQueue.NextUp.User.BotUser is null))
                await botifex.SendToUser(currentSession.SongQueue.NextUp.User.BotUser!, $"You'll be up next to sing {currentSession.SongQueue.NextUp.Title} after {currentSession.SongQueue.NowPlaying!.User.Name} sings {currentSession.SongQueue.NowPlaying.Title}. Don't go too far!");
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
            if (interaction is TelegramTextInteraction && interaction.Text == "/start")
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
                    response = await TryAddSong(karafunSong);
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

            if (!string.IsNullOrEmpty(response)) await e.Interaction.Reply(response);
            await e.Interaction.End();
        }

        /// <summary>
        /// Try to add a <see cref="SelectedSong"/> to the current <see cref="Session"/>'s queue
        /// </summary>
        /// <param name="song">The <see cref="SelectedSong"/> to try to add to the queue</param>
        /// <param name="response">A <see cref="string"/> reponse to let the user know how that went</param>
        public async Task<string> TryAddSong(SelectedSong song)
        {
            string response = string.Empty;
            switch (currentSession.GetInLine(song))
            {
                case Session.SongAddResult.SuccessInQueue:
                    int position = currentSession.SongQueue.Count;
                    response = $"Added {song.Title} to the queue at position {position}";
                    if (position == 1)
                    {
                        response += "\n\n" + (currentSession.IsStarted ? "It's your turn right now! Come on up to the stage!" : "You will be up first! Don't go anywhere");
                    }
                    else if (position == 2)
                    {
                        response += "\n\n" + (currentSession.IsStarted ? "You are up after this person finishes singing! Don't go anywhere" : "You will be up second! Don't go anywhere");
                    }
                    await KarayoteStatusUpdate();
                    break;
                case Session.SongAddResult.SuccessInReserve:
                    response = $"Added {song.Title} to your reserved songs";
                    break;
                case Session.SongAddResult.UserReserveFull:
                    response = $"Couldn't add {song.Title}, you've already selected 3 songs. You can delete one or select a new one after you sing next";
                    break;
                case Session.SongAddResult.AlreadySelected:
                    response = $"Couldn't add {song.Title}, someone already picked that one today";
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
            return response;
        }

        /// <summary>
        /// Event handler for when anything about the Karafun status changes -- currently disabled
        /// </summary>
        /// <param name="sender">The <see cref="Karafun"/> instance whose <see cref="Status"/> has changed</param>
        /// <param name="e"><see cref="StatusUpdateEventArgs"/> containing the <see cref="Status"/> of <see cref="Karafun"/>'s queue</param>
        /*private async void KarafunStatusUpdate(object? sender, StatusUpdateEventArgs e)
        {
            await KarayoteStatusUpdate(e.Status); // pass status to karaoke update method
        }*/

        /// <summary>
        /// Update the messenger's status messages with new information
        /// </summary>
        /// <returns><see cref="Task.CompletedTask"/></returns>
        private async Task KarayoteStatusUpdate()
        {
            log.LogDebug($"[{DateTime.Now}] karayote status update fired");
            if (currentSession is null || !currentSession.IsOpen) return;
            await botifex.SendStatusUpdate(currentSession.SongQueue.ToString());
        }

        /// <summary>
        /// Make sure any user Karayote needs to reference is matched to only one <see cref="BotifexUser"/>
        /// </summary>
        /// <param name="remoteUser">The <see cref="BotifexUser"/> who initiated an interaction</param>
        /// <returns>An existing or newly created <see cref="KarayoteUser"/></returns>
        private KarayoteUser CreateOrFindUser(BotifexUser remoteUser)
        {
            KarayoteUser? user = knownUsers.FirstOrDefault(u => u.Id == remoteUser.Guid);
            if (user is null)
            {
                user = new KarayoteUser(remoteUser);
                knownUsers.Add(user);
            }
            return user;
        }

        /// <summary>
        /// Make sure any non-Botifex user has a unique username, return the existing user if this user has been seen before
        /// </summary>
        /// <param name="username">A <see cref="string"/> identifying the user</param>
        /// <returns>The <see cref="KarayoteUser"/> object for this user</returns>
        public KarayoteUser CreateOrFindUser(string username)
        {
            KarayoteUser? user = knownUsers.FirstOrDefault(u => u.Name == username);
            if (user is null)
            {
                user = new KarayoteUser(username);
                knownUsers.Add(user);
            }
            return user;
        }

        /// <summary>
        /// Tell the queue to move a song from one position to another and trigger a status update
        /// </summary>
        /// <param name="oldIndex">The zero-indexed position of the song to find in the queue</param>
        /// <param name="newIndex">The zero-indexed position to move the song to</param>
        /// <returns><see cref="Task.CompletedTask"/></returns>
        public async Task MoveSong(int oldIndex, int newIndex)
        {
            currentSession.SongQueue.MoveSong(oldIndex, newIndex);
            await KarayoteStatusUpdate();
        }

        /// <summary>
        /// Remove a song from the queue and perform any necessary updates
        /// </summary>
        /// <param name="song">The <see cref="SelectedSong"/> to find and remove from the queue</param>
        /// <returns><see cref="Task.CompletedTask"/></returns>
        public async Task DeleteSong(SelectedSong song)
        {
            Tuple<SelectedSong, int>? oldSong = currentSession.SongQueue.GetUserSongWithPosition(song.User);
            if (oldSong is null) return;

            currentSession.RemoveSong(song.User, 1);
            await KarayoteStatusUpdate();

            string message = $"The host has removed {song.Title} from the queue. ";
            if (currentSession.SongQueue.HasUser(song.User))
            {
                Tuple<SelectedSong, int> newSong = currentSession.SongQueue.GetUserSongWithPosition(song.User)!;
                message += $"It's been replaced by your song in reserve: {newSong.Item1.Title}, still at number {newSong.Item2} in line";
            }
            else
                message += $"Please see them for more info. (You were at position {oldSong.Item2} in line)";
            
            if(song.User.BotUser is not null)
            {
                await botifex.SendToUser(song.User.BotUser, message);
            }
        }

        /// <summary>
        /// Handle admin request to move to the next song in the queue
        /// </summary>
        /// <param name="sung">Whether the song was actually sung or not</param>
        /// <returns><see cref="Task.CompletedTask"/></returns>
        public async Task AdvanceQueue(bool sung=true)
        {
            if(sung)
                await botifex.ReplaceStatusMessage($"{currentSession.SongQueue.NowPlaying!.User.Name} just sang {currentSession.SongQueue.NowPlaying.Title}");
            
            currentSession.NextSong(sung);      // advance queue
            await KarayoteStatusUpdate();       // update status posts
            await SendSingerNotifications();    // notify next 2 singers
        }
    }
}

