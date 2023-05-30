using System.Xml;

namespace KarafunAPI.Models
{
    /// <summary>
    /// Represent one of the catalogs of songs available within Karafun.
    /// </summary>
    //  Reported from GetCatalogList as an XML <catalogList> with members of the form <catalog id="{unique_id}" type="{type}">{caption}</item>
    public class Catalog
    {
        /// <summary>
        /// Internal Karafun database ID of this catalog
        /// </summary>
        public uint Id { get; internal set; }

        /// <summary>
        /// The possibilities for what type of catalog Karafun will report this to be
        /// </summary>
        public enum CatalogType
        {
            OnlineComplete,
            OnlineNews,
            OnlineFavorites,
            OnlineStyle,
            LocalPlaylist,
            LocalDirectory,
            Unknown
        }

        /// <summary>
        /// The <see cref="CatalogType"/> of this particular catalog
        /// </summary>
        public CatalogType Type { get; internal set; }

        /// <summary>
        /// A description of this catalog
        /// </summary>
        public string Caption { get; internal set; }

        /// <summary>
        /// Construct a <see cref="Catalog"/> object from XML
        /// </summary>
        /// <param name="n">The <see cref="XmlNode"/> to create this from</param>
        public Catalog(XmlNode n)
        {
            Id = uint.Parse(n.Attributes["id"].Value);
            Caption = n.InnerText;

            switch (n.Attributes["type"].Value)
            {
                case "onlineComplete": Type = CatalogType.OnlineComplete; break;
                case "onlineNews": Type=CatalogType.OnlineNews; break;
                case "onlineFavorites": Type= CatalogType.OnlineFavorites; break;
                case "onlineStyle": Type= CatalogType.OnlineStyle; break;
                case "localPlaylist": Type = CatalogType.LocalPlaylist; break;
                case "localDirectory": Type = CatalogType.LocalDirectory; break;
                default: Type = CatalogType.Unknown; break;
            }
        }

        /// <summary>
        /// Represent this <see cref="Catalog"/> object in text form
        /// </summary>
        /// <returns>A formatted <see cref="string"/> displaying this object's properties</returns>
        public override string ToString()
        {
            return $"{Id}) {Caption} ({Type})";
        }

        /// <summary>
        /// Parse a <see cref="List"/> of <see cref="Catalog"/>s from the XML response of GetCatalogList
        /// </summary>
        /// <param name="e">The <see cref="XmlDocument"/> directly from GetCatalogList</param>
        /// <returns>A <see cref="List"/> of the <see cref="Catalog"/>s Karafun has on offer</returns>
        internal static List<Catalog> ParseList(XmlDocument e)
        {
            List<Catalog> list = new List<Catalog>();
            foreach (XmlNode node in e.GetElementsByTagName("catalog"))
            {
                list.Add(new Catalog(node));
            }
            return (list.Count > 0) ? list : null;
        }
    }
}
