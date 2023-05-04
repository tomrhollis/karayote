using Karayote.Models;
using System.Collections.Concurrent;

namespace Karayote
{
    internal class SongQueue
    {
        private ConcurrentQueue<ISelectedSong> songQueue = new ConcurrentQueue<ISelectedSong>();
        public int Count { get => songQueue.Count; }

        public SongQueue() { }

        /// <summary>
        /// Checks to see if a user is already in the queue, and if not adds their selection
        /// Any checks to see if a song has already been sung should be done in the Session, which is keeping track of that
        /// </summary>
        /// <param name="song">A Karafun or Youtube song selected by a user</param>
        /// <returns>Whether the song was added to the queue or not</returns>
        internal bool AddSong(ISelectedSong song)
        {
            if (HasSong(song) || HasUser(song.User))
                return false;

            songQueue.Enqueue(song);
            return true;
        }

        internal bool HasSong(ISelectedSong song)
        {
            return (songQueue.FirstOrDefault(s => s.Id == song.Id) is not null) ? true : false;
        }

        internal bool HasUser(KarayoteUser user)
        {
            return (songQueue.FirstOrDefault(s=>s.User.Id == user.Id) is not null) ? true: false;
        }

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
