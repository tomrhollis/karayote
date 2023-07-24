using System.Xml;

namespace KarafunAPI.Models
{
    /// <summary>
    /// Represents the volumeList in the XML status response from Karafun
    /// </summary>
    // From the API documentation:
    //  <volumeList>
    //    <general caption = "{caption}" >{volume}</ general >
    //    [< bv caption = "{caption}" >{ volume}</ bv >]
    //    [<lead1 caption="{caption}" color="{color}">{volume}</ lead1 >]
    //    [<lead2 caption="{caption}" color="{color}">{volume}</ lead2 >]
    //  </ volumeList >
    public class VolumeControl
    {
        /// <summary>
        /// The overall volume control in Karafun
        /// </summary>
        public Volume General { get; internal set; }

        /// <summary>
        /// The control for Backing Vocal volume in Karafun, if it exists
        /// </summary>
        public Volume Bv { get; internal set; }

        /// <summary>
        /// The control for the primary Lead vocal volume in Karafun. Usually present but potentially optional
        /// </summary>
        public Volume Lead1 { get; internal set; }

        /// <summary>
        /// The control for the second Lead vocal volume in Karafun, if it exists
        /// </summary>
        public Volume Lead2 { get; internal set; }

        /// <summary>
        /// Constructor for a <see cref="VolumeControl"/> object
        /// </summary>
        /// <param name="n">The <see cref="XmlNode"/> to convert to this data structure</param>
        internal VolumeControl(XmlNode n)
        {
            if (n is null) return;
            foreach(XmlNode c in n?.ChildNodes)
            {
                switch (c.Name)
                {
                    case "general":
                        General = new(c); break;
                    case "bv":
                        Bv = new(c); break;
                    case "lead1":
                        Lead1 = new(c); break;
                    case "lead2":
                        Lead2 = new(c); break;
                    default: break;
                }
            }
        }

        /// <summary>
        /// Represent this object in string form
        /// </summary>
        /// <returns>A formatted <see cref="string"/> displaying all volume settings</returns>
        public override string ToString()
        {
            if (General is null) return string.Empty; // General is only null if we have no volume control data at all

            string output = "[Available Volume Controls]";
            output += "\n " + General.ToString();
            output += (Bv is not null) ? "\n " + Bv.ToString() : String.Empty;
            output += (Lead1 is not null) ? "\n " + Lead1.ToString() : String.Empty;
            output += (Lead2 is not null) ? "\n " + Lead2.ToString() : String.Empty;

            return output;
        }
    }
}
