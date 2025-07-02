using MessagePack;
using UnityEngine;

namespace DeathRoom.Common.dto
{
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

        public Vector3 ToUnityVector3()
        {
            return new Vector3(X, Y, Z);
        }

        public static implicit operator Vector3Serializable(Vector3 vector)
        {
            return new Vector3Serializable(vector);
        }

        public static implicit operator Vector3(Vector3Serializable serializable)
        {
            return serializable.ToUnityVector3();
        }
    }
}