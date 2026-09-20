using BepInEx;
using HarmonyLib;
using Jotunn;
using Jotunn.Utils;

namespace Hearthwait
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.NotEnforced, VersionStrictness.None)]
    public class HearthwaitPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.blackhearthx.hearthwait";
        public const string PluginName = "Hearthwait";
        public const string PluginVersion = "1.0.0";

        internal static HearthwaitPlugin Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            PluginConfig.Bind(Config);
            ModLocalization.Register();

            var harmony = new Harmony(PluginGUID);
            harmony.PatchAll();

            Jotunn.Logger.LogInfo($"{PluginName} {PluginVersion} loaded");
        }
    }
}
