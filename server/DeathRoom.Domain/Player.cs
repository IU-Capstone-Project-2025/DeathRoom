namespace DeathRoom.Domain;

public class Player
{
    public int Id { get; set; }
    public required string Login { get; set; }
    public required string HashedPassword { get; set; }
    public required string Nickname { get; set; }
    public int Rating { get; set; }
    public DateTime LastSeen { get; set; }
    // Навигационные свойства и коллекции можно оставить, но без привязки к EF
    public List<MatchPlayer> MatchHistory { get; set; } = new();
} 