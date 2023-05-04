using KarafunAPI.Models;


namespace Karayote.Models
{
    internal class KarafunSong : SelectedSong
    {
        Song Song { get; set; }

        internal override string Id { get => Song.Id.ToString(); } 

        internal override string Title { get => $"{Song.Artist} - {Song.Title}"; }

        public KarafunSong(Song song, KarayoteUser user)
        {
            Song = song;
            User = user;
        }


    }
}
