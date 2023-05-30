using KarafunAPI.Models;

namespace KarafunAPI
{
    /// <summary>
    /// Interface for the Karafun API
    /// </summary>
    public interface IKarafun
    {
        Status Status { get; }

        event EventHandler<StatusUpdateEventArgs> OnStatusUpdated;

        void AddToQueue(uint songId, uint position = 99999, string singer = null);
        void ChangeQueuePosition(uint oldPosition, uint newPosition);
        void ClearQueue();
        void GetCatalogList(Action<List<Catalog>> callback);
        void GetList(Action<List<Song>> callback, uint listId, uint limit = 100, uint offset = 0);
        void GetStatus(Action<Status?> callback, bool noqueue = false);
        void Next();
        void Pause();
        void Pitch(sbyte pitch);
        void Play();
        void RemoveFromQueue(uint position);
        void Search(Action<List<Song>> callback, string searchString, uint limit = 10, uint offset = 0);
        void Seek(uint time);
        void OnStopping();
        void OnStarted();
        void Tempo(sbyte tempo);
    }
}