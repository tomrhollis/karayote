using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace KarafunAPI.Models
{
    public class Status
    {
        public enum PlayerState
        {
            Idle,
            Infoscreen,
            Loading,
            Playing
        }

        public PlayerState State { get; internal set; }

        [Range(0,float.MaxValue)]
        public float? Position { get; internal set; }

        [Range(-6,6)]
        public sbyte Pitch { get; internal set; }

        [Range(-50,50)]
        public sbyte Tempo { get; internal set; }

        public VolumeControl Volumes { get; internal set; }

        public List<QueueItem> SongQueue { get; internal set; }

        public DateTime Timestamp { get; private set; } = DateTime.Now;


        internal Status(XmlDocument xml)
        {
            Debug.WriteLine(xml.ToString());
            
            switch (xml.GetElementsByTagName("status")[0].Attributes.GetNamedItem("state").Value.ToLower())
            {
                case "infoscreen": State= PlayerState.Infoscreen; break;
                case "loading": State=PlayerState.Loading; break;
                case "playing": State=PlayerState.Playing; break;
                case "idle":
                default: State = PlayerState.Idle; break;
            }

            string position = xml.GetElementsByTagName("position")?[0].InnerText;
            if(!String.IsNullOrEmpty(position)) Position = float.Parse(position);

            Pitch = sbyte.Parse(xml.GetElementsByTagName("pitch")?[0].InnerText ?? "0");
            Tempo = sbyte.Parse(xml.GetElementsByTagName("tempo")?[0].InnerText ?? "0");

            Volumes = new(xml.GetElementsByTagName("volumeList")?[0]);

            SongQueue = new();
            if(xml.GetElementsByTagName("queue")?[0] is not null)
            {
                foreach (XmlNode n in xml.GetElementsByTagName("queue")?[0].ChildNodes)
                {
                    SongQueue.Add(new QueueItem(n));
                }
            }
        }

        public override string ToString()
        {
            string output = "===<PLAYER STATUS>===";
            output += $"\nStatus: {State}" + (Position is not null ? $" at {Position}s" : String.Empty);
            output += $"\nPitch: {Pitch} | Tempo: {Tempo}%";
            output += "\n" + Volumes.ToString();
            if(SongQueue.Count > 0)
            {
                output += "\n---[THE QUEUE]---";
                foreach(QueueItem s in SongQueue)
                {
                    output += "\n" + s.ToString();
                }
            }
            return output;
        }
    }
}
