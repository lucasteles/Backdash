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

