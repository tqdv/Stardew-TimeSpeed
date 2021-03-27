using StardewModdingAPI.Utilities;

namespace RecatchLegendaryFish.Framework
{
    /// <summary>The mod configuration model.</summary>
    internal class ModConfig
    {
        /*********
        ** Accessors
        *********/
        /// <summary>A keybind which toggles whether the player can recatch fish.</summary>
        public KeybindList ToggleKey { get; set; } = new();
    }
}
