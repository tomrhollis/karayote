using Botifex.Models;

namespace Karayote.Models
{
    public class KarayoteUser
    {
        internal Guid Id { get; private set; }
        internal string Name { get; private set; } = string.Empty;

        internal KarayoteUser(BotifexUser botifexUser)
        {
            Id = botifexUser.Guid; // for now
            Name = botifexUser.UserName;
        }
    }
}
