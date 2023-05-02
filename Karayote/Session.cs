
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

        private bool noRepeats = true;

        public Session(bool norepeats = true) 
        {
            noRepeats = norepeats;
        }

        internal bool GetInLine(ISelectedSong song)
        {
            // eventually check first if other users have that song reserved
            if (noRepeats)
            {
                if (SongQueue.HasSong(song)) return false;
            }

            return SongQueue.AddSong(song);
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
