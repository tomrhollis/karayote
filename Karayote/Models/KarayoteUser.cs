using Botifex.Models;

namespace Karayote.Models
{
    internal class KarayoteUser
    {
        internal Guid Guid { get; private set; }

        internal KarayoteUser(BotifexUser botifexUser)
        {
            Guid = botifexUser.Guid; // for now
        }
    }
}
