using DeathRoom.Common.Dto;
using MessagePack;

namespace DeathRoom.Common.Network
{
    [MessagePackObject]
    public class PlayerShootPacket : IPacket
    {
        [Key(0)]
        public long ClientTick { get; set; }

        [Key(1)]
        public Vector3Serializable Direction { get; set; }
    }
} 