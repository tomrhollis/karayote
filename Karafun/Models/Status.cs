using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Xml;

namespace KarafunAPI.Models
{
    /// <summary>
    /// Represents a full status report from Karafun
    /// </summary>
    /*  From the API documentation: 
     *  <status state="{player_state}">
          [<position>{time_in_seconds}</position>]
          <volumeList>
            <general caption="{caption}">{volume}</general>
            [<bv caption="{caption}">{volume}</bv>]
            [<lead1 caption="{caption}" color="{color}">{volume}</lead1>]
            [<lead2 caption="{caption}" color="{color}">{volume}</lead2>]
          </volumeList>
          <pitch>{pitch}</pitch>
          <tempo>{tempo}</tempo>
          <queue>
              <item id="{queue_position}" status="{item_state}">
              <title>{song_name}</title>
              <artist>{artist_name}</artist>
              <year>{year}</year>
              <duration>{duration_in_seconds}</duration>
              [<singer>{singer_name}</singer>]
            </item>
            ...
          </queue>
        </status>
     */
    public class Status
    {
        /// <summary>
        /// The possibilities for how Karafun will report its state
        /// </summary>
        public enum PlayerState
        {
            Idle,
            Infoscreen,
            Loading,
            Playing
        }

        /// <summary>
        /// The current state of the Karafun player
        /// </summary>
        public PlayerState State { get; internal set; }

        /// <summary>
        /// The current position of play in the active song, in decimal seconds
        /// </summary>
        [Range(0,float.MaxValue)]
        public float? Position { get; internal set; }

        /// <summary>
        /// The pitch adjustment in integers of notes, by default 0. Maximum 6 in either direction
        /// </summary>
        [Range(-6,6)]
        public sbyte Pitch { get; internal set; }

        /// <summary>
        /// The tempo adjustment as an integer percentage. Max 50 in either direction
        /// </summary>
        [Range(-50,50)]
        public sbyte Tempo { get; internal set; }

        /// <summary>
        /// The current <see cref="VolumeControl"/> for the active song
        /// </summary>
        public VolumeControl Volumes { get; internal set; }

        /// <summary>
        /// All the songs that are queued up within the karafun player. There should not be more than 100 songs in this list
        /// </summary>
        public List<QueueItem> SongQueue { get; internal set; }

        /// <summary>
        /// A timestamp for tracking when this status was taken
        /// </summary>
        public DateTime Timestamp { get; private set; } = DateTime.Now;

        /// <summary>
        /// Construct this <see cref="Status"/> based on the <see cref="XmlNode"/> from Karafun
        /// </summary>
        /// <param name="xml">The <see cref="XmlNode"/> that Karafun sent back</param>
        internal Status(XmlDocument xml)
        {
            // find the status tag on the root element and set the Status enum
            switch (xml.GetElementsByTagName("status")[0].Attributes.GetNamedItem("state").Value.ToLower())
            {
                case "infoscreen": State= PlayerState.Infoscreen; break;
                case "loading": State=PlayerState.Loading; break;
                case "playing": State=PlayerState.Playing; break;
                case "idle":
                default: State = PlayerState.Idle; break;
            }

            // time position in the song
            string position = xml.GetElementsByTagName("position")?[0].InnerText;
            if(!String.IsNullOrEmpty(position)) Position = float.Parse(position);

            Pitch = sbyte.Parse(xml.GetElementsByTagName("pitch")?[0].InnerText ?? "0");
            Tempo = sbyte.Parse(xml.GetElementsByTagName("tempo")?[0].InnerText ?? "0");

            Volumes = new(xml.GetElementsByTagName("volumeList")?[0]);

            // build the queue object from the list at the end of the xml status
            SongQueue = new();
            if(xml.GetElementsByTagName("queue")?[0] is not null)
            {
                foreach (XmlNode n in xml.GetElementsByTagName("queue")?[0].ChildNodes)
                {
                    SongQueue.Add(new QueueItem(n));
                }
            }
        }

        /// <summary>
        /// Represent the information in this object in a readable form. Should NOT include the timestamp for the status update hook to work properly
        /// </summary>
        /// <returns>A formatted <see cref="string"/> with all the relevant information about the Karafun <see cref="Status"/></returns>
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
