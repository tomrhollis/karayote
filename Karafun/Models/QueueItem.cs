using System.ComponentModel.DataAnnotations;
using System.Xml;

namespace KarafunAPI.Models
{
    /// <summary>
    /// A song item from the queue listing in a status message.
    /// Does not contain a database ID. Contains queue status and singer name if available.
    /// </summary>
    public class QueueItem
    {
        /// <summary>
        /// The Queue ID (not database ID) of this song, ie its position in the queue
        /// </summary>
        [Range(0,99999)]
        public uint Id { get; internal set; }

        /// <summary>
        /// Possibilities for the status of this item
        /// </summary>
        public enum ItemStatus
        {
            Ready,
            Loading,
            Playing
        }

        /// <summary>
        /// The status of this item in the queue
        /// </summary>
        public ItemStatus? Status { get; internal set; }

        /// <summary>
        /// The title of this song
        /// </summary>
        public string Title { get; internal set; }

        /// <summary>
        /// The artist who originally performed this version of the song
        /// </summary>
        public string Artist { get; internal set; }
        
        /// <summary>
        /// The year the non-karaoke version of this song was published
        /// </summary>
        [Range(-9999, short.MaxValue)]
        public short Year { get; internal set; }

        /// <summary>
        /// The duration of this song in decimal seconds
        /// </summary>
        [Range(0, float.MaxValue)]
        public float Duration { get; internal set; }

        /// <summary>
        /// The name of the singer who selected this song
        /// </summary>
        public string Singer { get; internal set; }

        /// <summary>
        /// Constructor to create a <see cref="QueueItem"/> from XML
        /// </summary>
        /// <param name="n">The <see cref="XmlNode"/> with the info we need to make this</param>
        internal QueueItem(XmlNode n)
        {
            Id = uint.Parse(n.Attributes.GetNamedItem("id").Value);

            // find the status of this song
            switch (n.Attributes.GetNamedItem("status").Value.ToLower())
            {
                case "playing": Status = ItemStatus.Playing; break;
                case "ready": Status = ItemStatus.Ready; break;
                case "loading":                 
                default: Status = ItemStatus.Loading; break;
            }

            // process each other possible piece of data into its property
            foreach (XmlNode c in n.ChildNodes)
            {
                switch (c.Name)
                {
                    case "title":
                        Title = c.InnerText; break;
                    case "artist":
                        Artist = c.InnerText; break;
                    case "year":
                        Year = short.Parse(c.InnerText); break;
                    case "duration":
                        Duration = float.Parse(c.InnerText); break;
                    case "singer":
                        Singer = c.InnerText; break;
                    default: break;
                }
            }
        }

        /// <summary>
        /// Represent this <see cref="QueueItem"/> as readable text
        /// </summary>
        /// <returns>A formatted <see cref="string"/> showing the data in this object</returns>
        public override string ToString()
        {
            string output = $"{Id + 1}: [{Status}] {Artist} - {Title} | {Year} {Math.Floor(Duration / 60)}:{(Duration % 60):00}"
                + (!String.IsNullOrEmpty(Singer) ? $" ({Singer})" : string.Empty);
            return output;
        }
    }
}