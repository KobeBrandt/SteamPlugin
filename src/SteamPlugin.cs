namespace Loupedeck.SteamPlugin
{
    using System;

    // This class contains the plugin-level logic of the Loupedeck plugin.

    public class SteamPlugin : Plugin
    {
        // Gets a value indicating whether this is an API-only plugin.
        public override Boolean UsesApplicationApiOnly => true;

        // Gets a value indicating whether this is a Universal plugin or an Application plugin.
        public override Boolean HasNoApplication => true;

        // Gets the Steam games installed on this computer.
        public SteamLibrary Library { get; }

        // Initializes a new instance of the plugin class.
        public SteamPlugin()
        {
            // Initialize the plugin log.
            PluginLog.Init(this.Log);

            // Initialize the plugin resources.
            PluginResources.Init(this.Assembly);

            this.Library = new SteamLibrary();
        }

        // This method is called when the plugin is loaded.
        public override void Load()
        {
            // Scan the local Steam library folders for installed games.
            this.Library.Refresh();
        }

        // This method is called when the plugin is unloaded.
        public override void Unload()
        {
            this.Library.Dispose();
        }
    }
}
