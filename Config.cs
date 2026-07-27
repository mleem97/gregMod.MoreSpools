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

    // Top-level config object.  Key = cableType as string (e.g. "0", "1").
    internal class ModConfig
    {
        [JsonPropertyName("cable_types")]
        public Dictionary<string, LengthConfig[]> CableTypes { get; set; } = new();
    }

    internal static class ConfigManager
    {
        private static readonly string ConfigPath = Path.Combine(
            MelonEnvironment.UserDataDirectory, "LargerSpools.json");

        // Default lengths added when no config file exists yet.
        // Covers cableType 0 with three sensible sizes.
        // Add entries here for other cable types once their IDs are known.
        private static readonly ModConfig DefaultConfig = new ModConfig
        {
            CableTypes = new Dictionary<string, LengthConfig[]>
            {
                ["0"] = new[]
                {
                    new LengthConfig { LengthMeters = 500f,  PriceMultiplier = 2.0f },
                },
            }
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

                MelonLogger.Msg($"Config loaded from {ConfigPath}");
                return loaded;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to load config: {ex.Message} — using defaults.");
                return DefaultConfig;
            }
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
