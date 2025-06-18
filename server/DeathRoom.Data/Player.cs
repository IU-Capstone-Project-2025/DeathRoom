namespace DeathRoom.Data;

public class Player
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public DateTime LastSeen { get; set; }
} 