using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace DeathRoom.GameServer;

public class ServerRunner : IHostedService
{
    private readonly IServiceProvider _provider;
    private IServiceScope? _scope;
    private GameServer? _gameServer;

    public ServerRunner(IServiceProvider provider)
    {
        _provider = provider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _scope = _provider.CreateScope();
        _gameServer = _scope.ServiceProvider.GetRequiredService<GameServer>();
        _gameServer.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _gameServer?.Stop();
        _scope?.Dispose();
        return Task.CompletedTask;
    }
} 