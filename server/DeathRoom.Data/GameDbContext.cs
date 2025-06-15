using Microsoft.EntityFrameworkCore;

namespace DeathRoom.Data;

public class GameDbContext : DbContext
{
    public DbSet<Player> Players { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
       //базы пока нет
        const string connectionString = "Host=localhost;Port=5432;Database=deathroom;Username=postgres;Password=aboba";
        optionsBuilder.UseNpgsql(connectionString);
    }
} 