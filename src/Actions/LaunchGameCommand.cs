namespace Loupedeck.SteamPlugin
{
    using System;
    using System.Diagnostics;

    // This class implements an Action Editor command that launches a Steam game by its app ID
    // using the steam:// protocol.

    public class LaunchGameCommand : ActionEditorCommand
    {
        private const String GameIdControlName = "gameId";

        public LaunchGameCommand()
        {
            this.Name = "LaunchSteamGame";
            this.DisplayName = "Launch Game";
            this.GroupName = "Steam";
            this.Description = "Launches a Steam game using its app ID";

            this.ActionEditor.AddControlEx(
                new ActionEditorTextbox(name: GameIdControlName, labelText: "Game ID:")
                    .SetPlaceholder("e.g. 730")
                    .SetRequired());
        }

        // This method is called when the user presses the button assigned to this action.
        protected override Boolean RunCommand(ActionEditorActionParameters actionParameters)
        {
            if (!actionParameters.TryGetString(GameIdControlName, out var gameId) || !IsValidGameId(gameId))
            {
                PluginLog.Warning($"Invalid Steam game ID: '{gameId}'");
                return false;
            }

            var uri = $"steam://rungameid/{gameId.Trim()}";

            try
            {
                // UseShellExecute lets the OS hand the steam:// URI to the registered Steam client.
                Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
                PluginLog.Info($"Launched {uri}");
                return true;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Failed to launch {uri}");
                return false;
            }
        }

        // Steam app IDs are positive integers.
        private static Boolean IsValidGameId(String gameId) =>
            UInt32.TryParse(gameId?.Trim(), out var id) && id > 0;
    }
}
