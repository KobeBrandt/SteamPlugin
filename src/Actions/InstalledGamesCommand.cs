namespace Loupedeck.SteamPlugin
{
    using System;
    using System.Diagnostics;

    // This class adds one action per Steam game installed on this computer, in the "Library" group.
    // Each action launches its game using the steam:// protocol.

    public class InstalledGamesCommand : PluginDynamicCommand
    {
        private const String LibraryGroupName = "Library";

        // The parameterless base constructor makes this a parameterized command: each parameter added
        // with AddParameter shows up as its own action. The (displayName, ...) constructor would instead
        // register a single action that is run with an empty parameter.
        public InstalledGamesCommand()
            : base()
        {
            this.GroupName = LibraryGroupName;
        }

        private SteamLibrary Library => ((SteamPlugin)this.Plugin).Library;

        // This method is called when the action is loaded; the Plugin property is available from here on.
        protected override Boolean OnLoad()
        {
            this.Library.GamesChanged += this.OnGamesChanged;
            this.AddGameParameters();
            return true;
        }

        protected override Boolean OnUnload()
        {
            this.Library.GamesChanged -= this.OnGamesChanged;
            return true;
        }

        // This method is called when the user presses a game's button. The action parameter is the app ID.
        protected override void RunCommand(String actionParameter)
        {
            var uri = $"steam://rungameid/{actionParameter}";

            try
            {
                Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
                PluginLog.Info($"Launched {uri}");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Failed to launch {uri}");
            }
        }

        private void OnGamesChanged(Object sender, EventArgs e)
        {
            this.RemoveAllParameters();
            this.AddGameParameters();
            this.ParametersChanged(); // Notify the plugin service that the list of games has changed.
        }

        private void AddGameParameters()
        {
            foreach (var game in this.Library.Games)
            {
                this.AddParameter(game.AppId.ToString(), game.Name, LibraryGroupName);
            }
        }
    }
}
