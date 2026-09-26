# Steam Plugin for Logi Options+ / Loupedeck

Launch your Steam games from a Logitech or Loupedeck device.

## Features

- **Library**: adds one action per Steam game installed on your computer, showing the game's Steam icon. The list updates automatically when you install or uninstall a game.
- **Launch Game**: a configurable action that launches any Steam game by its app ID (for example `730`).

Games are launched through the `steam://rungameid/<appid>` protocol, so the Steam client must be installed.

## Requirements

- Windows or macOS with the Steam client installed
- Logi Options+ or Loupedeck software (version 6.0 or later)
- [.NET 10 SDK](https://dotnet.microsoft.com/download) to build

## Building

```bash
dotnet build SteamPlugin.sln
```

The build output goes to `bin/<Configuration>/`, and a `.link` file is written to the Logi Plugin Service plugin folder so the service loads the plugin straight from the build output. Restart the plugin service (or reload plugins) to pick up changes.

## Project structure

| Path | Purpose |
| --- | --- |
| `src/SteamPlugin.cs` | Plugin entry point |
| `src/SteamLibrary.cs` | Finds installed games by reading Steam's local library files and watches for changes |
| `src/Actions/` | Plugin actions (`InstalledGamesCommand`, `LaunchGameCommand`) |
| `src/Helpers/` | Logging and resource helpers |
| `src/package/metadata/` | Package metadata (`LoupedeckPackage.yaml`) and icon |

## Resources

- [Logi Actions SDK documentation](https://logitech.github.io/actions-sdk-docs/)

## License

MIT
