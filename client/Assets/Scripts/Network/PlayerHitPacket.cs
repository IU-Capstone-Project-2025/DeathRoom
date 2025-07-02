using MessagePack;
using UnityEngine;

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
        public Vector3 Direction { get; set; }
    }
}