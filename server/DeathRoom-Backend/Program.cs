using DeathRoom.GameServer;

var server = new GameServer();
server.Start();

Console.CancelKeyPress += (sender, e) =>
{
    server.Stop();
};

AppDomain.CurrentDomain.ProcessExit += (s, e) => server.Stop();