using DeathRoom.GameServer;
using DeathRoom.Data;

var dbContext = new GameDbContext();
var server = new GameServer(dbContext);
server.Start();

Console.CancelKeyPress += (sender, e) =>
{
    server.Stop();
};

AppDomain.CurrentDomain.ProcessExit += (s, e) => server.Stop();