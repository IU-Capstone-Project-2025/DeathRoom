using MessagePack;

namespace DeathRoom.Common.network
{
    [Union(0, typeof(PlayerMovePacket))]
    [Union(1, typeof(WorldStatePacket))]
    [Union(2, typeof(LoginPacket))]
    [Union(3, typeof(PlayerShootPacket))]
    // Add other packet types here with unique integer keys
    public interface IPacket
    {
    }
} 