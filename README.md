[![CI](https://github.com/lucasteles/Backdash/actions/workflows/ci.yml/badge.svg)](https://github.com/lucasteles/Backdash/actions/workflows/ci.yml)
[![Nuget](https://img.shields.io/nuget/v/Backdash.svg?style=flat)](https://www.nuget.org/packages/Backdash)

![](https://raw.githubusercontent.com/lucasteles/Backdash/site/test_report_badge.svg)
![](https://raw.githubusercontent.com/lucasteles/Backdash/site/lines_badge.svg)
![](https://img.shields.io/badge/Lang-C%23-green)
![](https://raw.githubusercontent.com/lucasteles/Backdash/site/dotnet_version_badge.svg)
![https://editorconfig.org/](https://img.shields.io/badge/style-EditorConfig-black)

# Backdash 🕹️

![](assets/images/banner.png)

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

💡 Please check the **[DOCUMENTATION](https://lucasteles.github.io/Backdash/docs/introduction)** for usage details.

### Demos:

| Title           | Link                                                                                                   |
|-----------------|--------------------------------------------------------------------------------------------------------|
| Console         | [![Console](https://img.youtube.com/vi/n-3G0AE5Ti0/default.jpg)](https://youtu.be/n-3G0AE5Ti0)         |
| Monogame Local  | [![Monogame Local](https://img.youtube.com/vi/JYf2MemyJaY/default.jpg)](https://youtu.be/JYf2MemyJaY)  |
| Monogame Online | [![Monogame Online](https://img.youtube.com/vi/LGM_9XfzRUI/default.jpg)](https://youtu.be/LGM_9XfzRUI) |

## Samples

Check the samples on the [/samples](https://github.com/lucasteles/Backdash/tree/master/samples) directory:

There are examples for up to 4 players:

- [Simple console game](https://github.com/lucasteles/Backdash/tree/master/samples/ConsoleGame)
- [Monogame SpaceWar](https://github.com/lucasteles/Backdash/tree/master/samples/SpaceWar) [^2]
- [Monogame SpaceWar with Lobby over internet](https://github.com/lucasteles/Backdash/tree/master/samples/SpaceWar.Lobby) [^1][^2]

[^1]: The sample needs a [web server](https://github.com/lucasteles/Backdash/tree/master/samples/LobbyServer) to
exchange players addresses. check the sample `README.md` for more information.

[^2]: If you are using *ARM* *MacOS*
you [may need the x64 version of .NET SDK](https://community.monogame.net/t/tutorial-for-setting-up-monogame-on-m1-m2-apple-silicon/19669)
to build some samples.

## Building from source

1. Clone this repo.
2. Building
    - With samples run `dotnet build -c Release Backdash.sln` on the root directory.
    - SDK only run `dotnet build -c Release src/Backdash` on the root directory.
    - Alternatively open the solution file `Backdash.sln` on your IDE.

## Licensing

[Backdash](https://github.com/lucasteles/Backdash) is available under The MIT License. This
means [Backdash](https://github.com/lucasteles/Backdash) is free for commercial and non-commercial use.
Attribution is not required, but appreciated.
