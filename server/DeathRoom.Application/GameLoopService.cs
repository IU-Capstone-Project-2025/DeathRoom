using DeathRoom.Domain;
using System.Threading;

namespace DeathRoom.Application;

public class GameLoopService
{
    private readonly PlayerSessionService _playerSessionService;
    private readonly WorldStateService _worldStateService;
    private readonly Func<WorldState, Task> _broadcastWorldState;
    private readonly int _broadcastIntervalMs;
    private readonly int _idleIntervalMs;
    private long _serverTick = 0;

    public GameLoopService(
        PlayerSessionService playerSessionService,
        WorldStateService worldStateService,
        Func<WorldState, Task> broadcastWorldState,
        int broadcastIntervalMs,
        int idleIntervalMs)
    {
        _playerSessionService = playerSessionService;
        _worldStateService = worldStateService;
        _broadcastWorldState = broadcastWorldState;
        _broadcastIntervalMs = broadcastIntervalMs;
        _idleIntervalMs = idleIntervalMs;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("[GameLoopService] RunAsync: старт игрового цикла");
        while (!cancellationToken.IsCancellationRequested)
        {
            _serverTick++;
            var players = _playerSessionService.GetAllPlayers().ToList();
            if (players.Any())
            {
                var worldState = new WorldState
                {
                    PlayerStates = players.Select(p => p.Clone()).ToList(),
                    ServerTick = _serverTick
                };
                _worldStateService.SaveWorldState(_serverTick, worldState);
                await _broadcastWorldState(worldState);
                await Task.Delay(_broadcastIntervalMs, cancellationToken);
            }
            else
            {
                await Task.Delay(_idleIntervalMs, cancellationToken);
            }
        }
        Console.WriteLine("[GameLoopService] RunAsync: завершение игрового цикла");
    }

    public long GetCurrentTick() => _serverTick;
} 