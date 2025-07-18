﻿using MessagePack;
using UnityEngine;

namespace DeathRoom.Common.dto
{
    public enum ProjectionCode 
    {
        xy,
        xz,
        yz
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

        public Vector3Serializable(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

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

        public Vector3Serializable projection(ProjectionCode code) 
        {
            switch (code) 
            {
                case ProjectionCode.xy:
                    return new Vector3Serializable(this.X, this.Y, 0);

                case ProjectionCode.xz:
                    return new Vector3Serializable(this.X, 0, this.Z);

                case ProjectionCode.yz:
                    return new Vector3Serializable(0, this.Y, this.Z);
            }
            return this;
        }

        public static float operator!(Vector3Serializable operand)
            => (float)System.Math.Sqrt(operand.X*operand.X + operand.Y*operand.Y + operand.Z*operand.Z);

        public static Vector3Serializable operator+(Vector3Serializable left, Vector3Serializable right)
            => new Vector3Serializable(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

        public static Vector3Serializable operator-(Vector3Serializable left, Vector3Serializable right)
            => new Vector3Serializable(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

        public static float operator*(Vector3Serializable left, Vector3Serializable right)
            => left.X*right.X + left.Y*right.Y + left.Z*right.Z;

        public static Vector3Serializable operator/(Vector3Serializable left, float right)
            => new Vector3Serializable(left.X / right, left.Y / right, left.Z / right);

        public static implicit operator Vector3Serializable(Vector3 vector)
        {
            return new Vector3Serializable(vector);
        }

        public static implicit operator Vector3(Vector3Serializable serializable)
        {
            return serializable.ToUnityVector3();
        }

        public override string ToString()
            => $"({X:F2}, {Y:F2}, {Z:F2})";
    }
}