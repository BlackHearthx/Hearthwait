using System;
using HarmonyLib;
using UnityEngine;

namespace Hearthwait
{
    /// <summary>
    /// Smelters only expose Switch hovers (ore/fuel mouths). Looking at the hull hits
    /// WearNTear/Piece — inject the same timers there. Rebuild from GetHoverText every
    /// frame so the clock does not freeze.
    /// </summary>
    internal static class HoverBodyPatch
    {
        internal static void Postfix(Hud __instance, Player player)
        {
            if (__instance == null || player == null || __instance.m_hoverName == null)
            {
                return;
            }

            try
            {
                if (TextViewer.instance != null && TextViewer.instance.IsVisible())
                {
                    return;
                }
            }
            catch
            {
            }

            GameObject hover;
            try
            {
                hover = player.GetHoverObject();
            }
            catch
            {
                return;
            }

            if (hover == null)
            {
                return;
            }

            try
            {
                var bodyExtra = StationTimes.ForBodyHover(hover);
                if (string.IsNullOrEmpty(bodyExtra))
                {
                    return;
                }

                string baseText = null;
                try
                {
                    var hoverable = hover.GetComponentInParent<Hoverable>();
                    if (hoverable != null)
                    {
                        baseText = hoverable.GetHoverText();
                    }
                }
                catch
                {
                }

                if (string.IsNullOrEmpty(baseText))
                {
                    __instance.m_hoverName.text = bodyExtra;
                    return;
                }

                // Switch / piece GetHoverText already includes our postfix — do not stack.
                if (HasSameClock(baseText, bodyExtra))
                {
                    __instance.m_hoverName.text = baseText;
                    return;
                }

                __instance.m_hoverName.text = baseText + "\n" + bodyExtra;
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogDebug("Hearthwait body hover: " + ex.Message);
            }
        }

        private static bool HasSameClock(string baseText, string extra)
        {
            var plainBase = StripColors(baseText);
            var plainExtra = StripColors(extra);
            if (plainBase.IndexOf(plainExtra, StringComparison.Ordinal) >= 0)
            {
                return true;
            }

            // Match label prefix so "Pronto em 3m 01s" counts as already present vs "Pronto em 3m 00s"
            // only when base already has a timer line from GetHoverText (same frame → same seconds).
            foreach (var line in plainExtra.Split('\n'))
            {
                var t = line.Trim();
                if (t.Length < 5)
                {
                    continue;
                }

                // Take "Pronto em" / "Volta em" / "Queima por" style prefix (first two words).
                var parts = t.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                {
                    continue;
                }

                var prefix = parts[0] + " " + parts[1];
                if (plainBase.IndexOf(prefix, StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static string StripColors(string s)
        {
            if (string.IsNullOrEmpty(s) || s.IndexOf('<') < 0)
            {
                return s;
            }

            var chars = new char[s.Length];
            var n = 0;
            var skip = false;
            foreach (var c in s)
            {
                if (c == '<')
                {
                    skip = true;
                    continue;
                }

                if (c == '>')
                {
                    skip = false;
                    continue;
                }

                if (!skip)
                {
                    chars[n++] = c;
                }
            }

            return new string(chars, 0, n);
        }
    }
}
