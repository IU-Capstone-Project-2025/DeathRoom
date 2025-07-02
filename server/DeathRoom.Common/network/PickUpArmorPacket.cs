using MessagePack;

namespace DeathRoom.Common.network
{
    [MessagePackObject]
    public class PickUpArmorPacket : IPacket
    {
        [Key(0)]
        public long ClientTick { get; set; }
    }
} 
