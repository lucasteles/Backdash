using SpaceWar;

AppSettings settings = new()
{
    Port = args is [{ } portArg, ..] && int.TryParse(portArg, out var portNum) ? portNum : 9000,
};

using var game = new Game1(settings);
game.Run();
