using DeathRoom.Data;
using DeathRoom.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;
using DeathRoom.Common.network;
using DeathRoom.Common.dto;

namespace DeathRoom.Tests;

public class GameDbContextTests
{
    [Fact]
    public void Player_can_be_added_and_retrieved()
    {
        var options = new DbContextOptionsBuilder<GameDbContext>()
            // .UseInMemoryDatabase("test-db")
            .Options;

        using (var ctx = new GameDbContext(options))
        {
            var player = new Player
            {
                Login = "tester",
                HashedPassword = "hash",
                Nickname = "Tester",
                Rating = 100,
                LastSeen = DateTime.UtcNow
            };
            ctx.Players.Add(player);
            ctx.SaveChanges();
        }

        using (var ctx = new GameDbContext(options))
        {
            var player = ctx.Players.Single(p => p.Login == "tester");
            Assert.Equal("Tester", player.Nickname);
        }
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

        var bytes = PacketProcessor.Pack(packet);
        var (type, unpacked) = PacketProcessor.Unpack(bytes);

        Assert.Equal(PacketType.PlayerMove, type);
        var movePacket = Assert.IsType<PlayerMovePacket>(unpacked);
        Assert.Equal(packet.Position.X, movePacket.Position.X);
        Assert.Equal(packet.Position.Y, movePacket.Position.Y);
        Assert.Equal(packet.Position.Z, movePacket.Position.Z);
    }
}