using Botifex.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Karayote.Models
{
    /// <summary>
    /// Represent a unique user of the Karayote service
    /// </summary>
    public class KarayoteUser
    {
        /// <summary>
        /// A unique ID assigned to this user
        /// </summary>
        [Key]
        internal string Id { get; private set; }

        /// <summary>
        /// The name of this user as we know it
        /// </summary>
        internal string Name { get; private set; } = string.Empty;

        /// <summary>
        /// The number of songs they are holding in reserve currently
        /// </summary>
        internal int ReservedSongCount { get => reservedSongs.Count; }

        internal BotifexUser? BotUser { get; private set; } = null;
        private List<SelectedSong> reservedSongs { get; set; }
        private readonly object _lock = new object(); // primitive thread safety but more flexibility needed than modern options allow

        internal static readonly int MAX_RESERVED_SONGS = 2;

        /// <summary>
        /// Construct a <see cref="KarayoteUser"/> based on a <see cref="BotifexUser"/>
        /// </summary>
        /// <param name="botifexUser">The <see cref="BotifexUser"/> who is interacting with this service</param>
        internal KarayoteUser(BotifexUser botifexUser)
        {
            Id = botifexUser.Guid.ToString(); // for now
            BotUser = botifexUser;
            Name = botifexUser.UserName;
            reservedSongs = new List<SelectedSong>();
        }

        /// <summary>
        /// Construct a <see cref="KarayoteUser"/> without tying it to a botifex account. For admin-controlled dummy users.
        /// </summary>
        /// <param name="name">The <see cref="string"/> text of a username</param>
        internal KarayoteUser(string name)
        {
            reservedSongs = new List<SelectedSong>();
            Id = Guid.NewGuid().ToString();
            Name = name;
        }

        /// <summary>
        /// Generic constructor for database
        /// </summary>
        public KarayoteUser() { }

        /// <summary>
        /// Add a <see cref="SelectedSong"/> to this user's reserved songs
        /// </summary>
        /// <param name="song">The <see cref="SelectedSong"/> to add</param>
        /// <returns>A <see cref="bool"/> indicating whether the addition was successful nor not</returns>
        internal bool AddReservedSong(SelectedSong song)
        {
            lock( _lock )
            {
                // fail if they have too many reserved songs
                if (reservedSongs.Count >= MAX_RESERVED_SONGS) return false;

                reservedSongs.Add(song);
                return true;
            }

        }

        /// <summary>
        /// Retrieve a list of this user's reserved songs
        /// </summary>
        /// <returns>A <see cref="List"/> of <see cref="SelectedSong"/>s in their reserve</returns>
        internal List<SelectedSong> GetReservedSongs() 
        {
            lock (_lock)
            {
                return new List<SelectedSong>(reservedSongs);
            }            
        }

        /// <summary>
        /// Retrive the reserved song at a particular priority position
        /// </summary>
        /// <param name="position">0-indexed <see cref="int"/> representing the song's position in the reserved songs list</param>
        /// <returns>The <see cref="SelectedSong"/> found at that position, or <see cref="null"/> if the position was invalid</returns>
        internal SelectedSong? GetSelectedSong(int position)
        {
            lock (_lock)
            {
                try
                {
                    return reservedSongs[position];
                }
                catch (ArgumentOutOfRangeException)
                {
                    return null;
                }
            }
        }
        
        /// <summary>
        /// Remove the song at a position
        /// </summary>
        /// <param name="position">The 0-indexed <see cref="int"/> position of the song in the reserved songs list</param>
        /// <returns>A <see cref="SelectedSong"/> if one was found at that position, otherwise <see cref="null"/></returns>
        internal SelectedSong? RemoveReservedSong(int position=0)
        {
            lock(_lock)
            {
                try
                {
                    SelectedSong removedSong = reservedSongs[position];
                    reservedSongs.RemoveAt(position);
                    return removedSong;
                }
                catch (ArgumentOutOfRangeException)
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Replace a reserved song with a new <see cref="SelectedSong"/>
        /// </summary>
        /// <param name="position">The <see cref="int"/> position in the list of reserved songs that should be replaced</param>
        /// <param name="song">The new <see cref="SelectedSong"/> to put in that position</param>
        /// <returns>The old <see cref="SelectedSong"/> that was removed from the specified position</returns>
        internal SelectedSong? ReplaceReservedSong(int position, SelectedSong song)
        {
            lock (_lock)
            {
                try
                {
                    SelectedSong oldSong = reservedSongs[position];
                    reservedSongs[position] = song;
                    return oldSong;
                } 
                catch (ArgumentOutOfRangeException)
                {
                    return null;
                }
            }            
        }

        /// <summary>
        /// Switch the songs at two positions in the reserved songs list
        /// </summary>
        /// <param name="position1">The <see cref="SelectedSong"/> at one of the positions in the reserve list</param>
        /// <param name="position2">The <see cref="SelectedSong"/> at another position</param>
        /// <returns>A <see cref="bool"/> indicating whether the operation was successful or not</returns>
        internal bool SwitchReservedSongs(int position1, int position2)
        {
            lock (_lock)
            {
                try
                {
                    SelectedSong placeholder = reservedSongs[position1];
                    reservedSongs[position1] = reservedSongs[position2];
                    reservedSongs[position2] = placeholder;
                    return true;
                }
                catch (ArgumentOutOfRangeException)
                {
                    return false;
                }
            }            
        }
    }
}
