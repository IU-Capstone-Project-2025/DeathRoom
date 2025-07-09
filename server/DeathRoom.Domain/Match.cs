namespace DeathRoom.Domain;

public class Match
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<MatchPlayer> PlayerResults { get; set; } = new();
} 