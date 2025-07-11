using DeathRoom.Common.Dto;
using MessagePack;

namespace DeathRoom.Common.Network
{
    [MessagePackObject]
    public class PlayerHitPacket : IPacket
    {
        [Key(0)]
        public int TargetId { get; set; }

        [Key(1)]
        public long ClientTick { get; set; }
        
        [Key(2)]
        public Vector3Serializable Direction { get; set; }
    }
} 