using MessagePack;
using UnityEngine;
using DeathRoom.Common.dto;

namespace DeathRoom.Common.network
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