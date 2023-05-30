
using Karayote.Models;

namespace Karayote
{
    /// <summary>
    /// A representation of the scheduled karaoke session for the night, tracking its information and state
    /// </summary>
    internal class Session
    {
        /// <summary>
        /// When the queue is scheduled to open for additions or actually was opened
        /// </summary>
        public DateTime? OpenTime { get; private set; }

        /// <summary>
        /// When the first song is scheduled to be sung, or actually was
        /// </summary>
        public DateTime? StartTime { get; private set; }

        /// <summary>
        /// When the karaoke event is scheduled to end, or actually did
        /// </summary>
        public DateTime? EndTime { get; private set; }

        /// <summary>
        /// Whether the queue is actually open for additions right now
        /// </summary>
        public bool IsOpen { get => (OpenTime is not null && DateTime.Now > OpenTime) && (EndTime is null || DateTime.Now < EndTime) && !QueueFull; }
        
        /// <summary>
        /// Whether the <see cref="SongQueue"/> is long enough to overrun the scheduled <see cref="EndTime"/>
        /// </summary>
        public bool QueueFull { get => false; } // placeholder until actual time-summing logic exists for the queue

        /// <summary>
        /// The <see cref="Karayote.SongQueue"/> holding the songs waiting to be sung at this event
        /// </summary>
        public SongQueue SongQueue { get; private set; } = new SongQueue();

        private List<SelectedSong> selectedSongs = new List<SelectedSong>();
        private bool noRepeats = true; // plaeholder for a potential future settings option.
                                       // Until then it's not allowed for multiple people to sing the same song in a session

        /// <summary>
        /// A list of possible outcomes of a song addition request for the calling method to refer to and make decisions on
        /// </summary>
        public enum SongAddResult
        {
            SuccessInQueue,
            SuccessInReserve,
            UserReserveFull,
            AlreadySelected,
            QueueClosed,
            UnknownFailure
        }

        /// <summary>
        /// Constructor to create a new <see cref="Session"/> for a scheduled karaoke event
        /// </summary>
        /// <param name="norepeats">Whether it should be allowed for different people to select the same song in a session</param>
        public Session(bool norepeats = true) 
        {
            noRepeats = norepeats;
        }

        /// <summary>
        /// Handle a user's request to sing a song by adding it to the song queue or to their personal reserved songs
        /// </summary>
        /// <param name="song">The <see cref="SelectedSong"/> a user wants to sing</param>
        /// <returns>The <see cref="SongAddResult"/> describing the outcome of this request</returns>
        internal SongAddResult GetInLine(SelectedSong song)
        {
            if (!IsOpen) return SongAddResult.QueueClosed;

            // eventually check first if other users have that song reserved or sung already
            if (noRepeats && selectedSongs.FirstOrDefault(s=>s.Id == song.Id) is not null)
                return SongAddResult.AlreadySelected;

            if (SongQueue.HasUser(song.User))
            {
                if (song.User.AddReservedSong(song))
                {
                    selectedSongs.Add(song);
                    return SongAddResult.SuccessInReserve;
                }
                else return SongAddResult.UserReserveFull;
            }

            SongQueue.AddSong(song);
            return SongAddResult.SuccessInQueue;
        }

        /// <summary>
        /// Remove one of a <see cref="KarayoteUser"/>'s selected songs
        /// </summary>
        /// <param name="user">The <see cref="KarayoteUser"/> requesting a song be removed</param>
        /// <param name="position">The position (starting at 1) of the song to remove. 1 being in the queue, higher being in their reserve</param>
        /// <returns>A <see cref="bool"/> whether the song was removed or not</returns>
        internal bool RemoveSong(KarayoteUser user, int position)
        {
            SelectedSong? removedSong = null;            

            // when the song is in the main queue
            if(position == 1)
            {
                // if user has no reserved songs simply remove this one
                if (user.ReservedSongCount == 0)
                    removedSong = SongQueue.RemoveUserSong(user);
                // replace the song with the first reserved song if it exists
                else
                    removedSong = SongQueue.ReplaceUserSong(user, user.RemoveReservedSong()!);
            }

            // when the song is in the user's reserve
            else if (position > 1 && (position - 1) < KarayoteUser.MAX_RESERVED_SONGS)
                removedSong = user.RemoveReservedSong(position - 2);

            // if it worked, clean the song out of the song history for this session so it can be selected again
            if (removedSong is not null)
            {
                selectedSongs.Remove(removedSong);
                return true;
            }
            return false; // if we got here it didn't work for some reason
        }

        /// <summary>
        /// Switch the order of two <see cref="SelectedSong"/>s a <see cref="KarayoteUser"/> has chosen
        /// </summary>
        /// <param name="user">The <see cref="KarayoteUser"/> who wants to switch the songs</param>
        /// <param name="position1">The <see cref="int"/> position of one of the songs to switch (1 is song in queue, 2 is position 0 in the reserve, 3 is position 1 etc)</param>
        /// <param name="position2">The <see cref="int"/> position of the other song to switch (1 is song in queue, 2 is position 0 in the reserve, 3 is position 1 etc)</param>
        /// <returns></returns>
        internal bool SwitchUserSongs(KarayoteUser user, int position1, int position2)
        {
            int earlierSongPosition = Math.Min(position1, position2);
            int laterSongPosition = Math.Max(position1, position2);

            // position 1 is in the main queue
            if (earlierSongPosition == 1)
            {
                // get the later song in the user's reserve that will be replaced
                SelectedSong? songFromReserve = user.GetSelectedSong(position2 - 2);
                
                // failed to find the reserve song
                if (songFromReserve is null)
                    return false;

                SelectedSong? songFromQueue = SongQueue.ReplaceUserSong(user, songFromReserve);
                
                // failed to replace a queued song
                if (songFromQueue is null)
                    return false;

                SelectedSong? replacedSong = user.ReplaceReservedSong(position2 - 2, songFromQueue);
                // if replacing the reserve song didn't work, revert the change to the queue
                if (replacedSong is null)
                {
                    SongQueue.ReplaceUserSong(user, songFromQueue);
                    return false;
                }
                return true;
            }

            // when both positions are in the user's reserve
            else
                return user.SwitchReservedSongs(position1 - 2, position2 - 2);
        }

        /// <summary>
        /// Open the queue early instead of at the scheduled time
        /// </summary>
        internal void Open()
        {
            OpenTime = DateTime.Now;
        }

        /// <summary>
        /// Start pulling songs from the queue for singing now instead of at the scheduled time
        /// </summary>
        internal void Start()
        {
            StartTime = DateTime.Now;
        }

        /// <summary>
        /// Close the queue to further submissions but keep the session going
        /// </summary>
        internal void Close()
        {
            // for closing off the queue, but may be undone now and then
        }

        /// <summary>
        /// Reopen the queue if it was closed early
        /// </summary>
        internal void Reopen()
        {
            // unsure if this will be used yet
        }

        /// <summary>
        /// End the session right now instead of at the scheduled time
        /// </summary>
        internal void End()
        {
            EndTime = DateTime.Now;
        }
    }
}
