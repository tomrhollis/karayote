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

        internal static List<Item> ParseList(XmlElement e)
        {
            throw new NotImplementedException();
        }
    }
}
