using Botifex.Models;
using System.Collections.Concurrent;

namespace Karayote.Models
{
    public class KarayoteUser
    {
        internal Guid Id { get; private set; }
        internal string Name { get; private set; } = string.Empty;

        internal ConcurrentDictionary<int, SelectedSong> reservedSongs { get; private set; }

        private readonly int MAX_RESERVED_SONGS = 2;

        internal KarayoteUser(BotifexUser botifexUser)
        {
            Id = botifexUser.Guid; // for now
            Name = botifexUser.UserName;
            reservedSongs = new ConcurrentDictionary<int, SelectedSong>();
        }

        /// <summary>
        /// 
        /// Since the Session checks for whether a song is already selected, this method does not.
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>
        internal bool AddReservedSong(SelectedSong song)
        {
            if (reservedSongs.Count >= MAX_RESERVED_SONGS) return false;

            return reservedSongs.TryAdd(reservedSongs.Count, song);
        }

        internal IEnumerable<SelectedSong> GetReservedSongs() 
        { 
            for(int i=0; i<reservedSongs.Count; i++)
            {
                yield return reservedSongs[i];
            }
        }
    }
}
