## Developer Guide

> adapted from [GGPO](https://github.com/pond3r/ggpo/edit/master/doc/DeveloperGuide.md)

### Installing

[NuGet package](https://www.nuget.org/packages/Backdash) available:

```ps
$ dotnet add package Backdash
```

## Game State and Inputs

Your game probably has many moving parts. [Backdash](https://github.com/lucasteles/Backdash) only depends on these two:

- **Game State** describes the current state of everything in your game. In a shooter, this would include the position
  of the ship and all the enemies on the screen, the location of all the bullets, how much health each opponent has, the
  current score, etc. etc.

- **Game Inputs** are the set of things which modify the game state. These obviously include the joystick and button
  presses done by the player, but can include other non-obvious inputs as well. For example, if your game uses the
  current time of day to calculate something in the game, the current time of day at the beginning of a frame is also an
  input.

There are many other things in your game engine that are neither game state nor inputs. For example, your audio and
video renderers are not game state since they don't have an effect on the outcome of the game. If you have a special
effects engine that's generating effects that do not have an impact on the game, they can be excluded from the game
state as well.

## Using State and Inputs for Synchronization

Each player in a [Backdash](https://github.com/lucasteles/Backdash) networked game has a complete copy of your game
running. [Backdash](https://github.com/lucasteles/Backdash) needs to keep both copies of the
game state in sync to ensure that both players are experiencing the same game. It would be much too expensive to send an
entire copy of the game state between players every frame. Instead [Backdash](https://github.com/lucasteles/Backdash)
sends the players' inputs to each other and has
each player step the game forward. In order for this to work, your game engine must meet three criteria:

1. The game simulation must be fully deterministic. That is, for any given game state and inputs, advancing the game
   state by exactly 1 frame must result in identical game states for all players.
2. The game state must be fully encapsulated and serializable.
3. Your game engine must be able to load, save, and execute a single simulation frame without rendering the result of
   that frame. **This will be used to implement rollbacks**.

## Programming Guide

The following section contains a walk-through for porting your application
to [Backdash](https://github.com/lucasteles/Backdash).

For a detailed description of the
[Backdash](https://github.com/lucasteles/Backdash) API, please see
the [API Reference Docs](https://lucasteles.github.io/Backdash/api/Backdash.html).

### Interfacing with [Backdash](https://github.com/lucasteles/Backdash)

[Backdash](https://github.com/lucasteles/Backdash) is designed to be easy to interface with new and existing game
engines. It handles most of the implementation of handling rollbacks by calling out to your application via
the [`IRollbackHandler<TState>`](https://lucasteles.github.io/Backdash/api/Backdash.IRollbackHandler-1.html) hooks.

### Creating the [`IRollbackSession`](https://lucasteles.github.io/Backdash/api/Backdash.IRollbackSession-2.html) Object

The [`IRollbackSession<TInput, TState>`](https://lucasteles.github.io/Backdash/api/Backdash.IRollbackSession-2.html)
object is your interface to the [Backdash](https://github.com/lucasteles/Backdash) framework. Create
one with
the [`RollbackNetcode.CreateSession`](https://lucasteles.github.io/Backdash/api/Backdash.RollbackNetcode.html#Backdash_RollbackNetcode_CreateSession__2_System_Int32_Backdash_RollbackOptions_Backdash_SessionServices___0___1__)
function passing the port to bind to locally and optionally other configuration with
a instance of [`RollbackOptions`](https://lucasteles.github.io/Backdash/api/Backdash.RollbackOptions.html).

For example, giving the user pre-defined types for the **Game State** and **Game Input**

```csharp
public class MyGameState : IEquatable<MyGameState> {
    public MyGameState() {
        /* initialize state */
    }

    /* members */
}

[Flags]
public enum MyGameInput {
    /* members */
}
```

To start a new session bound to port 9001, you would do:

```csharp
using Backdash;
using Backdash.Core;

var session = RollbackNetcode.CreateSession<MyGameInput, MyGameState>(9001);

// ...
```

You can also set many configurations passing an instance
of [`RollbackOptions`](https://lucasteles.github.io/Backdash/api/Backdash.RollbackOptions.html)

```csharp
using Backdash;
using Backdash.Core;

const int networkPort = 9001;

RollbackOptions options = new()
{
    FrameDelay = 2,
    Log = new()
    {
        EnabledLevel = LogLevel.Debug,
    },
};

var session = RollbackNetcode.CreateSession<MyGameInput, MyGameState>(networkPort, options);

```

You should also define an implementation of
the [`IRollbackHandler<TState>`](https://lucasteles.github.io/Backdash/api/Backdash.IRollbackHandler-1.html)
filled in with your game's callback functions for managing game state.

```csharp
public class MySessionHandler : IRollbackHandler<MyGameState>
{
    public void OnSessionStart() { /* ... */ }
    public void OnSessionClose() { /* ... */ }
    public void SaveState(in Frame frame, ref MyGameState state) { /* ... */ }
    public void LoadState(in Frame frame, in MyGameState gameState) { /* ... */ }
    public void AdvanceFrame() { /* ... */ }
    public void TimeSync(FrameSpan framesAhead) { /* ... */ }
    public void OnPeerEvent(PlayerHandle player, PeerEventInfo evt) { /* ... */ }
}
```

And then, set it into the session:

```csharp
session.SetHandler(new MySessionHandler());
```

The [`IRollbackSession`](https://lucasteles.github.io/Backdash/api/Backdash.IRollbackSession-2.html) object should only
be used for a single game session. If you need to connect to another opponent, dispose your existing object using
the [`.Dispose`](https://learn.microsoft.com/pt-br/dotnet/api/system.idisposable.dispose?view=net-8.0)
method and start a new one:

```csharp
/* Close the current session to start a new one */
session.Dispose();
```

### Sending Player Locations

When you created the [`IRollbackSession`](https://lucasteles.github.io/Backdash/api/Backdash.IRollbackSession-2.html)
you don't specify any information about the players participating in the game. To do so, call
the [`.AddPlayer()`](https://lucasteles.github.io/Backdash/api/Backdash.IRollbackSession-2.html#Backdash_IRollbackSession_2_AddPlayers_System_Collections_Generic_IReadOnlyList_Backdash_Player__)
method function with a instance of [`Player`](https://lucasteles.github.io/Backdash/api/Backdash.Player.html) for each
player. The following example show how you might
use [`.AddPlayer()`](https://lucasteles.github.io/Backdash/api/Backdash.IRollbackSession-2.html#Backdash_IRollbackSession_2_AddPlayers_System_Collections_Generic_IReadOnlyList_Backdash_Player__)
in a 2 player game:

```csharp
LocalPlayer player1 = new(1); // local player number 1

var player2Endpoint = IPEndPoint.Parse("192.168.0.100:8001"); // player 2 ip and port
RemotePlayer player2 = new(2, player2Endpoint); // remote player number 2

ResultCode result;
result = session.AddPlayer(player1);
// ...
result = session.AddPlayer(player2);
// ...
```

Check the [samples](https://github.com/lucasteles/Backdash/tree/master/samples) more complex cases.

### Starting session

After setting up players you must call the
session [.Start()](https://lucasteles.github.io/Backdash/api/Backdash.IRollbackSession-2.html#Backdash_IRollbackSession_2_Start_System_Threading_CancellationToken_)
method. This will start all the background work like socket receiver, input queue, peer synchronization, etc.

```csharp
session.Start();
```

### Synchronizing Local and Remote Inputs

Input synchronization happens on
the [session](https://lucasteles.github.io/Backdash/api/Backdash.IRollbackSession-1.html) at the top of each game frame.
This is done by
calling [`AddLocalInput`](https://lucasteles.github.io/Backdash/api/Backdash.IRollbackSession-1.html#Backdash_IRollbackSession_1_AddLocalInput_Backdash_PlayerHandle__0_)
for each local player
and [`SynchronizeInputs`](https://lucasteles.github.io/Backdash/api/Backdash.IRollbackSession-1.html#Backdash_IRollbackSession_1_SynchronizeInputs)
to fetch the inputs for remote players. Be sure to check the return value of `SynchronizeInputs`. If it returns a value
other than [ResultCode.Ok`](https://lucasteles.github.io/Backdash/api/Backdash.ResultCode.html), you should
**not advance your game state**. This usually happens because [Backdash](https://github.com/lucasteles/Backdash) has not
received packets from the remote player in a while and has reached its internal prediction limit.

After synchronizing you can read the players inputs using
the [`GetInput`](https://lucasteles.github.io/Backdash/api/Backdash.IRollbackSession-1.html#Backdash_IRollbackSession_1_GetInput_System_Int32_)
method for a single player
or [`GetInputs`](https://lucasteles.github.io/Backdash/api/Backdash.IRollbackSession-1.html#Backdash_IRollbackSession_1_GetInputs_System_Span_Backdash_Data_SynchronizedInput__0___)
to load all players inputs into a buffer.

For example, if your code looks like this currently for a local game:

```csharp
MyGameInput player1Input = GetControllerInput(0);
MyGameInput player2Input = GetControllerInput(1);

/* send p1 and p2 to the game */
AdvanceGameState(player1Input, player2Input, gameState);
```

You should change it to read as follows:

```csharp
// usually a reusable reference
var gameInputs = new MyGameInput[2];

// you must keep the player handlers or read then with session.GetPlayers()
var player1Handle = player1.Handle;

var localInput = GetControllerInput(0); // read the controller

// notify Backdash of the local player's inputs
var result = session.AddLocalInput(player1Handle, localInput);

if (result is ResultCode.Ok)
{
    result = session.SynchronizeInputs();
    if (result is ResultCode.Ok)
    {
        session.GetInputs(gameInputs);
        AdvanceGameState(gameInputs[0], gameInputs[1], gameState);
    }
}
```

You should
call [`SynchronizeInputs`](https://lucasteles.github.io/Backdash/api/Backdash.IRollbackSession-1.html#Backdash_IRollbackSession_1_SynchronizeInputs)
every frame, even those that happen during a rollback. Make sure you always use the values returned
from [`GetInputs`](https://lucasteles.github.io/Backdash/api/Backdash.IRollbackSession-1.html#Backdash_IRollbackSession_1_GetInputs_System_Span_Backdash_Data_SynchronizedInput__0___)
rather than the values you've read from the local controllers to advance your game state. During a
rollback [`SynchronizeInputs`](https://lucasteles.github.io/Backdash/api/Backdash.IRollbackSession-1.html#Backdash_IRollbackSession_1_SynchronizeInputs)
will replace the values passed
into [`AddLocalInput`](https://lucasteles.github.io/Backdash/api/Backdash.IRollbackSession-1.html#Backdash_IRollbackSession_1_AddLocalInput_Backdash_PlayerHandle__0_)
with the values used for previous frames. Also, if you've manually added input delay for the local player to smooth out
the effect of rollbacks, the inputs you pass
into [`AddLocalInput`](https://lucasteles.github.io/Backdash/api/Backdash.IRollbackSession-1.html#Backdash_IRollbackSession_1_AddLocalInput_Backdash_PlayerHandle__0_)
won't actually be returned
in [`GetInputs`](https://lucasteles.github.io/Backdash/api/Backdash.IRollbackSession-1.html#Backdash_IRollbackSession_1_GetInputs_System_Span_Backdash_Data_SynchronizedInput__0___)
until after the frame delay.

### Implementing your save, load, and free Callbacks

[Backdash](https://github.com/lucasteles/Backdash) will use the `load_game_state` and `save_game_state` callbacks to
periodically save and restore the state of your
game. The `save_game_state` function should create a buffer containing enough information to restore the current state
of the game and return it in the `buffer` out parameter. The `load_game_state` function should restore the game state
from a previously saved buffer. For example:

```
struct GameState gamestate;  // Suppose the authoritative value of our game's state is in here.

bool __cdecl
ggpo_save_game_state_callback(unsigned char **buffer, int *len,
                              int *checksum, int frame)
{
   *len = sizeof(gamestate);
   *buffer = (unsigned char *)malloc(*len);
   if (!*buffer) {
      return false;
   }
   memcpy(*buffer, &gamestate, *len);
   return true;
}

bool __cdecl
ggpo_load_game_state_callback(unsigned char *buffer, int len)
{
   memcpy(&gamestate, buffer, len);
   return true;
}
```

[Backdash](https://github.com/lucasteles/Backdash) will call your `free_buffer` callback to dispose of the memory you
allocated in your `save_game_state` callback
when it is no longer need.

```
void __cdecl
ggpo_free_buffer(void *buffer)
{
   free(buffer);
}
```

### Implementing Remaining Callbacks

As mentioned previously, there are no optional callbacks in the `GGPOSessionCallbacks` structure. They all need to at
least `return true`, but the remaining callbacks do not necessarily need to be implemented right away. See the comments
in `ggponet.h` for more information.

### Calling the [Backdash](https://github.com/lucasteles/Backdash) Advance and Idle Functions

We're almost done. Promise. The last step is notify [Backdash](https://github.com/lucasteles/Backdash) every time your
gamestate finishes advancing by one frame. Just
call `ggpo_advance_frame` after you've finished one frame but before you've started the next.

[Backdash](https://github.com/lucasteles/Backdash) also needs some amount of time to send and receive packets do its own
internal bookkeeping. At least once per-frame
you should call the `ggpo_idle` function with the number of milliseconds you're
allowing [Backdash](https://github.com/lucasteles/Backdash) to spend.

## Tuning Your Application: Frame Delay vs. Speculative Execution

[Backdash](https://github.com/lucasteles/Backdash) uses both frame delay and speculative execution to hide latency. It
does so by allowing the application developer
the choice of how many frames that they'd like to delay input by. If it takes more time to transmit a packet than the
number of frames specified by the game, [Backdash](https://github.com/lucasteles/Backdash) will use speculative
execution to hide the remaining latency. This number
can be tuned by the application mid-game if you so desire. Choosing a proper value for the frame delay depends very much
on your game. Here are some helpful hints.

In general you should try to make your frame delay as high as possible without affecting the qualitative experience of
the game. For example, a fighting game requires pixel perfect accuracy, excellent timing, and extremely tightly
controlled joystick motions. For this type of game, any frame delay larger than 1 can be noticed by most intermediate
players, and expert players may even notice a single frame of delay. On the other hand, board games or puzzle games
which do not have very strict timing requirements may get away with setting the frame latency as high as 4 or 5 before
users begin to notice.

Another reason to set the frame delay high is to eliminate the glitching that can occur during a rollback. The longer
the rollback, the more likely the user is to notice the discontinuities caused by temporarily executing the incorrect
prediction frames. For example, suppose your game has a feature where the entire screen will flash for exactly 2 frames
immediately after the user presses a button. Suppose further that you've chosen a value of 1 for the frame latency and
the time to transmit a packet is 4 frames. In this case, a rollback is likely to be around 3 frames (4 – 1 = 3). If the
flash occurs on the first frame of the rollback, your 2-second flash will be entirely consumed by the rollback, and the
remote player will never get to see it!  In this case, you're better off either specifying a higher frame latency value
or redesigning your video renderer to delay the flash until after the rollback occurs.

## Sample Application

The Vector War application in the source directory contains a simple application which
uses [Backdash](https://github.com/lucasteles/Backdash) to synchronize the two
clients. The command line arguments are:

```
vectorwar.exe  <localport>  <num players> ('local' | <remote ip>:<remote port>) for each player
```

See the .cmd files in the bin directory for examples on how to start 2, 3, and 4 player games.

## Best Practices and Troubleshooting

Below is a list of recommended best practices you should consider while porting your application to GGPO. Many of these
recommendations are easy to follow even if you're not starting a game from scratch. Most applications will already
conform to most of the recommendations below.

### Isolate Game State from Non-Game State

[Backdash](https://github.com/lucasteles/Backdash) will periodically request that you save and load the entire state of
your game. For most games the state that needs
to be saved is a tiny fraction of the entire game. Usually the video and audio renderers, look up tables, textures,
sound data and your code segments are either constant from frame to frame or not involved in the calculation of game
state. These do not need to be saved or restored.

You should isolate non-game state from the game state as much as possible. For example, you may consider encapsulating
all your game state into a single C structure. This both clearly delineates what is game state and was is not and makes
it trivial to implement the save and load callbacks (see the Reference Guide for more information).

### Define a Fixed Time Quanta for Advancing Your Game State

[Backdash](https://github.com/lucasteles/Backdash) will occasionally need to rollback and single-step your application
frame by frame. This is difficult to do if your
game state advances by a variable tick rate. You should try to make your game state advanced by a fixed time quanta per
frame, even if your render loop does not.

### Separate Updating Game State from Rendering in Your Game Loop

[Backdash](https://github.com/lucasteles/Backdash) will call your advance frame callback many times during a rollback.
Any effects or sounds which are genearted
during the rollback need to be deferred until after the rollback is finished. This is most easily accomplished by
separating your game state from your render state. When you're finished, your game loop may look something like this:

```
   Bool finished = FALSE;
   GameState state;
   Inputs inputs;

   do {
      GetControllerInputs(&inputs);
      finished = AdvanceGameState(&inputs, &state);
      if (!finished) {
         RenderCurrentFrame(&gamestate);
      }
   while (!finished);
```

In other words, your game state should be determined solely by the inputs, your rendering code should be driven by the
current game state, and you should have a way to easily advance the game state forward using a set of inputs without
rendering.

### Make Sure Your Game State Advances Deterministically

Once you have your game state identified, make sure the next game state is computed solely from your game inputs. This
should happen naturally if you have correctly identified all the game state and inputs, but it can be tricky sometimes.
Here are some things which are easy to overlook:

#### Beware of Random Number Generators

Many games use random numbers in the computing of the next game state. If you use one, you must ensure that they are
fully deterministic, that the seed for the random number generator is same at frame 0 for both players, and that the
state of the random number generator is included in your game state. Doing both of these will ensure that the random
numbers which get generated for a particular frame are always the same, regardless of how many
times [Backdash](https://github.com/lucasteles/Backdash) needs to
rollback to that frame.

#### Beware of External Time Sources (aka. Wall clock time)

Be careful if you use the current time of day in your game state calculation. This may be used for an effect on the game
or to derive other game state (e.g. using the timer as a seed to the random number generator). The time on two computers
or game consoles is almost never in sync and using time in your game state calculations can lead to synchronization
issues. You should either eliminate the use of time in your game state or include the current time for one of the
players as part of the input to a frame and always use that time in your calculations.

The use of external time sources in non-gamestate calculations is fine (e.g. computing the duration of effects on
screen, or the attenuation of audio samples).

### Beware of Dangling References

If your game state contains any dynamically allocated memory be very careful in your save and load functions to rebase
your pointers as you save and load your data. One way to mitigate this is to use a base and offset to reference
allocated memory instead of a pointer. This can greatly reduce the number of pointers you need to rebase.

### Beware of Static Variables or Other Hidden State

The language your game is written in may have features which make it difficult to track down all your state. Static
automatic variables in C are an example of this behavior. You need to track down all these locations and convert them to
a form which can be saved. For example, compare:

```
   // This will totally get you into trouble.
   int get_next_counter(void) {
      static int counter = 0; /* no way to roll this back... */
      counter++;
      return counter;
   }
```

To:

```
   // If you must, this is better
   static int global_counter = 0; /* move counter to a global */

   int get_next_counter(void) {
      global_counter++;
      return global_counter; /* use the global value */
   }

   bool __cdecl
   ggpo_load_game_state_callback(unsigned char *buffer, int len)
   {
      ...
      global_counter = *((int *)buffer) /* restore it in load callback */
      ...
      return true;
   }
```

### Use the [Backdash](https://github.com/lucasteles/Backdash) Sync Test Feature. A Lot.

Once you've ported your application to [Backdash](https://github.com/lucasteles/Backdash), you can use
the `ggpo_start_synctest` function to help track down synchronization issues which may be the result of leaky game
state.

The sync test session is a special, single player session which is designed to find errors in your simulation's
determinism. When running in a synctest session, [Backdash](https://github.com/lucasteles/Backdash) will execute a 1
frame rollback for every frame of your game. It
compares the state of the frame when it was executed the first time to the state executed during the rollback, and
raises an error if they differ. If you used the `ggpo_log` function during your game's execution, you can diff the log
of the initial frame vs the log of the rollback frame to track down errors.

By running synctest on developer systems continuously when writing game code, you can identify desync causing bugs
immediately after they're introduced.

## Where to Go from Here

This document describes the most basic features of GGPO. To learn more, I recommend starting with reading the comments
in the `ggponet.h` header and just diving into the code. Good luck!

