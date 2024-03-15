using SpaceWar;

var settings = AppSettings.LoadFromJson("appsettings.json");
settings.ParseArgs(args);

using var game = new Game1(settings);
game.Run();
