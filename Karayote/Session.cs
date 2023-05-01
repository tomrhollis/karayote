
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

        public Session() 
        {
            
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
