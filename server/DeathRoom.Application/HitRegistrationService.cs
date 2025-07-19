using DeathRoom.Domain;

namespace DeathRoom.Application;

public class HitRegistrationService
{
    private const int ARMOR_DAMAGE = 20;
    private const int HEALTH_DAMAGE = 10;
    private const float ARMOR_DAMAGE_REDUCTION = 0.5f;

    public bool RegisterHit(PlayerState target, int damage, long tick)
    {
        // Возвращает true, если игрок умер
        return target.TakeDamage(ARMOR_DAMAGE, HEALTH_DAMAGE, tick);
    }

    public void GiveArmor(PlayerState target, long tick)
    {
        target.ObtainArmor(tick);
    }

    public void HealPlayer(PlayerState target, int healAmount)
    {
        target.Heal(healAmount);
    }

    public void AddArmorToPlayer(PlayerState target, int armorAmount, long tick)
    {
        target.AddArmor(armorAmount, tick);
    }
} 