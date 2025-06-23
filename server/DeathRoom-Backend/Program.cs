using DeathRoom.Data;
using DeathRoom_Backend;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        var connectionString = hostContext.Configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<GameDbContext>(options =>
            options.UseNpgsql(connectionString));
        
        services.AddHostedService<GameServer>();
    })
    .Build()
    .Run();