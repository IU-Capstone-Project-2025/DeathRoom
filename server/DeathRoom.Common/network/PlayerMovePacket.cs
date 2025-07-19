using DeathRoom.Common.Dto;
using MessagePack;

namespace DeathRoom.Common.Network
{
    [MessagePackObject]
    public class PlayerMovePacket : IPacket
    {
        [Key(0)]
        public Vector3Serializable Position { get; set; }
        
        [Key(1)]
        public Vector3Serializable Rotation { get; set; }

        [Key(2)]
        public long ClientTick { get; set; }
    }
} 