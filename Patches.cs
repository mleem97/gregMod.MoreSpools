using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace GregModMoreSpools
{
    // =========================================================================
    // Patch: MainGameManager.Awake (Postfix)
    // Earliest guaranteed point where cableSpinnerPrefab is populated.
    // Runs before OnLoad() restores saves — ensures our IDs exist when the
    // save system tries to resolve prefabIDs.
    // =========================================================================
    [HarmonyPatch(typeof(MainGameManager), nameof(MainGameManager.Awake))]
    internal static class PatchMainGameManagerAwake
    {
        private static void Postfix(MainGameManager __instance)
        {
            MelonLogger.Msg("MainGameManager.Awake → setting up registry.");
            Core.SetupRegistry(__instance);
        }
    }

    // =========================================================================
    // Patch: MainGameManager.Start (Postfix)
    // Safety net: if Awake's SetupRegistry failed for any reason (Registry is
    // empty), try again in Start.  Only runs once per scene load because
    // SetupRegistry populates Registry on success.
    // =========================================================================
    [HarmonyPatch(typeof(MainGameManager), nameof(MainGameManager.Start))]
    internal static class PatchMainGameManagerStart
    {
        private static void Postfix(MainGameManager __instance)
        {
            if (Core.Registry.Count == 0)
            {
                MelonLogger.Warning("Registry empty after Awake — retrying in Start.");
                Core.SetupRegistry(__instance);
            }
        }
    }

    // =========================================================================
    // Patch: ComputerShop.GetPrefabForItem (Prefix)
    // Intercepts CableSpinner purchases with our custom itemIDs (100+) and
    // returns a freshly cloned prefab.
    // =========================================================================
    [HarmonyPatch(typeof(ComputerShop), nameof(ComputerShop.GetPrefabForItem))]
    internal static class PatchGetPrefabForItem
    {
        private static bool Prefix(int itemID, PlayerManager.ObjectInHand itemType,
                                   ref GameObject __result)
        {
            // Only intercept CableSpinner (6) with our registered IDs.
            if ((int)itemType != 6) return true;
            if (!Core.Registry.TryGetValue(itemID, out var entry)) return true;

            var mgm = MainGameManager.instance;
            if (mgm == null) return true;

            __result = Core.BuildSpinnerPrefab(mgm, itemID, entry);
            return false;
        }
    }
}
