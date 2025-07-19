using MessagePack;

namespace DeathRoom.Common.Dto
{
    [MessagePackObject]
    public class PlayerSnapshot
    {
        [Key(0)]
        public long ServerTick { get; set; }

        [Key(1)]
        public Vector3Serializable Position { get; set; }

        [Key(2)]
        public Vector3Serializable Rotation { get; set; }
    }
} 