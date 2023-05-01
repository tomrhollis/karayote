using System.Text.RegularExpressions;

namespace Karayote
{
    internal class YoutubeSong
    {
        public string Id { get; private set; }
        public string Title { get; private set; } = string.Empty;
        public YoutubeSong(string id) 
        {
            // this needs to be checked for validity against the YouTube API eventually, instead of assuming they'll always use 11 character ids
            if (id.Length != 11)
                throw new ArgumentException("Invalid YouTube Id");

            Id = id;            
        }

        public YoutubeSong(Uri uri)
        {
            Match idInLink = Regex.Match(uri.OriginalString, "(?<=watch\\?v=|/videos/|embed\\/|youtu.be\\/|\\/v\\/|watch\\?v%3D|%2Fvideos%2F|embed%2F|youtu.be%2F|%2Fv%2F)[^#\\&\\?\\n]*{11}", RegexOptions.IgnoreCase);
            if (idInLink.Success && idInLink.Value.Length == 11)
                Id = idInLink.Value;
            else
                throw new ArgumentException("Invalid YouTube Link");
        }
    }
}
