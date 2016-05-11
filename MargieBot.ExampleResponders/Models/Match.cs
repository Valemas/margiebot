using System;
using System.Collections.Generic;

namespace MargieBot.ExampleResponders.Models
{
    public class Match
    {
        public Match()
        {
            AllPlayers = new List<Player>();
        }
        public List<Player> AllPlayers { get; set; }
        public Team Team1 { get; set; }
        public Team Team2 { get; set; }
        public DateTime MatchDate { get; set; }
        public int MatchId { get; set; }
    }
}