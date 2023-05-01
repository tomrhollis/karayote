using KarafunAPI.Models;


namespace Karayote.Models
{
    internal class KarafunSong : ISelectedSong
    {
        Song Song { get; set; }

        string ISelectedSong.Id => Song.Id.ToString();

        public string Title => $"{Song.Artist} - {Song.Title}";

        public KarayoteUser User { get; set; }

        KarafunSong(Song song, KarayoteUser user)
        {
            Song = song;
            User = user;
        }

        public override string ToString()
        {
            return $"{User.Name}] {Title}";
        }
    }
}
