using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(GregModMoreSpools.Core), "gregMod.MoreSpools", "1.1.1", "TeamGreg Modding (leoms1408 / mleem97)")]
[assembly: MelonGame("Waseku", "Data Center")]

namespace GregModMoreSpools
{
    // Snapshot of a vanilla cable type captured during SetupRegistry.
    // Used in AddShopItems to find the right shop-item template and base price.
    internal class VanillaType
    {
        internal int    PrefabID;
        internal int    CableType;     // CableSpinner.cableType value (controls appearance)
        internal string DisplayName;   // filled in by AddShopItems from shopItemSO.itemName
        internal int    ShopPrice;     // filled in by AddShopItems
        internal Sprite ShopSprite;    // filled in by AddShopItems
    }

    public class Core : MelonMod
    {
        // Registry: custom prefabID (100+) → SpinnerEntry
        internal static readonly Dictionary<int, SpinnerEntry> Registry = new();

        // Vanilla cable types discovered in SetupRegistry, in the order they
        // appear in cableSpinnerPrefab.  Index used to compute custom prefabIDs.
        internal static readonly List<VanillaType> VanillaTypes = new();

        // Cache: vanilla prefabID → vanilla GameObject. Built in SetupRegistry.
        // Avoids a linear scan of the entire prefab array on every purchase.
        internal static readonly Dictionary<int, GameObject> BasePrefabCache = new();

        // Inactive holder so template GOs are never visible to the world tracker.
        internal static GameObject TemplateHolder { get; private set; }

        // MOD_ID_BASE + (typeIndex * LengthList.MaxPerType) + lengthIndex = prefabID
        internal const int MOD_ID_BASE = 100;

        // Guard: prevents AddShopItems from running twice for the same scene load.
        private bool _shopSetupDone;

        public override void OnInitializeMelon()
        {
            var config = ConfigManager.Load();
            LengthList.LoadFromConfig(config);
            HarmonyInstance.PatchAll();
            LoggerInstance.Msg("gregMod.MoreSpools v1.1.1 loaded!");
        }

