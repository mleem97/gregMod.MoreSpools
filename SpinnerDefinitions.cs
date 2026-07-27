using System;
using System.Collections.Generic;
using MelonLoader;

namespace GregModMoreSpools
{
    // -----------------------------------------------------------------------
    // Describes one length variant for a cable type.
    // -----------------------------------------------------------------------
    internal class LengthDef
    {
        internal readonly float  LengthMeters;
        internal readonly float  PriceMultiplier;  // relative to vanilla base price
        internal readonly string GuidSuffix;       // auto-derived from length; never change

        internal LengthDef(float lengthMeters, float priceMultiplier)
        {
            LengthMeters    = lengthMeters;
            PriceMultiplier = priceMultiplier;
            GuidSuffix      = $"{(int)lengthMeters}m";  // e.g. "500m", "1000m"
        }
    }

    // -----------------------------------------------------------------------
    // Runtime registry entry — built in SetupRegistry from vanilla type info
    // and a LengthDef.  Stores everything BuildSpinnerPrefab needs.
    // -----------------------------------------------------------------------
    internal class SpinnerEntry
    {
        internal readonly float LengthMeters;
        internal readonly int   BasePrefabID;  // vanilla prefabID to clone from
        internal readonly int   CableType;     // original CableSpinner.cableType to preserve

        internal SpinnerEntry(float lengthMeters, int basePrefabID, int cableType)
        {
            LengthMeters = lengthMeters;
            BasePrefabID = basePrefabID;
            CableType    = cableType;
        }
    }

    // -----------------------------------------------------------------------
    // Length definitions — loaded from UserData/LargerSpools.json at startup.
    //
    // ID layout (order and MaxPerType must NEVER change after release):
    //   MOD_ID_BASE + (typeIndex * LengthList.MaxPerType) + lengthIndex
    //
    // MaxPerType is a hard ceiling — the JSON may contain at most MaxPerType
    // entries per cable type; extras are silently ignored.
    //
    // GuidSuffix is derived from the length value (e.g. "500m").  Once a save
    // has been created, never change an existing length value — rename the
    // suffix in the save instead.
    // -----------------------------------------------------------------------
    internal static class LengthList
    {
        // ID-layout constant — NEVER change after first release.
        internal const int MaxPerType = 16;

        // Populated by LoadFromConfig(); read-only after that.
        internal static Dictionary<int, LengthDef[]> ByType { get; private set; } = new();

        // Called once in Core.OnInitializeMelon after the config is loaded.
        internal static void LoadFromConfig(ModConfig config)
        {
            var result = new Dictionary<int, LengthDef[]>();

            foreach (var kv in config.CableTypes)
            {
                if (!int.TryParse(kv.Key, out int cableType))
                {
                    MelonLogger.Warning($"Config: skipping non-integer key '{kv.Key}'.");
                    continue;
                }

                if (kv.Value == null || kv.Value.Length == 0) continue;

                var defs  = new List<LengthDef>();
                int count = 0;
                foreach (var lc in kv.Value)
                {
                    if (count >= MaxPerType)
                    {
                        MelonLogger.Warning($"Config: cableType {cableType} has more than " +
                                            $"{MaxPerType} entries — extras ignored.");
                        break;
                    }
                    if (lc.LengthMeters <= 0f)
                    {
                        MelonLogger.Warning($"Config: skipping entry with length <= 0 " +
                                            $"for cableType {cableType}.");
                        continue;
                    }
                    defs.Add(new LengthDef(lc.LengthMeters, lc.PriceMultiplier));
                    count++;
                }

                if (defs.Count > 0)
                    result[cableType] = defs.ToArray();
            }

            ByType = result;
            MelonLogger.Msg($"Config: {ByType.Count} cable type(s) loaded.");
        }

        internal static LengthDef[] ForType(int cableType)
            => ByType.TryGetValue(cableType, out var defs) ? defs : Array.Empty<LengthDef>();
    }
}
