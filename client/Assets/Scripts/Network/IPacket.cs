using MessagePack;

namespace DeathRoom.Common.network {
	[Union(0, typeof(LoginPacket))]
	[Union(1, typeof(PlayerMovePacket))]
	[Union(2, typeof(WorldStatePacket))]
	[Union(3, typeof(PlayerShootPacket))]
	[Union(4, typeof(PlayerHitPacket))]
	[Union(5, typeof(PlayerShootBroadcastPacket))]
	[Union(5, typeof(PlayerAnimationPacket))]
	public interface IPacket { }
}
