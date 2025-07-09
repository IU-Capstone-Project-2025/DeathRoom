using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DeathRoom.GameServer;

public class ServerRunner : IHostedService
{
    private readonly IServiceProvider _provider;
    private IServiceScope _scope;
    private GameServer? _gameServer;
    private Task? _gameLoopTask;

    public ServerRunner(IServiceProvider provider)
    {
        Console.WriteLine("[ServerRunner] Конструктор вызван");
        _provider = provider;
    }

    ~ServerRunner()
    {
        Console.WriteLine("[ServerRunner] Деструктор вызван");
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("[ServerRunner] StartAsync: запуск сервера");
        _scope = _provider.CreateScope();
        _gameServer = _scope.ServiceProvider.GetRequiredService<GameServer>();
        _gameLoopTask = _gameServer.Start(cancellationToken);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("[ServerRunner] StopAsync: начало остановки сервера");
        _gameServer?.Stop();
        if (_gameLoopTask != null)
            await _gameLoopTask;
        Console.WriteLine("[ServerRunner] StopAsync: игровой цикл завершён, освобождаю ресурсы");
        _scope?.Dispose();
        Console.WriteLine("[ServerRunner] StopAsync: завершено");
    }
} 