namespace DeathRoom.Domain;

public class PlayerSnapshot
{
    public long ServerTick { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }
} 