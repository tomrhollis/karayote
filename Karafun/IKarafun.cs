using KarafunAPI.Models;

namespace KarafunAPI
{
    public interface IKarafun
    {
        Status Status { get; }

        Task<Status> AddToQueue(uint songId, uint position = 99999, string singer = null);
        Task<Status> ChangeQueuePosition(uint oldPosition, uint newPosition);
        Task<Status> ClearQueue();
        Task<List<Catalog>> GetCatalogList();
        Task<List<Item>> GetList(uint listId, uint limit = 100, uint offset = 0);
        Task<Status> GetStatus(bool noqueue = false);
        Task<Status> Next();
        Task<Status> Pause();
        Task<Status> Pitch(sbyte pitch);
        Task<Status> Play();
        Task<Status> RemoveFromQueue(uint position);
        Task<List<Item>> Search(string searchString, uint limit = 10, uint offset = 0);
        Task<Status> Seek(uint time);
        void Stop();
        void Start(string s = null);
        Task<Status> Tempo(sbyte tempo);
    }
}