using DeathRoom.Common.Network;
using MessagePack;

namespace DeathRoom.Common.network {
	[Union(0, typeof(LoginPacket))]
	[Union(1, typeof(PlayerMovePacket))]
	[Union(2, typeof(WorldStatePacket))]
	[Union(3, typeof(PlayerShootPacket))]
	[Union(4, typeof(PlayerHitPacket))]
	[Union(5, typeof(PickUpHealthPacket))]
	[Union(6, typeof(PickUpArmorPacket))]
	[Union(7, typeof(PlayerShootBroadcastPacket))]
	[Union(8, typeof(PlayerAnimationPacket))]
	public interface IPacket { }
}
