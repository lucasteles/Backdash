# Best Practices and Troubleshooting

Below is a list of recommended best practices you should consider while porting your application
to [Backdash](https://github.com/lucasteles/Backdash). Many of these recommendations are easy to follow even if you're
not starting a game from scratch. Most applications will already conform to most of the recommendations below.

## Isolate **Game State** from **Non-Game State**

[Backdash](https://github.com/lucasteles/Backdash) will periodically request that you save and load the entire state of
your game. For most games, the state that needs to be saved is a tiny fraction of the entire game. Usually, the video and
audio renderers, look-up tables, textures, sound data, and your code segments are either constant from frame to frame or
not involved in the calculation of the game state. These do not need to be saved or restored.

You should isolate **non-game state** from the **game state** as **much as possible**. For example, you may consider
encapsulating all your game state into a single `class`/`record` type. This both clearly delineates what is
**game state** and was is not and makes it trivial to implement the save and load callbacks.

## Define a Fixed Time Quanta for Advancing Your Game State

[Backdash](https://github.com/lucasteles/Backdash) will occasionally need to rollback and single-step your application
frame by frame. This is difficult to do if your game state advances by a variable tick rate. You should try to make your
game state advanced by a fixed time quanta per frame, even if your render loop does not.

## Separate Updating Game State from Rendering in Your Game Loop

[Backdash](https://github.com/lucasteles/Backdash) will call your advance frame callback many times during a rollback.
Any effects or sounds which are generated
during the rollback need to be deferred until after the rollback is finished. This is most easily accomplished by
separating your game state from your render state. When you're finished, your game loop may look something like this:

```csharp
   bool finished = false;
   GameState state;
   Inputs inputs;

   do {
      GetControllerInputs(ref inputs);
      finished = AdvanceGameState(inputs, state);
      if (!finished) {
         RenderCurrentFrame(gamestate);
      }
   while (!finished);
```

In other words, your game state should be determined solely by the inputs, your rendering code should be driven by the
current game state and you should have a way to easily advance the game state forward using a set of inputs without
rendering.

## Make Sure Your Game State Advances Deterministically

Once you have your game state identified, make sure the next game state is computed solely from your game inputs. This
should happen naturally if you have correctly identified all the game state and inputs, but it can be tricky sometimes.
Here are some things that are easy to overlook:

### Beware of Random Number Generators

Many games use random numbers in the computing of the next game state. If you use one, you must ensure that they are
fully deterministic, that the seed for the random number generator is the same at frame 0 for both players, and that the
state of the random number generator is included in your game state. Doing both of these will ensure that the random
numbers which get generated for a particular frame are always the same, regardless of how many
times [Backdash](https://github.com/lucasteles/Backdash) needs to rollback to that frame.

### Beware of External Time Sources (eg. random, clock time)

Be careful if you use the current time of day in your game state calculation. This may be used for an effect on the game
or to derive another game state (e.g. using the timer as a seed to the random number generator). The time on two computers
or game consoles is almost never in sync and using time in your game state calculations can lead to synchronization
issues. You should either eliminate the use of time in your game state or include the current time for one of the
players as part of the input to a frame and always use that time in your calculations.

The use of external time sources in **non-game state** calculations is fine (_e.g. computing the duration of effects on
screen, or the attenuation of audio samples_).

> [!INFORMATION]
> We provide an implementation of a _[Deterministic Random](https://lucasteles.github.io/Backdash/api/Backdash.Synchronizing.Random.IDeterministicRandom.html)_  out of the box
> which can be accessed directly from the _[Rollback Session](https://lucasteles.github.io/Backdash/api/Backdash.INetcodeSession-1.html#Backdash_INetcodeSession_1_Random)_

## Beware of Dangling References

If your game state contains any reference type be very careful in your `save` and `load` functions to rebase
your reference pointers as you `save` and `load` your data. When copying data, be sure that you are no copying an object
reference instead of the values.

## Beware of Static Variables or Other Hidden State

The language your game is written in may have features that make it difficult to track down all your state. [Static
automatic variables in `C`](https://www.javatpoint.com/auto-and-static-variable-in-c)
or [static members in `C#`](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/static-classes-and-static-class-members)
are examples of this behavior. You need to track down all these locations and convert them to
a form that can be saved. For example, compare:

```csharp
// This will totally get you into trouble.
public record MyGameState
{
    private static int Counter;

    public int NextCounter => Counter++;
}
```

To:

```csharp
// If you must, this is better
public static class GlobalState
{
    public static int Counter; // move counter to a global static class

    public static int GetNextCounter() => Counter++;
}

public record MyGameState
{
    public int CurrentCounter; // keeps track of the counter value
}

public class MySessionHandler : INetcodeSessionHandler
{
    MyGameState currentGameState = new();

    public void LoadState(in Frame frame, ref readonly BinaryBufferReader reader)
    {
        currentGameState.CurrentCounter = reader.ReadInt32();
        GlobalState.Counter = currentGameState.CurrentCounter;
    }

    /* ...  */
}
```

## Use the [Backdash](https://github.com/lucasteles/Backdash) Sync Test Feature. A Lot.

Once you've ported your application to [Backdash](https://github.com/lucasteles/Backdash), you can use
the [`CreateSyncTestSession`](https://lucasteles.github.io/Backdash/api/Backdash.RollbackNetcode.html#Backdash_RollbackNetcode_CreateSyncTestSession__2_System_Nullable_Backdash_Data_FrameSpan__Backdash_NetcodeOptions_Backdash_SessionServices___0___1__System_Boolean_)
function to help track down synchronization issues which may be the result of a leaky game state.

The sync test session is a special, single player session which is designed to find errors in your simulation's
determinism. When running in a **sync-test session**, [Backdash](https://github.com/lucasteles/Backdash) by default will
execute a 1 frame rollback for every frame of your game. It compares the state of the frame when it was executed the
first time to the state executed during the rollback, and raises an error if they differ during your game's execution.
If you set the [`LogLevel`](https://lucasteles.github.io/Backdash/api/Backdash.Core.LogLevel.html) to at
least `Information` the json of the states will be also logged, you can diff the log of the initial frame vs the log of
the rollback frame to track down errors.

By running **sync-test** on developer systems continuously when writing game code, you can identify **de-sync** causing
bugs immediately after they're introduced.

You can also set the **sync-test session** to auto-generate random inputs to help find de-syncs:

```csharp
using Backdash;
using Backdash.Sync.Input;

var session = RollbackNetcode.CreateSyncTestSession<MyGameInput>(
    options: new()
    {
        Log = new()
        {
            EnabledLevel = LogLevel.Debug,
        },
    },
    services: new()
    {
        InputGenerator = new RandomInputGenerator<MyGameInput>(),
    }
);
```

If you want a meaningful string representation of your state in the sync-test, you need to implement
 the method `GetStateString` in your handler.

```csharp
// print the state as json
public string GetStateString(in Frame frame, ref readonly BinaryBufferReader reader)
{
    GameState state = new();

    state.LoadState(in reader); // read and fill the state properties

    return JsonSerializer.Serialize(state, jsonOptions);
}

static readonly JsonSerializerOptions jsonOptions = new()
{
    WriteIndented = true,
    IncludeFields = true,
};

```

> [!NOTE]
> For better debugging the `RollbackNetcode.WithInputType<>().ForSyncTest(...)` accepts an implementation of the `IStateDesyncHandler`
> which is called whenever a state desync happens in the test.
>
> You can use it for enhanced state logging or showing semantic diffs.



## Where to Go from Here

This document describes the most basic features of [Backdash](https://github.com/lucasteles/Backdash).
To learn more, I recommend starting with reading
the [API Docs](https://lucasteles.github.io/Backdash/docs/introduction.html) and exploring
the [examples](https://github.com/lucasteles/Backdash/tree/master/samples).

Good luck!

