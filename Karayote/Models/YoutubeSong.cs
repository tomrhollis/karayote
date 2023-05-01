using System.Text.RegularExpressions;

namespace Karayote.Models
{
    internal class YoutubeSong : ISelectedSong
    {
        public string Id { get; private set; }
        public string Title { get; private set; } = string.Empty;
        public KarayoteUser User { get; private set; }

        public YoutubeSong(string id, KarayoteUser user)
        {
            // this needs to be checked for validity against the YouTube API eventually, instead of assuming they'll always use 11 character ids
            if (id.Length != 11)
                throw new ArgumentException("Invalid YouTube Id");

            SetProperties(id, user);
        }

        public YoutubeSong(Uri uri, KarayoteUser user)
        {
            Match idInLink = Regex.Match(uri.OriginalString, "(?<=watch\\?v=|/videos/|embed\\/|youtu.be\\/|\\/v\\/|watch\\?v%3D|%2Fvideos%2F|embed%2F|youtu.be%2F|%2Fv%2F)[^#\\&\\?\\n]*", RegexOptions.IgnoreCase);
            
            if (idInLink.Success && idInLink.Value.Length == 11)
                SetProperties(idInLink.Value, user);

            else
                throw new ArgumentException("Invalid YouTube Link");
        }

        private void SetProperties(string id, KarayoteUser user)
        {
            Id = id;
            User = user;
            LoadYoutubeTitle();
        }

        private void LoadYoutubeTitle()
        {
            Title = "Placeholder for Youtube Title";
        }

        public override string ToString()
        {
            return $"{User.Name}] {Title}";
        }
    }
}
