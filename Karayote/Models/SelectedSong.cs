
namespace Karayote.Models
{
    /// <summary>
    /// Abstraction encompassing songs that can be queued from any source
    /// </summary>
    internal abstract partial class SelectedSong
    {
        /// <summary>
        /// The unique ID of this song, converted to string if necessary
        /// </summary>
        internal abstract string Id { get; }

        /// <summary>
        /// The title of the song. Recommend using some kind of similar format when possible
        /// </summary>
        internal abstract string Title { get; }

        /// <summary>
        /// The <see cref="KarayoteUser"/> who requested this song
        /// </summary>
        internal KarayoteUser User { get; private protected set; }

        /*
        internal enum SelectionState
        {
            Reserved,
            Queued,
            Singing,
            SangToday
        }
        internal SelectionState State { get; set; }*/

        /// <summary>
        /// Construct a new <see cref="SelectedSong"/> for a specific user
        /// </summary>
        /// <param name="user">The <see cref="KarayoteUser"/> who requested this song</param>
        internal SelectedSong(KarayoteUser user)
        {
            User = user;
        }

        /// <summary>
        /// Represent this <see cref="SelectedSong"/> in string form
        /// </summary>
        /// <returns>A default <see cref="string"/> representing the information in this object</returns>
        public override string ToString()
        {
            return $"{User.Name}: {Title}";
        }
    }
}
