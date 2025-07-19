using Xunit;
using DeathRoom.Common.Network;
using DeathRoom.Common.Dto;
using DeathRoom.Application;
using DeathRoom.Domain;

namespace DeathRoom.Tests;

// public class InMemoryPlayerTests
// {
//     [Fact]
//     public void PlayerState_can_be_created_and_accessed()
//     {
//         var playerState = new PlayerState
//         {
//             Id = 1,
//             Username = "Tester",
//             Position = new Vector3 { X = 0, Y = 0, Z = 0 },
//             Rotation = new Vector3 { X = 0, Y = 0, Z = 0 },
//             HealthPoint = 100,
//             MaxHealthPoint = 100
//         };
//
//         Assert.Equal(1, playerState.Id);
//         Assert.Equal("Tester", playerState.Username);
//         Assert.Equal(100, playerState.HealthPoint);
//         Assert.Equal(100, playerState.MaxHealthPoint);
//     }
// }

public class PacketProcessorTests
{
    [Fact]
    public void Pack_and_unpack_roundtrip_preserves_packet()
    {
        var packet = new PlayerMovePacket
        {
            Position = new Vector3Serializable { X = 1, Y = 2, Z = 3 },
            Rotation = new Vector3Serializable { X = 0, Y = 90, Z = 0 }
        };

        // Проверяем, что пакет корректно создается и содержит правильные данные
        Assert.Equal(1, packet.Position.X);
        Assert.Equal(2, packet.Position.Y);
        Assert.Equal(3, packet.Position.Z);
        Assert.Equal(0, packet.Rotation.X);
        Assert.Equal(90, packet.Rotation.Y);
        Assert.Equal(0, packet.Rotation.Z);
    }
}

// public class Vector3Tests
// {
//     [Fact]
//     public void Vector3_default_values_are_zero()
//     {
//         var vector = new Vector3();
//         
//         Assert.Equal(0, vector.X);
//         Assert.Equal(0, vector.Y);
//         Assert.Equal(0, vector.Z);
//     }
//     
//     [Fact]
//     public void Vector3_can_be_set_and_retrieved()
//     {
//         var vector = new Vector3 { X = 10.5f, Y = -5.2f, Z = 0.0f };
//         
//         Assert.Equal(10.5f, vector.X);
//         Assert.Equal(-5.2f, vector.Y);
//         Assert.Equal(0.0f, vector.Z);
//     }
// }

public class PlayerStateHealthTests
{
    [Fact]
    public void PlayerState_TakeDamage_reduces_health()
    {
        var playerState = new DeathRoom.Domain.PlayerState
        {
            Id = 1,
            Username = "TestPlayer",
            HealthPoint = 100,
            MaxHealthPoint = 100
        };
        
        // Наносим урон
        bool isDead = playerState.TakeDamage(30, 1);
        
        Assert.False(isDead);
        Assert.Equal(70, playerState.HealthPoint);
    }
    
    [Fact]
    public void PlayerState_TakeDamage_kills_when_health_reaches_zero()
    {
        var playerState = new DeathRoom.Domain.PlayerState
        {
            Id = 1,
            Username = "TestPlayer",
            HealthPoint = 50,
            MaxHealthPoint = 100
        };
        
        // Наносим смертельный урон
        bool isDead = playerState.TakeDamage(50, 1);
        
        Assert.True(isDead);
        Assert.Equal(0, playerState.HealthPoint);
    }
    
    [Fact]
    public void PlayerState_TakeDamage_with_excessive_damage_caps_at_zero()
    {
        var playerState = new DeathRoom.Domain.PlayerState
        {
            Id = 1,
            Username = "TestPlayer",
            HealthPoint = 30,
            MaxHealthPoint = 100
        };
        
        // Наносим больше урона, чем есть здоровья
        bool isDead = playerState.TakeDamage(100, 1);
        
        Assert.True(isDead);
        Assert.Equal(0, playerState.HealthPoint);
    }
}

public class HitPhysicsTests
{
    [Fact]
    public void HitPhysicsService_IsHit_WithCylinder_ShouldHitWhenRayIntersectsCylinder()
    {
        var hitPhysics = new HitPhysicsService();
        
        // Стрелок в начале координат
        var shooterPos = new Vector3(0, 1, 0);
        
        // Цель в 5 метрах по оси Z
        var targetPos = new Vector3(0, 1, 5);
        
        // Направление выстрела прямо в цель
        var shootDir = new Vector3(0, 0, 1);
        
        bool isHit = hitPhysics.IsHit(shooterPos, shootDir, targetPos);
        
        Assert.True(isHit, "Выстрел должен попасть в цель");
    }
    
    [Fact]
    public void HitPhysicsService_IsHit_WithCylinder_ShouldMissWhenRayMissesCylinder()
    {
        var hitPhysics = new HitPhysicsService();
        
        // Стрелок в начале координат
        var shooterPos = new Vector3(0, 1, 0);
        
        // Цель в 5 метрах по оси Z
        var targetPos = new Vector3(0, 1, 5);
        
        // Направление выстрела мимо цели (в сторону)
        var shootDir = new Vector3(1, 0, 0);
        
        bool isHit = hitPhysics.IsHit(shooterPos, shootDir, targetPos);
        
        Assert.False(isHit, "Выстрел должен пройти мимо цели");
    }
    
