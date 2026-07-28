using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using MelonLoader;
using MelonLoader.Utils;

namespace GregModMoreSpools
{
    // One length entry as stored in the JSON config.
    internal class LengthConfig
    {
        [JsonPropertyName("length_m")]
        public float LengthMeters { get; set; }

        [JsonPropertyName("price_multiplier")]
        public float PriceMultiplier { get; set; }
    }

    internal class CustomLengthConfig
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("length_m")]
        public float LengthMeters { get; set; } = 2500f;

        [JsonPropertyName("price_multiplier")]
        public float PriceMultiplier { get; set; } = 6.0f;
    }

    // Top-level config object.  Key = cableType as string (e.g. "0", "1").
    internal class ModConfig
    {
        [JsonPropertyName("cable_types")]
        public Dictionary<string, LengthConfig[]> CableTypes { get; set; } = new();

        [JsonPropertyName("custom_length")]
        public CustomLengthConfig CustomLength { get; set; } = new();
    }

    internal static class ConfigManager
    {
        private static readonly string ConfigPath = Path.Combine(
            MelonEnvironment.UserDataDirectory, "LargerSpools.json");

        // Defaults are appended during migration, so the existing 500m entry keeps
        // its old index/prefab ID in already-created saves.
        private static readonly ModConfig DefaultConfig = new ModConfig
        {
            CableTypes = new Dictionary<string, LengthConfig[]>
            {
                ["0"] = new[]
                {
                    new LengthConfig { LengthMeters = 1000f, PriceMultiplier = 3.0f },
                    new LengthConfig { LengthMeters = 2000f, PriceMultiplier = 4.5f },
                    new LengthConfig { LengthMeters = 5000f, PriceMultiplier = 8.0f },
                    new LengthConfig { LengthMeters = 10000f, PriceMultiplier = 14.0f },
                },
            },
            CustomLength = new CustomLengthConfig { Enabled = true, LengthMeters = 2500f, PriceMultiplier = 6.0f },
        };

        // Loads the config from disk; creates a default file on first run.
        internal static ModConfig Load()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    WriteFile(DefaultConfig);
                    MelonLogger.Msg($"Default config created at:\n  {ConfigPath}");
                    return DefaultConfig;
                }

                var json   = File.ReadAllText(ConfigPath);
                var loaded = JsonSerializer.Deserialize<ModConfig>(json);

                if (loaded?.CableTypes == null || loaded.CableTypes.Count == 0)
                {
                    MelonLogger.Warning("Config file empty or invalid — using defaults.");
                    return DefaultConfig;
                }

                bool changed = MergeDefaults(loaded);
                if (changed) WriteFile(loaded);
                MelonLogger.Msg($"Config loaded from {ConfigPath}");
                return loaded;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to load config: {ex.Message} — using defaults.");
                return DefaultConfig;
            }
        }

        private static bool MergeDefaults(ModConfig config)
        {
            bool changed = false;
            if (config.CustomLength == null)
            {
                config.CustomLength = new CustomLengthConfig
                {
                    Enabled = DefaultConfig.CustomLength.Enabled,
                    LengthMeters = DefaultConfig.CustomLength.LengthMeters,
                    PriceMultiplier = DefaultConfig.CustomLength.PriceMultiplier,
                };
                changed = true;
            }

            foreach (var defaults in DefaultConfig.CableTypes)
            {
                if (!config.CableTypes.TryGetValue(defaults.Key, out var existing))
                {
                    config.CableTypes[defaults.Key] = defaults.Value;
                    changed = true;
                    continue;
                }

                var merged = new List<LengthConfig>(existing);
                foreach (var candidate in defaults.Value)
                {
                    bool present = false;
                    foreach (var current in merged)
                    {
                        if (Math.Abs(current.LengthMeters - candidate.LengthMeters) < 0.01f)
                        {
                            present = true;
                            break;
                        }
                    }
                    if (present) continue;
                    merged.Add(candidate);
                    changed = true;
                }
                config.CableTypes[defaults.Key] = merged.ToArray();
            }
            return changed;
        }

        private static void WriteFile(ModConfig config)
        {
            try
            {
                var opts = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(ConfigPath, JsonSerializer.Serialize(config, opts));
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to write config: {ex.Message}");
            }
        }
    }
}
