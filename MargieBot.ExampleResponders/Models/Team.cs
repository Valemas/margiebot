using System.Collections.Generic;

namespace MargieBot.ExampleResponders.Models
{
    public class Team
    {
        public bool IsWinner { get; set; }
        public List<Player> Players { get; set; }
        public int MostValuablePlayerId { get; set; }
        public int Goals { get; set; }
    }
}