        // -----------------------------------------------------------------------
        // Called from PatchMainGameManagerAwake (and as safety net from Start).
        //
        // Custom prefabID layout:
        //   MOD_ID_BASE + (typeIndex * LengthList.All.Length) + lengthIndex
        // -----------------------------------------------------------------------
        internal static void SetupRegistry(MainGameManager mgm)
        {
            Registry.Clear();
            VanillaTypes.Clear();
            BasePrefabCache.Clear();

            var prefabs = mgm.cableSpinnerPrefab;
            if (prefabs == null || prefabs.Length == 0)
            {
                MelonLogger.Warning("cableSpinnerPrefab is empty — skipping setup.");
                return;
            }

            // --- Step 1: scan vanilla range, group by cableType, keep shortest per type ---
            // Indices >= MOD_ID_BASE belong to us (possibly from a prior run).
            int scanLimit = System.Math.Min(prefabs.Length, MOD_ID_BASE);
            MelonLogger.Msg($"Scanning vanilla range [0..{scanLimit - 1}] " +
                            $"(array length = {prefabs.Length}).");

            // key = cableType, value = (shortest length seen, prefabID of that entry)
            var bestPerType = new Dictionary<int, (float len, int prefabID)>();

            for (int i = 0; i < scanLimit; i++)
            {
                var go = prefabs[i];
                if (go == null) continue;

                var spinner = go.GetComponent<CableSpinner>();
                float len = spinner != null ? spinner.cableLenght : -1f;
                if (len <= 0f) continue;

                var usable   = go.GetComponent<UsableObject>();
                int pid      = usable  != null ? usable.prefabID   : i;
                int cableTyp = spinner != null ? spinner.cableType : 0;

                // Populate the base-prefab cache for every vanilla entry.
                if (!BasePrefabCache.ContainsKey(pid))
                    BasePrefabCache[pid] = go;

                if (!bestPerType.TryGetValue(cableTyp, out var existing) || len < existing.len)
                    bestPerType[cableTyp] = (len, pid);
            }

            if (bestPerType.Count == 0)
            {
                MelonLogger.Error("No valid vanilla CableSpinner found.");
                return;
            }

            // --- Step 2: build VanillaTypes — one representative per cableType ---
            foreach (var kv in bestPerType.OrderBy(k => k.Key))
            {
                var (len, pid) = kv.Value;
                int cableTyp = kv.Key;
                MelonLogger.Msg($"  base type [{VanillaTypes.Count}]: " +
                                $"prefabID={pid}, cableType={cableTyp}, length={len}m");
                VanillaTypes.Add(new VanillaType { PrefabID = pid, CableType = cableTyp });
            }

            if (VanillaTypes.Count == 0)
            {
                MelonLogger.Error("No base vanilla types found.");
                return;
            }

            // --- Step 3: prepare the extended array ---
            // Recreate template holder (destroys old one from a prior run).
            if (TemplateHolder != null)
                Object.Destroy(TemplateHolder);
            TemplateHolder = new GameObject("gregModMoreSpools_TemplateHolder");
            TemplateHolder.SetActive(false);
            Object.DontDestroyOnLoad(TemplateHolder);

            int totalCustom    = VanillaTypes.Count * LengthList.MaxPerType;
            int requiredLength = MOD_ID_BASE + totalCustom;

            // Reuse the existing array if it is already large enough (DontDestroyOnLoad
            // reuse path); otherwise allocate a fresh one.
            GameObject[] extended;
            if (prefabs.Length >= requiredLength)
            {
                extended = prefabs; // overwrite our own slots in place
            }
            else
            {
                extended = new GameObject[requiredLength];
                for (int i = 0; i < scanLimit; i++)
                    extended[i] = prefabs[i];
            }

            // --- Step 4: register and write custom templates ---
            for (int typeIdx = 0; typeIdx < VanillaTypes.Count; typeIdx++)
            {
                var vt     = VanillaTypes[typeIdx];
                var lengths = LengthList.ForType(vt.CableType);
                for (int lenIdx = 0; lenIdx < lengths.Length; lenIdx++)
                {
                    var ld    = lengths[lenIdx];
                    int id    = MOD_ID_BASE + typeIdx * LengthList.MaxPerType + lenIdx;
                    var entry = new SpinnerEntry(ld.LengthMeters, vt.PrefabID, vt.CableType);
                    Registry[id] = entry;

                    var template = BuildSpinnerPrefab(mgm, id, entry, TemplateHolder.transform);
                    if (template != null)
                        template.name = $"CableSpinner_template_{id}";
                    extended[id] = template;

                    MelonLogger.Msg($"Registered '{vt.DisplayName} – {ld.LengthMeters}m': " +
                                    $"prefabID={id}, basePrefabID={vt.PrefabID}");
                }
            }

            mgm.cableSpinnerPrefab = extended;
            MelonLogger.Msg($"Setup complete: {VanillaTypes.Count} types, " +
                            $"up to {LengthList.MaxPerType} lengths each → IDs {MOD_ID_BASE}–{requiredLength - 1}");
        }

        // -----------------------------------------------------------------------
        // Clones the vanilla prefab identified by entry.BasePrefabID, applies the
        // custom length and prefabID.  parent != null → stored under TemplateHolder
        // (invisible to world tracker); null → live delivery clone.
        //
        // Uses BasePrefabCache for O(1) lookup; falls back to linear scan only
        // if the cache misses (e.g. called before SetupRegistry fully ran).
        // -----------------------------------------------------------------------
        internal static GameObject BuildSpinnerPrefab(MainGameManager mgm, int prefabID,
                                                      SpinnerEntry entry, Transform parent = null)
        {
            GameObject basePrefab = null;

            // Fast path: dictionary lookup.
            if (BasePrefabCache.TryGetValue(entry.BasePrefabID, out var cached))
            {
                basePrefab = cached;
            }
            else
            {
                // Slow path fallback: linear scan.
                var arr = mgm.cableSpinnerPrefab;
                for (int i = 0; i < arr.Length; i++)
                {
                    var go = arr[i];
                    if (go == null) continue;
                    var u = go.GetComponent<UsableObject>();
                    if (u != null && u.prefabID == entry.BasePrefabID)
                    {
                        basePrefab = go;
                        BasePrefabCache[entry.BasePrefabID] = go; // warm the cache
                        break;
                    }
                }
            }

            if (basePrefab == null)
            {
                MelonLogger.Error($"Base prefab (prefabID={entry.BasePrefabID}) not found.");
                return null;
            }

            var clone = parent != null
                ? Object.Instantiate(basePrefab, parent, false)
                : Object.Instantiate(basePrefab);
            clone.name = $"CableSpinner_custom_{prefabID}";

            var spinner = clone.GetComponent<CableSpinner>();
            if (spinner != null)
            {
                spinner.cableLenght      = entry.LengthMeters;
                spinner.cableLenghtInUse = 0f;
                spinner.cableType        = entry.CableType; // keep vanilla type for correct appearance
            }

            var usable = clone.GetComponent<UsableObject>();
            if (usable != null)
                usable.prefabID = prefabID;

            return clone;
        }

