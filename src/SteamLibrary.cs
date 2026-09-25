namespace Loupedeck.SteamPlugin
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.Win32;

    // A game installed through Steam on this computer.
    public record SteamGame(UInt32 AppId, String Name);

    // This class finds the games installed on this computer by reading Steam's local library files:
    //   <Steam>/steamapps/libraryfolders.vdf lists every library folder (one per drive the user added),
    //   <library>/steamapps/appmanifest_<appid>.acf describes each installed game.
    // It also watches those folders so the list updates when a game is installed or uninstalled.

    public class SteamLibrary : IDisposable
    {
        // Steamworks Common Redistributables: installed alongside games, but not something you launch.
        private const UInt32 RedistributablesAppId = 228980;

        // Matches a "key" "value" line in Steam's VDF/ACF text format.
        private static readonly Regex KeyValueRegex = new Regex("^\\s*\"(?<key>[^\"]+)\"\\s+\"(?<value>(?:[^\"\\\\]|\\\\.)*)\"", RegexOptions.Compiled);

        private readonly List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();

        // Gets the installed games from the most recent scan, sorted by name.
        public IReadOnlyList<SteamGame> Games { get; private set; } = Array.Empty<SteamGame>();

        // Raised after the game list has been rescanned.
        public event EventHandler GamesChanged;

        // Scans all Steam library folders for installed games and starts watching them for changes.
        public void Refresh()
        {
            var steamPath = FindSteamPath();
            if (steamPath == null)
            {
                PluginLog.Warning("Steam installation not found; no games will be listed");
                return;
            }

            var libraryFolders = GetLibraryFolders(steamPath);

            this.Games = libraryFolders
                .SelectMany(GetInstalledGames)
                .Where(game => game.AppId != RedistributablesAppId)
                .DistinctBy(game => game.AppId)
                .OrderBy(game => game.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            PluginLog.Info($"Found {this.Games.Count} installed Steam games in {libraryFolders.Count} library folder(s)");
            this.WatchLibraryFolders(libraryFolders);
            this.GamesChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose() => this.StopWatching();

        private static String FindSteamPath()
        {
            if (OperatingSystem.IsWindows())
            {
                var path = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as String;
                return path != null && Directory.Exists(path) ? Path.GetFullPath(path) : null;
            }

            if (OperatingSystem.IsMacOS())
            {
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "Application Support", "Steam");
                return Directory.Exists(path) ? path : null;
            }

            return null;
        }

        // Returns the "steamapps" folder of every Steam library. The main Steam folder is always a library.
        private static List<String> GetLibraryFolders(String steamPath)
        {
            var steamAppsFolders = new List<String> { Path.Combine(steamPath, "steamapps") };

            var libraryFoldersFile = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
            if (File.Exists(libraryFoldersFile))
            {
                steamAppsFolders.AddRange(ReadValues(libraryFoldersFile, "path").Select(path => Path.Combine(path, "steamapps")));
            }

            return steamAppsFolders
                .Select(Path.GetFullPath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(Directory.Exists)
                .ToList();
        }

        private static IEnumerable<SteamGame> GetInstalledGames(String steamAppsFolder)
        {
            foreach (var manifestFile in Directory.EnumerateFiles(steamAppsFolder, "appmanifest_*.acf"))
            {
                var appIdText = ReadValues(manifestFile, "appid").FirstOrDefault();
                var name = ReadValues(manifestFile, "name").FirstOrDefault();

                if (UInt32.TryParse(appIdText, out var appId) && !String.IsNullOrWhiteSpace(name))
                {
                    yield return new SteamGame(appId, name);
                }
            }
        }

        // Returns every value for the given key in a VDF/ACF file, with escaped characters (e.g. "\\") unescaped.
        private static IEnumerable<String> ReadValues(String filePath, String key)
        {
            String[] lines;
            try
            {
                lines = File.ReadAllLines(filePath);
            }
            catch (IOException ex)
            {
                PluginLog.Warning(ex, $"Could not read '{filePath}'");
                yield break;
            }

            foreach (var line in lines)
            {
                var match = KeyValueRegex.Match(line);
                if (match.Success && String.Equals(match.Groups["key"].Value, key, StringComparison.OrdinalIgnoreCase))
                {
                    yield return Regex.Unescape(match.Groups["value"].Value);
                }
            }
        }

        // Rescans when a game's manifest file is added or removed (a game was installed or uninstalled).
        private void WatchLibraryFolders(List<String> steamAppsFolders)
        {
            this.StopWatching();

            foreach (var folder in steamAppsFolders)
            {
                var watcher = new FileSystemWatcher(folder, "appmanifest_*.acf");
                watcher.Created += (sender, e) => this.Refresh();
                watcher.Deleted += (sender, e) => this.Refresh();
                watcher.EnableRaisingEvents = true;
                this._watchers.Add(watcher);
            }
        }

        private void StopWatching()
        {
            foreach (var watcher in this._watchers)
            {
                watcher.Dispose();
            }

            this._watchers.Clear();
        }
    }
}
