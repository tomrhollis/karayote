using Karayote.Models;

namespace Karayote
{
    internal class SongQueue
    {
        private List<SelectedSong> songQueue = new List<SelectedSong>();
        private static readonly object _lock = new object();

        public int Count { get => songQueue.Count; }

        public SongQueue() { }

        /// <summary>
        /// Checks to see if a user is already in the queue, and if not adds their selection
        /// Any checks to see if a song shouldn't be added should happen in the Session
        /// </summary>
        /// <param name="song">A Karafun or Youtube song selected by a user</param>
        internal void AddSong(SelectedSong song)
        {
            lock (_lock)
            {
                songQueue.Add(song);
            }
        }

        internal bool HasSong(SelectedSong song)
        {
            bool hasSong = false;
            lock (_lock)
            {
                hasSong = songQueue.FirstOrDefault(s => s.Id == song.Id) is not null;
            }
            return hasSong;
        }

        internal bool HasUser(KarayoteUser user)
        {
            bool hasUser = false;
            lock (_lock)
            {
                hasUser = songQueue.FirstOrDefault(s => s.User.Id == user.Id) is not null;
            }
            return hasUser;
        }

        internal Tuple<SelectedSong,int>? GetUserSong(KarayoteUser user)
        {
            lock (_lock)
            {
                SelectedSong? selectedSong = songQueue.FirstOrDefault(s => s.User.Id == user.Id);
                if (selectedSong is null) return null;

                int position = songQueue.ToList().IndexOf(selectedSong) + 1;
                return new Tuple<SelectedSong, int>(selectedSong, position);
            }            
        }

        internal SelectedSong? RemoveUserSong(KarayoteUser user)
        {
            lock (_lock)
            {
                SelectedSong? songToRemove = songQueue.FirstOrDefault(s => s.User.Id == user.Id);
                if (songToRemove is not null)
                    songQueue.Remove(songToRemove);

                return songToRemove;
            }
        }

        internal SelectedSong? ReplaceUserSong(KarayoteUser user, SelectedSong newSong)
        {
            SelectedSong? removedSong = null;
            lock(_lock)
            {
                try
                {
                    int position = songQueue.FindIndex(s => s.User.Id == user.Id);
                    removedSong = songQueue[position];
                    songQueue[position] = newSong;
                }
                catch (ArgumentNullException anx)
                { }
            }
            return removedSong;
        }


        /*
        internal SelectedSong SwapUserSong(KarayoteUser user, SelectedSong song)
        {
            //
        }
           */

        public override string ToString()
        {
            string queue = "SONG QUEUE\n" +
                           "----------\n";
            if (songQueue.Count > 0)
            {
                int i = 1;
                foreach (var song in songQueue)
                {
                    queue += $"{i}] {song}\n";
                    i++;
                }
            }
            else
            {
                queue += "Empty";
            }

            return queue.Trim();
        }
    }
}
