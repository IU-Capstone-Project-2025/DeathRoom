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
        [Key(0)]
        public int Id { get; set; }
        
        [Key(1)]
        public string Username { get; set; } = string.Empty;

        [Key(2)]
        public Vector3Serializable Position { get; set; }

        [Key(3)]
        public Vector3Serializable Rotation { get; set; }

        [Key(4)]
        public int HealthPoint { get; set; }

        [Key(5)]
        public int MaxHealthPoint { get; set; }
    }

    [MessagePackObject]
    public class AnimationUpdatePacket : IPacket
    {
        [Key(0)]
        public int PlayerId { get; set; }

        [Key(1)]
        public long ClientTick { get; set; }

        [Key(2)]
        public Dictionary<string, bool> BoolParams { get; set; }

        [Key(3)]
        public Dictionary<string, float> FloatParams { get; set; }

        [Key(4)]
        public Dictionary<string, int> IntParams { get; set; }

        public AnimationUpdatePacket()
        {
            BoolParams = new Dictionary<string, bool>();
            FloatParams = new Dictionary<string, float>();
            IntParams = new Dictionary<string, int>();
        }
    }

} 
