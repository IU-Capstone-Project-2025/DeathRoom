using DeathRoom.Common.dto;
using MessagePack;

namespace DeathRoom.Common.network
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