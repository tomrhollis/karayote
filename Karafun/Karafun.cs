using KarafunAPI.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Xml;

namespace KarafunAPI
{
    /// <summary>
    /// Service for communicating with the Karafun websocket server
    /// </summary>
    public class Karafun : IKarafun
    {
        private Status status = null;
        public Status Status
        {
            get=>status;
            private set
            {
                if (value is not null && value.ToString() != status?.ToString())
                {
                    status = value;
                    EventHandler<StatusUpdateEventArgs> handler = OnStatusUpdated;
                    if (handler != null)
                    {
                        handler(this, new StatusUpdateEventArgs(status));
                    }
                }
            }
        }

        private ConcurrentQueue<Task> requestQueue = new ConcurrentQueue<Task>();

        internal bool InUse { get; private set; } = false;

        public event EventHandler<StatusUpdateEventArgs> OnStatusUpdated;

        private bool stopping = false;
        private ClientWebSocket karafun = new();
        private Uri wsLocation = new Uri("ws://localhost:57570"); // default server address on same device

        private ILogger<Karafun> log;
        private IConfigurationSection cfg;
        private IHostApplicationLifetime appLifetime;

        public Karafun(ILogger<Karafun> log, IConfiguration cfg, IHostApplicationLifetime appLifetime)
        {
            this.log = log;
            this.cfg = cfg.GetSection("Karafun");
            this.appLifetime = appLifetime;

            appLifetime.ApplicationStarted.Register(OnStarted);
            appLifetime.ApplicationStopping.Register(OnStopping);
            appLifetime.ApplicationStopped.Register(OnStopped);

            // see if there's a different server location specified in config and use that instead
            string newLocation = this.cfg.GetSection("Server").Value;
            Uri newUri = null;
            if (!String.IsNullOrEmpty(newLocation) && Uri.TryCreate(newLocation, UriKind.Absolute, out newUri))
                wsLocation = newUri;

        }

        /// <summary>
        /// Starts the listener process
        /// </summary>
        public async void OnStarted()
        {
            //log.LogDebug("Starting websocket listener for address " + wsLocation);
            await Task.Run(Listen);
        }

        /// <summary>
        /// Signal the websocket listener process to stop
        /// </summary>
        public void OnStopping()
        {
            stopping = true;
        }

        /// <summary>
        /// Shut down the websocket client
        /// </summary>        
        public void OnStopped()
        {
            karafun.Dispose();
        }

        /// <summary>
        /// Repeatedly ask the websocket server for the status of the Karafun software. Should be called as its own thread.
        /// </summary>
        private void Listen()
        {
            // keep asking for updated status every second
            while (!stopping)
            {
                DateTime lastStatusUpdate = DateTime.Now.Subtract(new TimeSpan(0,0,3));

                // if there's stuff in the queue and the server is free, do the next task
                if (!requestQueue.IsEmpty && !InUse)
                {
                    Task request;
                    if(requestQueue.TryDequeue(out request))
                        request.Start();
                }
                
                // if it's been a while since the last status update, add one to the pile
                if (Status is null ||                    
                    (DateTime.Now.Subtract(lastStatusUpdate).TotalSeconds > 12 || 
                            (requestQueue.IsEmpty && DateTime.Now.Subtract(Status.Timestamp).TotalSeconds > 3)))
                {                    
                    GetStatus(callback: new Action<Status?>((Status? status) =>
                    {
                        Status = status;
                        lastStatusUpdate = System.DateTime.Now;
                    }));
                    lastStatusUpdate = System.DateTime.Now;
                }

                if (Status is null)
                    Thread.Sleep(3000);
                else
                    Thread.Sleep(50);
            }
        }

        /// <summary>
        /// Grab the data ready on the websocket server
        /// </summary>
        /// <returns>a string with an xml-formatted response from the server</returns>
        private async Task<string> GetData()
        {
            string response = "";
            byte[] buffer = new byte[1024];
            WebSocketReceiveResult inboundInfo = await karafun.ReceiveAsync(buffer, CancellationToken.None);
            response = Encoding.UTF8.GetString(buffer, 0, inboundInfo.Count);

            while (!inboundInfo.EndOfMessage)
            {
                inboundInfo = await karafun.ReceiveAsync(buffer, CancellationToken.None);
                response += Encoding.UTF8.GetString(buffer, 0, inboundInfo.Count);
            }

            return response;
        }


