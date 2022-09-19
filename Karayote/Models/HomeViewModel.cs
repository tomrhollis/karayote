using KarafunAPI.Models;
using System.Collections.Concurrent;

namespace Karayote.Models
{
    public class HomeViewModel
    {
        public Status Status { get; }

        public HomeViewModel(Status s)
        {
            Status = s;
        }
    }
}
