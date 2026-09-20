using BepInEx.Configuration;

namespace Hearthwait
{
    internal static class PluginConfig
    {
        internal static ConfigEntry<bool> EnableFermenter;
        internal static ConfigEntry<bool> EnableSmelter;
        internal static ConfigEntry<bool> EnableBeehive;
        internal static ConfigEntry<bool> EnableSap;
        internal static ConfigEntry<bool> EnableCooking;
        internal static ConfigEntry<bool> EnablePlant;
        internal static ConfigEntry<bool> EnablePickable;

        internal static void Bind(ConfigFile cfg)
        {
            const string sec = "Hover timers";
            EnableFermenter = cfg.Bind(sec, "Mead fermenter", true, "Remaining fermentation time.");
            EnableSmelter = cfg.Bind(sec, "Smelters and mills", true, "Kiln, smelter, blast furnace, eitr, spinning wheel, windmill.");
            EnableBeehive = cfg.Bind(sec, "Beehive", true, "Time until the next honey and until the hive is full.");
            EnableSap = cfg.Bind(sec, "Sap collector", true, "Time until the next sap and until the collector is full.");
            EnableCooking = cfg.Bind(sec, "Cooking and oven", true, "Time until food is done, and until it burns.");
            EnablePlant = cfg.Bind(sec, "Crops and saplings", true, "Time until a planted crop or tree is grown.");
            EnablePickable = cfg.Bind(sec, "Respawning plants", true, "Time until picked berries, mushrooms and similar grow back.");
        }
    }
}
