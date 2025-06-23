using DeathRoom.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeathRoom.Data;

public class GameDbContext : DbContext
{
    public DbSet<Player> Players { get; set; }
    public DbSet<Match> Matches { get; set; }
    public DbSet<MatchPlayer> MatchPlayers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        const string connectionString = "Host=localhost;Port=5432;Database=deathroom;Username=postgres;Password=aboba";
        optionsBuilder
            .UseNpgsql(connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Login).IsUnique();
        });

        modelBuilder.Entity<Match>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<MatchPlayer>(entity =>
        {
            entity.HasKey(mp => new { mp.PlayerId, mp.MatchId });

            entity.HasOne(mp => mp.Player)
                .WithMany(p => p.MatchHistory)
                .HasForeignKey(mp => mp.PlayerId);

            entity.HasOne(mp => mp.Match)
                .WithMany(m => m.PlayerResults)
                .HasForeignKey(mp => mp.MatchId);
        });
    }
} 