using DeathRoom.GameServer;
using DeathRoom.Application;
using DeathRoom.Domain;
using DeathRoom.Common.Network;
using LiteNetLib;
using MessagePack;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        int broadcastIntervalMs = int.TryParse(Environment.GetEnvironmentVariable("DEATHROOM_BROADCAST_INTERVAL_MS"), out var bInt) && bInt > 0 ? bInt : 15;
        int idleIntervalMs = int.TryParse(Environment.GetEnvironmentVariable("DEATHROOM_IDLE_INTERVAL_MS"), out var iInt) && iInt > 0 ? iInt : 100;
        int worldStateHistoryLength = int.TryParse(Environment.GetEnvironmentVariable("DEATHROOM_WORLDSTATE_HISTORY_LENGTH"), out var hLen) && hLen > 0 ? hLen : 20;
        int worldStateSaveInterval = int.TryParse(Environment.GetEnvironmentVariable("DEATHROOM_WORLDSTATE_SAVE_INTERVAL"), out var sInt) && sInt > 0 ? sInt : 10;

        services.AddSingleton<PlayerSessionService>();
        services.AddSingleton<WorldStateService>(_ => new WorldStateService(worldStateHistoryLength, worldStateSaveInterval));
        services.AddSingleton<HitRegistrationService>();
        services.AddSingleton<HitPhysicsService>();

        // NetManager и GameServer будут связаны через фабрику
        services.AddSingleton<GameServer>();
        // Удаляю регистрацию NetManager
        // services.AddSingleton<NetManager>(sp => sp.GetRequiredService<GameServer>().NetManager);

        // Реальные делегаты для Application-слоя
        services.AddSingleton<GameLoopService>(sp =>
        {
            var playerSession = sp.GetRequiredService<PlayerSessionService>();
            var worldState = sp.GetRequiredService<WorldStateService>();
            // var netManager = sp.GetRequiredService<NetManager>(); // Удалено
            return new GameLoopService(
                playerSession,
                worldState,
                ws => Task.CompletedTask, // Временно заглушка, реальный делегат будет установлен в GameServer
                broadcastIntervalMs,
                idleIntervalMs
            );
        });
        services.AddSingleton<PacketHandlerService>(sp =>
        {
            var playerSession = sp.GetRequiredService<PlayerSessionService>();
            var worldState = sp.GetRequiredService<WorldStateService>();
            var hitRegistration = sp.GetRequiredService<HitRegistrationService>();
            var hitPhysics = sp.GetRequiredService<HitPhysicsService>();
            // var netManager = sp.GetRequiredService<NetManager>(); // Удалено
            return new PacketHandlerService(
                playerSession,
                worldState,
                hitRegistration,
                hitPhysics,
                (player, id, tick) =>
                {
                    var peerObj = playerSession.GetPeerById(id);
                    if (peerObj != null && peerObj.GetType().Name == "NetPeer")
                    {
                        var disconnectMethod = peerObj.GetType().GetMethod("Disconnect");
                        disconnectMethod?.Invoke(peerObj, null);
                    }
                    return Task.CompletedTask;
                },
                (username, type) => { Console.WriteLine($"Player {username} logged in. Type: {type}"); return Task.CompletedTask; },
                (peer, type) => { Console.WriteLine($"Unknown packet from {peer}: {type}"); return Task.CompletedTask; },
                (peer, error) => { Console.WriteLine($"Error from {peer}: {error}"); return Task.CompletedTask; },
                () => sp.GetRequiredService<GameLoopService>().GetCurrentTick()
            );
        });
        services.AddHostedService<ServerRunner>();
    });

var host = builder.Build();
Console.WriteLine("DeathRoom сервер стартует...");
await host.RunAsync(); 
Console.WriteLine("[Program] Host.RunAsync завершён, программа заканчивается");
Environment.Exit(0); 