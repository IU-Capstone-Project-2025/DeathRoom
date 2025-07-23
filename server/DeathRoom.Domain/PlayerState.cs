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

    public bool TakeDamage(int armorDamage, int healthDamage, long tick)
    {
        Console.WriteLine($"[PlayerState] TakeDamage - Player: {Id}, Initial - Armor: {ArmorPoint}, Health: {HealthPoint}, ArmorDmg: {armorDamage}, HealthDmg: {healthDamage}");
        
        if (ArmorExpirationTick > tick) 
        { 
            Console.WriteLine($"[PlayerState] Armor expired (ExpTick: {ArmorExpirationTick}, Current: {tick}), resetting armor");
            ArmorPoint = 0; 
        }
        
        if (ArmorPoint > 0)
        {
            Console.WriteLine($"[PlayerState] Processing armor damage - Current Armor: {ArmorPoint}, Armor Damage: {armorDamage}");
            if (ArmorPoint >= armorDamage)
            {
                ArmorPoint -= armorDamage;
                Console.WriteLine($"[PlayerState] Armor absorbed all damage - New Armor: {ArmorPoint}");
            }
            else
            {
                int remainingDamage = armorDamage - ArmorPoint;
                Console.WriteLine($"[PlayerState] Armor partially absorbed damage - Remaining damage: {remainingDamage}");
                ArmorPoint = 0;
                healthDamage += remainingDamage;
                Console.WriteLine($"[PlayerState] Remaining damage added to health damage - New Health Damage: {healthDamage}");
            }
        }
        else
        {
            // Если брони нет, весь урон по броне идет на здоровье
            healthDamage += armorDamage;
            Console.WriteLine($"[PlayerState] No armor, all damage goes to health - New Health Damage: {healthDamage}");
        }

        // Наносим урон по здоровью
        this.HealthPoint -= healthDamage;
        Console.WriteLine($"[PlayerState] Applied health damage - New Health: {HealthPoint}");
        
        if (this.HealthPoint <= 0)
        {
            HealthPoint = 0;
            Console.WriteLine($"[PlayerState] Player {Id} has died!");
            return true; // Игрок умер
        }
        Console.WriteLine($"[PlayerState] Player {Id} survived the hit");
        return false; // Игрок жив
    }

    public bool TakeDamage(int damage, long tick)
    {
        // Обратная совместимость - разделяем урон пополам
        int armorDamage = damage / 2;
        int healthDamage = damage - armorDamage;
        return TakeDamage(armorDamage, healthDamage, tick);
    }

    public void ObtainArmor(long tick)
    {
        this.ArmorPoint = this.MaxArmorPoint;
        this.ArmorExpirationTick = tick;
    }

    public void Heal(int healAmount)
    {
        this.HealthPoint = Math.Min(this.HealthPoint + healAmount, this.MaxHealthPoint);
    }

    public void AddArmor(int armorAmount, long tick)
    {
        this.ArmorPoint = Math.Min(this.ArmorPoint + armorAmount, this.MaxArmorPoint);
        this.ArmorExpirationTick = tick;
    }
} 