using DeathRoom.Domain;

namespace DeathRoom.Application;

public class HitRegistrationService
{
    public bool RegisterHit(PlayerState target, int damage, long tick)
    {
        // Возвращает true, если игрок умер
        return target.TakeDamage(damage, tick);
    }

    public void GiveArmor(PlayerState target, long tick)
    {
        target.ObtainArmor(tick);
    }
} 