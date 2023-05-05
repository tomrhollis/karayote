
using Karayote.Models;

namespace Karayote
{
    internal class Session
    {
        public DateTime? OpenTime { get; private set; }
        public DateTime? StartTime { get; private set; }
        public DateTime? EndTime { get; private set; }

        public bool IsOpen { get => (OpenTime is not null && DateTime.Now > OpenTime) && (EndTime is null || DateTime.Now < EndTime) && !QueueFull; }
        
        public bool QueueFull { get => false; } // placeholder

        public SongQueue SongQueue { get; private set; } = new SongQueue();
        private List<SelectedSong> selectedSongs { get; set; } = new List<SelectedSong>();

        private bool noRepeats = true;

        public enum SongAddResult
        {
            SuccessInQueue,
            SuccessInReserve,
            UserReserveFull,
            AlreadySelected,
            UnknownFailure
        }

        public Session(bool norepeats = true) 
        {
            noRepeats = norepeats;
        }

        internal SongAddResult GetInLine(SelectedSong song)
        {
            // eventually check first if other users have that song reserved
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

        internal bool RemoveSong(KarayoteUser user, int position)
        {
            SelectedSong? removedSong = null;            

            if(position == 1)
            {
                if (user.reservedSongs.Count == 0)
                    removedSong = SongQueue.RemoveUserSong(user);

                else
                    removedSong = SongQueue.ReplaceUserSong(user, user.RemoveReservedSong()!);
            }

            else if (position > 1 && (position - 1) < KarayoteUser.MAX_RESERVED_SONGS)
                removedSong = user.RemoveReservedSong(position - 2);

            if (removedSong is not null)
            {
                selectedSongs.Remove(removedSong);
                return true;
            }
            return false;
        }

        internal void Open()
        {
            OpenTime = DateTime.Now;
        }

        internal void Start()
        {
            StartTime = DateTime.Now;
        }

        internal void Close()
        {
            // for closing off the queue, but may be undone now and then
        }

        internal void Reopen()
        {
            // unsure if this will be used yet
        }

        internal void End()
        {
            EndTime = DateTime.Now;
        }
    }
}
