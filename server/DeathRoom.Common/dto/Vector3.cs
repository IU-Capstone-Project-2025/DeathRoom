using MessagePack;

namespace DeathRoom.Common.dto
{
    [MessagePackObject]
    public struct Vector3
    {
        [Key(0)]
        public float X { get; set; }
        [Key(1)]
        public float Y { get; set; }
        [Key(2)]
        public float Z { get; set; }
    }
} 