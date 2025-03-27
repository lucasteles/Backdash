[![Nuget](https://img.shields.io/nuget/v/Backdash.svg?style=flat)](https://www.nuget.org/packages/Backdash)
![](https://raw.githubusercontent.com/lucasteles/Backdash/site/dotnet_version_badge.svg)
![https://editorconfig.org/](https://img.shields.io/badge/style-EditorConfig-black)

[![CI](https://github.com/lucasteles/Backdash/actions/workflows/ci.yml/badge.svg)](https://github.com/lucasteles/Backdash/actions/workflows/ci.yml)
![](https://raw.githubusercontent.com/lucasteles/Backdash/site/test_report_badge.svg)
![](https://raw.githubusercontent.com/lucasteles/Backdash/site/lines_of_code.svg)


# Backdash 🕹️

[![](assets/images/banner.png)](https://github.com/lucasteles/Backdash)

Highly configurable and extensible implementation
of [Rollback Netcode](https://en.wikipedia.org/wiki/Netcode#Rollback) with full asynchronous IO.

> **Heavily** inspired by [GGPO](https://github.com/pond3r/ggpo).

## Overview

Traditional techniques account for network transmission time by adding delay to a players input, resulting in a
sluggish, laggy game-feel. Rollback networking uses input prediction and speculative execution to send player inputs to
the game immediately, providing the illusion of a zero-latency network. Using rollback, the same timings, reactions,
visual and audio queues, and muscle memory your players build up playing offline will translate directly
online. [Backdash](https://github.com/lucasteles/Backdash) is designed to make incorporating rollback networking (_aka.
Rollback [Netcode](https://words.infil.net/w02-netcode.html)_) into new and existing games as easy as possible.

## Getting started

[NuGet package](https://www.nuget.org/packages/Backdash) available:

```ps
$ dotnet add package Backdash
```

> [!TIP]
> 💡 Please check the **[DOCUMENTATION](https://lucasteles.github.io/Backdash/docs/introduction)** for usage details.

### Demos:

| Title            | Link                                                                                                        |
|------------------|-------------------------------------------------------------------------------------------------------------|
| Terminal         | [![Terminal](https://img.youtube.com/vi/n-3G0AE5Ti0/default.jpg)](https://youtu.be/n-3G0AE5Ti0)             |
| Monogame Local   | [![Monogame Local](https://img.youtube.com/vi/JYf2MemyJaY/default.jpg)](https://youtu.be/JYf2MemyJaY)       |
| Monogame Lobby   | [![Monogame Online](https://img.youtube.com/vi/LGM_9XfzRUI/default.jpg)](https://youtu.be/LGM_9XfzRUI)      |
| Godot Lobby      | [![Godot Online](https://img.youtube.com/vi/8M8QnTiJZzA/default.jpg)](https://youtu.be/8M8QnTiJZzA)         |
| Save/Load Replay | [![Save and load Replay](https://img.youtube.com/vi/iSbOJpLCx5M/default.jpg)](https://youtu.be/iSbOJpLCx5M) |

## Samples

Check the samples on the [/samples](https://github.com/lucasteles/Backdash/tree/master/samples) directory:

There are examples for up to 4 players:

- [Simple console game](https://github.com/lucasteles/Backdash/tree/master/samples/ConsoleGame)
- [Monogame SpaceWar](https://github.com/lucasteles/Backdash/tree/master/samples/SpaceWar) [^2]
- [Monogame SpaceWar with lobby over internet](https://github.com/lucasteles/Backdash/tree/master/samples/SpaceWar.Lobby) [^1][^2]
- [Godot SpaceWar with lobby over internet](https://github.com/lucasteles/BackdashGodotSample)

[^1]: The sample needs a [web server](https://github.com/lucasteles/Backdash/tree/master/samples/LobbyServer) to
exchange players addresses. check the sample `README.md` for more information.

[^2]: If you are using *ARM* *MacOS*
you [may need the x64 version of .NET SDK](https://community.monogame.net/t/tutorial-for-setting-up-monogame-on-m1-m2-apple-silicon/19669)
to build some samples.

## Building from source

You need to have installed [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download)
![](https://raw.githubusercontent.com/lucasteles/Backdash/site/dotnet_version_badge.svg)

1. Clone this repository.
2. Restore tools
    - On root directory run: `dotnet tool restore`
3. Building Library (_root directory_)
    - SDK only: `dotnet nuke build --configuration Release`
        - Alternatively open the solution file `Backdash.sln` on your IDE.
    - SDK and samples `dotnet nuke build-samples --configuration Release`.
        - Alternatively open the solution file `Samples/Backdash.Samples.sln` on your IDE.

## Licensing

[Backdash](https://github.com/lucasteles/Backdash) is available under The MIT License. This
means [Backdash](https://github.com/lucasteles/Backdash) is free for commercial and non-commercial use.

Attribution is not required, but appreciated.