        // -----------------------------------------------------------------------
        // Scene load hook — injects shop buttons after every non-menu load.
        // Resets the deduplication flag so items are (re-)added each scene.
        // -----------------------------------------------------------------------
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (buildIndex == 0) return;
            _shopSetupDone = false;
            MelonCoroutines.Start(AddShopItems());
        }

        // -----------------------------------------------------------------------
        // Waits for the shop to initialise (poll loop instead of a fixed delay),
        // then injects buttons into "HL Mods".
        //
        // The deduplication flag (_shopSetupDone) prevents a second coroutine
        // started in the same scene from adding buttons twice.
        //
        // For each vanilla cable type × each custom length:
        //   (a) regular button  — default cable colour
        //   (b) RGB button      — vanilla hex colour picker (isCustomColor = true)
        // -----------------------------------------------------------------------
        private IEnumerator AddShopItems()
        {
            // Poll until the shop is ready, bail out after 15 seconds.
            float waited = 0f;
            MainGameManager mgm  = null;
            Il2Cpp.ComputerShop shop = null;

            while (waited < 15f)
            {
                mgm  = MainGameManager.instance;
                shop = mgm?.computerShop;
                if (shop != null) break;
                waited += UnityEngine.Time.unscaledDeltaTime;
                yield return null;
            }

            if (shop == null)
            {
                LoggerInstance.Warning("Shop not ready after 15 s — skipped.");
                yield break;
            }

            // Deduplication guard: only one coroutine per scene load may proceed.
            if (_shopSetupDone) yield break;
            _shopSetupDone = true;

            // Collect all vanilla CableSpinner shop entries keyed by itemID (= prefabID).
            // These are used both as UI clone templates and as price/sprite sources.
            var vanillaShopItems = new Dictionary<int, ShopItem>();
            if (shop.shopItems != null)
            {
                foreach (var si in shop.shopItems)
                {
                    if (si == null || si.shopItemSO == null) continue;
                    if ((int)si.shopItemSO.itemType != 6) continue; // ObjectInHand.CableSpinner
                    vanillaShopItems[si.shopItemSO.itemID] = si;
                }
            }

            if (vanillaShopItems.Count == 0)
            {
                LoggerInstance.Warning("No vanilla CableSpinner shop items found — skipped.");
                yield break;
            }

            MelonLogger.Msg($"Found {vanillaShopItems.Count} vanilla CableSpinner shop item(s).");

            // Backfill name, price, and sprite from vanilla shop items.
            foreach (var vt in VanillaTypes)
            {
                if (vanillaShopItems.TryGetValue(vt.PrefabID, out var si))
                {
                    vt.DisplayName = si.shopItemSO.itemName;
                    vt.ShopPrice   = si.shopItemSO.price;
                    vt.ShopSprite  = si.shopItemSO.sprite;
                    MelonLogger.Msg($"  type prefabID={vt.PrefabID} → '{vt.DisplayName}', price={vt.ShopPrice}");
                }
                else
                {
                    vt.DisplayName = $"Cable Spool (type {vt.CableType})";
                    MelonLogger.Warning($"  No shop item for prefabID={vt.PrefabID} — using fallback name.");
                }
            }

            // Pick any vanilla item as the UI template for cloning.
            ShopItem templateSource = null;
            foreach (var si in vanillaShopItems.Values) { templateSource = si; break; }

            var shopParent = shop.shopItemParent;
            if (shopParent == null) { LoggerInstance.Warning("shopItemParent null."); yield break; }

            var modsTransform = shopParent.transform.Find("HL Mods");
            if (modsTransform != null)
                shopParent = modsTransform.gameObject;
            else
                LoggerInstance.Warning("'HL Mods' not found — falling back to shopItemParent.");

            float itemHeight = 0f;
            var sourceRt = templateSource.GetComponent<UnityEngine.RectTransform>();
            if (sourceRt != null)
                itemHeight = sourceRt.rect.height;

            int addedCount = 0;

            for (int typeIdx = 0; typeIdx < VanillaTypes.Count; typeIdx++)
            {
                var vt = VanillaTypes[typeIdx];

                int    basePrice  = vt.ShopPrice  > 0 ? vt.ShopPrice  : templateSource.shopItemSO.price;
                Sprite baseSprite = vt.ShopSprite != null ? vt.ShopSprite : templateSource.shopItemSO.sprite;

                var lengths = LengthList.ForType(vt.CableType);
                for (int lenIdx = 0; lenIdx < lengths.Length; lenIdx++)
                {
                    var ld      = lengths[lenIdx];
                    int id      = MOD_ID_BASE + typeIdx * LengthList.MaxPerType + lenIdx;
                    if (!Registry.ContainsKey(id)) continue;

                    int price   = (int)(basePrice * ld.PriceMultiplier);
                    string label = $"{vt.DisplayName} – {ld.LengthMeters}m";
                    string guid  = $"larger_spools_t{typeIdx}_{ld.GuidSuffix}";

                    // (a) Regular
                    if (AddShopButton(templateSource, shopParent, id, baseSprite,
                                      label, price, 0, guid, false) != null)
                        addedCount++;

                    // (b) RGB
                    if (AddShopButton(templateSource, shopParent, id, baseSprite,
                                      label + " (RGB)", price,
                                      0, guid + "_rgb", true) != null)
                        addedCount++;
                }
            }

            var containerRt = shopParent.GetComponent<UnityEngine.RectTransform>();
            if (containerRt != null && itemHeight > 0f && addedCount > 0)
            {
                var sd = containerRt.sizeDelta;
                sd.y += itemHeight * addedCount;
                containerRt.sizeDelta = sd;
            }

            UnityEngine.Canvas.ForceUpdateCanvases();
            LoggerInstance.Msg($"Added {addedCount} shop button(s).");
        }

