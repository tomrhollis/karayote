using Karayote.Models;
using System.Collections.Concurrent;

namespace Karayote
{
    internal class SongQueue
    {
        private ConcurrentQueue<ISelectedSong> songQueue = new ConcurrentQueue<ISelectedSong>();

        public SongQueue() { }

        /// <summary>
        /// Checks to see if a user is already in the queue, and if not adds their selection
        /// Any checks to see if a song has already been sung should be done in the Session, which is keeping track of that
        /// </summary>
        /// <param name="song">A Karafun or Youtube song selected by a user</param>
        /// <returns>Whether the song was added to the queue or not</returns>
        internal bool AddSong(ISelectedSong song)
        {
            if (songQueue.FirstOrDefault(s => s.User == song.User) is not null)
                return false;

            songQueue.Enqueue(song);
            return true;
        }

        public override string ToString()
        {
            string queue = "SONG QUEUE\n" +
                           "----------\n";
            foreach (var song in songQueue)
            {
                queue += song.ToString() + "\n";
            }
            return queue.Trim();
        }
    }
}
