using DeathRoom.Common.network;
using DeathRoom.Network;
using MessagePack;

[Union(0, typeof(LoginPacket))]
[Union(1, typeof(PlayerMovePacket))]
[Union(2, typeof(WorldStatePacket))]
[Union(3, typeof(PlayerShootPacket))]
[Union(4, typeof(PlayerHitPacket))]
public interface IPacket { }