        /// <summary>
        /// Make a request of the websocket server
        /// </summary>
        /// <param name="request">A string of XML generated by one of the public command methods</param>
        /// <returns>The XML response of the server as an XmlDocument object</returns>
        private async Task<XmlDocument> Request(string request)
        {
            if (stopping) return null;

            // wait until the coast is clear
            while (InUse)
            {
                Thread.Sleep(20);
            }
            InUse = true;

            // need to reconnect and flush status data to send a request            
            try
            {
                await karafun.ConnectAsync(wsLocation, CancellationToken.None);
            }
            catch (WebSocketException ex)
            {
                return null;
            }
            await GetData();

            ArraySegment<byte> data = Encoding.UTF8.GetBytes(request);
            await karafun.SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None);
            string response = await GetData();

#if DEBUG
            //Debug.WriteLine(response);
#endif
            // need to dispose and recreate websocket client to start fresh next request or will get old data (unsure why)
            await karafun.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
            karafun.Dispose();
            karafun = new();
            InUse = false;
            
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(response);
            return xml;
        }

        /// <summary>
        /// Send a request for the Karafun player status
        /// </summary>
        /// <param name="noqueue">Omit the queue list from the XML response</param>
        /// <returns>The current status information from the Karafun software</returns>
        public void GetStatus(Action<Status?> callback, bool noqueue = false)
        {
            string message = $"<action type=\"getStatus\"{(noqueue ? " noqueue" : "")}></action>";

            requestQueue.Enqueue(new Task(async () =>
            {
                XmlDocument status = await Request(message);
                callback.Invoke(status is null ? null : new Status(status));
            }));
        }
        
        /// <summary>
        /// Get a list of the available song catalogs
        /// </summary>
        /// <returns>A list of song catalogs</returns>
        public void GetCatalogList(Action<List<Catalog>> callback)
        {
            string message = "<action type=\"getCatalogList\"></action>";

            requestQueue.Enqueue(new Task(async () =>
            {
                callback.Invoke(Catalog.ParseList(await Request(message)));
            }));
        }
        
        /// <summary>
        /// List songs from a particular catalog
        /// </summary>
        /// <param name="listId">ID of the catalog to list songs from</param>
        /// <param name="limit">The maximum number of songs to return</param>
        /// <param name="offset">The number of songs to skip</param>
        /// <returns>A list of song items</returns>
        public async Task<List<Song>> GetList(uint listId, uint limit = 100, uint offset = 0)
        {
            string message = $"<action type=\"getList\" id=\"{listId}\" offset=\"{ offset}\" limit=\"{limit}\"></action>";
            return Song.ParseList(await Request(message));
        }

        /// <summary>
        /// Search for a particular song or artist by string
        /// </summary>
        /// <param name="searchString">The search string to look for</param>
        /// <param name="limit">The maximum number of songs to return</param>
        /// <param name="offset">The number of results to skip</param>
        /// <returns>A list of song items</returns>
        public void Search(Action<List<Song>> callback, string searchString, uint limit = 10, uint offset = 0)
        {
            string message = $"<action type=\"search\" offset=\"{ offset}\" limit=\"{limit}\">{searchString}</action>";

            requestQueue.Enqueue(new Task(async () =>
            {
                callback.Invoke(Song.ParseList(await Request(message)));
            }));
        }
        
        /// <summary>
        /// Tell the Karafun player to start playing
        /// </summary>
        /// <returns>An updated status object</returns>
        public async Task<Status> Play()
        {
            string message = "<action type=\"play\"></action>";
            return new Status(await Request(message));
        }

        /// <summary>
        /// Tell the Karafun player to pause playback
        /// </summary>
        /// <returns>An updated status object</returns>
        public async Task<Status> Pause()
        {
            string message = "<action type=\"pause\"></action>";
            return new Status(await Request(message));
        }

        /// <summary>
        /// Tell the Karafun player to skip to the next song in the queue
        /// </summary>
        /// <returns>An updated status object</returns>
        public async Task<Status> Next()
        {
            string message = "<action type=\"next\"></action>";
            return new Status(await Request(message));
        }

