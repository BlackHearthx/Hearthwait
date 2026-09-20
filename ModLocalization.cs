using System;
using System.Collections.Generic;
using System.IO;
using Jotunn.Managers;
using UnityEngine;

namespace Hearthwait
{
    internal static class ModLocalization
    {
        internal static void Register()
        {
            var loc = LocalizationManager.Instance.GetLocalization();
            var loaded = 0;
            foreach (var lang in new[] { "English", "Portuguese_Brazilian", "Portuguese_European" })
            {
                if (TryLoad(loc, lang))
                {
                    loaded++;
                }
            }

            if (loaded == 0)
            {
                loc.AddTranslation("English", new Dictionary<string, string>
                {
                    { "hearthwait_ready_in", "Ready in {0}" },
                    { "hearthwait_ready_now", "Ready" },
                    { "hearthwait_next_in", "Next in {0}" },
                    { "hearthwait_full_in", "Full in {0}" },
                    { "hearthwait_full_ready", "Full — ready to collect" },
                    { "hearthwait_burn_in", "Burns in {0}" },
                    { "hearthwait_burnt", "Burnt" },
                    { "hearthwait_waiting_wind", "Waiting on the wind" },
                        { "hearthwait_plant_stalled", "Won't grow like this" },
                        { "hearthwait_ferment_paused", "Paused — needs cover" },
                    { "hearthwait_needs_fuel", "Needs fuel" },
                    { "hearthwait_empty", "Empty" },
                    { "hearthwait_picked", "Picked" },
                    { "hearthwait_grows_in", "Grows back in {0}" },
                    { "hearthwait_harvest_ready", "Ready — {0}" },
                    { "hearthwait_burns_for", "Burns for {0}" },
                    { "hearthwait_fire_out", "Fire is out" },
                    { "hearthwait_egg_stacked", "Split the stack to hatch" },
                    { "hearthwait_egg_cold", "Too cold to hatch" },
                    { "hearthwait_hatches_in", "Hatches in {0}" },
                    { "hearthwait_hatch_ready", "About to hatch" },
                    { "hearthwait_grows_up_in", "Grows up in {0}" },
                    { "hearthwait_grown_ready", "Ready to grow up" },
                    { "hearthwait_tames_in", "Tames in {0}" },
                    { "hearthwait_tame_ready", "Ready to tame" },
                    { "hearthwait_fed_for", "Fed for {0}" },
                    { "hearthwait_hungry", "Hungry" },
                    { "hearthwait_births_in", "Births in {0}" },
                    { "hearthwait_birth_ready", "About to give birth" },
                    { "hearthwait_love_points", "Love {0}/{1}" }
                });
                Jotunn.Logger.LogWarning("Hearthwait: Translations folder missing — English inline fallback");
            }
        }

        private static bool TryLoad(Jotunn.Entities.CustomLocalization loc, string language)
        {
            try
            {
                var path = FindPath(language);
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    return false;
                }

                loc.AddJsonFile(language, File.ReadAllText(path));
                return true;
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogWarning($"Hearthwait: failed loading {language} — {ex.Message}");
                return false;
            }
        }

        private static string FindPath(string language)
        {
            var file = Path.Combine("Translations", language, "hearthwait.json");
            var nextToDll = Path.Combine(Path.GetDirectoryName(typeof(HearthwaitPlugin).Assembly.Location) ?? "", file);
            if (File.Exists(nextToDll))
            {
                return nextToDll;
            }

            var cwd = Path.Combine(Directory.GetCurrentDirectory(), file);
            return File.Exists(cwd) ? cwd : null;
        }

        internal static string L(string token, params object[] args)
        {
            var raw = Localization.instance != null
                ? Localization.instance.Localize("$" + token)
                : "$" + token;
            if (args == null || args.Length == 0)
            {
                return raw;
            }

            try
            {
                return string.Format(raw, args);
            }
            catch
            {
                return raw;
            }
        }
    }
}
