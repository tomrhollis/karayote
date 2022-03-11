using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Xml;

namespace KarafunAPI.Models
{
    public class Volume
    {
        public string Name { get; internal set; }

        [Range (0,100)]
        public byte Level { get; internal set; }

        public string Caption { get; internal set; }

        public string Color { get; internal set; }


        internal Volume(XmlNode n)
        {
            Name = n.Name;
            Level = byte.Parse(n.InnerText ?? "0");
            Caption = n.Attributes.GetNamedItem("caption").Value;
            Color = n.Attributes.GetNamedItem("color")?.Value;
        }

        public override string ToString()
        {
            string output = $"> {Name}: {Level}%" + (Caption is not null ? $" ({Caption})" : string.Empty)
                                                  + (Color is not null ? $" Color: {Color}" : string.Empty);
            return output;
        }
    }
}