# LAN team deathmatch

Unity 6000.6.0f1; Netcode for GameObjects 2.13.2; Unity Transport 6.6.0.

## Play

Open `Assets/Scenes/MultiplayerArena.unity` and press Play, or use the **LAN Team Deathmatch** button in MainMenu.

1. On the host, click **Host match**. The server listens on UDP port 7777.
2. On another computer on the same LAN, start the same game build, enter the host computer's LAN IPv4 address, and click **Join match**. Use `127.0.0.1` when both processes run on the same computer.
3. If Windows requests firewall access, the user must allow the game on their private network for other computers to connect.

For a second instance, use Unity's **File > Build Profiles** and build Windows. Include the multiplayer scene (the setup adds it to the scene list). The regular build starts at MainMenu; choose LAN Team Deathmatch. Both players must use matching project/build versions.

The server-only button runs a match without a local player. No Relay, accounts, matchmaking or public server browser is configured.

## Rules and controls

- Up to 12 connections, automatically assigned to the smaller team. Existing players keep their teams when someone leaves.
- Red and Blue player bodies identify teams. Friendly fire is off; teammates still block bullets.
- 100 health, 30-round magazine, 30 damage per hit, 600 rounds/minute, 2-second reload, unlimited reserve ammunition.
- Three-second respawn with one second of protection. Spawn selection prefers points further from living enemies.
- First team to 25 kills wins. After ten seconds, scores reset and everyone respawns.
- WASD move, Shift sprint, Space jump, mouse look, left mouse fire, right mouse zoom, R reload, Tab scoreboard, Escape local menu.
- Escape blocks local controls without pausing the server. Players can still be killed while their menu is open.

## Implementation

- `MultiplayerMenu`: direct-IP connection UI, connection approval, disconnect/reconnect, local HUD and scoreboard.
- `MultiplayerPlayer`: owner-only camera/input, owner-authoritative NetworkTransform movement, server-authoritative ammo/reload/raycast damage/health/respawning, replicated shot tracers.
- `TeamDeathmatch`: server-owned teams, scores, winner, spawn selection and match restart.
- `Assets/Multiplayer/MultiplayerPlayer.prefab`: generated from MainPlayer, retaining the rifle model and camera; the original prototype scripts are replaced on this prefab.
- `MultiplayerArenaSetup`: **Vietnam Game > Create Multiplayer Arena** creates a separate copy of the saved SampleScene and its multiplayer assets if they do not exist. It does not overwrite an existing arena. Spawn transforms can be moved in the arena hierarchy.

SampleScene retains its single-player scripts. The multiplayer scene uses the first rifle only; melee, weapon switching and pickups are not networked. Existing prototype HUD and local gameplay scripts on world objects are disabled in the multiplayer scene.

## Prototype limits

This is a starting point for a classic match-based FPS, not a full Call of Duty 2 feature set. Movement trusts the owning client and shots use the server's current positions, without rewind/lag compensation. It is not hardened for competitive public servers. Remote bodies currently show team-colored capsules without aim/locomotion animation. Spawn points are initial placements and need playtesting on the map. There is no team selection, loadout menu, round timer, map rotation, spectator camera, host migration or persistent player identity yet.

## Integration test

`MultiplayerSmokeTest` is an opt-in development-only three-client test. It requires a development build of MultiplayerArena as its first scene. The editor build helper consumes `Temp/BuildMultiplayerSmoke.request` after script reload and writes `Temp/MultiplayerSmokeBuild.result`; the build goes to `Builds/MultiplayerSmoke/VietnamMultiplayer.exe`.

Launch a server with `-batchmode -nographics -multiplayerSmoke server <absolute-result-path>`, then three client processes with `-batchmode -nographics -multiplayerSmoke client <different-absolute-result-path>` each. Use `-logFile <different-log-path>` for each process. The test uses loopback UDP port 17877, moves players above the terrain to isolate combat, and quits with PASS/FAIL result files. It checks camera ownership, balanced teams, friendly-fire suppression, ammo consumption, kills/deaths, reload, respawn, score limit and automatic match reset. This opt-in test does not replace manual movement, visual or two-computer LAN checks.
