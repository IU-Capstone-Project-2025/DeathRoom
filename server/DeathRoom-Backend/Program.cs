using DeathRoom.GameServer;
using DeathRoom.Application;
using DeathRoom.Domain;
using DeathRoom.Common.network;
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
        services.AddSingleton<NetManager>(sp => sp.GetRequiredService<GameServer>().NetManager);

        // Реальные делегаты для Application-слоя
        services.AddSingleton<GameLoopService>(sp =>
        {
            var playerSession = sp.GetRequiredService<PlayerSessionService>();
            var worldState = sp.GetRequiredService<WorldStateService>();
            var netManager = sp.GetRequiredService<NetManager>();
            return new GameLoopService(
                playerSession,
                worldState,
                async (ws) =>
                {
                    var packet = new WorldStatePacket
                    {
                        PlayerStates = ws.PlayerStates.Select(p => new DeathRoom.Common.dto.PlayerState
                        {
                            Id = p.Id,
                            Username = p.Username,
                            Position = new DeathRoom.Common.dto.Vector3Serializable { X = p.Position.X, Y = p.Position.Y, Z = p.Position.Z },
                            Rotation = new DeathRoom.Common.dto.Vector3Serializable { X = p.Rotation.X, Y = p.Rotation.Y, Z = p.Rotation.Z },
                            HealthPoint = p.HealthPoint,
                            MaxHealthPoint = p.MaxHealthPoint,
                            ArmorPoint = p.ArmorPoint,
                            MaxArmorPoint = p.MaxArmorPoint,
                            ArmorExpirationTick = p.ArmorExpirationTick
                        }).ToList(),
                        ServerTick = ws.ServerTick
                    };
                    var data = MessagePackSerializer.Serialize<IPacket>(packet);
                    netManager.SendToAll(data, DeliveryMethod.Unreliable);
                },
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
            var netManager = sp.GetRequiredService<NetManager>();
            return new PacketHandlerService(
                playerSession,
                worldState,
                hitRegistration,
                hitPhysics,
                async (player, id, tick) =>
                {
                    // Отключение peer при смерти
                    var peerObj = playerSession.GetPeerById(id);
                    if (peerObj is NetPeer netPeer)
                        netPeer.Disconnect();
                },
                async (username, type) => Console.WriteLine($"Player {username} logged in. Type: {type}"),
                async (peer, type) => Console.WriteLine($"Unknown packet from {peer}: {type}"),
                async (peer, error) => Console.WriteLine($"Error from {peer}: {error}"),
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