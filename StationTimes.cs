using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Hearthwait
{
    internal static class StationTimes
    {
        private static readonly MethodInfo CookGetSlot = AccessTools.Method(typeof(CookingStation), "GetSlot");
        private static readonly MethodInfo HiveSinceUpdate = AccessTools.Method(typeof(Beehive), "GetTimeSinceLastUpdate");
        private static readonly MethodInfo SapSinceUpdate = AccessTools.Method(typeof(SapCollector), "GetTimeSinceLastUpdate");

        internal static string ForHover(GameObject hover)
        {
            if (hover == null)
            {
                return null;
            }

            var lines = new List<string>();
            Try(lines, () => FermenterLine(hover));
            Try(lines, () => SmelterLine(hover));
            Try(lines, () => HiveLine(hover));
            Try(lines, () => SapLine(hover));
            Try(lines, () => CookLine(hover));
            Try(lines, () => PlantLine(hover));
            Try(lines, () => PickableLine(hover));

            return lines.Count == 0 ? null : string.Join("\n", lines.ToArray());
        }

        private static void Try(List<string> lines, Func<string> fn)
        {
            try
            {
                var text = fn();
                if (!string.IsNullOrEmpty(text))
                {
                    lines.Add(text);
                }
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogDebug("Hearthwait hover: " + ex.Message);
            }
        }

        private static string FermenterLine(GameObject hover)
        {
            if (!PluginConfig.EnableFermenter.Value)
            {
                return null;
            }

            var fer = hover.GetComponentInParent<Fermenter>();
            if (fer == null || fer.m_nview == null || !fer.m_nview.IsValid())
            {
                return null;
            }

            if (fer.GetStatus() != Fermenter.Status.Fermenting)
            {
                return null;
            }

            var start = new DateTime(fer.m_nview.GetZDO().GetLong("StartTime", 0L));
            if (start.Ticks <= 0)
            {
                return null;
            }

            var left = fer.m_fermentationDuration - (ZNet.instance.GetTime() - start).TotalSeconds;
            return left <= 0 ? null : ReadyIn(left);
        }

        private static string SmelterLine(GameObject hover)
        {
            if (!PluginConfig.EnableSmelter.Value)
            {
                return null;
            }

            var sm = hover.GetComponentInParent<Smelter>();
            if (sm == null || sm.m_nview == null || !sm.m_nview.IsValid())
            {
                return null;
            }

            var queue = sm.GetQueueSize();
            if (queue <= 0)
            {
                return null;
            }

            var needsFuel = sm.m_fuelItem != null && sm.m_maxFuel > 0;
            if (needsFuel && sm.GetFuel() < 1f)
            {
                return null;
            }

            var speed = 1f;
            if (sm.m_windmill != null)
            {
                speed = sm.m_windmill.GetPowerOutput();
                if (speed <= 0.01f)
                {
                    return Localization.instance.Localize("$hearthwait_waiting_wind");
                }
            }

            var acc = sm.m_nview.GetZDO().GetFloat("accTime", 0f);
            var workSeconds = queue * sm.m_secPerProduct - acc;
            if (workSeconds < 0f)
            {
                workSeconds = 0f;
            }

            if (needsFuel)
            {
                var fuelCap = Mathf.FloorToInt(sm.GetFuel());
                if (fuelCap < queue)
                {
                    workSeconds = Mathf.Max(0f, fuelCap * sm.m_secPerProduct - acc);
                }
            }

            var real = workSeconds / speed;
            return real <= 0f ? null : ReadyIn(real);
        }

        private static string HiveLine(GameObject hover)
        {
            if (!PluginConfig.EnableBeehive.Value)
            {
                return null;
            }

            var hive = hover.GetComponentInParent<Beehive>();
            if (hive == null || hive.m_nview == null || !hive.m_nview.IsValid())
            {
                return null;
            }

            if (hive.GetHoneyLevel() >= hive.m_maxHoney)
            {
                return null;
            }

            var product = hive.m_nview.GetZDO().GetFloat("product", 0f) + InvokeFloat(HiveSinceUpdate, hive);
            var next = hive.m_secPerUnit - product;
            var remainingUnits = hive.m_maxHoney - hive.GetHoneyLevel();
            var full = (remainingUnits - 1) * hive.m_secPerUnit + Mathf.Max(0f, next);

            var parts = new List<string>();
            if (next > 0.5f)
            {
                parts.Add(NextIn(next));
            }

            if (remainingUnits > 1 && full > next + 0.5f)
            {
                parts.Add(FullIn(full));
            }

            return parts.Count == 0 ? null : string.Join("\n", parts.ToArray());
        }

        private static string SapLine(GameObject hover)
        {
            if (!PluginConfig.EnableSap.Value)
            {
                return null;
            }

            var sap = hover.GetComponentInParent<SapCollector>();
            if (sap == null || sap.m_nview == null || !sap.m_nview.IsValid())
            {
                return null;
            }

            if (sap.GetLevel() >= sap.m_maxLevel)
            {
                return null;
            }

            var product = sap.m_nview.GetZDO().GetFloat("product", 0f) + InvokeFloat(SapSinceUpdate, sap);
            var next = sap.m_secPerUnit - product;
            var remainingUnits = sap.m_maxLevel - sap.GetLevel();
            var full = (remainingUnits - 1) * sap.m_secPerUnit + Mathf.Max(0f, next);

            var parts = new List<string>();
            if (next > 0.5f)
            {
                parts.Add(NextIn(next));
            }

            if (remainingUnits > 1 && full > next + 0.5f)
            {
                parts.Add(FullIn(full));
            }

            return parts.Count == 0 ? null : string.Join("\n", parts.ToArray());
        }

        private static string CookLine(GameObject hover)
        {
            if (!PluginConfig.EnableCooking.Value)
            {
                return null;
            }

            var station = hover.GetComponentInParent<CookingStation>();
            if (station == null || station.m_slots == null)
            {
                return null;
            }

            var parts = new List<string>();
            for (var i = 0; i < station.m_slots.Length; i++)
            {
                if (!TryReadCookSlot(station, i, out var item, out var cooked))
                {
                    continue;
                }

                var conv = station.GetItemConversion(item);
                if (conv == null)
                {
                    continue;
                }

                var untilDone = conv.m_cookTime - cooked;
                if (untilDone > 0.25f)
                {
                    var name = Localization.instance.Localize(conv.m_to.GetHoverName());
                    parts.Add(name + " — " + ReadyIn(untilDone));
                    continue;
                }

                var untilBurn = conv.m_cookTime * 2f - cooked;
                if (untilBurn > 0.25f)
                {
                    parts.Add(ModLocalization.L("hearthwait_burn_in", Format(untilBurn)));
                }
            }

            return parts.Count == 0 ? null : string.Join("\n", parts.ToArray());
        }

        private static bool TryReadCookSlot(CookingStation station, int index, out string item, out float cooked)
        {
            item = "";
            cooked = 0f;
            if (CookGetSlot == null)
            {
                return false;
            }

            var ps = CookGetSlot.GetParameters();
            var args = new object[ps.Length];
            args[0] = index;
            for (var i = 1; i < ps.Length; i++)
            {
                var t = ps[i].ParameterType;
                if (t.IsByRef)
                {
                    t = t.GetElementType();
                }

                args[i] = t != null && t.IsValueType ? Activator.CreateInstance(t) : null;
            }

            CookGetSlot.Invoke(station, args);
            item = args.Length > 1 ? args[1] as string ?? "" : "";
            if (args.Length > 2 && args[2] is float f)
            {
                cooked = f;
            }

            return !string.IsNullOrEmpty(item);
        }

        private static string PlantLine(GameObject hover)
        {
            if (!PluginConfig.EnablePlant.Value)
            {
                return null;
            }

            var plant = hover.GetComponentInParent<Plant>();
            if (plant == null || plant.m_nview == null || !plant.m_nview.IsValid())
            {
                return null;
            }

            var grow = plant.GetGrowTime();
            var elapsed = plant.TimeSincePlanted();
            var left = grow - elapsed;
            return left <= 0.5 ? null : ReadyIn(left);
        }

        private static string PickableLine(GameObject hover)
        {
            if (!PluginConfig.EnablePickable.Value)
            {
                return null;
            }

            var pick = hover.GetComponentInParent<Pickable>();
            if (pick == null || pick.m_nview == null || !pick.m_nview.IsValid())
            {
                return null;
            }

            if (pick.m_respawnTimeMinutes <= 0)
            {
                return null;
            }

            if (!pick.m_picked)
            {
                return null;
            }

            var name = ((UnityEngine.Object)pick).name;
            if (!string.IsNullOrEmpty(name) && name.IndexOf("surt", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return null;
            }

            var pickedAt = new DateTime(pick.m_nview.GetZDO().GetLong("picked_time", 0L));
            if (pickedAt.Ticks <= 0)
            {
                return null;
            }

            var left = pick.m_respawnTimeMinutes * 60f - (ZNet.instance.GetTime() - pickedAt).TotalSeconds;
            return left <= 0 ? null : ReadyIn(left);
        }

        private static float InvokeFloat(MethodInfo method, object instance)
        {
            if (method == null || instance == null)
            {
                return 0f;
            }

            var value = method.Invoke(instance, null);
            if (value is float f)
            {
                return f;
            }

            if (value is double d)
            {
                return (float)d;
            }

            return 0f;
        }

        private static string ReadyIn(double seconds) => ModLocalization.L("hearthwait_ready_in", Format(seconds));

        private static string NextIn(double seconds) => ModLocalization.L("hearthwait_next_in", Format(seconds));

        private static string FullIn(double seconds) => ModLocalization.L("hearthwait_full_in", Format(seconds));

        internal static string Format(double seconds)
        {
            if (seconds < 0)
            {
                seconds = 0;
            }

            var t = TimeSpan.FromSeconds(Math.Ceiling(seconds));
            if (t.TotalHours >= 1)
            {
                return string.Format("{0}h {1:D2}m", (int)t.TotalHours, t.Minutes);
            }

            if (t.TotalMinutes >= 1)
            {
                return string.Format("{0}m {1:D2}s", t.Minutes, t.Seconds);
            }

            return string.Format("{0}s", t.Seconds);
        }
    }
}
