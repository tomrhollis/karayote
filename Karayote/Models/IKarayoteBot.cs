using System.Threading;
using System.Threading.Tasks;

namespace Karayote.Models
{
    public interface IKarayoteBot
    {
        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
    }
}