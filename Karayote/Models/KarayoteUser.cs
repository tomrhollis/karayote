using Botifex.Models;

namespace Karayote.Models
{
    public class KarayoteUser
    {
        internal Guid Id { get; private set; }
        internal string Name { get; private set; } = string.Empty;

        internal List<SelectedSong> reservedSongs { get; private set; }
        private readonly object _lock = new object();

        internal static readonly int MAX_RESERVED_SONGS = 2;

        internal KarayoteUser(BotifexUser botifexUser)
        {
            Id = botifexUser.Guid; // for now
            Name = botifexUser.UserName;
            reservedSongs = new List<SelectedSong>();
        }

        /// <summary>
        /// 
        /// Since the Session checks for whether a song is already selected, this method does not.
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>
        internal bool AddReservedSong(SelectedSong song)
        {
            lock( _lock )
            {
                if (reservedSongs.Count >= MAX_RESERVED_SONGS) return false;

                reservedSongs.Add(song);
                return true;
            }

        }

        internal List<SelectedSong> GetReservedSongs() 
        {
            lock (_lock)
            {
                return new List<SelectedSong>(reservedSongs);
            }            
        }

        internal SelectedSong? RemoveReservedSong(int position=0)
        {
            lock(_lock)
            {
                try
                {
                    SelectedSong removedSong = reservedSongs[position];
                    reservedSongs.RemoveAt(position);
                    return removedSong;
                }
                catch (ArgumentOutOfRangeException aorx)
                {
                    return null;
                }
            }
        }
    }
}
