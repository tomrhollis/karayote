using KarafunAPI.Models;

namespace Karayote.Models
{
    /// <summary>
    /// Represents a song found in Karafun
    /// </summary>
    internal class KarafunSong : SelectedSong
    {
        /// <summary>
        /// The song object used by the Karafun API
        /// </summary>
        Song Song { get; set; }

        /// <summary>
        /// The numerical internal id Karafun uses for this song, <see cref="string"/>ified
        /// </summary>
        public override string Id { get; set; } 

        /// <summary>
        /// A formatted title for this song made up of the artist's name and the name of the song
        /// </summary>
        public override string Title { get => $"{Song.Artist} - {Song.Title}"; }

        /// <summary>
        /// Construct a KarafunSong
        /// </summary>
        /// <param name="song">The <see cref="KarafunAPI.Models.Song"/> used by KarafunAPI to represent this selection</param>
        /// <param name="user">The <see cref="KarayoteUser"/> who selected this song</param>
        public KarafunSong(Song song, KarayoteUser user) : base(user)
        {
            Song = song;
            Id = Song.Id.ToString();
        }

        /// <summary>
        /// Generic constructor for database
        /// </summary>
        public KarafunSong() { }
    }
}
