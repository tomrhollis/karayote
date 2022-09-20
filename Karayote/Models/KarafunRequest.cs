namespace Karayote.Models
{
    public class KarafunRequest : ISongRequest
    {
        public DateTime RequestTime { get; }

        public string UserId { get; set; }
        public User User { get; }

        public string? ExtraName { get; set; }
        public ISong Song { get ; }

        public KarafunRequest(User u, KarafunSong s)
        {
            RequestTime = DateTime.Now;
            User = u;
            Song = s;
        }

        // if changing song, construct a new request and add it to the queue with the old request time
        public KarafunRequest(ISongRequest r, KarafunSong ns, string nxn = null)
        {
            RequestTime = r.RequestTime;
            User = r.User;
            ExtraName = nxn ?? r.ExtraName;
            Song = ns;
        }
    }
}
