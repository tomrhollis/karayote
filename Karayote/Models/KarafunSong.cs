namespace Karayote.Models
{
    public class KarafunSong : ISong
    {
        private KarafunAPI.Models.Song _song;

        public string Title => $"{_song.Artist } - {_song.Title }";

        public float Duration => _song.Duration;

        public KarafunSong(KarafunAPI.Models.Song song)
        {
            _song = song;
        }
    }
}
