using SpaceWar;

AppSettings settings = new(args);
using var game = new Game1(settings);
game.Run();
