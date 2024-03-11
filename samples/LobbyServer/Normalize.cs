using System.Text.RegularExpressions;

namespace LobbyServer;

public static class Normalize
{
    public static string Name(string name) =>
        Regex.Replace(name.Trim().ToLower(), "[^a-zA-Z0-9]", "_");
}
