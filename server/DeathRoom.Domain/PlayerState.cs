namespace DeathRoom.Domain;

public class PlayerState
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }
    public int HealthPoint { get; set; }
    public int MaxHealthPoint { get; set; } = 100;
    public int ArmorPoint { get; set; }
    public int MaxArmorPoint { get; set; } = 100;
    public long ArmorExpirationTick { get; set; }
    public Queue<PlayerSnapshot> Snapshots { get; } = new();

    public PlayerState Clone()
    {
        return new PlayerState
        {
            Id = this.Id,
            Username = this.Username,
            Position = new Vector3(this.Position.X, this.Position.Y, this.Position.Z),
            Rotation = new Vector3(this.Rotation.X, this.Rotation.Y, this.Rotation.Z),
            HealthPoint = this.HealthPoint
        };
    }

    public bool TakeDamage(int damage, long tick)
    {
        if (this.ArmorExpirationTick > tick) { this.ArmorPoint = 0; }
        if (this.ArmorPoint >= damage)
        {
            this.ArmorPoint -= damage;
            return false;
        }
        else if (this.ArmorPoint > 0)
        {
            damage -= this.ArmorPoint;
            this.ArmorPoint = 0;
        }

        this.HealthPoint -= damage;
        if (this.HealthPoint <= 0)
        {
            this.HealthPoint = 0;
            return true;
        }
        return false;
    }

    public void ObtainArmor(long tick)
    {
        this.ArmorPoint = this.MaxArmorPoint;
        this.ArmorExpirationTick = tick;
    }
} 