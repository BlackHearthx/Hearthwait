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
                    { "hearthwait_next_in", "Next in {0}" },
                    { "hearthwait_full_in", "Full in {0}" },
                    { "hearthwait_burn_in", "Burns in {0}" },
                    { "hearthwait_waiting_wind", "Waiting on the wind" }
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
