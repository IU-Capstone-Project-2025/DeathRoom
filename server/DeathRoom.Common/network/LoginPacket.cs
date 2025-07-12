using MessagePack;

namespace DeathRoom.Common.Network
{
    [MessagePackObject]
    public class LoginPacket : IPacket
    {
        [Key(0)]
        public string Username { get; set; } = string.Empty;

        [Key(1)]
        public string Password { get; set; } = string.Empty;
    }
} 