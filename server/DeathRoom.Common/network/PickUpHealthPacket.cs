using DeathRoom.Common.Dto;
using MessagePack;

namespace DeathRoom.Common.Network
{
    [MessagePackObject]
    public class PickUpHealthPacket : IPacket
    {
        [Key(0)]
        public long ClientTick { get; set; }
        
        [Key(1)]
        public int HealthAmount { get; set; } = 50; // Количество здоровья, которое восстанавливает аптечка
    }
} 