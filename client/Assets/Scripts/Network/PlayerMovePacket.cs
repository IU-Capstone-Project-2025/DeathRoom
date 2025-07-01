using MessagePack;
using UnityEngine;

namespace DeathRoom.Common.network
{
    [MessagePackObject]
    public class PlayerMovePacket : IPacket
    {
        [Key(0)]
        public Vector3Serializable Position { get; set; }

        [Key(1)]
        public Vector3Serializable Rotation { get; set; }
    }
} 
