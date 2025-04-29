using Backdash;
using Backdash.Synchronizing.State;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace SpaceWar;

/// <summary>
/// This prints the text diff of a state when a desync happens over a SyncTest session.
/// </summary>
sealed class DiffPlexDesyncHandler : IStateDesyncHandler
{
    public void Handle(INetcodeSession session, in StateSnapshot previous, in StateSnapshot current)
    {
        var diff = InlineDiffBuilder.Diff(previous.Value, current.Value);

        var savedColor = Console.ForegroundColor;

        foreach (var line in diff.Lines)
        {
            switch (line.Type)
            {
                case ChangeType.Inserted:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("+ ");
                    break;
                case ChangeType.Deleted:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("- ");
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write("  ");
                    break;
            }

            Console.WriteLine(line.Text);
        }

        Console.ForegroundColor = savedColor;
    }
}
