using MessagePack;

namespace DeathRoom.Common.dto
{
    [MessagePackObject]
    public class PlayerSnapshot
    {
        [Key(0)]
        public long ServerTick { get; set; }

        [Key(1)]
        public Vector3 Position { get; set; }

        [Key(2)]
        public Vector3 Rotation { get; set; }
    }
} 