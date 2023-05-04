using Google.Apis.YouTube.v3.Data;
using System.Text.RegularExpressions;

namespace Karayote.Models
{
    internal class YoutubeSong : SelectedSong
    {
        internal Video? Video = null;
        private string id = "";
        internal override string Id { get=>id; }
        internal override string Title { get => (Video is null) ? $"YouTube video with ID {Id}" : Video.Snippet.Title; }


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
            this.id = id;
            User = user;
        }
    }
}
