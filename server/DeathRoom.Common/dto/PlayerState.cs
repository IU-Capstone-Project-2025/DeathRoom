using System.Collections.Generic;
using MessagePack;

namespace DeathRoom.Common.dto
{
    [MessagePackObject]
    public class PlayerState
    {
        [Key(0)]
        public int Id { get; set; }
        [Key(1)]
        public string Username { get; set; }
        [Key(2)]
        public Vector3 Position { get; set; }
        [Key(3)]
        public Vector3 Rotation { get; set; }
        [Key(4)]
        public int HealthPoint { get; set; }
        [Key(5)]
        public int MaxHealthPoint { get; set; } = 100;
        [IgnoreMember]
        public Queue<PlayerSnapshot> Snapshots { get; } = new Queue<PlayerSnapshot>();

        public PlayerState Clone()
        {
            return new PlayerState
            {
                Id = this.Id,
                Username = this.Username,
                Position = new Vector3(this.Position.X, this.Position.Y, this.Position.Z),
                Rotation = new Vector3(this.Rotation.X, this.Rotation.Y, this.Rotation.Z),
                HealthPoint = this.HealthPoint
            };
        }
    }
} 
