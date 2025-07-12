using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using Microsoft.Extensions.Logging;

namespace DeathRoom.GameServer;

public class ServerRunner : IHostedService
{
    private readonly IServiceProvider _provider;
    private IServiceScope? _scope;
    private GameServer? _gameServer;
    private Task? _gameLoopTask;
    private CancellationTokenSource? _cts;
    private readonly ILogger<ServerRunner> _logger;

    public ServerRunner(IServiceProvider provider, ILogger<ServerRunner> logger)
    {
        _logger = logger;
        _logger.LogInformation("Конструктор вызван");
        _provider = provider;
    }

    ~ServerRunner()
    {
        _logger.LogInformation("Деструктор вызван");
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("StartAsync: запуск сервера");
        _scope = _provider.CreateScope();
        try
        {
            _gameServer = _scope.ServiceProvider.GetRequiredService<GameServer>();
            _logger.LogInformation("GameServer получен из DI");
        }
        catch (Exception ex)
        {
            _logger.LogError($"ОШИБКА при получении GameServer из DI: {ex}");
            throw;
        }
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _gameLoopTask = _gameServer.Start(_cts.Token);
        _logger.LogInformation("GameLoopTask запущен");
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("StopAsync: начало остановки сервера");
        _cts?.Cancel();
        _gameServer?.Stop();
        if (_gameLoopTask != null)
            await _gameLoopTask;
        _logger.LogInformation("StopAsync: игровой цикл завершён, освобождаю ресурсы");
        _scope?.Dispose();
        _logger.LogInformation("StopAsync: завершено");
    }
} 