        // -----------------------------------------------------------------------
        // Clones sourceItem, applies the custom SO data, and adds it to parent.
        // Returns the new GameObject or null on failure.
        // -----------------------------------------------------------------------
        private static GameObject AddShopButton(ShopItem source, GameObject parent,
                                                int prefabID, Sprite sprite,
                                                string label, int price, int xpToUnlock,
                                                string guid, bool isCustomColor)
        {
            var newSO = ScriptableObject.CreateInstance<ShopItemSO>();
            newSO.itemName      = label;
            newSO.price         = price;
            newSO.xpToUnlock    = xpToUnlock;
            newSO.itemType      = source.shopItemSO.itemType; // ObjectInHand.CableSpinner
            newSO.itemID        = prefabID;
            newSO.eol           = source.shopItemSO.eol;
            newSO.sprite        = sprite;
            newSO.isCustomColor = isCustomColor;

            var cloned = Object.Instantiate(source.gameObject, parent.transform, false);
            cloned.name = "ShopItem_" + label.Replace(" ", "_")
                                             .Replace("(", "").Replace(")", "")
                                             .Replace("–", "-");
            cloned.transform.localPosition = Vector3.zero;
            cloned.transform.localScale    = Vector3.one;

            var shopItem = cloned.GetComponent<ShopItem>();
            if (shopItem == null)
            {
                MelonLogger.Error($"ShopItem component missing for '{label}'.");
                Object.Destroy(cloned);
                return null;
            }

            shopItem.shopItemSO = newSO;
            shopItem.guid       = guid;
            cloned.SetActive(true);

            MelonLogger.Msg($"Shop button: '{label}' " +
                            $"(id={prefabID}, price={price}, rgb={isCustomColor})");
            return cloned;
        }
    }
}
