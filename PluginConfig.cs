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
        internal static ConfigEntry<bool> EnableFireplace;
        internal static ConfigEntry<bool> EnableEgg;
        internal static ConfigEntry<bool> EnableGrowup;
        internal static ConfigEntry<bool> EnableTame;
        internal static ConfigEntry<bool> EnablePregnant;

        internal static void Bind(ConfigFile cfg)
        {
            const string sec = "Hover timers";
            EnableFermenter = cfg.Bind(sec, "Mead fermenter", true, "Remaining fermentation time.");
            EnableSmelter = cfg.Bind(sec, "Smelters and mills", true, "Kiln, smelter, blast furnace, eitr, spinning wheel, windmill.");
            EnableBeehive = cfg.Bind(sec, "Beehive", true, "Time until the next honey and until the hive is full.");
            EnableSap = cfg.Bind(sec, "Sap collector", true, "Time until the next sap and until the collector is full.");
            EnableCooking = cfg.Bind(sec, "Cooking and oven", true, "Time until food is done, and until it burns.");
            EnablePlant = cfg.Bind(sec, "Crops and saplings", true, "Time until a planted crop or tree is grown.");
            EnablePickable = cfg.Bind(sec, "Bushes and nature", true, "Ready label on bushes/berries/mushrooms; countdown when picked and growing back.");
            EnableFireplace = cfg.Bind(sec, "Fires and torches", true, "How long the fire will keep burning.");
            EnableEgg = cfg.Bind(sec, "Eggs hatching", true, "Time until a warm egg hatches.");
            EnableGrowup = cfg.Bind(sec, "Young animals growing", true, "Time until a cub/chick/piglet grows up.");
            EnableTame = cfg.Bind(sec, "Taming and feeding", true, "Time left to tame, and how long until hungry again.");
            EnablePregnant = cfg.Bind(sec, "Pregnancy", true, "Time until offspring is born.");
        }
    }
}
