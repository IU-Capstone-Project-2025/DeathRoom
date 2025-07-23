using DeathRoom.Domain;
using DeathRoom.Common.Network;
using System;

namespace DeathRoom.Application;

public class HitRegistrationService
{
    private const int ARMOR_DAMAGE = 20;
    private const int HEALTH_DAMAGE = 15;
    private const float ARMOR_DAMAGE_REDUCTION = 0.5f;
    
    private readonly Action<Domain.PlayerState, int>? _onPlayerDeath;
    
    public HitRegistrationService(Action<Domain.PlayerState, int>? onPlayerDeath = null)
    {
        _onPlayerDeath = onPlayerDeath;
    }

    public bool RegisterHit(Domain.PlayerState target, int damage, long tick, int? killerId = null)
    {
        Console.WriteLine($"[HitRegistration] Registering hit - Target: {target.Id}, Damage: {damage}, Killer: {killerId}, Armor: {target.ArmorPoint}, Health: {target.HealthPoint}");
        bool died = target.TakeDamage(ARMOR_DAMAGE, HEALTH_DAMAGE, tick);
        Console.WriteLine($"[HitRegistration] After damage - Died: {died}, New Armor: {target.ArmorPoint}, New Health: {target.HealthPoint}");
        
        if (died)
        {
            Console.WriteLine($"[HitRegistration] Player {target.Id} died. Invoking death callback with killer: {killerId ?? -1}");
            _onPlayerDeath?.Invoke(target, killerId ?? -1);
        }
        return died;
    }

    public void GiveArmor(Domain.PlayerState target, long tick)
    {
        target.ObtainArmor(tick);
    }

    public void HealPlayer(Domain.PlayerState target, int healAmount)
    {
        target.Heal(healAmount);
    }

    public void AddArmorToPlayer(Domain.PlayerState target, int armorAmount, long tick)
    {
        target.AddArmor(armorAmount, tick);
    }
} 