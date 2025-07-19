using DeathRoom.Common.dto;
using MessagePack;
using UnityEngine;

namespace DeathRoom.Common.network
{
    [MessagePackObject]
    public class PlayerShootBroadcastPacket : IPacket
    {
        [Key(0)]
        public int ShooterId { get; set; }

        [Key(1)]
        public Vector3Serializable Direction { get; set; }

        [Key(2)]
        public long ClientTick { get; set; }

        [Key(3)]
        public long ServerTick { get; set; }
    }
}
