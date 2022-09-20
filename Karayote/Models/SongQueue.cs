using KarafunAPI;
using System.Collections.Concurrent;

namespace Karayote.Models
{
    public class SongQueue : ISongQueue
    {
        private ILogger _logger;
        private IKarafun _karafun;

        public ConcurrentBag<ISongRequest> SongRequests { get; }

        public SongQueue(ILogger<SongQueue> logger, IKarafun karafun)
        {
            _logger = logger;
            _karafun = karafun;
            SongRequests = new ConcurrentBag<ISongRequest>();
        }
    }
}
