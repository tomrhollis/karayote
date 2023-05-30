﻿using Karayote.Models;

namespace Karayote
{
    /// <summary>
    /// Provides a queue of <see cref="SelectedSong" />s in a form that objects can be easily swapped out or removed from the middle as needed />
    /// </summary>
    internal class SongQueue
    {
        private List<SelectedSong> songQueue = new List<SelectedSong>();
        private static readonly object _lock = new object();

        /// <summary>
        /// The count of how many <see cref="SelectedSong"/>s are waiting to be sung
        /// </summary>
        public int Count { get => songQueue.Count; }

        /// <summary>
        /// Default constructor to create a new <see cref="SongQueue"/>
        /// </summary>
        public SongQueue() { }

        /// <summary>
        /// Checks to see if a user is already in the queue, and if not adds their selection
        /// Any checks to see if a song shouldn't be added should happen in the Session
        /// </summary>
        /// <param name="song">A <see cref="SelectedSong"/> from Karafun or Youtube song requested by a user</param>
        internal void AddSong(SelectedSong song)
        {
            lock (_lock)
            {
                songQueue.Add(song);
            }
        }

        /// <summary>
        /// Checks if the song is already in the queue
        /// </summary>
        /// <param name="song">The <see cref="SelectedSong"/> to check the queue against</param>
        /// <returns>True if the song exists, false if it does not</returns>
        internal bool HasSong(SelectedSong song)
        {
            bool hasSong = false;
            lock (_lock)
            {
                hasSong = songQueue.FirstOrDefault(s => s.Id == song.Id) is not null;
            }
            return hasSong;
        }

        /// <summary>
        /// Checks if a user is already in the queue. Karayote does not allow a user to have more than one song in the queue
        /// </summary>
        /// <param name="user">The <see cref="KarayoteUser"/> to check for presence in the queue</param>
        /// <returns>True if the user exists in the queue, false if not</returns>
        internal bool HasUser(KarayoteUser user)
        {
            bool hasUser = false;
            lock (_lock)
            {
                hasUser = songQueue.FirstOrDefault(s => s.User.Id == user.Id) is not null;
            }
            return hasUser;
        }

        /// <summary>
        /// Find a specific user's <see cref="SelectedSong" /> in the queue and return it with its position if it exists />
        /// </summary>
        /// <param name="user">The <see cref="KarayoteUser"/> who may have a song in the queue</param>
        /// <returns>A <see cref="Tuple"/> of <see cref="SelectedSong"/> and <see cref="int"/> if a song is found, null otherwise</returns>
        internal Tuple<SelectedSong,int>? GetUserSongWithPosition(KarayoteUser user)
        {
            lock (_lock)
            {
                SelectedSong? selectedSong = songQueue.FirstOrDefault(s => s.User.Id == user.Id);
                if (selectedSong is null) return null;

                int position = songQueue.IndexOf(selectedSong) + 1;
                return new Tuple<SelectedSong, int>(selectedSong, position);
            }            
        }

        /// <summary>
        /// Remove a specific user's song from the queue
        /// </summary>
        /// <param name="user">The <see cref="KarayoteUser"/> to check the queue for</param>
        /// <returns>The <see cref="SelectedSong"/> that was removed</returns>
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

        /// <summary>
        /// Replace a specific <see cref="KarayoteUser"/>'s <see cref="SelectedSong"/> with a different one. If that user isn't in the queue, nothing happens.
        /// </summary>
        /// <param name="user">The <see cref="KarayoteUser"/> to check the queue for</param>
        /// <param name="newSong">The <see cref="SelectedSong"/> that should now be in the queue instead</param>
        /// <returns>The <see cref="SelectedSong"/> that was replace, null if there was nothing to replace</returns>
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
                catch (ArgumentNullException)
                { }
            }
            return removedSong;
        }

        /// <summary>
        /// Convert this object to a <see cref="string"/>, as you do
        /// </summary>
        /// <returns>A <see cref="string"/> listing the songs in the queue in a formatted way</returns>
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
