using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace KarafunAPI.Models
{
    /// <summary>
    /// A song item returned by a catalog listing or search. 
    /// Contains database ID, does not contain singer name, queue position or status
    /// </summary>
    public class Item
    {
        public uint Id { get; internal set; }
        public string Title { get; internal set; }
        public string Artist { get; internal set; }
        
        [Range(-4000, short.MaxValue)]
        public short Year { get; internal set; }

        [Range(0, float.MaxValue)]
        public float Duration { get; internal set; }

        public Item(XmlNode n)
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

        internal static List<Item> ParseList(XmlDocument e)
        {
            List<Item> list = new List<Item>();
            foreach (XmlNode node in e.GetElementsByTagName("item"))
            {
                list.Add(new Item(node));
            }
            return (list.Count > 0) ? list : null;
        }
    }
}
