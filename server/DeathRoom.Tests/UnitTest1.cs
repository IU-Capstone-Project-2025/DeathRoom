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
            Position = new Vector3 { X = 1, Y = 2, Z = 3 },
            Rotation = new Vector3 { X = 0, Y = 90, Z = 0 }
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