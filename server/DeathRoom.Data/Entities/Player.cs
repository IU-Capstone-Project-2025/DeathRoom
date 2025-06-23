using System;
using System.Collections.Generic;

namespace DeathRoom.Data.Entities
{
    public class Player
    {
        public int Id { get; set; }
        public required string Login { get; set; }
        public required string HashedPassword { get; set; }
        public required string Nickname { get; set; }
        public int Rating { get; set; }
        public DateTime LastSeen { get; set; }

        public ICollection<MatchPlayer> MatchHistory { get; set; } = new List<MatchPlayer>();
    }
} 