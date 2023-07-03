using Botifex;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Karayote.Models
{
    /// <summary>
    /// Provides a queue of <see cref="SelectedSong" />s in a form that objects can be easily swapped out or removed from the middle as needed />
    /// </summary>
    internal class SongQueue : ObservableObject
    {
        private static readonly object _lock = new object();
        private IBotifex botifex;

        /// <summary>
        /// Database ID of this object
        /// </summary>
        [Key]
        public int Id { get; set; }

        public int SessionId { get; set; }
        public Session Session { get; set; }


        /// <summary>
        /// Where all the queued songs are stored
        /// </summary>
        public ObservableCollection<SelectedSong> TheQueue { get; private set; } = new ObservableCollection<SelectedSong>();

        private string textVersion;
        /// <summary>
        /// A text representation of this to track if an update occurred and trigger update events
        /// </summary>
        public string TextVersion
        {
            get => textVersion;
            set
            {
                if(textVersion != value)
                    SetProperty(ref textVersion, value);
            }
        }

        /// <summary>
        /// The count of how many <see cref="SelectedSong"/>s are waiting to be sung
        /// </summary>
        public int Count { get => TheQueue.Count; }

        /// <summary>
        /// Return the currently active <see cref="SelectedSong"/>, if it exists
        /// </summary>
        internal SelectedSong? NowPlaying { get => TheQueue.Count > 0 ? TheQueue[0] : null; }

        /// <summary>
        /// Return the <see cref="SelectedSong"/> that's next up in line, if it exists
        /// </summary>
        internal SelectedSong? NextUp { get => TheQueue.Count > 1 ? TheQueue[1] : null; }

        /// <summary>
        /// Create a new <see cref="SongQueue"/>
        /// </summary>
        /// <param name="botifex">Injected <see cref="Botifex"/> service to send log messages to</param>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
                               // (it complains about textVersion not being set, but it does get set when TextVersion is set)
        public SongQueue(IBotifex botifex)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            this.botifex = botifex;
            // populate observable text representation
            TextVersion = ToString();
            
            // subscribe to collection changed event to update observable text version as needed
            TheQueue.CollectionChanged += (s, e) =>
            {
                TextVersion = ToString();
            };
        }

        /// <summary>
        /// Generic constructor for database
        /// </summary>
        public SongQueue() { }

        /// <summary>
        /// Checks to see if a user is already in the queue, and if not adds their selection
        /// Any checks to see if a song shouldn't be added should happen in the Session
        /// </summary>
        /// <param name="song">A <see cref="SelectedSong"/> from Karafun or Youtube song requested by a user</param>
        /// <returns><see cref="Task.CompletedTask"/></returns>
        internal async Task AddSong(SelectedSong song)
        {
            lock (_lock)
            {
                TheQueue.Add(song);
            }
            
            // send log message to console and messengers as backups in case DB restore fails or not implemeneted yet
            string logOutput = $"[{DateTime.Now.ToLocalTime().ToShortTimeString()}] New add: {song.UIString}";
            if (song is YoutubeSong)
                logOutput += $" {((YoutubeSong)song).Link}";            
            await botifex.LogAll(logOutput);
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
                hasSong = TheQueue.FirstOrDefault(s => s.Id == song.Id) is not null;
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
                hasUser = TheQueue.FirstOrDefault(s => s.User.Id == user.Id) is not null;
            }
            return hasUser;
        }

        /// <summary>
        /// Find a specific user's <see cref="SelectedSong" /> in the queue and return it with its position if it exists />
        /// </summary>
        /// <param name="user">The <see cref="KarayoteUser"/> who may have a song in the queue</param>
        /// <returns>A <see cref="Tuple"/> of <see cref="SelectedSong"/> and <see cref="int"/> if a song is found, null otherwise</returns>
        internal Tuple<SelectedSong, int>? GetUserSongWithPosition(KarayoteUser user)
        {
            lock (_lock)
            {
                SelectedSong? selectedSong = TheQueue.FirstOrDefault(s => s.User.Id == user.Id);
                if (selectedSong is null) return null;

                int position = TheQueue.IndexOf(selectedSong) + 1;
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
                SelectedSong? songToRemove = TheQueue.FirstOrDefault(s => s.User.Id == user.Id);
                if (songToRemove is not null)
                    TheQueue.Remove(songToRemove);

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
            lock (_lock)
            {
                try
                {
                    int position = TheQueue.IndexOf(TheQueue.First(s => s.User.Id == user.Id));
                    removedSong = TheQueue[position];
                    TheQueue[position] = newSong;
                }
                catch (ArgumentNullException)
                { }
                catch (InvalidOperationException)
                { }
            }
            return removedSong;
        }

        /// <summary>
        /// Moves a song from one place in the queue to another
        /// </summary>
        /// <param name="oldIndex">The zero-indexed position of the song to find in the queue</param>
        /// <param name="newIndex">The zero-indexed position to move the song to</param>
        public void MoveSong(int oldIndex, int newIndex)
        {
            lock (_lock)
            {
                oldIndex = Math.Clamp(oldIndex, 0, TheQueue.Count - 1);
                newIndex = Math.Clamp(newIndex, 0, TheQueue.Count - 1);           

                TheQueue.Move(oldIndex, newIndex);
            }
            
        }

        /// <summary>
        /// Remove the first song from the song queue and return it. This represents the currently active song being completed or canceled.
        /// </summary>
        /// <returns>The <see cref="SelectedSong"/> that was just completed, or <see cref="null"/> if the queue was empty</returns>
        public SelectedSong? Pop()
        {
            if (TheQueue.Count == 0) return null;
            lock (_lock)
            {
                SelectedSong song = TheQueue.First();
                TheQueue.RemoveAt(0);
                return song;
            }
        }

        /// <summary>
        /// Convert this object to a <see cref="string"/>, as you do
        /// </summary>
        /// <returns>A <see cref="string"/> listing the songs in the queue in a formatted way</returns>
        public override string ToString()
        {
            string queue = "SONG QUEUE\n" +
                           "----------\n";
            if (TheQueue.Count > 0)
            {
                int i = 1;
                lock (_lock)
                {
                    string position = "Now";
                    foreach (var song in TheQueue)
                    {
                        queue += $"{position}] {song}\n";
                        i++;
                        position = i == 2 ? "Next" : i.ToString();
                    }
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
