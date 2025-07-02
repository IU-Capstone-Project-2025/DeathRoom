using DeathRoom.Data;
using DeathRoom.GameServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Временно отключаем базу данных для тестирования
        // var connectionString = context.Configuration.GetConnectionString("DefaultConnection")!
        //     ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        // services.AddDbContext<GameDbContext>(options => options.UseNpgsql(connectionString));

        // Добавляем GameDbContext как фиктивную службу или используем in-memory базу
        services.AddDbContext<GameDbContext>(options => options.UseInMemoryDatabase("TempDb"));

        services.AddScoped<GameServer>();
        services.AddHostedService<ServerRunner>();
    })
    .Build();

// Временно отключаем миграции
// using (var scope = host.Services.CreateScope())
// {
//     var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
//     db.Database.Migrate();
// }

await host.RunAsync(); 