
using System;

namespace Karayote.Models
{
    /// <summary>
    /// Abstraction encompassing songs that can be queued from any source
    /// </summary>
    public abstract partial class SelectedSong
    {
        /// <summary>
        /// The unique ID of this song, converted to string if necessary
        /// </summary>
        public abstract string Id { get; }

        /// <summary>
        /// The title of the song. Recommend using some kind of similar format when possible
        /// </summary>
        public abstract string Title { get; }

        /// <summary>
        /// The <see cref="KarayoteUser"/> who requested this song
        /// </summary>
        public KarayoteUser User { get; private protected set; }

        /// <summary>
        /// The time the song was sung, that is the singer finished singing with at least half of the song done
        /// </summary>
        public DateTime? SungTime { get; private protected set; }

        /// <summary>
        /// Whether the song has already been sung or not
        /// </summary>
        public bool WasSung { get => SungTime != null; } 

        /// <summary>
        /// Construct a new <see cref="SelectedSong"/> for a specific user
        /// </summary>
        /// <param name="user">The <see cref="KarayoteUser"/> who requested this song</param>
        public SelectedSong(KarayoteUser user)
        {
            User = user;
        }

        /// <summary>
        /// Set the time the song was sung to flag that this selection was actually sung
        /// </summary>
        public void SetSungTime()
        {
            SungTime = DateTime.Now;
        }

        /// <summary>
        /// Represent this <see cref="SelectedSong"/> in string form
        /// </summary>
        /// <returns>A default <see cref="string"/> representing the information in this object</returns>
        public override string ToString()
        {
            // add the username to the front in all cases and the time it was sung to the end of the string if that already happened
            string sungTime = WasSung ? $" (Sung at {SungTime!.Value.ToLocalTime().Hour}:{SungTime!.Value.ToLocalTime().Minute})" : String.Empty;
            return $"{User.Name}: {Title}{sungTime}";
        }
    }
}
