using DeathRoom.Domain;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace DeathRoom.Application;

public class GameLoopService
{
    private readonly PlayerSessionService _playerSessionService;
    private readonly WorldStateService _worldStateService;
    private readonly Func<WorldState, Task> _broadcastWorldState;
    private readonly int _broadcastIntervalMs;
    private readonly int _idleIntervalMs;
    private readonly ILogger<GameLoopService> _logger;
    private long _serverTick = 0;

    public GameLoopService(
        PlayerSessionService playerSessionService,
        WorldStateService worldStateService,
        Func<WorldState, Task> broadcastWorldState,
        int broadcastIntervalMs,
        int idleIntervalMs,
        ILogger<GameLoopService> logger)
    {
        _logger = logger;
        _playerSessionService = playerSessionService;
        _worldStateService = worldStateService;
        _broadcastWorldState = broadcastWorldState;
        _broadcastIntervalMs = broadcastIntervalMs;
        _idleIntervalMs = idleIntervalMs;
        _logger.LogInformation("[INFO] Конструктор вызван");
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[INFO] RunAsync: старт игрового цикла");
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                _serverTick++;
            var players = _playerSessionService.GetAllPlayers().ToList();
                _logger.LogTrace("[TICK] Tick {Tick}, игроков: {Count}", _serverTick, players.Count);
            if (players.Any())
            {
                var worldState = new WorldState
                {
                    PlayerStates = players.Select(p => p.Clone()).ToList(),
                    ServerTick = _serverTick
                };
                _worldStateService.SaveWorldState(_serverTick, worldState);
                await _broadcastWorldState(worldState);
                    _logger.LogDebug($"[DEBUG] Broadcast world state, tick {_serverTick}");
                await Task.Delay(_broadcastIntervalMs, cancellationToken);
            }
            else
            {
                await Task.Delay(_idleIntervalMs, cancellationToken);
            }
        }
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("[WARN] RunAsync: TaskCanceledException — игровой цикл завершён");
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ERROR] RunAsync: Exception: {ex}");
        }
        _logger.LogInformation("[INFO] RunAsync: завершение игрового цикла");
    }

    public long GetCurrentTick() => _serverTick;
} 