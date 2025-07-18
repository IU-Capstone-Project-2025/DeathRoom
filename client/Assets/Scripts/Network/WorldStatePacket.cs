using MessagePack;
using System.Collections.Generic;
using DeathRoom.Common.dto;

namespace DeathRoom.Common.network
{
    [MessagePackObject]
    public class WorldStatePacket : IPacket
    {
        [Key(0)]
        public List<PlayerState> PlayerStates { get; set; } = new List<PlayerState>();
        
        [Key(1)]
        public long ServerTick { get; set; }

    }

    [MessagePackObject]
    public class PlayerState
    {
        [Key(0)] public int Id { get; set; }
        [Key(1)] public string Username { get; set; } = string.Empty;
        [Key(2)] public Vector3Serializable Position { get; set; }
        [Key(3)] public Vector3Serializable Rotation { get; set; }
        [Key(4)] public int HealthPoint { get; set; }
        [Key(5)] public int MaxHealthPoint { get; set; }
        [Key(6)] public bool IsRunning { get; set; }
        [Key(7)] public bool IsCrouching { get; set; }
        [Key(8)] public bool IsShooting { get; set; }
        [Key(9)] public bool IsAiming { get; set; }
        [Key(10)] public bool IsOnAir { get; set; }
        [Key(11)] public float MoveX { get; set; }
        [Key(12)] public float MoveZ { get; set; }
    }


}
