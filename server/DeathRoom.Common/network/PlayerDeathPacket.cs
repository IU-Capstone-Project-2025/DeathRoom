using MessagePack;

namespace DeathRoom.Common.Network
{
    [MessagePackObject]
    public class PlayerDeathPacket : IPacket
    {
        [Key(0)]
        public int PlayerId { get; set; }
        
        [Key(1)]
        public long ServerTick { get; set; }
    }
}
