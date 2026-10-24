using Game.Prefabs;
using Game.SceneFlow;
using Game.UI;
using Newtonsoft.Json;
using Pinboard.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;

namespace Pinboard.Systems
{
    // Finding assets in the game: their names, pictures and where they come from,
    // and picking one up when the player clicks it in the panel.
    public partial class PinboardUISystem
    {
        // Every asset in the game by name, built the first time it's needed
        // (finding an asset whose type we don't know) and reused after that.
        private Dictionary<string, PrefabBase> m_PrefabsByName;

        // The language the stored names and pictures were last refreshed for.
        // Opening the panel only refreshes them again if the language has changed.
        private string m_RefreshedLocale;

        // Assets already reported as missing this session, so the log gets one line each.
        private readonly HashSet<string> m_ReportedMissing = new HashSet<string>();

        // The asset types most pins are, tried in turn before searching every asset.
        private static readonly string[] s_CommonPrefabTypes =
        {
            "StaticObjectPrefab", "BuildingPrefab", "VegetationPrefab",
            "PropPrefab", "ObjectGeometryPrefab", "MarkerObjectPrefab",
        };

        // ── Describing an asset ───────────────────────────────────────────────

        // The asset's name in the player's language, or a readable version of its code name.
        private string GetDisplayName(PrefabBase prefab)
        {
            var dict = GameManager.instance.localizationManager.activeDictionary;
            if (dict.TryGetValue("Assets.NAME[" + prefab.name + "]", out var name) && !string.IsNullOrEmpty(name))
                return name;
            return prefab.name.Replace('_', ' ');
        }

        // Everything a pin remembers about an asset, read from the game.
        private FavouriteEntry EntryFromPrefab(PrefabBase prefab)
        {
            var entry = new FavouriteEntry
            {
                Name = prefab.name,
                Type = prefab.GetType().Name,
                DisplayName = GetDisplayName(prefab),
                Thumbnail = ImageSystem.GetThumbnail(prefab) ?? "",
            };
            ApplySource(entry, prefab);
            return entry;
        }

        // Fills in an entry's pack / mod from the game.
        private static void ApplySource(FavouriteEntry entry, PrefabBase prefab)
        {
            AssetSources.Detect(prefab, out var pack, out var modId, out _);
            entry.SourcePack = pack;
            entry.SourceModId = modId;
        }

        private static string CurrentLocale()
        {
            try { return GameManager.instance?.localizationManager?.activeLocaleId ?? ""; }
            catch { return ""; }
        }

        // Pins remember the asset's name and picture from when they were pinned.
        // Refresh them from the game on load, so a language switch or an updated
        // asset shows correctly. The saved values stay as a backup if an asset is gone.
        private void RefreshStoredAssetInfo()
        {
            var locale = CurrentLocale();
            if (locale != m_RefreshedLocale)
            {
                // Pack names are in the player's language, so look them up again too.
                AssetSources.ClearCache();
                m_RefreshedLocale = locale;
            }
            foreach (var entry in AllStoredEntries())
            {
                if (entry == null || string.IsNullOrEmpty(entry.Name) || string.IsNullOrEmpty(entry.Type)) continue;
                try
                {
                    if (!m_PrefabSystem.TryGetPrefab(new PrefabID(entry.Type, entry.Name), out PrefabBase prefab)) continue;
                    entry.DisplayName = GetDisplayName(prefab);
                    ApplySource(entry, prefab);
                    var thumb = ImageSystem.GetThumbnail(prefab);
                    if (!string.IsNullOrEmpty(thumb)) entry.Thumbnail = thumb;
                }
                catch (Exception ex)
                {
                    Mod.log.Warn($"[Pinboard] Couldn't refresh name for {entry.Name}: {ex.Message}");
                }
            }
        }

        // ── Finding what we already know ──────────────────────────────────────

        // Every entry in the save: the pins, then Recently Placed, Most Used and the
        // quick pins. An empty quick pin slot comes out as null.
        private IEnumerable<FavouriteEntry> AllStoredEntries()
        {
            return m_Favourites.Collections.SelectMany(c => c.Pins)
                .Concat(m_Favourites.RecentlyPlaced)
                .Concat(m_Favourites.MostUsed)
                .Concat(m_Favourites.QuickPins);
        }

        // The saved entry for an asset, wherever it is. Not a copy.
        private FavouriteEntry FindStoredEntry(string name)
        {
            return m_Favourites == null ? null : AllStoredEntries().FirstOrDefault(e => e?.Name == name);
        }