        /// <summary>
        /// Tell the Karafun player to move to a different point in the current track
        /// </summary>
        /// <param name="time">The time (in seconds) to move to in the song</param>
        /// <returns>An updated status object</returns>
        public async Task<Status> Seek(uint time)
        {
            string message = $"<action type=\"seek\">{time}</action>";
            return new Status(await Request(message));
        }

        /// <summary>
        /// Tell the Karafun player to adjust the pitch of song playback
        /// </summary>
        /// <param name="pitch">Number of notes to shift the pitch from -6 to 6</param>
        /// <returns>An updated status object</returns>
        public async Task<Status> Pitch(sbyte pitch)
        {
            pitch = Math.Clamp(pitch, (sbyte)-6, (sbyte)6);
            string message = $"<action type=\"pitch\">{pitch}</action>";
            return new Status(await Request(message));
        }

        /// <summary>
        /// Tell the Karafun player to adjust the tempo of song playback
        /// </summary>
        /// <param name="tempo">Percentage to shift the speed of playback from -50 to 50</param>
        /// <returns>An updated status object</returns>
        public async Task<Status> Tempo(sbyte tempo)
        {
            tempo = Math.Clamp(tempo, (sbyte)-50, (sbyte)50);
            string message = $"<action type=\"tempo\">{tempo}</action>";
            return new Status(await Request(message));
        }

        /// <summary>
        /// Adjust one of the volume levels for song playback
        /// </summary>
        /// <param name="type">Which volume to change (use the current status object for the available types)</param>
        /// <param name="level">The level to set the volume to, from 0 to 100</param>
        /// <returns>An updated status object</returns>
        public async Task<Status> SetVolume(string type, byte level)
        {
            level = Math.Clamp(level, (byte)0, (byte)100);
            string message = $"<action type=\"setVolume\" volume_type=\"{type}\">{level}</action>";
            return new Status(await Request(message));
        }

        /// <summary>
        /// Remove all songs from the Karafun player's queue
        /// </summary>
        /// <returns>An updated status object</returns>
        public async Task<Status> ClearQueue()
        {
            string message = "<action type=\"clearQueue\"></action>";
            return new Status(await Request(message));
        }

        /// <summary>
        /// Add a song to the Karafun queue
        /// </summary>
        /// <param name="songId">Unique ID of the selected song</param>
        /// <param name="position">The place in the queue to insert this song. 0 is top, 99999 is bottom</param>
        /// <param name="singer">Optional name of the person who chose this song</param>
        /// <returns>An updated status object</returns>
        public async Task<Status> AddToQueue(uint songId, uint position = 99999, string singer = null)
        {
            if (position > 99999) position = 99999;
            string message = $"<action type=\"addToQueue\" song=\"{songId}\" singer=\"{singer}\">{position}</action>";
            return new Status(await Request(message));
        }

        /// <summary>
        /// Remove a song from the Karafun queue
        /// </summary>
        /// <param name="position">Queue position of the song to remove</param>
        /// <returns>An updated status object</returns>
        public async Task<Status> RemoveFromQueue(uint position)
        {
            if (position > 99999) position = 99999;
            string message = $"<action type=\"removeFromQueue\" id=\"{position}\"></action>";
            return new Status(await Request(message));
        }

        /// <summary>
        /// Move a song from one place in the queue to another
        /// </summary>
        /// <param name="oldPosition">The current queue position of the song to move</param>
        /// <param name="newPosition">The desired new position of the song, 0 for top 99999 for bottom</param>
        /// <returns>An updated status object</returns>
        public async Task<Status> ChangeQueuePosition(uint oldPosition, uint newPosition)
        {
            if (oldPosition > 99999) oldPosition = 99999;
            if (newPosition > 99999) newPosition = 99999;
            string message = $"<action type=\"changeQueuePosition\" id=\"{oldPosition}\">{newPosition}</action>";
            return new Status(await Request(message));
        }
    }

    public class StatusUpdateEventArgs : EventArgs
    {
        public Status Status { get; set; }
        
        public StatusUpdateEventArgs(Status s)
        {
            Status = s;
        }
    }
}
