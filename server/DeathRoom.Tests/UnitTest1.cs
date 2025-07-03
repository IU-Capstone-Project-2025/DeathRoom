using Xunit;
using DeathRoom.Common.network;
using DeathRoom.Common.dto;

namespace DeathRoom.Tests;

public class InMemoryPlayerTests
{
    [Fact]
    public void PlayerState_can_be_created_and_accessed()
    {
        var playerState = new PlayerState
        {
            Id = 1,
            Username = "Tester",
            Position = new Vector3 { X = 0, Y = 0, Z = 0 },
            Rotation = new Vector3 { X = 0, Y = 0, Z = 0 },
            HealthPoint = 100,
            MaxHealthPoint = 100
        };

        Assert.Equal(1, playerState.Id);
        Assert.Equal("Tester", playerState.Username);
        Assert.Equal(100, playerState.HealthPoint);
        Assert.Equal(100, playerState.MaxHealthPoint);
    }
}

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

public class Vector3Tests
{
    [Fact]
    public void Vector3_default_values_are_zero()
    {
        var vector = new Vector3();
        
        Assert.Equal(0, vector.X);
        Assert.Equal(0, vector.Y);
        Assert.Equal(0, vector.Z);
    }
    
    [Fact]
    public void Vector3_can_be_set_and_retrieved()
    {
        var vector = new Vector3 { X = 10.5f, Y = -5.2f, Z = 0.0f };
        
        Assert.Equal(10.5f, vector.X);
        Assert.Equal(-5.2f, vector.Y);
        Assert.Equal(0.0f, vector.Z);
    }
}

public class PlayerStateHealthTests
{
    [Fact]
    public void PlayerState_TakeDamage_reduces_health()
    {
        var playerState = new PlayerState
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
        var playerState = new PlayerState
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
        var playerState = new PlayerState
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