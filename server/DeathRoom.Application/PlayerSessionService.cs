using System.Collections.Concurrent;
using DeathRoom.Domain;

namespace DeathRoom.Application;

public class PlayerSessionService
{
    private readonly ConcurrentDictionary<object, PlayerState> _players = new(); // object = NetPeer (будет внедряться через DI)
    private readonly Dictionary<int, PlayerState> _inMemoryPlayers = new();
    private int _nextPlayerId = 1;
    private readonly Dictionary<int, object> _peersById = new();

    public PlayerSessionService()
    {
        Console.WriteLine("[PlayerSessionService] Конструктор вызван");
    }

    public PlayerState RegisterPlayer(string username)
    {
        var playerState = new PlayerState
        {
            Id = _nextPlayerId++,
            Username = username,
            Position = new Vector3(),
            Rotation = new Vector3(),
            HealthPoint = 100,
            MaxHealthPoint = 100
        };
        _inMemoryPlayers[playerState.Id] = playerState;
        return playerState;
    }

    public bool TryAddSession(object peer, PlayerState playerState)
        => _players.TryAdd(peer, playerState);

    public bool TryRemoveSession(object peer, out PlayerState? playerState)
        => _players.TryRemove(peer, out playerState);

    public bool TryGetSession(object peer, out PlayerState? playerState)
        => _players.TryGetValue(peer, out playerState);

    public void RemoveInMemoryPlayer(int id) => _inMemoryPlayers.Remove(id);

    public IEnumerable<PlayerState> GetAllPlayers() => _players.Values;

    public void RegisterPeer(int playerId, object peer)
    {
        _peersById[playerId] = peer;
    }

    public object? GetPeerById(int playerId)
    {
        _peersById.TryGetValue(playerId, out var peer);
        return peer;
    }
} 