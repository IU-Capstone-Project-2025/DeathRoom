using MessagePack;
using System.Collections.Generic;
using DeathRoom.Common.dto;
using UnityEngine;

namespace DeathRoom.Common.network
{
    [MessagePackObject]
    public class WorldStatePacket : IPacket
    {
        [Key(0)]
        public List<PlayerState> PlayerStates{ get; set; }
        
        [Key(1)]
        public long ServerTick;

    }

    [MessagePackObject]
    public class PlayerState
    {
        [Key(0)]
        public int Id { get; set; }
        
        [Key(1)]
        public string Username { get; set; }

        [Key(2)]
        public Vector3Serializable Position { get; set; }

        [Key(3)]
        public Vector3Serializable Rotation { get; set; }

        [Key(4)]
        public int HealthPoint { get; set; }

        [Key(5)]
        public int MaxHealthPoint { get; set; }
    }
    
} 
