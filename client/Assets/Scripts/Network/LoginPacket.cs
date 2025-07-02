using MessagePack;

namespace DeathRoom.Common.network
{
    [MessagePackObject]
    public class LoginPacket : IPacket
    {
        [Key(0)]
        public string Username { get; set; }

        [Key(1)]
        public string Password { get; set; }
    }
} 
