using KarafunAPI.Models;


namespace Karayote
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
