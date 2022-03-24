using System.ComponentModel.DataAnnotations;

namespace Karayote.Models
{
    public class Song
    {
        public int Id { get; set; }
        public uint KarafunId { get; internal set; }
        public string Title { get; internal set; }
        public string Artist { get; internal set; }

        [Range(-4000, short.MaxValue)]
        public short Year { get; internal set; }

        [Range(0, float.MaxValue)]
        public float Duration { get; internal set; }

        public ICollection<SongRequest> SongRequests { get; set; }

        public Song() { }

        public Song(KarafunAPI.Models.Song song)
        {
            KarafunId = song.Id;
            Title = song.Title;
            Artist = song.Artist;
            Year = song.Year;
            Duration = song.Duration;
        }
    }
}
