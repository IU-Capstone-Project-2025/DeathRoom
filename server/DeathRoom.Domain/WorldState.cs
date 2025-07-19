namespace DeathRoom.Domain;
 
public class WorldState
{
    public List<PlayerState> PlayerStates { get; set; } = new();
    public long ServerTick { get; set; }
} 