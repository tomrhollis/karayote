using System.ComponentModel.DataAnnotations;
using System.Xml;

namespace KarafunAPI.Models
{
    /// <summary>
    /// A song item returned by a catalog listing or search. 
    /// Contains database ID, does not contain singer name, queue position or status
    /// </summary>
    public class Song
    {
        /// <summary>
        /// Database ID used by Karafun to represent this song
        /// </summary>
        public uint Id { get; internal set; }

        /// <summary>
        /// The name of the song
        /// </summary>
        public string Title { get; internal set; }

        /// <summary>
        /// The artist whose version of this song the karaoke song is based on
        /// </summary>
        public string Artist { get; internal set; }
        
        /// <summary>
        /// Year of publication of the original song
        /// </summary>
        [Range(-4000, short.MaxValue)]
        public short Year { get; internal set; }

        /// <summary>
        /// Duration of the song in decimal seconds
        /// </summary>
        [Range(0, float.MaxValue)]
        public float Duration { get; internal set; }

        /// <summary>
        /// Constructor to build a <see cref="Song"/> from XML
        /// </summary>
        /// <param name="n">The <see cref="XmlNode"/> used to create this object</param>
        public Song(XmlNode n)
        {
            Id = uint.Parse(n.Attributes["id"].Value); 
            foreach(XmlNode child in n.ChildNodes)
            {
                switch (child.Name)
                {
                    case "title": Title = child.InnerText; break;
                    case "artist": Artist = child.InnerText; break;
                    case "year": Year = short.Parse(child.InnerText); break;
                    case "duration": Duration = float.Parse(child.InnerText); break;
                    default: break;
                }
            }
        }

        /// <summary>
        /// Static method to parse XML into a <see cref="List"/> of <see cref="Song"/>s
        /// </summary>
        /// <param name="e">The <see cref="XmlDocument"/> coming straight from the GetList or Search methods of <see cref="Karafun"/></param>
        /// <returns>A <see cref="List"/> of <see cref="Song"/>s</returns>
        internal static List<Song> ParseList(XmlDocument e)
        {
            List<Song> list = new List<Song>();
            foreach (XmlNode node in e.GetElementsByTagName("item"))
            {
                list.Add(new Song(node));
            }
            return (list.Count > 0) ? list : null;
        }

        /// <summary>
        /// Represent this <see cref="Song"/> in text
        /// </summary>
        /// <returns>A formatted <see cref="string"/> of the data in this object</returns>
        public override string ToString()
        {
            return $"{Artist} - {Title} | {Year} {Math.Floor(Duration/60)}:{(Duration%60):00}"; // convert float seconds to m:ss
        }
    }
}
