using MessagePack;

namespace DeathRoom.Common.dto
{
    [MessagePackObject]
    public class PlayerState
    {
        [Key(0)]
        public int Id { get; set; }
        [Key(1)]
        public required string Username { get; set; }
        [Key(2)]
        public Vector3 Position { get; set; }
        [Key(3)]
        public Vector3 Rotation { get; set; }
    }
} 