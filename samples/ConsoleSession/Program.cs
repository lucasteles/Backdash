using System.Diagnostics;
using ConsoleSession;

#pragma warning disable S1172
#pragma warning disable S2583
#pragma warning disable S2589
// ReSharper disable AccessToDisposedClosure

var intervalInSeconds = 1 / 60f;

using Game game = new(args);
using PeriodicTimer timer = new(TimeSpan.FromSeconds(intervalInSeconds));
using CancellationTokenSource cts = new();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cts.Cancel();
};

try
{
    var timestamp = Stopwatch.GetTimestamp();
    do
    {
        var delta = Stopwatch.GetElapsedTime(timestamp);
        timestamp = Stopwatch.GetTimestamp();

        game.Update(delta);
    } while (await timer.WaitForNextTickAsync(cts.Token));

    Console.Clear();
}
catch (TaskCanceledException)
{
    Console.Clear();
    Console.WriteLine("Cancel requested");
}
catch (OperationCanceledException)
{
    Console.Clear();
    Console.WriteLine("Cancel requested");
}


Console.WriteLine("Ending...");