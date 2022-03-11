using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace KarafunAPI.Models
{
    public class VolumeControl
    {
        public Volume General { get; internal set; }
        public Volume Bv { get; internal set; }
        public Volume Lead1 { get; internal set; }
        public Volume Lead2 { get; internal set; }

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

        public override string ToString()
        {
            if (General is null) return string.Empty;
            string output = "[Available Volume Controls]";
            output += "\n " + General.ToString();
            output += (Bv is not null) ? "\n " + Bv.ToString() : String.Empty;
            output += (Lead1 is not null) ? "\n " + Lead1.ToString() : String.Empty;
            output += (Lead2 is not null) ? "\n " + Lead2.ToString() : String.Empty;

            return output;
        }
    }
}
