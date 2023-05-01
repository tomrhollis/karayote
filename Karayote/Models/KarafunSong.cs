using KarafunAPI.Models;


namespace Karayote.Models
{
    internal class KarafunSong : ISelectedSong
    {
        Song Song { get; set; }

        string ISelectedSong.Id => Song.Id.ToString();

        public string Title => $"{Song.Artist} - {Song.Title}";

        KarafunSong(Song song)
        {
            Song = song;
        }
    }
}
