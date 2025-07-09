using DeathRoom.Domain;

namespace DeathRoom.Application;

public class WorldStateService
{
    private readonly int _worldStateHistoryLength;
    private readonly int _worldStateSaveInterval;
    private readonly Queue<(long Tick, WorldState State)> _worldStateHistory = new();

    public WorldStateService(int historyLength, int saveInterval)
    {
        _worldStateHistoryLength = historyLength;
        _worldStateSaveInterval = saveInterval;
    }

    public void SaveWorldState(long tick, WorldState state)
    {
        if (tick % _worldStateSaveInterval == 0)
        {
            _worldStateHistory.Enqueue((tick, state));
            if (_worldStateHistory.Count > _worldStateHistoryLength)
                _worldStateHistory.Dequeue();
        }
    }

    public WorldState? GetWorldStateAtTick(long tick)
    {
        if (_worldStateHistory.Count == 0) return null;
        var arr = _worldStateHistory.ToArray();
        for (int i = 0; i < arr.Length - 1; i++)
        {
            if (arr[i].Tick <= tick && tick <= arr[i + 1].Tick)
            {
                if (arr[i].Tick == tick) return arr[i].State;
                if (arr[i + 1].Tick == tick) return arr[i + 1].State;
                return InterpolateWorldState(tick, arr[i], arr[i + 1]);
            }
        }
        if (tick < arr[0].Tick) return arr[0].State;
        return arr[^1].State;
    }

    private WorldState InterpolateWorldState(long tick, (long Tick, WorldState State) before, (long Tick, WorldState State) after)
    {
        var result = new WorldState();
        float t = (float)(tick - before.Tick) / (after.Tick - before.Tick);
        foreach (var beforePlayer in before.State.PlayerStates)
        {
            var afterPlayer = after.State.PlayerStates.FirstOrDefault(p => p.Id == beforePlayer.Id);
            if (afterPlayer != null)
            {
                var interpPlayer = beforePlayer.Clone();
                interpPlayer.Position = InterpolateVector3(beforePlayer.Position, afterPlayer.Position, t);
                interpPlayer.Rotation = InterpolateVector3(beforePlayer.Rotation, afterPlayer.Rotation, t);
                interpPlayer.HealthPoint = (int)(beforePlayer.HealthPoint * (1 - t) + afterPlayer.HealthPoint * t);
                result.PlayerStates.Add(interpPlayer);
            }
        }
        return result;
    }

    private Vector3 InterpolateVector3(Vector3 a, Vector3 b, float t)
    {
        return new Vector3(
            a.X + (b.X - a.X) * t,
            a.Y + (b.Y - a.Y) * t,
            a.Z + (b.Z - a.Z) * t
        );
    }
} 