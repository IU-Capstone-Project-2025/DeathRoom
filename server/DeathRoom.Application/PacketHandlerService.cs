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
    private readonly Func<string, string, Task> _onPlayerLogin;
    private readonly Func<string, string, Task> _onUnknownPacket;
    private readonly Func<string, string, Task> _onError;
    private readonly Func<long> _getCurrentTick;
    private readonly Func<IPacket, Task> _broadcastPacket;
    private readonly ILogger<PacketHandlerService> _logger;

    public PacketHandlerService(
        PlayerSessionService playerSessionService,
        WorldStateService worldStateService,
        HitPhysicsService hitPhysicsService,
        Func<string, string, Task> onPlayerLogin,
        Func<string, string, Task> onUnknownPacket,
        Func<string, string, Task> onError,
        Func<long> getCurrentTick,
        Func<IPacket, Task> broadcastPacket,
        ILogger<PacketHandlerService> logger)
    {
        _logger = logger;
        _logger.LogInformation("Конструктор вызван");
        _playerSessionService = playerSessionService;
        _worldStateService = worldStateService;
        _hitPhysicsService = hitPhysicsService;
        _onPlayerLogin = onPlayerLogin;
        _onUnknownPacket = onUnknownPacket;
        _onError = onError;
        _getCurrentTick = getCurrentTick;
        _broadcastPacket = broadcastPacket;
        
        // Initialize HitRegistrationService with death callback
        _hitRegistrationService = new HitRegistrationService(OnPlayerDeath);
    }

    public async Task HandlePacket(object peer, byte[] data)
    {
        if (peer == null) throw new ArgumentNullException(nameof(peer));
        if (data == null) throw new ArgumentNullException(nameof(data));
        
        var packet = MessagePack.MessagePackSerializer.Deserialize<IPacket>(data);
        _logger.LogInformation("[PACKET] Получен пакет: {PacketType} от {Peer}", packet?.GetType().Name ?? "Unknown", peer.ToString());
        
        if (packet == null)
        {
            await (_onError?.Invoke(peer.ToString(), "Packet deserialization failed") ?? Task.CompletedTask);
            return;
        }

        switch (packet)
        {
            case LoginPacket loginPacket:
                await HandleLoginPacket(peer, loginPacket);
                break;
                
            case PlayerMovePacket movePacket:
                HandlePlayerMovePacket(peer, movePacket);
                break;
                
            case PlayerShootPacket shootPacket:
                HandlePlayerShootPacket(peer, shootPacket);
                break;
                
            case PlayerHitPacket hitPacket:
                HandlePlayerHitPacket(peer, hitPacket);
                break;
                
            case PickUpArmorPacket pickArmorPacket:
                HandlePickUpArmorPacket(peer, pickArmorPacket);
                break;
                
            case PickUpHealthPacket pickHealthPacket:
                HandlePickUpHealthPacket(peer, pickHealthPacket);
                break;
                
            case PlayerDeathPacket deathPacket:
                await HandlePlayerDeathPacket(peer, deathPacket);
                break;
                
            default:
                await (_onUnknownPacket?.Invoke(peer.ToString(), packet.GetType().Name) ?? Task.CompletedTask);
                break;
        }
    }

    private async Task HandleLoginPacket(object peer, LoginPacket loginPacket)
    {
        var playerState = _playerSessionService.RegisterPlayer(loginPacket.Username);
        if (playerState != null)
        {
            _playerSessionService.TryAddSession(peer, playerState);
            _playerSessionService.RegisterPeer(playerState.Id, peer);
            if (_onPlayerLogin != null)
            {
                await _onPlayerLogin(loginPacket.Username ?? string.Empty, "Login");
            }
        }
    }

    private void HandlePlayerMovePacket(object peer, PlayerMovePacket movePacket)
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

    private void HandlePlayerShootPacket(object peer, PlayerShootPacket shootPacket)
    {
        if (_playerSessionService.TryGetSession(peer, out DomainPlayerState? shooterState) && shooterState != null)
        {
            _logger.LogInformation("[SHOOT] Игрок {PlayerName} (ID: {PlayerId}) выстрелил в направлении ({X}, {Y}, {Z}) на тике {Tick}", 
                shooterState.Username, shooterState.Id, shootPacket.Direction.X, shootPacket.Direction.Y, shootPacket.Direction.Z, shootPacket.ClientTick);
        
            // Создаем пакет для отправки всем клиентам о выстреле
            var shootBroadcastPacket = new PlayerShootBroadcastPacket
            {
                ShooterId = shooterState.Id,
                Direction = shootPacket.Direction,
                ClientTick = shootPacket.ClientTick,
                ServerTick = _getCurrentTick()
            };
        
            // Отправляем информацию о выстреле всем клиентам
            BroadcastPacketToAllClients(shootBroadcastPacket);
        }
    }

    private void HandlePlayerHitPacket(object peer, PlayerHitPacket hitPacket)
    {
        if (_playerSessionService.TryGetSession(peer, out DomainPlayerState? shooterState) && shooterState != null)
        {
            _logger.LogInformation("[HIT] Игрок {PlayerName} (ID: {PlayerId}) сообщает о попадании в игрока {TargetId} на тике {Tick}", 
                shooterState.Username, shooterState.Id, hitPacket.TargetId, hitPacket.ClientTick);

            var worldStateAtShot = _worldStateService.GetWorldStateAtTick(hitPacket.ClientTick);
            if (worldStateAtShot == null) 
            {
                _logger.LogWarning("[HIT] Не найдено состояние мира для тика {Tick}", hitPacket.ClientTick);
                return;
            }
            
            var shooter = worldStateAtShot.PlayerStates.FirstOrDefault(p => p.Id == shooterState.Id);
            var target = worldStateAtShot.PlayerStates.FirstOrDefault(p => p.Id == hitPacket.TargetId);
            
            if (shooter == null || target == null) 
            {
                _logger.LogWarning("[HIT] Не найден стрелок или цель в историческом состоянии мира");
                return;
            }

            var shooterPos = new DeathRoom.Domain.Vector3(shooter.Position.X, shooter.Position.Y, shooter.Position.Z);
            var shootDir = new DeathRoom.Domain.Vector3(hitPacket.Direction.X, hitPacket.Direction.Y, hitPacket.Direction.Z);
            var targetPos = new DeathRoom.Domain.Vector3(target.Position.X, target.Position.Y, target.Position.Z);
            
            if (_hitPhysicsService.IsHit(shooterPos, shootDir, targetPos))
            {
                _logger.LogInformation("[HIT] Попадание подтверждено физикой");
                
                var liveTargetPeer = _playerSessionService.GetPeerById(hitPacket.TargetId);
                if (liveTargetPeer != null && _playerSessionService.TryGetSession(liveTargetPeer, out DomainPlayerState? liveTarget) && liveTarget != null)
                {
                    _logger.LogInformation("[HIT] BEFORE DAMAGE - Игрок {PlayerName} (ID: {PlayerId}). HP: {HP}, Armor: {Armor}", 
                        liveTarget.Username, liveTarget.Id, liveTarget.HealthPoint, liveTarget.ArmorPoint);
                    
                    bool died = _hitRegistrationService.RegisterHit(liveTarget, 0, hitPacket.ClientTick);
                    
                    _logger.LogInformation("[HIT] AFTER DAMAGE - Игрок {PlayerName} (ID: {PlayerId}). HP: {HP}, Armor: {Armor}, Died: {Died}", 
                        liveTarget.Username, liveTarget.Id, liveTarget.HealthPoint, liveTarget.ArmorPoint, died);
                    
                    if (died)
                    {
                        liveTarget.HealthPoint = liveTarget.MaxHealthPoint;
                        liveTarget.ArmorPoint = 0;
                        _logger.LogInformation("[RESPAWN] Игрок {PlayerName} (ID: {PlayerId}) умер и восстановлен. HP: {HP}, Armor: {Armor}", 
                            liveTarget.Username, liveTarget.Id, liveTarget.HealthPoint, liveTarget.ArmorPoint);
                    }
                }
                else
                {
                    _logger.LogWarning("[HIT] Не найден живой игрок с ID {TargetId} для применения урона", hitPacket.TargetId);
                }
            }
            else
            {
                _logger.LogWarning("[HIT] Попадание НЕ подтверждено физикой - возможная попытка читерства от {PlayerName}", shooterState.Username);
            }
        }
    }

    private void HandlePickUpArmorPacket(object peer, PickUpArmorPacket pickArmorPacket)
    {
        if (_playerSessionService.TryGetSession(peer, out DomainPlayerState? playerState) && playerState != null)
        {
            _hitRegistrationService.AddArmorToPlayer(playerState, pickArmorPacket.ArmorAmount, pickArmorPacket.ClientTick + (long)(200 * 60));
            _logger.LogInformation("[PICKUP] Игрок {PlayerName} (ID: {PlayerId}) подобрал броню: +{ArmorAmount}", 
                playerState.Username, playerState.Id, pickArmorPacket.ArmorAmount);
        }
    }

    private void HandlePickUpHealthPacket(object peer, PickUpHealthPacket pickHealthPacket)
    {
        if (_playerSessionService.TryGetSession(peer, out var playerState) && playerState != null)
        {
            _hitRegistrationService.HealPlayer(playerState, pickHealthPacket.HealthAmount);
            _logger.LogInformation("[PICKUP] Игрок {PlayerName} (ID: {PlayerId}) подобрал аптечку: +{HealthAmount} HP", 
                playerState.Username, playerState.Id, pickHealthPacket.HealthAmount);
        }
    }
    
    private async Task HandlePlayerDeathPacket(object peer, PlayerDeathPacket deathPacket)
    {
        _logger.LogInformation($"[DEATH] Player {deathPacket.PlayerId} has died at tick {deathPacket.ServerTick}");
        
        // Broadcast the death packet to all clients
        if (_broadcastPacket != null)
        {
            await _broadcastPacket(deathPacket);
        }
    }

    private async Task BroadcastPacketToAllClients(IPacket packet)
    {
        await _broadcastPacket(packet);
    }
    
    private void OnPlayerDeath(Domain.PlayerState deadPlayer)
    {
        var currentTick = _getCurrentTick();
        _logger.LogInformation("[DEATH] Player {PlayerName} (ID: {PlayerId}) died at tick {Tick}", 
            deadPlayer.Username, deadPlayer.Id, currentTick);
            
        var deathPacket = new PlayerDeathPacket
        {
            PlayerId = deadPlayer.Id,
            ServerTick = currentTick
        };
        
        _logger.LogInformation("[DEATH] Sending PlayerDeathPacket - Player: {PlayerId}, Tick: {Tick}",
            deathPacket.PlayerId, deathPacket.ServerTick);
            
        try
        {
            // Broadcast death event to all clients
            _ = BroadcastPacketToAllClients(deathPacket);
            _logger.LogInformation("[DEATH] Successfully broadcasted death packet for player {PlayerId}", deadPlayer.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DEATH] Failed to broadcast death packet for player {PlayerId}", deadPlayer.Id);
        }
    }
} 