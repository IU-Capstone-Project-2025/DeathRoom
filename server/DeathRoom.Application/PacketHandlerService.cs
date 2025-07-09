using DeathRoom.Domain;
using DeathRoom.Common.network;
using DeathRoom.Common.dto;
using LiteNetLib;
using DomainPlayerState = DeathRoom.Domain.PlayerState;
using DtoPlayerState = DeathRoom.Common.dto.PlayerState;

namespace DeathRoom.Application;

public class PacketHandlerService
{
    private readonly PlayerSessionService _playerSessionService;
    private readonly WorldStateService _worldStateService;
    private readonly HitRegistrationService _hitRegistrationService;
    private readonly HitPhysicsService _hitPhysicsService;
    private readonly Func<DomainPlayerState, int, long, Task> _onPlayerDeath;
    private readonly Func<string, string, Task> _onPlayerLogin;
    private readonly Func<string, string, Task> _onUnknownPacket;
    private readonly Func<string, string, Task> _onError;
    private readonly Func<long> _getCurrentTick;

    public PacketHandlerService(
        PlayerSessionService playerSessionService,
        WorldStateService worldStateService,
        HitRegistrationService hitRegistrationService,
        HitPhysicsService hitPhysicsService,
        Func<DomainPlayerState, int, long, Task> onPlayerDeath,
        Func<string, string, Task> onPlayerLogin,
        Func<string, string, Task> onUnknownPacket,
        Func<string, string, Task> onError,
        Func<long> getCurrentTick)
    {
        _playerSessionService = playerSessionService;
        _worldStateService = worldStateService;
        _hitRegistrationService = hitRegistrationService;
        _hitPhysicsService = hitPhysicsService;
        _onPlayerDeath = onPlayerDeath;
        _onPlayerLogin = onPlayerLogin;
        _onUnknownPacket = onUnknownPacket;
        _onError = onError;
        _getCurrentTick = getCurrentTick;
    }

    public void HandlePacket(object peer, IPacket packet)
    {
        if (packet is LoginPacket loginPacket)
        {
            var playerState = _playerSessionService.RegisterPlayer(loginPacket.Username);
            _playerSessionService.TryAddSession(peer, playerState);
            _playerSessionService.RegisterPeer(playerState.Id, peer);
            _onPlayerLogin?.Invoke(loginPacket.Username, "Login");
        }
        else if (packet is PlayerMovePacket movePacket)
        {
            if (_playerSessionService.TryGetSession(peer, out DomainPlayerState playerState))
            {
                playerState.Position = new DeathRoom.Domain.Vector3(movePacket.Position.X, movePacket.Position.Y, movePacket.Position.Z);
                playerState.Rotation = new DeathRoom.Domain.Vector3(movePacket.Rotation.X, movePacket.Rotation.Y, movePacket.Rotation.Z);
                var snapshot = new DeathRoom.Domain.PlayerSnapshot
                {
                    ServerTick = _getCurrentTick(),
                    Position = new DeathRoom.Domain.Vector3(movePacket.Position.X, movePacket.Position.Y, movePacket.Position.Z),
                    Rotation = new DeathRoom.Domain.Vector3(movePacket.Rotation.X, movePacket.Rotation.Y, movePacket.Rotation.Z)
                };
                playerState.Snapshots.Enqueue(snapshot);
                if (playerState.Snapshots.Count > 64)
                    playerState.Snapshots.Dequeue();
            }
        }
        else if (packet is PlayerHitPacket hitPacket)
        {
            if (_playerSessionService.TryGetSession(peer, out DomainPlayerState shooterState))
            {
                var worldStateAtShot = _worldStateService.GetWorldStateAtTick(hitPacket.ClientTick);
                if (worldStateAtShot == null) return;
                var shooter = worldStateAtShot.PlayerStates.FirstOrDefault(p => p.Id == shooterState.Id);
                var target = worldStateAtShot.PlayerStates.FirstOrDefault(p => p.Id == hitPacket.TargetId);
                var currTick = hitPacket.ClientTick;
                if (shooter == null || target == null) return;
                var shooterPos = new DeathRoom.Domain.Vector3(shooter.Position.X, shooter.Position.Y, shooter.Position.Z);
                var shootDir = new DeathRoom.Domain.Vector3(hitPacket.Direction.X, hitPacket.Direction.Y, hitPacket.Direction.Z);
                var targetPos = new DeathRoom.Domain.Vector3(target.Position.X, target.Position.Y, target.Position.Z);
                if (_hitPhysicsService.IsHit(shooterPos, shootDir, targetPos))
                {
                    if (_playerSessionService.TryGetSession(peer, out DomainPlayerState liveTarget))
                    {
                        bool died = _hitRegistrationService.RegisterHit(liveTarget, 10, currTick); // пример: 10 урона
                        if (died)
                        {
                            var targetPeer = _playerSessionService.GetPeerById(target.Id);
                            if (targetPeer is LiteNetLib.NetPeer netPeer)
                                netPeer.Disconnect();
                        }
                    }
                }
            }
        }
        else if (packet is PickUpArmorPacket pickArmorPacket)
        {
            if (_playerSessionService.TryGetSession(peer, out DomainPlayerState playerState))
            {
                _hitRegistrationService.GiveArmor(playerState, pickArmorPacket.ClientTick + (long)(200 * 60));
            }
        }
        else
        {
            _onUnknownPacket?.Invoke(peer.ToString(), packet.GetType().Name);
        }
    }
} 