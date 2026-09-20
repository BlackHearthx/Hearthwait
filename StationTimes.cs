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
        private static readonly MethodInfo FermentElapsed = AccessTools.Method(typeof(Fermenter), "GetFermentationTime");

        internal static string ForBodyHover(GameObject hover)
        {
            if (hover == null)
            {
                return null;
            }

            var lines = new List<string>();

            // Smelter / kiln / mill: body mesh is not the Switch — this is the main fix.
            TryAdd(lines, () =>
            {
                var sm = hover.GetComponentInParent<Smelter>();
                return sm != null ? SmelterText(sm) : null;
            });

            TryAdd(lines, () =>
            {
                var fer = hover.GetComponentInParent<Fermenter>();
                return fer != null ? FermenterText(fer) : null;
            });

            TryAdd(lines, () =>
            {
                var hive = hover.GetComponentInParent<Beehive>();
                return hive != null ? HiveText(hive) : null;
            });

            TryAdd(lines, () =>
            {
                var sap = hover.GetComponentInParent<SapCollector>();
                return sap != null ? SapText(sap) : null;
            });

            TryAdd(lines, () =>
            {
                var cook = hover.GetComponentInParent<CookingStation>();
                return cook != null ? CookText(cook) : null;
            });

            TryAdd(lines, () =>
            {
                var fire = hover.GetComponentInParent<Fireplace>();
                return fire != null ? FireplaceText(fire) : null;
            });

            TryAdd(lines, () =>
            {
                var plant = hover.GetComponentInParent<Plant>();
                return plant != null ? PlantText(plant) : null;
            });

            TryAdd(lines, () =>
            {
                var pick = hover.GetComponentInParent<Pickable>();
                return pick != null ? PickableText(pick) : null;
            });

            TryAdd(lines, () =>
            {
                var egg = hover.GetComponentInParent<EggGrow>();
                return egg != null ? EggText(egg) : null;
            });

            TryAdd(lines, () =>
            {
                var tame = hover.GetComponentInParent<Tameable>();
                return tame != null ? CreatureText(tame) : null;
            });

            return lines.Count == 0 ? null : string.Join("\n", lines.ToArray());
        }

        private static void TryAdd(List<string> lines, Func<string> fn)
        {
            try
            {
                var text = fn();
                if (!string.IsNullOrEmpty(text) && !lines.Contains(text))
                {
                    lines.Add(text);
                }
            }
            catch
            {
            }
        }

        internal static string FermenterText(Fermenter fer)
        {
            if (!PluginConfig.EnableFermenter.Value || fer == null || fer.m_nview == null || !fer.m_nview.IsValid())
            {
                return null;
            }

            try
            {
                var status = fer.GetStatus();
                if (status == Fermenter.Status.Ready)
                {
                    return HoverStyle.Done(ModLocalization.L("hearthwait_ready_now"));
                }

                if (status == Fermenter.Status.Empty)
                {
                    return HoverStyle.Paused(ModLocalization.L("hearthwait_empty"));
                }

                if (status != Fermenter.Status.Fermenting)
                {
                    return null;
                }

                // Vanilla resets StartTime while uncovered — countdown would look frozen.
                if (!fer.m_hasRoof || fer.m_exposed)
                {
                    return HoverStyle.Paused(ModLocalization.L("hearthwait_ferment_paused"));
                }

                // OttoLens: wall-clock from ZDOVars.s_startTime (same as GetFermentationTime).
                var elapsed = GetFermentElapsedSeconds(fer);
                if (elapsed < 0)
                {
                    return null;
                }

                var left = fer.m_fermentationDuration - elapsed;
                if (left <= 0.5)
                {
                    return HoverStyle.Done(ModLocalization.L("hearthwait_ready_now"));
                }

                return HoverStyle.Waiting(ModLocalization.L("hearthwait_ready_in", Format(left)));
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogDebug("Hearthwait fermenter: " + ex.Message);
                return null;
            }
        }

        internal static string SmelterText(Smelter sm)
        {
            if (!PluginConfig.EnableSmelter.Value || sm == null || sm.m_nview == null || !sm.m_nview.IsValid())
            {
                return null;
            }

            try
            {
                var parts = new List<string>();
                var processed = 0;
                try
                {
                    processed = sm.GetProcessedQueueSize();
                }
                catch
                {
                }

                if (processed > 0)
                {
                    parts.Add(HoverStyle.Done(ModLocalization.L("hearthwait_ready_now")));
                }

                var queue = sm.GetQueueSize();
                if (queue <= 0)
                {
                    return parts.Count == 0 ? null : string.Join("\n", parts.ToArray());
                }

                var needsFuel = sm.m_fuelItem != null && sm.m_maxFuel > 0;
                if (needsFuel && sm.GetFuel() < 1f)
                {
                    parts.Add(HoverStyle.Paused(ModLocalization.L("hearthwait_needs_fuel")));
                    return string.Join("\n", parts.ToArray());
                }

                var speed = sm.m_windmill != null ? sm.m_windmill.GetPowerOutput() : 1f;
                if (speed <= 0.01f)
                {
                    parts.Add(HoverStyle.Paused(ModLocalization.L("hearthwait_waiting_wind")));
                    return string.Join("\n", parts.ToArray());
                }

                // OttoLens: bakeTimer = progress on current bar (not accTime).
                var bake = sm.m_nview.GetZDO().GetFloat(ZDOVars.s_bakeTimer, 0f);
                // Between UpdateSmelter ticks (1s), estimate with time since last owner tick.
                var since = SecondsSinceZdoStart(sm.m_nview.GetZDO());
                var bakeNow = bake + (float)since * speed;

                var nextLeft = Mathf.Max(0f, sm.m_secPerProduct - bakeNow) / speed;
                var queueLeft = Mathf.Max(0f, queue * sm.m_secPerProduct - bakeNow) / speed;

                if (needsFuel && sm.m_fuelPerProduct > 0)
                {
                    var fuelSeconds = sm.GetFuel() * (sm.m_secPerProduct / sm.m_fuelPerProduct) / speed;
                    queueLeft = Mathf.Min(queueLeft, fuelSeconds);
                    nextLeft = Mathf.Min(nextLeft, fuelSeconds);
                }

                if (queueLeft <= 0.5f)
                {
                    parts.Add(HoverStyle.Done(ModLocalization.L("hearthwait_ready_now")));
                }
                else
                {
                    parts.Add(HoverStyle.Waiting(ModLocalization.L("hearthwait_ready_in", Format(queueLeft))));
                    if (queue > 1 && nextLeft + 0.5f < queueLeft)
                    {
                        parts.Add(HoverStyle.Paint(HoverStyle.Soft, ModLocalization.L("hearthwait_next_in", Format(nextLeft))));
                    }
                }

                return string.Join("\n", parts.ToArray());
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogDebug("Hearthwait smelter: " + ex.Message);
                return null;
            }
        }

        internal static string HiveText(Beehive hive)
        {
            if (!PluginConfig.EnableBeehive.Value || hive == null || hive.m_nview == null || !hive.m_nview.IsValid())
            {
                return null;
            }

            try
            {
                var level = hive.GetHoneyLevel();
                if (level >= hive.m_maxHoney)
                {
                    return HoverStyle.Done(ModLocalization.L("hearthwait_full_ready"));
                }

                var product = hive.m_nview.GetZDO().GetFloat("product", 0f) + InvokeFloat(HiveSinceUpdate, hive);
                var next = hive.m_secPerUnit - product;
                var remainingUnits = hive.m_maxHoney - level;
                var full = (remainingUnits - 1) * hive.m_secPerUnit + Mathf.Max(0f, next);

                var parts = new List<string>();
                if (level > 0)
                {
                    parts.Add(HoverStyle.Done(ModLocalization.L("hearthwait_ready_now")));
                }

                if (next > 0.5f)
                {
                    parts.Add(HoverStyle.Waiting(ModLocalization.L("hearthwait_next_in", Format(next))));
                }

                if (remainingUnits > 1 && full > next + 0.5f)
                {
                    parts.Add(HoverStyle.Paint(HoverStyle.Soft, ModLocalization.L("hearthwait_full_in", Format(full))));
                }

                return parts.Count == 0 ? null : string.Join("\n", parts.ToArray());
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogDebug("Hearthwait hive: " + ex.Message);
                return null;
            }
        }

        internal static string SapText(SapCollector sap)
        {
            if (!PluginConfig.EnableSap.Value || sap == null || sap.m_nview == null || !sap.m_nview.IsValid())
            {
                return null;
            }

            try
            {
                var level = sap.GetLevel();
                if (level >= sap.m_maxLevel)
                {
                    return HoverStyle.Done(ModLocalization.L("hearthwait_full_ready"));
                }

                var product = sap.m_nview.GetZDO().GetFloat("product", 0f) + InvokeFloat(SapSinceUpdate, sap);
                var next = sap.m_secPerUnit - product;
                var remainingUnits = sap.m_maxLevel - level;
                var full = (remainingUnits - 1) * sap.m_secPerUnit + Mathf.Max(0f, next);

                var parts = new List<string>();
                if (level > 0)
                {
                    parts.Add(HoverStyle.Done(ModLocalization.L("hearthwait_ready_now")));
                }

                if (next > 0.5f)
                {
                    parts.Add(HoverStyle.Waiting(ModLocalization.L("hearthwait_next_in", Format(next))));
                }

                if (remainingUnits > 1 && full > next + 0.5f)
                {
                    parts.Add(HoverStyle.Paint(HoverStyle.Soft, ModLocalization.L("hearthwait_full_in", Format(full))));
                }

                return parts.Count == 0 ? null : string.Join("\n", parts.ToArray());
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogDebug("Hearthwait sap: " + ex.Message);
                return null;
            }
        }

        internal static string CookText(CookingStation station)
        {
            if (!PluginConfig.EnableCooking.Value || station == null || station.m_slots == null)
            {
                return null;
            }

            try
            {
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

                    var name = Localization.instance.Localize(conv.m_to.GetHoverName());
                    var untilDone = conv.m_cookTime - cooked;
                    if (untilDone > 0.25f)
                    {
                        parts.Add(HoverStyle.Waiting(name + " — " + ModLocalization.L("hearthwait_ready_in", Format(untilDone))));
                        continue;
                    }

                    var untilBurn = conv.m_cookTime * 2f - cooked;
                    if (untilBurn > 0.25f)
                    {
                        parts.Add(HoverStyle.Done(name + " — " + ModLocalization.L("hearthwait_ready_now"))
                                  + "  " + HoverStyle.Warning(ModLocalization.L("hearthwait_burn_in", Format(untilBurn))));
                    }
                    else
                    {
                        parts.Add(HoverStyle.Warning(ModLocalization.L("hearthwait_burnt")));
                    }
                }

                return parts.Count == 0 ? null : string.Join("\n", parts.ToArray());
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogDebug("Hearthwait cook: " + ex.Message);
                return null;
            }
        }

        internal static string PlantText(Plant plant)
        {
            if (!PluginConfig.EnablePlant.Value || plant == null || plant.m_nview == null || !plant.m_nview.IsValid())
            {
                return null;
            }

            try
            {
                // Plant.Grow() bails on anything but Healthy, so a countdown here would be a lie.
                if (plant.GetStatus() != Plant.Status.Healthy)
                {
                    return HoverStyle.Paused(ModLocalization.L("hearthwait_plant_stalled"));
                }

                var grow = plant.GetGrowTime();
                var elapsed = plant.TimeSincePlanted();
                var left = grow - elapsed;
                if (left <= 0.5)
                {
                    return HoverStyle.Done(ModLocalization.L("hearthwait_ready_now"));
                }

                return HoverStyle.Waiting(ModLocalization.L("hearthwait_ready_in", Format(left)));
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogDebug("Hearthwait plant: " + ex.Message);
                return null;
            }
        }

        internal static string PickableText(Pickable pick)
        {
            if (!PluginConfig.EnablePickable.Value || pick == null)
            {
                return null;
            }

            try
            {
                var objName = ((UnityEngine.Object)pick).name ?? "";
                if (objName.IndexOf("surt", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return null;
                }

                // Ripe bush / berry / mushroom / flax — vanilla hides hover when picked.
                if (!pick.m_picked)
                {
                    var label = GetPickableItemLabel(pick);
                    if (!string.IsNullOrEmpty(label))
                    {
                        return HoverStyle.Done(ModLocalization.L("hearthwait_harvest_ready", label));
                    }

                    return HoverStyle.Done(ModLocalization.L("hearthwait_ready_now"));
                }

                if (pick.m_respawnTimeMinutes <= 0 || pick.m_nview == null || !pick.m_nview.IsValid())
                {
                    return HoverStyle.Paused(ModLocalization.L("hearthwait_picked"));
                }

                if (ZNet.instance == null)
                {
                    return HoverStyle.Paused(ModLocalization.L("hearthwait_picked"));
                }

                var full = pick.m_respawnTimeMinutes * 60.0;
                long pickedTicks = pick.m_nview.GetZDO().GetLong(ZDOVars.s_pickedTime, 0L);

                // Pickable.UpdateRespawn stamps picked_time on its own 60s tick; 1 means "respawn now".
                double left;
                if (pickedTicks == 0L)
                {
                    left = full;
                }
                else if (pickedTicks == 1L)
                {
                    left = 0;
                }
                else
                {
                    left = full - (ZNet.instance.GetTime() - new DateTime(pickedTicks)).TotalSeconds;
                }

                if (left <= 0.5)
                {
                    return HoverStyle.Done(ModLocalization.L("hearthwait_ready_now"));
                }

                return HoverStyle.Waiting(ModLocalization.L("hearthwait_grows_in", Format(left)));
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogDebug("Hearthwait pickable: " + ex.Message);
                return null;
            }
        }

        internal static string PickableItemText(PickableItem item)
        {
            if (!PluginConfig.EnablePickable.Value || item == null)
            {
                return null;
            }

            try
            {
                if (item.m_picked)
                {
                    return HoverStyle.Paused(ModLocalization.L("hearthwait_picked"));
                }

                if (item.m_itemPrefab == null)
                {
                    return HoverStyle.Done(ModLocalization.L("hearthwait_ready_now"));
                }

                var label = Localization.instance.Localize(item.m_itemPrefab.m_itemData.m_shared.m_name);
                return HoverStyle.Done(ModLocalization.L("hearthwait_harvest_ready", label));
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogDebug("Hearthwait pickable item: " + ex.Message);
                return null;
            }
        }

        internal static string FireplaceText(Fireplace fire)
        {
            if (!PluginConfig.EnableFireplace.Value || fire == null || fire.m_nview == null || !fire.m_nview.IsValid())
            {
                return null;
            }

            try
            {
                if (fire.m_infiniteFuel)
                {
                    return null;
                }

                var fuel = Mathf.Max(0f, fire.m_nview.GetZDO().GetFloat(ZDOVars.s_fuel, 0f));
                if (fuel <= 0f)
                {
                    return HoverStyle.Paused(ModLocalization.L("hearthwait_needs_fuel"));
                }

                if (!fire.IsBurning())
                {
                    return HoverStyle.Paused(ModLocalization.L("hearthwait_fire_out"));
                }

                if (fire.m_secPerFuel <= 0f)
                {
                    return null;
                }

                var left = fuel * fire.m_secPerFuel;
                return HoverStyle.Waiting(ModLocalization.L("hearthwait_burns_for", Format(left)));
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogDebug("Hearthwait fireplace: " + ex.Message);
                return null;
            }
        }

        internal static string EggText(EggGrow egg)
        {
            if (!PluginConfig.EnableEgg.Value || egg == null || egg.m_nview == null || !egg.m_nview.IsValid())
            {
                return null;
            }

            try
            {
                var item = egg.GetComponent<ItemDrop>();
                if (item != null && item.m_itemData != null && item.m_itemData.m_stack > 1)
                {
                    return HoverStyle.Paused(ModLocalization.L("hearthwait_egg_stacked"));
                }

                var growStart = egg.m_nview.GetZDO().GetFloat(ZDOVars.s_growStart, 0f);
                if (growStart <= 0f || ZNet.instance == null)
                {
                    return HoverStyle.Paused(ModLocalization.L("hearthwait_egg_cold"));
                }

                var left = egg.m_growTime - (ZNet.instance.GetTimeSeconds() - growStart);
                if (left <= 0.5)
                {
                    return HoverStyle.Done(ModLocalization.L("hearthwait_hatch_ready"));
                }

                return HoverStyle.Waiting(ModLocalization.L("hearthwait_hatches_in", Format(left)));
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogDebug("Hearthwait egg: " + ex.Message);
                return null;
            }
        }

        /// <summary>Taming, hunger, grow-up, and pregnancy on one creature hover.</summary>
        internal static string CreatureText(Tameable tame)
        {
            if (tame == null || tame.m_nview == null || !tame.m_nview.IsValid())
            {
                return null;
            }

            try
            {
                var parts = new List<string>();
                var zdo = tame.m_nview.GetZDO();

                if (PluginConfig.EnableGrowup.Value)
                {
                    var grow = tame.GetComponent<Growup>();
                    if (grow != null)
                    {
                        var growLine = GrowupLine(grow);
                        if (!string.IsNullOrEmpty(growLine))
                        {
                            parts.Add(growLine);
                        }
                    }
                }

                if (PluginConfig.EnablePregnant.Value)
                {
                    var proc = tame.GetComponent<Procreation>();
                    if (proc != null)
                    {
                        var preg = PregnantLine(proc, zdo);
                        if (!string.IsNullOrEmpty(preg))
                        {
                            parts.Add(preg);
                        }
                    }
                }

                if (PluginConfig.EnableTame.Value)
                {
                    if (tame.IsTamed())
                    {
                        var fed = FedLine(tame, zdo);
                        if (!string.IsNullOrEmpty(fed))
                        {
                            parts.Add(fed);
                        }
                    }
                    else
                    {
                        var rem = zdo.GetFloat(ZDOVars.s_tameTimeLeft, tame.m_tamingTime);
                            if (rem > 0.5f)
                            {
                                parts.Add(HoverStyle.Waiting(ModLocalization.L("hearthwait_tames_in", Format(rem))));
                            }
                        else
                        {
                            parts.Add(HoverStyle.Done(ModLocalization.L("hearthwait_tame_ready")));
                        }
                    }
                }

                return parts.Count == 0 ? null : string.Join("\n", parts.ToArray());
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogDebug("Hearthwait creature: " + ex.Message);
                return null;
            }
        }

        private static string GrowupLine(Growup grow)
        {
            if (grow.m_growTime <= 0f || ZNet.instance == null)
            {
                return null;
            }

            var ai = grow.GetComponent<BaseAI>();
            if (ai == null)
            {
                return null;
            }

            var elapsed = ai.GetTimeSinceSpawned().TotalSeconds;
            var left = grow.m_growTime - elapsed;
            if (left <= 0.5)
            {
                return HoverStyle.Done(ModLocalization.L("hearthwait_grown_ready"));
            }

            return HoverStyle.Waiting(ModLocalization.L("hearthwait_grows_up_in", Format(left)));
        }

        private static string PregnantLine(Procreation proc, ZDO zdo)
        {
            long ticks = zdo.GetLong(ZDOVars.s_pregnant, 0L);
            if (ticks <= 0 || ZNet.instance == null)
            {
                var love = zdo.GetInt(ZDOVars.s_lovePoints, 0);
                if (love > 0 && proc.m_requiredLovePoints > 0)
                {
                    return HoverStyle.Paint(
                        HoverStyle.Soft,
                        ModLocalization.L("hearthwait_love_points", love, proc.m_requiredLovePoints));
                }

                return null;
            }

            var left = proc.m_pregnancyDuration - (ZNet.instance.GetTime() - new DateTime(ticks)).TotalSeconds;
            if (left <= 0.5)
            {
                return HoverStyle.Done(ModLocalization.L("hearthwait_birth_ready"));
            }

            return HoverStyle.Waiting(ModLocalization.L("hearthwait_births_in", Format(left)));
        }

        private static string FedLine(Tameable tame, ZDO zdo)
        {
            long fedAt = zdo.GetLong(ZDOVars.s_tameLastFeeding, 0L);
            if (fedAt <= 0 || ZNet.instance == null || tame.m_fedDuration <= 0f)
            {
                return HoverStyle.Paused(ModLocalization.L("hearthwait_hungry"));
            }

            var left = tame.m_fedDuration - (ZNet.instance.GetTime() - new DateTime(fedAt)).TotalSeconds;
            if (left <= 0.5)
            {
                return HoverStyle.Paused(ModLocalization.L("hearthwait_hungry"));
            }

            return HoverStyle.Waiting(ModLocalization.L("hearthwait_fed_for", Format(left)));
        }

        private static double GetFermentElapsedSeconds(Fermenter fer)
        {
            if (FermentElapsed != null)
            {
                try
                {
                    var value = FermentElapsed.Invoke(fer, null);
                    if (value is double d)
                    {
                        return d;
                    }

                    if (value is float f)
                    {
                        return f;
                    }
                }
                catch
                {
                }
            }

            if (ZNet.instance == null)
            {
                return -1;
            }

            long startTicks = fer.m_nview.GetZDO().GetLong(ZDOVars.s_startTime, 0L);
            if (startTicks <= 0)
            {
                return -1;
            }

            return (ZNet.instance.GetTime() - new DateTime(startTicks)).TotalSeconds;
        }

        private static double SecondsSinceZdoStart(ZDO zdo)
        {
            if (zdo == null || ZNet.instance == null)
            {
                return 0;
            }

            long ticks = zdo.GetLong(ZDOVars.s_startTime, 0L);
            if (ticks <= 0)
            {
                return 0;
            }

            var seconds = (ZNet.instance.GetTime() - new DateTime(ticks)).TotalSeconds;
            return seconds < 0 ? 0 : seconds;
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

        private static string GetPickableItemLabel(Pickable pick)
        {
            try
            {
                if (pick.m_itemPrefab != null)
                {
                    var drop = pick.m_itemPrefab.GetComponent<ItemDrop>();
                    if (drop != null && drop.m_itemData?.m_shared != null)
                    {
                        return Localization.instance.Localize(drop.m_itemData.m_shared.m_name);
                    }
                }
            }
            catch
            {
            }

            return null;
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

        internal static string Format(double seconds)
        {
            if (seconds < 0)
            {
                seconds = 0;
            }

            var whole = Math.Max(0, (int)Math.Floor(seconds));
            var d = whole / 86400;
            var h = whole % 86400 / 3600;
            var m = whole % 3600 / 60;
            var s = whole % 60;

            // Seconds only matter while they are readable — they are noise on a 5h bush.
            if (d > 0)
            {
                return string.Format("{0}d {1}h", d, h);
            }

            if (h > 0)
            {
                return string.Format("{0}h {1:D2}m", h, m);
            }

            if (m > 0)
            {
                return string.Format("{0}m {1:D2}s", m, s);
            }

            return string.Format("{0}s", s);
        }
    }
}
