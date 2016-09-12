/// \section intro Program Introduction
/// - 
/// Major <b>Player.cs</b>
/// \details <b>Details</b>
/// - The players listing and times
///
/// <li>\author         Geun Young Gil & Marcus Rankin</li>
/// <li>\version        1.00.00</li>
/// <li>\date           2015.12.18</li>
/// <li>\pre            Nothing
/// <li>\warning        Nothing
/// <li>\copyright      KitchenSoft</li>
/// <ul>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace slidePuzzle_uwp
{
    // reference to https://msdn.microsoft.com/en-us/library/bb412179(v=vs.110).aspx
    [DataContract] // JSON
    public class Player
    {
        public int Rank { get; set; }   // only used for observable

        [DataMember] //JSON
        public TimeSpan PlayTime { get; set; }
        [DataMember] // JSON
        public string Name { get; set; }

        public Player(TimeSpan t, string name)
        {
            this.Rank = -1;
            this.PlayTime = t;
            this.Name = name;
        }

        public Player(int rank, TimeSpan t, string name)
        {
            this.Rank = rank;
            this.PlayTime = t;
            this.Name = name;
        }
    }

    public static class PlayerList
    {
        public static List<Player> GetPlayersEx()
        {
            List<Player> tmpList = new List<Player>();
            tmpList.Add(new Player(new TimeSpan(0, 0, 10), "Gil"));
            tmpList.Add(new Player(new TimeSpan(0, 0, 20), "Marcus"));
            tmpList.Add(new Player(new TimeSpan(0, 0, 30), "Jim"));
            tmpList.Add(new Player(new TimeSpan(0, 1, 10), "Taylor Swift"));
            tmpList.Add(new Player(new TimeSpan(0, 2, 10), "Holy Moly"));
            tmpList.Add(new Player(new TimeSpan(0, 2, 10), "This is the heaven"));
            tmpList.Add(new Player(new TimeSpan(0, 0, 40), "Meh!"));
            tmpList.Add(new Player(new TimeSpan(0, 0, 41), "Im the King"));

            return tmpList;
        }
    }
}
