using DeathRoom.Domain;
using DeathRoom.Common.Network;
using DeathRoom.Common.Dto;
using DomainPlayerState = DeathRoom.Domain.PlayerState;
using DtoPlayerState = DeathRoom.Common.Dto.PlayerState;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<PacketHandlerService> _logger;

    public PacketHandlerService(
        PlayerSessionService playerSessionService,
        WorldStateService worldStateService,
        HitRegistrationService hitRegistrationService,
        HitPhysicsService hitPhysicsService,
        Func<DomainPlayerState, int, long, Task> onPlayerDeath,
        Func<string, string, Task> onPlayerLogin,
        Func<string, string, Task> onUnknownPacket,
        Func<string, string, Task> onError,
        Func<long> getCurrentTick,
        ILogger<PacketHandlerService> logger)
    {
        _logger = logger;
        _logger.LogInformation("Конструктор вызван");
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

    public async Task HandlePacket(object peer, byte[] data)
    {
        if (peer == null) throw new ArgumentNullException(nameof(peer));
        if (data == null) throw new ArgumentNullException(nameof(data));
        var packet = MessagePack.MessagePackSerializer.Deserialize<IPacket>(data);
        _logger.LogInformation("[PACKET] Получен пакет: {PacketType} от {Peer}", packet?.GetType().Name ?? "Unknown", peer?.ToString() ?? "null");
        if (packet == null)
        {
            await (_onError?.Invoke(peer?.ToString() ?? string.Empty, "Packet deserialization failed") ?? Task.CompletedTask);
            return;
        }
        if (packet is LoginPacket loginPacket)
        {
            var playerState = _playerSessionService.RegisterPlayer(loginPacket.Username);
            if (playerState != null)
            {
                _playerSessionService.TryAddSession(peer, playerState);
                _playerSessionService.RegisterPeer(playerState.Id, peer);
                await (_onPlayerLogin?.Invoke(loginPacket.Username ?? string.Empty, "Login") ?? Task.CompletedTask);
            }
        }
        else if (packet is PlayerMovePacket movePacket)
        {
            if (_playerSessionService.TryGetSession(peer, out DomainPlayerState? playerState) && playerState != null)
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
            if (_playerSessionService.TryGetSession(peer, out DomainPlayerState? shooterState) && shooterState != null)
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
                    if (_playerSessionService.TryGetSession(peer, out DomainPlayerState? liveTarget) && liveTarget != null)
                    {
                        bool died = _hitRegistrationService.RegisterHit(liveTarget, 10, currTick);
                        if (died)
                        {
                            var targetPeer = _playerSessionService.GetPeerById(target.Id);
                            if (targetPeer != null && targetPeer.GetType().Name == "NetPeer")
                            {
                                var disconnectMethod = targetPeer.GetType().GetMethod("Disconnect");
                                disconnectMethod?.Invoke(targetPeer, null);
                            }
                        }
                    }
                }
            }
        }
        else if (packet is PickUpArmorPacket pickArmorPacket)
        {
            if (_playerSessionService.TryGetSession(peer, out DomainPlayerState? playerState) && playerState != null)
            {
                _hitRegistrationService.GiveArmor(playerState, pickArmorPacket.ClientTick + (long)(200 * 60));
            }
        }
        else
        {
            await (_onUnknownPacket?.Invoke(peer?.ToString() ?? string.Empty, packet?.GetType().Name ?? "Unknown") ?? Task.CompletedTask);
        }
    }
} 