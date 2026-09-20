using System;
using HarmonyLib;

namespace Hearthwait
{
    /// <summary>
    /// Inject timers into GetHoverText (OttoLens / FermenterUtilities pattern).
    /// Hud.UpdateCrosshair assigns GetHoverText() every frame — that keeps the clock live.
    /// </summary>
    internal static class HoverTimePatch
    {
        internal static void Apply(Harmony harmony)
        {
            Patch(harmony, typeof(Fermenter), "GetHoverText", nameof(FermenterHover));
            Patch(harmony, typeof(Beehive), "GetHoverText", nameof(BeehiveHover));
            Patch(harmony, typeof(SapCollector), "GetHoverText", nameof(SapHover));
            Patch(harmony, typeof(CookingStation), "GetHoverText", nameof(CookHover));
            Patch(harmony, typeof(Plant), "GetHoverText", nameof(PlantHover));
            Patch(harmony, typeof(Pickable), "GetHoverText", nameof(PickableHover));
            Patch(harmony, typeof(PickableItem), "GetHoverText", nameof(PickableItemHover));
            Patch(harmony, typeof(Fireplace), "GetHoverText", nameof(FireplaceHover));
            Patch(harmony, typeof(EggGrow), "GetHoverText", nameof(EggHover));
            Patch(harmony, typeof(Tameable), "GetHoverText", nameof(TameableHover));

            foreach (var name in new[] { "OnHoverAddOre", "OnHoverAddFuel", "OnHoverEmptyOre" })
            {
                Patch(harmony, typeof(Smelter), name, nameof(SmelterHover));
            }

            // Body mesh hover (furnace hull, not only the mouth switches).
            var crosshair = AccessTools.Method(typeof(Hud), "UpdateCrosshair");
            if (crosshair != null)
            {
                harmony.Patch(
                    crosshair,
                    postfix: new HarmonyMethod(typeof(HoverBodyPatch), nameof(HoverBodyPatch.Postfix)));
            }
        }

        private static void Patch(Harmony harmony, Type type, string method, string postfix)
        {
            var m = AccessTools.Method(type, method);
            if (m == null)
            {
                Jotunn.Logger.LogWarning($"Hearthwait: missing {type.Name}.{method}");
                return;
            }

            harmony.Patch(m, postfix: new HarmonyMethod(typeof(HoverTimePatch), postfix));
        }

        private static void FermenterHover(Fermenter __instance, ref string __result)
            => Append(ref __result, StationTimes.FermenterText(__instance));

        private static void BeehiveHover(Beehive __instance, ref string __result)
            => Append(ref __result, StationTimes.HiveText(__instance));

        private static void SapHover(SapCollector __instance, ref string __result)
            => Append(ref __result, StationTimes.SapText(__instance));

        private static void CookHover(CookingStation __instance, ref string __result)
            => Append(ref __result, StationTimes.CookText(__instance));

        private static void PlantHover(Plant __instance, ref string __result)
            => Append(ref __result, StationTimes.PlantText(__instance));

        private static void PickableHover(Pickable __instance, ref string __result)
            => Append(ref __result, StationTimes.PickableText(__instance));

        private static void PickableItemHover(PickableItem __instance, ref string __result)
            => Append(ref __result, StationTimes.PickableItemText(__instance));

        private static void FireplaceHover(Fireplace __instance, ref string __result)
            => Append(ref __result, StationTimes.FireplaceText(__instance));

        private static void EggHover(EggGrow __instance, ref string __result)
            => Append(ref __result, StationTimes.EggText(__instance));

        private static void TameableHover(Tameable __instance, ref string __result)
            => Append(ref __result, StationTimes.CreatureText(__instance));

        private static void SmelterHover(Smelter __instance, ref string __result)
            => Append(ref __result, StationTimes.SmelterText(__instance));

        private static void Append(ref string result, string extra)
        {
            if (string.IsNullOrEmpty(extra))
            {
                return;
            }

            if (string.IsNullOrEmpty(result))
            {
                result = extra;
                return;
            }

            if (result.IndexOf(extra, StringComparison.Ordinal) >= 0)
            {
                return;
            }

            result = result + "\n" + extra;
        }
    }
}