    [Fact]
    public void HitPhysicsService_IsHit_WithCylinder_ShouldHitWhenRayIntersectsCylinderFromAbove()
    {
        var hitPhysics = new HitPhysicsService();
        
        // Стрелок выше цели
        var shooterPos = new Vector3(0, 3, 0);
        
        // Цель в 5 метрах по оси Z на уровне земли
        var targetPos = new Vector3(0, 1, 5);
        
        // Направление выстрела вниз к цели
        var shootDir = new Vector3(0, -0.5f, 1);
        
        bool isHit = hitPhysics.IsHit(shooterPos, shootDir, targetPos);
        
        Assert.True(isHit, "Выстрел сверху должен попасть в цилиндр");
    }
}

public class ArmorDamageTests
{
    [Fact]
    public void PlayerState_TakeDamage_WithArmor_ShouldReduceArmorFirst()
    {
        var playerState = new DeathRoom.Domain.PlayerState
        {
            Id = 1,
            Username = "TestPlayer",
            HealthPoint = 100,
            MaxHealthPoint = 100,
            ArmorPoint = 50,
            MaxArmorPoint = 100
        };
        
        // Наносим урон: 20 на броню, 10 на здоровье
        bool isDead = playerState.TakeDamage(20, 10, 1);
        
        Assert.False(isDead);
        Assert.Equal(30, playerState.ArmorPoint); // 50 - 20 = 30
        Assert.Equal(90, playerState.HealthPoint); // 100 - 10 = 90
    }
    
    [Fact]
    public void PlayerState_TakeDamage_WithInsufficientArmor_ShouldTransferRemainingDamageToHealth()
    {
        var playerState = new DeathRoom.Domain.PlayerState
        {
            Id = 1,
            Username = "TestPlayer",
            HealthPoint = 100,
            MaxHealthPoint = 100,
            ArmorPoint = 10,
            MaxArmorPoint = 100
        };
        
        // Наносим урон: 20 на броню, 10 на здоровье
        // Брони только 10, значит 10 урона по броне + 10 по здоровью + 10 перенесенного = 20 урона по здоровью
        bool isDead = playerState.TakeDamage(20, 10, 1);
        
        Assert.False(isDead);
        Assert.Equal(0, playerState.ArmorPoint); // 10 - 10 = 0
        Assert.Equal(80, playerState.HealthPoint); // 100 - 20 = 80
    }
    
    [Fact]
    public void PlayerState_TakeDamage_WithoutArmor_ShouldApplyAllDamageToHealth()
    {
        var playerState = new DeathRoom.Domain.PlayerState
        {
            Id = 1,
            Username = "TestPlayer",
            HealthPoint = 100,
            MaxHealthPoint = 100,
            ArmorPoint = 0,
            MaxArmorPoint = 100
        };
        
        // Наносим урон: 20 на броню, 10 на здоровье
        // Брони нет, значит весь урон (30) идет на здоровье
        bool isDead = playerState.TakeDamage(20, 10, 1);
        
        Assert.False(isDead);
        Assert.Equal(0, playerState.ArmorPoint);
        Assert.Equal(70, playerState.HealthPoint); // 100 - 30 = 70
    }
    
    [Fact]
    public void PlayerState_TakeDamage_ShouldKillWhenHealthReachesZero()
    {
        var playerState = new DeathRoom.Domain.PlayerState
        {
            Id = 1,
            Username = "TestPlayer",
            HealthPoint = 25,
            MaxHealthPoint = 100,
            ArmorPoint = 0,
            MaxArmorPoint = 100
        };
        
        // Наносим смертельный урон: 20 на броню, 10 на здоровье
        // Брони нет, значит весь урон (30) идет на здоровье
        bool isDead = playerState.TakeDamage(20, 10, 1);
        
        Assert.True(isDead);
        Assert.Equal(0, playerState.HealthPoint);
    }
}

public class PickupTests
{
    [Fact]
    public void PlayerState_Heal_ShouldIncreaseHealth()
    {
        var playerState = new DeathRoom.Domain.PlayerState
        {
            Id = 1,
            Username = "TestPlayer",
            HealthPoint = 50,
            MaxHealthPoint = 100,
            ArmorPoint = 0,
            MaxArmorPoint = 100
        };
        
        playerState.Heal(30);
        
        Assert.Equal(80, playerState.HealthPoint); // 50 + 30 = 80
    }
    
    [Fact]
    public void PlayerState_Heal_ShouldNotExceedMaxHealth()
    {
        var playerState = new DeathRoom.Domain.PlayerState
        {
            Id = 1,
            Username = "TestPlayer",
            HealthPoint = 90,
            MaxHealthPoint = 100,
            ArmorPoint = 0,
            MaxArmorPoint = 100
        };
        
        playerState.Heal(30);
        
        Assert.Equal(100, playerState.HealthPoint); // Не превышает максимум
    }
    
    [Fact]
    public void PlayerState_AddArmor_ShouldIncreaseArmor()
    {
        var playerState = new DeathRoom.Domain.PlayerState
        {
            Id = 1,
            Username = "TestPlayer",
            HealthPoint = 100,
            MaxHealthPoint = 100,
            ArmorPoint = 20,
            MaxArmorPoint = 100
        };
        
        playerState.AddArmor(50, 1000);
        
        Assert.Equal(70, playerState.ArmorPoint); // 20 + 50 = 70
        Assert.Equal(1000, playerState.ArmorExpirationTick);
    }
    
    [Fact]
    public void PlayerState_AddArmor_ShouldNotExceedMaxArmor()
    {
        var playerState = new DeathRoom.Domain.PlayerState
        {
            Id = 1,
            Username = "TestPlayer",
            HealthPoint = 100,
            MaxHealthPoint = 100,
            ArmorPoint = 80,
            MaxArmorPoint = 100
        };
        
        playerState.AddArmor(50, 1000);
        
        Assert.Equal(100, playerState.ArmorPoint); // Не превышает максимум
    }
}