# Introduction

[Backdash](https://github.com/lucasteles/Backdash) is a multiplayer network SDK to help you
implement [Rollback Netcode](https://words.infil.net/w02-netcode.html) on your game.

It started as [.NET](https://dotnet.microsoft.com) port of the first rollback netcode
SDK [GGPO](https://github.com/pond3r/ggpo) (_written in C++_)
to be used in any game engine that
uses [C#](https://dotnet.microsoft.com/en-us/languages/csharp), [F#](https://dotnet.microsoft.com/en-us/languages/fsharp)
or any [.NET](https://dotnet.microsoft.com) dialect as scripting language. Dispensing with the need to create native
builds and native binds to the _C++_ [GGPO](https://github.com/pond3r/ggpo) code.

And also adding more options
for [configuration](https://lucasteles.github.io/Backdash/api/Backdash.RollbackOptions.html)
and [extensibility](https://lucasteles.github.io/Backdash/api/Backdash.SessionServices-2.html).

> 💡 This is not able to run on [Unity](https://unity.com/) until they
> finish [the CoreCLR port](https://blog.unity.com/engine-platform/porting-unity-to-coreclr).

## How Does It Work?

Rollback networking is designed to be integrated into a fully deterministic peer-to-peer engine. With full determinism,
the game is guaranteed to play out the same way on all players computers if we simply feed them the same inputs. One way
to achieve this is to exchange inputs for all players over the network, only execution a frame of gameplay logic when
all players have received all the inputs from their peers. This often results in sluggish, unresponsive gameplay. The
longer it takes to get inputs over the network, the slower the game becomes.

In rollback networking, game logic is allowed to proceed with just the inputs from the local player. If the remote
inputs have not yet arrived when it's time to execute a frame, the networking code will predict what it expects the
remote players to do based on previously seen inputs. Since there's no waiting, the game feels just as responsive as it
does offline. When those inputs finally arrive over the network, they can be compared to the ones that were predicted
earlier. If they differ, the game can be re-simulated from the point of divergence to the current visible frame.

Don't worry if that sounds like a headache. [Backdash](https://github.com/lucasteles/Backdash) was designed specifically
to implement the rollback algorithms and low-level networking logic in a way that's easy to integrate into your existing
game loop. If you simply implement the functionality to save your game state, load it back up, and execute a frame of
game state without rendering its outcome, [Backdash](https://github.com/lucasteles/Backdash) can take care of the rest.

## Learning resources

**More information about how it works and why is it good:**

- [Analysis: Why Rollback Netcode Is Better](https://www.youtube.com/watch?v=0NLe4IpdS1w) _(video)_.
- [Infil's Netcode Article](https://words.infil.net/w02-netcode-p2.html).
- [Talking Rollback Netcode With Adam "Keits" Heart](https://www.youtube.com/watch?v=1RI5scXYhK0) _(video)_.
- [GDC Rollback Networking in Mortal Kombat and Injustice 2](https://www.youtube.com/watch?v=7jb0FOcImdg) _(video)_.
- [EVO 2017: GGPO panel](https://www.youtube.com/watch?v=k9JTIn1SVQ4) _(video)_.
- [Fight the Lag! The Trick Behind GGPO's Low Latency Netcode](https://drive.google.com/file/d/1cV0fY8e_SC1hIFF5E1rT8XRVRzPjU8W9/view)
- [Cross Counter LIVE feat. Mike Z](https://www.youtube.com/watch?v=Tu2kAdmUCaI&t=41m22s) _(video)_.

