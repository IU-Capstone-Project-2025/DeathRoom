using DeathRoom.Common.dto;
using MessagePack;

namespace DeathRoom.Common.network
{
    [MessagePackObject]
    public class PlayerShootPacket : IPacket
    {
        [Key(0)]
        public Vector3 Direction { get; set; }
    }
} 