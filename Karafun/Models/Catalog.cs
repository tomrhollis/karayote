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
            LocalDirectory
        }

        public CatalogType Type { get; internal set; }

        public string Caption { get; internal set; }

        internal static List<Catalog> ParseList(XmlElement e)
        {
            throw new NotImplementedException();
        }
    }
}
