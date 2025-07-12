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
        // Проверяем, не истекла ли броня
        if (this.ArmorExpirationTick > tick) { this.ArmorPoint = 0; }
        
        // Сначала наносим урон по броне
        if (this.ArmorPoint > 0)
        {
            if (this.ArmorPoint >= armorDamage)
            {
                this.ArmorPoint -= armorDamage;
            }
            else
            {
                // Если брони недостаточно, остаток урона идет на здоровье
                int remainingDamage = armorDamage - this.ArmorPoint;
                this.ArmorPoint = 0;
                healthDamage += remainingDamage;
            }
        }
        else
        {
            // Если брони нет, весь урон по броне идет на здоровье
            healthDamage += armorDamage;
        }

        // Наносим урон по здоровью
        this.HealthPoint -= healthDamage;
        if (this.HealthPoint <= 0)
        {
            this.HealthPoint = 0;
            return true; // Игрок умер
        }
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