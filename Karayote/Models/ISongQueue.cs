using System.Collections.Concurrent;

namespace Karayote.Models
{
    public interface ISongQueue
    {
        ConcurrentBag<ISongRequest> SongRequests { get; }

    }
}
