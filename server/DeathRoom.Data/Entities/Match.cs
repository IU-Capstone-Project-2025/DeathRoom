using System;
using System.Collections.Generic;

namespace DeathRoom.Data.Entities
{
    public class Match
    {
        public Guid Id { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<MatchPlayer> PlayerResults { get; set; } = new List<MatchPlayer>();
    }
} 