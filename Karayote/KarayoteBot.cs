﻿using Botifex;
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
using Google.Apis.Util;
using Telegram.Bot.Types;
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


                            if (currentSession.GetInLine(song))
                            {
                                response = $"Added {song.Title} to the queue at position {currentSession.SongQueue.Count}";
                                KarayoteStatusUpdate(null, new StatusUpdateEventArgs(karafun.Status));
                            }
                            else
                                response = $"Couldn't add video with id {song.Id}, you already have a song in the queue or someone else picked that";
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
                    e.Interaction.End();
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

                    if (currentSession.GetInLine(karafunSong))
                    {
                        response = $"Added {karafunSong.Title} to the queue at position {currentSession.SongQueue.Count}";
                        KarayoteStatusUpdate(null, new StatusUpdateEventArgs(karafun.Status));
                    }
                    else
                        response = $"Couldn't add {karafunSong.Title}, you already have a song in the queue or someone else picked that";
                    
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

