using System.ComponentModel.DataAnnotations;
using System.Xml;

namespace KarafunAPI.Models
{
    /// <summary>
    /// Represent one volume control currently in use by Karafun
    /// </summary>
    public class Volume
    {
        /// <summary>
        /// The name of this control, either general, bv, lead1, or lead2
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// The percentage level this volume control is set to
        /// </summary>
        [Range (0,100)]
        public byte Level { get; internal set; }

        /// <summary>
        /// Any caption the song has included for this volume control
        /// </summary>
        public string Caption { get; internal set; }

        /// <summary>
        /// The color this vocal is set to on the screen. Only present for Lead1 and Lead2
        /// </summary>
        public string Color { get; internal set; }

        /// <summary>
        /// Constructor for a <see cref="Volume"/> object
        /// </summary>
        /// <param name="n">The <see cref="XmlNode"/> to parse to create this object</param>
        internal Volume(XmlNode n)
        {
            Name = n.Name;
            Level = byte.Parse(n.InnerText ?? "0");
            Caption = n.Attributes.GetNamedItem("caption").Value;
            Color = n.Attributes.GetNamedItem("color")?.Value;
        }

        /// <summary>
        /// Represent this object in a readable way
        /// </summary>
        /// <returns>A formatted <see cref="string"/> describing this volume setting</returns>
        public override string ToString()
        {
            string output = $"> {Name}: {Level}%" + (Caption is not null ? $" ({Caption})" : string.Empty)
                                                  + (Color is not null ? $" Color: {Color}" : string.Empty);
            return output;
        }
    }
}