# IT25 Capstone Project for Game Development

## Project Title
Lutuoan

## Group Members
1. Dion Gaelle Subion
2. Nikko Uy.
3. Rachelle Ann Rebucan
4. Rojane Althea Brito
5. Luke Acosta

## Description
Lutuoan (v1.0) is a Unity 2D carinderia cooking and time-management game with an official WebGL release build (`Build/WebGL/`) configured for both desktop and mobile browsers. Players prep ingredients, cook dishes in pots and pans, pour drinks, and serve customers before their patience runs out while managing burning cookware with a fire extinguisher. The game features a 3-day campaign progression loop (Opening Shift, Lunch Rush, and Fiesta Peak) with a Day 1 tutorial, a kitchen Upgrade Shop (`UpgradeShopScene`) powered by earned Skill Points, and an unlockable Endless Mode with dynamic rush tiers, 3 lives, and a persistent Top-10 leaderboard.

## Tools Used
- Unity `6000.6.7f1` (2D URP, Input System, uGUI, TextMeshPro, WebGL Build Profiles)
- C#
- Custom Responsive WebGL Template (`Assets/WebGLTemplates/Responsive/index.html`, WebGL 2, Gzip compression with decompression fallback)
- Local JSON & Browser Storage (`SaveSystem` and `BrowserStorageBridge`)
- Firebase Unity SDK `14.1.0` (bundled in `Assets/Firebase`)
- GitHub Desktop

## Database Used
- Local JSON save storage (`savegame.json` with `.bak` backup in `Application.persistentDataPath`) paired with `BrowserStorageBridge` (`PlayerPrefs` and WebGL `localStorage`/IDBFS) for campaign saves and the Endless Mode leaderboard.
- Firebase SDK (`14.1.0`) is included in `Assets/Firebase` for future cloud database integration, though active gameplay currently stores data locally.

## Data Saved
- **Campaign & Upgrade Save (`SaveData`):**
  - Save version, created/last-saved UTC timestamps, and total play time
  - Current day (`1–3`), highest day cleared, and Endless Mode unlock status
  - Skill Points and kitchen upgrade ranks (`Prep Mastery`, `Fast Burner`, `Hospitality`)
  - Coins, high score, Endless high score, Endless best served, dishes served, unlocked recipes, and completed lessons
- **Endless Mode Leaderboard (`LeaderboardEntry`, Top 10):**
  - Player name (up to 8 characters)
  - Score
  - Time spent (seconds)
  - Dishes served
  - Highest combo streak
  - Recorded UTC timestamp

## Repository Access
- The instructor GitHub account `gracheleliza` was added as collaborator.

## Known Limitations
- Save data and the Top-10 Endless leaderboard are stored locally per device or browser (`savegame.json`, `PlayerPrefs`, and WebGL `localStorage`/IDBFS); online leaderboard syncing and user authentication are not yet active (the Firebase SDK in `Assets/Firebase` is not wired into runtime scripts).
- WebGL release builds require a browser with WebGL 2 support and must be served over HTTP/HTTPS (opening `Build/WebGL/index.html` directly over `file://` is blocked by browsers).
- Campaign mode currently covers Days 1–3 (`Opening Shift`, `Lunch Rush`, `Fiesta Peak`) plus Endless Mode; three additional upgrade cards in `UpgradeShopScene` are locked previews for future versions.
- No Internet connection is required after loading; the game is playable offline.

## References
List all tutorials, documentation, or guides used.
