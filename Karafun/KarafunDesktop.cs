using KarafunAPI.Models;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Xml;

namespace KarafunAPI
{
    public class KarafunDesktop : IKarafun
    {
        public Status Status { get; private set; }
        internal bool InUse { get; private set; } = false;

        private bool stopping = false;
        private ClientWebSocket karafun = new();
        private readonly Uri wsLocation = new Uri("ws://localhost:57570");


        public KarafunDesktop()
        {
            Task.Run(Listen);
        }

        public void Stop()
        {
            stopping = true;
        }

        private async Task Listen()
        {
            while (!stopping)
            {
                if (!InUse) Status = await GetStatus();
                Debug.WriteLine(Status.ToString());
                Thread.Sleep(1000);
            }
            await karafun.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
        }

        private async Task<XmlDocument> Request(string request)
        {
            while (InUse) Thread.Sleep(100);
            InUse = true;

            if (!(karafun.State == WebSocketState.Open))
                karafun.ConnectAsync(wsLocation, CancellationToken.None).Wait();

            ArraySegment<byte> data = Encoding.UTF8.GetBytes(request);
            await karafun.SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None);
            
            byte[] buffer = new byte[1024];
            string response = "";

            WebSocketReceiveResult inboundInfo = await karafun.ReceiveAsync(buffer, CancellationToken.None);
            response = Encoding.UTF8.GetString(buffer, 0, inboundInfo.Count);

            while (!inboundInfo.EndOfMessage)
            {
                inboundInfo = await karafun.ReceiveAsync(buffer, CancellationToken.None);
                response += Encoding.UTF8.GetString(buffer, 0, inboundInfo.Count);
            }
#if DEBUG
            //Debug.WriteLine(response);
#endif
            InUse = false;
            
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(response);
            return xml;
        }

        public async Task<Status> GetStatus(bool noqueue = false)
        {
            string message = $"<action type=\"getStatus\"{(noqueue ? " noqueue" : "")}></action>";
            return new Status(await Request(message));
        }
        
        public async Task<List<Catalog>> GetCatalogList()
        {
            string message = "<action type=\"getCatalogList\"></action>";
            return Catalog.ParseList(await Request(message));
        }
        
        public async Task<List<Item>> GetList(uint listId, uint limit = 100, uint offset = 0)
        {
            string message = $"<action type=\"getList\" id=\"{listId}\" offset=\"{ offset}\" limit=\"{limit}\"></action>";
            return Item.ParseList(await Request(message));
        }

        public async Task<List<Item>> Search(string searchString, uint limit = 10, uint offset = 0)
        {
            string message = $"<action type=\"search\" offset=\"{ offset}\" limit=\"{limit}\">{searchString}</action>";
            return Item.ParseList(await Request(message));
        }
        
        public async Task<Status> Play()
        {
            string message = "<action type=\"play\"></action>";
            return new Status(await Request(message));
        }

        public async Task<Status> Pause()
        {
            string message = "<action type=\"pause\"></action>";
            return new Status(await Request(message));
        }
        public async Task<Status> Next()
        {
            string message = "<action type=\"next\"></action>";
            return new Status(await Request(message));
        }
        public async Task<Status> Seek(uint time)
        {
            string message = $"<action type=\"seek\">{time}</action>";
            return new Status(await Request(message));
        }
        public async Task<Status> Pitch(sbyte pitch)
        {
            pitch = Math.Clamp(pitch, (sbyte)-6, (sbyte)6);
            string message = $"<action type=\"pitch\">{pitch}</action>";
            return new Status(await Request(message));
        }
        public async Task<Status> Tempo(sbyte tempo)
        {
            tempo = Math.Clamp(tempo, (sbyte)-50, (sbyte)50);
            string message = $"<action type=\"tempo\">{tempo}</action>";
            return new Status(await Request(message));
        }

        public async Task<Status> SetVolume(string type, byte level)
        {
            level = Math.Clamp(level, (byte)0, (byte)100);
            string message = $"<action type=\"setVolume\" volume_type=\"{type}\">{level}</action>";
            return new Status(await Request(message));
        }

        public async Task<Status> ClearQueue()
        {
            string message = "<action type=\"clearQueue\"></action>";
            return new Status(await Request(message));
        }

        public async Task<Status> AddToQueue(uint songId, uint position = 99999, string singer = null)
        {
            if (position > 99999) position = 99999;
            string message = $"<action type=\"addToQueue\" song=\"{songId}\" singer=\"{singer}\">{position}</action>";
            return new Status(await Request(message));
        }

        public async Task<Status> RemoveFromQueue(uint position)
        {
            if (position > 99999) position = 99999;
            string message = $"<action type=\"removeFromQueue\" id=\"{position}\"></action>";
            return new Status(await Request(message));
        }

        public async Task<Status> ChangeQueuePosition(uint oldPosition, uint newPosition)
        {
            if (oldPosition > 99999) oldPosition = 99999;
            if (newPosition > 99999) newPosition = 99999;
            string message = $"<action type=\"changeQueuePosition\" id=\"{oldPosition}\">{newPosition}</action>";
            return new Status(await Request(message));
        }

    }
}
