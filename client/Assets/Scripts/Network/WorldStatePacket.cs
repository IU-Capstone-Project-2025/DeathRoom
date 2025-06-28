using MessagePack;
using System.Collections.Generic;
using UnityEngine;

namespace DeathRoom.Network
{
    [MessagePackObject]
    public class WorldStatePacket : IPacket
    {
        [Key(0)]
        public Dictionary<int, PlayerState> Players { get; set; }

        public WorldStatePacket()
        {
            Players = new Dictionary<int, PlayerState>();
        }
        
        [Key(1)]
        public long ServerTick;

    }

    [MessagePackObject]
    public class PlayerState
    {
        [Key(0)]
        public int PlayerId { get; set; }
        
        [Key(1)]
        public string Username { get; set; }

        [Key(2)]
        public Vector3Serializable Position { get; set; }

        [Key(3)]
        public Vector3Serializable Rotation { get; set; }

        [Key(4)]
        public float Health { get; set; }
    }

    [MessagePackObject]
    public struct Vector3Serializable
    {
        [Key(0)]
        public float X { get; set; }

        [Key(1)]
        public float Y { get; set; }

        [Key(2)]
        public float Z { get; set; }

        public Vector3Serializable(Vector3 vector)
        {
            X = vector.x;
            Y = vector.y;
            Z = vector.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(X, Y, Z);
        }

        public static implicit operator Vector3Serializable(Vector3 vector)
        {
            return new Vector3Serializable(vector);
        }

        public static implicit operator Vector3(Vector3Serializable serializable)
        {
            return serializable.ToVector3();
        }
    }
} 