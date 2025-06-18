using DeathRoom.Common.dto;
using MessagePack;

namespace DeathRoom.Common.network
{
    [MessagePackObject]
    public class PlayerMovePacket : IPacket
    {
        [Key(0)]
        public Vector3 Position { get; set; }
        
        [Key(1)]
        public Vector3 Rotation { get; set; }
    }
} 