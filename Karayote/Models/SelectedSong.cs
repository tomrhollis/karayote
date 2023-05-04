using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Karayote.Models
{
    internal abstract partial class SelectedSong
    {
        internal abstract string Id { get; } // stringified uint for karafun, video id for youtube
        internal abstract string Title { get; } // artist and song name for karafun, page title for youtube

        internal KarayoteUser User { get; private protected set; }

        /*
        internal enum SelectionState
        {
            Reserved,
            Queued,
            Singing,
            SangToday
        }
        internal SelectionState State { get; set; }*/


        public override string ToString()
        {
            return $"{User.Name}: {Title}";
        }
    }
}
