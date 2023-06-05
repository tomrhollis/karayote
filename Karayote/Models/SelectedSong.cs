
using System;

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

        /// <summary>
        /// The time the song was sung, that is the singer finished singing with at least half of the song done
        /// </summary>
        internal DateTime? SungTime { get; private protected set; }

        /// <summary>
        /// Whether the song has already been sung or not
        /// </summary>
        internal bool WasSung { get => SungTime != null; } 

        /// <summary>
        /// Construct a new <see cref="SelectedSong"/> for a specific user
        /// </summary>
        /// <param name="user">The <see cref="KarayoteUser"/> who requested this song</param>
        internal SelectedSong(KarayoteUser user)
        {
            User = user;
        }

        /// <summary>
        /// Set the time the song was sung to flag that this selection was actually sung
        /// </summary>
        internal void SetSungTime()
        {
            SungTime = DateTime.Now;
        }

        /// <summary>
        /// Represent this <see cref="SelectedSong"/> in string form
        /// </summary>
        /// <returns>A default <see cref="string"/> representing the information in this object</returns>
        public override string ToString()
        {
            string sungTime = WasSung ? $" (Sung at {SungTime!.Value.ToLocalTime().Hour}:{SungTime!.Value.ToLocalTime().Minute})" : String.Empty;
            return $"{User.Name}: {Title}{sungTime}";
        }
    }
}
