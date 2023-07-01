using System;

namespace Karayote.Models
{
    /// <summary>
    /// A class for songs added by the admins through the admin interface, which right now aren't tied to Karafun or Youtube.
    /// This class will eventually be deprecated in favor of functionality to look up Karafun and Youtube songs in the admin interface
    /// </summary>
    public class PlaceholderSong : SelectedSong
    {
        /// <summary>
        /// A random ID for this song to fulfil the requirements of the parent class
        /// </summary>
        public override string Id { get; set; }

        private string title;
        /// <summary>
        /// The title of this song as it should appear in the queue
        /// </summary>
        public override string Title { get => title; }

        /// <summary>
        /// Construct a <see cref="PlaceholderSong"/> with a user and a song title
        /// </summary>
        /// <param name="user">The <see cref="KarayoteUser"/> who asked to sing this song</param>
        /// <param name="title">The <see cref="string"/> representing the title of the song (preferably Artist - Title)</param>
        public PlaceholderSong(KarayoteUser user, string title) : base(user)
        {
            this.title = title;
            Id = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Generic constructor for database
        /// </summary>
        public PlaceholderSong() { }
    }
}
