using Google.Apis.YouTube.v3.Data;
using System;
using System.Text.RegularExpressions;

namespace Karayote.Models
{
    /// <summary>
    /// Represents a Youtube song selection anywhere a song needs to be used or saved
    /// </summary>
    internal class YoutubeSong : SelectedSong
    {
        internal Video? Video = null;

        /// <summary>
        /// Return the 11-character YouTube video ID
        /// </summary>
        public override string Id { get; set; }

        /// <summary>
        /// Get the title of the Youtube video as it would display under the video on the actual site
        /// </summary>
        public override string Title { get => (Video is null) ? $"YouTube video with ID {Id}" : Video.Snippet.Title; }

        /// <summary>
        /// Get a link to this song on YouTube
        /// </summary>
        public string Link => $"https://www.youtube.com/watch?v={Id}";

        /// <summary>
        /// String representation for the UI, with type info
        /// </summary>
        public override string UIString => $"[YT] {base.ToString()}";

        /// <summary>
        /// Constructor for a new <see cref="YoutubeSong"/> based on an ID string
        /// </summary>
        /// <param name="id">The unique ID of the Youtube video in <see cref="string"/> form</param>
        /// <param name="user">The <see cref="KarayoteUser"/> who requested this video as a song</param>
        /// <exception cref="ArgumentException"></exception>
        public YoutubeSong(string id, KarayoteUser user) : base(user)
        {
            // this needs to be checked for validity against the YouTube API eventually, instead of assuming they'll always use 11 character ids
            if (id.Length != 11)
                throw new ArgumentException("Invalid YouTube Id");

            Id = id;
        }

        /// <summary>
        /// Constructor for a new <see cref="YoutubeSong"/> based on the URL of a youtube video
        /// </summary>
        /// <param name="uri">A <see cref="Uri"/> representing the location of a youtube video online</param>
        /// <param name="user">The <see cref="KarayoteUser"/> who requested this video as a song</param>
        /// <exception cref="ArgumentException"></exception>
        public YoutubeSong(Uri uri, KarayoteUser user) : base(user)
        {
            // extract the id
            Match idInLink = Regex.Match(uri.OriginalString, "(?<=watch\\?v=|/videos/|embed\\/|youtu.be\\/|\\/v\\/|watch\\?v%3D|%2Fvideos%2F|embed%2F|youtu.be%2F|%2Fv%2F)[^#\\&\\?\\n]*", RegexOptions.IgnoreCase);

            if (idInLink.Success && idInLink.Value.Length == 11)
                Id = idInLink.Value;

            else
                throw new ArgumentException("Invalid YouTube Link");
        }

        /// <summary>
        /// Generic constructor for database
        /// </summary>
        public YoutubeSong() { }
    }
}
