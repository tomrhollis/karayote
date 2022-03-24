using Microsoft.AspNetCore.Mvc;

namespace Karayote.Models
{
    public class SongRequest
    {
        public int SongId { get; set; }
        public Song Song { get; set; }

        public string UserId { get; set; }
        public User User { get; set; }
                
        public DateTime Timestamp { get; set; }

        public SongRequest()
        {
            Timestamp = DateTime.Now;
        }

        public SongRequest(Song s, User u)
        {
            Song = s;
            User = u;
            Timestamp = DateTime.Now;
        }

        /* TODO: implement this with [FromServices] to inject User and Song repositories, to find by int
        public SongRequest(uint s, int u)
        {

        }*/
    }
}
