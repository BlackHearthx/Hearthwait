using HarmonyLib;
using UnityEngine;

namespace Hearthwait
{
    [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateCrosshair))]
    internal static class HoverTimePatch
    {
        private static void Postfix(Hud __instance, Player player)
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

            GameObject hover = null;
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

            var extra = StationTimes.ForHover(hover);
            if (string.IsNullOrEmpty(extra))
            {
                return;
            }

            var current = __instance.m_hoverName.text;
            if (string.IsNullOrEmpty(current) || current.IndexOf(extra, System.StringComparison.Ordinal) >= 0)
            {
                return;
            }

            __instance.m_hoverName.text = current + "\n" + extra;
        }
    }
}
