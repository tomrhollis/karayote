using Botifex.Models;

namespace Karayote.Models
{
    internal class KarayoteUser
    {
        internal Guid Guid { get; private set; }
        internal string Name { get; private set; } = string.Empty;

        internal KarayoteUser(BotifexUser botifexUser)
        {
            Guid = botifexUser.Guid; // for now
            Name = botifexUser.UserName;
        }
    }
}
