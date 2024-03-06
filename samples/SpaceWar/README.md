# SpaceWar - NetCode Sample

## Running

Run one of the scripts in [/scripts](/samples/SpaceWar/scripts) to start multiple clients of the game. 

> On Windows run the `.cmd` [/scripts/windows](/samples/SpaceWar/scripts/windows)

> On Linux/Mac run the `.sh` files [/scripts/linux](/samples/SpaceWar/scripts/linux)

Each script defines a configuration with up to 4 players, some of them with spectators, they are:

- **start_2players**: start 2 game peer instances.
- **start_2players_1spec:** Start 2 game peer instances with a single spectator on player 1.
- **start_2players_2spec:** Start 2 game peer instances with two spectators. each observing one player.
- **start_3players:** Start 3 game peer instances.
- **start_4players:** Start 4 game peer instances.
- **start_4players_2spec:** Start 4 game peer instances with two spectators. one observing player 1 and the other
  observing player 2.

## Controls

- **Arrows**: Move
- **Left Control**: Fire
- **Enter**: Missile
