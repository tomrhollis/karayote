using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace KarafunAPI.Models
{
    public class Catalog
    {
        public uint Id { get; internal set; }

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

        public CatalogType Type { get; internal set; }

        public string Caption { get; internal set; }

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

        public override string ToString()
        {
            return $"{Id}) {Caption} ({Type})";
        }

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
