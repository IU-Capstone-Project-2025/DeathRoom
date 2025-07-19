namespace DeathRoom.Domain;

public class MatchPlayer
{
    public int PlayerId { get; set; }
    public Player Player { get; set; } = null!;
    public Guid MatchId { get; set; }
    public Match Match { get; set; } = null!;
    public int Kills { get; set; }
    public int Deaths { get; set; }
} 