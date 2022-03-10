using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;

namespace KarafunAPI
{
    public class Karafun
    {
        private ClientWebSocket karafun = new();
        private bool stopping = false;
        
        public Karafun()
        {
            Task.Run(Listen);
        }

        private async Task Listen()
        {
            byte[] buffer = new byte[1024];
            string message = "";

            karafun.ConnectAsync(new Uri("ws://localhost:57570"), CancellationToken.None).Wait();
            while ((karafun.State == WebSocketState.Open) && !stopping)
            {
                
                WebSocketReceiveResult inboundInfo = await karafun.ReceiveAsync(buffer, CancellationToken.None);
                message = Encoding.UTF8.GetString(buffer, 0, inboundInfo.Count);

                while (!inboundInfo.EndOfMessage)
                {
                    inboundInfo = await karafun.ReceiveAsync(buffer, CancellationToken.None);
                    message += Encoding.UTF8.GetString(buffer, 0, inboundInfo.Count);
                }

                Debug.WriteLine(message);
                Thread.Sleep(500);
            }
        }

        private void Stop()
        {
            stopping = true;
            karafun.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);            
        }
    }
}