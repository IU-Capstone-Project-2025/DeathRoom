using MessagePack;
using DeathRoom.Common.network;

namespace DeathRoom.Common.Network
{
    [MessagePackObject]
    public class PlayerHealthUpdatePacket : IPacket
    {
        [Key(0)]
        public int Health { get; set; }
        
        [Key(1)]
        public int Armor { get; set; }
        
        [Key(2)]
        public long ClientTick { get; set; }
    }
}
