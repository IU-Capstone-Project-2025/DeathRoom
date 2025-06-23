using DeathRoom.Data;
using DeathRoom.GameServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var connectionString = context.Configuration.GetConnectionString("DefaultConnection")!
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<GameDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<GameServer>();
        services.AddHostedService<ServerRunner>();
    })
    .Build();

// Автоматическое применение миграций при старте
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
    db.Database.Migrate();
}

await host.RunAsync(); 