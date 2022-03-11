using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml;

namespace KarafunAPI.Models
{
    /// <summary>
    /// A song item from the queue listing in a status message.
    /// Does not contain a database ID. Contains queue status and singer name if available.
    /// </summary>
    public class QueueItem
    {
        [Range(0,99999)]
        public uint Id { get; internal set; }

        public enum ItemStatus
        {
            Ready,
            Loading,
            Playing
        }

        public ItemStatus? Status { get; internal set; }

        public string Title { get; internal set; }

        public string Artist { get; internal set; }
        
        [Range(-9999, short.MaxValue)]
        public short Year { get; internal set; }

        [Range(0, float.MaxValue)]
        public float Duration { get; internal set; }

        public string Singer { get; internal set; }

        internal QueueItem(XmlNode n)
        {
            Id = uint.Parse(n.Attributes.GetNamedItem("id").Value);

            switch (n.Attributes.GetNamedItem("status").Value.ToLower())
            {
                case "playing": Status = ItemStatus.Playing; break;
                case "ready": Status = ItemStatus.Ready; break;
                case "loading":                 
                default: Status = ItemStatus.Loading; break;
            }

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

        public override string ToString()
        {
            string output = $"{Id + 1}: [{Status}] {Artist} - {Title} | {Year} {Duration}s" + (!String.IsNullOrEmpty(Singer) ? $" ({Singer})" : string.Empty);
            return output;
        }
    }
}