        // Finds everything we know about an asset by name: the asset held right now,
        // the one clicked in the city, then anything saved. Returns a copy.
        private FavouriteEntry FindKnownEntry(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (m_Current != null && name == m_Current.Name) return m_Current.Copy();
            if (m_Inspected != null && name == m_Inspected.Name) return m_Inspected.Copy();
            return FindStoredEntry(name)?.Copy();
        }

        // ── Finding an asset in the game ──────────────────────────────────────

        private Dictionary<string, PrefabBase> AllPrefabsByName()
        {
            if (m_PrefabsByName != null) return m_PrefabsByName;
            var result = new Dictionary<string, PrefabBase>();
            var query = GetEntityQuery(ComponentType.ReadOnly<PrefabData>());
            using (var entities = query.ToEntityArray(Allocator.Temp))
            {
                foreach (var e in entities)
                {
                    if (m_PrefabSystem.TryGetPrefab(e, out PrefabBase prefab) && prefab != null && !result.ContainsKey(prefab.name))
                        result[prefab.name] = prefab;
                }
            }
            m_PrefabsByName = result;
            return result;
        }

        // Finds an asset by name alone. The list of every asset is made once and
        // reused, instead of going through all of them on every click.
        private PrefabBase FindPrefabByName(string name)
        {
            return AllPrefabsByName().TryGetValue(name, out var prefab) ? prefab : null;
        }

        // Looks up each pin in the game to fill in its picture, proper name and type.
        // Returns how many aren't installed here. The full scan of every asset only
        // happens once, and only if a pin can't be found the quick way.
        private int FillInFromGame(List<FavouriteEntry> pins)
        {
            var missing = 0;
            foreach (var pin in pins)
            {
                PrefabBase prefab = null;
                try
                {
                    if (!string.IsNullOrEmpty(pin.Type))
                        m_PrefabSystem.TryGetPrefab(new PrefabID(pin.Type, pin.Name), out prefab);
                    if (prefab == null) prefab = FindPrefabByName(pin.Name);
                }
                catch (Exception ex)
                {
                    Mod.log.Warn($"[Pinboard] Couldn't look up {pin.Name}: {ex.Message}");
                }

                if (prefab == null) { missing++; continue; }
                pin.Type = prefab.GetType().Name;
                pin.DisplayName = GetDisplayName(prefab);
                pin.Thumbnail = ImageSystem.GetThumbnail(prefab) ?? "";
                ApplySource(pin, prefab);
            }
            return missing;
        }

        // ── Picking an asset up ───────────────────────────────────────────────

        // Clicking an asset anywhere in the panel (or a quick-pick key) picks it up,
        // as if the player had chosen it in the game's own menus.
        private void OnSelectAsset(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            var entry = FindStoredEntry(name);

            // 1. Fast path — the type we stored when the asset was pinned or placed.
            if (entry != null && !string.IsNullOrEmpty(entry.Type) &&
                m_PrefabSystem.TryGetPrefab(new PrefabID(entry.Type, name), out PrefabBase byStored))
            {
                m_ToolSystem.ActivatePrefabTool(byStored);
                return;
            }

            // 2. Guess from the common placeable prefab types.
            foreach (var typeName in s_CommonPrefabTypes)
            {
                if (m_PrefabSystem.TryGetPrefab(new PrefabID(typeName, name), out PrefabBase byGuess))
                {
                    HealStoredType(entry, typeName);
                    m_ToolSystem.ActivatePrefabTool(byGuess);
                    return;
                }
            }

            // 3. Definitive — match by name against every registered prefab,
            //    so assets of any type (sculptures, custom ploppables, etc.) resolve.
            var found = FindPrefabByName(name);
            if (found != null)
            {
                HealStoredType(entry, found.GetType().Name);
                m_ToolSystem.ActivatePrefabTool(found);
                return;
            }

            // Usually a custom asset or content pack that isn't installed or enabled.
            // Tell the player in a pop-up (the panel shows it), and log it once.
            // Say where it comes from too, when we know (for example from a shared collection).
            m_MissingAssetSourceBinding.Update(entry == null ? "" : JsonConvert.SerializeObject(new { pack = entry.SourcePack, modId = entry.SourceModId }));
            m_MissingAssetBinding.Update(string.IsNullOrEmpty(entry?.DisplayName) ? name : entry.DisplayName);
            if (m_ReportedMissing.Add(name))
                Mod.log.Warn($"[Pinboard] Asset not found: {name}");
        }

        // Saves the type we found onto the entry, so next time takes the fast path.
        private void HealStoredType(FavouriteEntry entry, string type)
        {
            if (entry == null || entry.Type == type || string.IsNullOrEmpty(type)) return;
            entry.Type = type;
            RequestSave();
        }
    }
}
