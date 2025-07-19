using DeathRoom.Common.Dto;
using MessagePack;

namespace DeathRoom.Common.Network
{
    [MessagePackObject]
    public class PickUpArmorPacket : IPacket
    {
        [Key(0)]
        public long ClientTick { get; set; }
        
        [Key(1)]
        public int ArmorAmount { get; set; } = 100; // Количество брони, которое дает подбор
    }
} 
