using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DeathRoom.Data;

public class GameDbContextFactory : IDesignTimeDbContextFactory<GameDbContext>
{
    public GameDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<GameDbContext>();
        // Используем ту же строку подключения, что и в appsettings.json.
        // При необходимости можно переопределить через переменные окружения или args.
        var connectionString = Environment.GetEnvironmentVariable("DEATHROOM_CONNECTION") ??
                                 "Host=localhost;Port=5432;Database=deathroom;Username=postgres;Password=aboba";

        optionsBuilder.UseNpgsql(connectionString);

        return new GameDbContext(optionsBuilder.Options);
    }
} 