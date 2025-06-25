using System.Collections.Generic;
using DeathRoom.Common.dto;
using MessagePack;

namespace DeathRoom.Common.network
{
    [MessagePackObject]
    public class WorldStatePacket : IPacket
    {
        [Key(0)]
        public List<PlayerState> PlayerStates { get; set; } = new();

        [Key(1)]
        public long ServerTick { get; set; }
    }
} 