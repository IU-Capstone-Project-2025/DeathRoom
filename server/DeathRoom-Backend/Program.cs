using DeathRoom.GameServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<GameServer>();
        services.AddHostedService<ServerRunner>();
    })
    .Build();

await host.RunAsync(); 