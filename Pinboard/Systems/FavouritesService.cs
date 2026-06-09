using Newtonsoft.Json;
using Pinboard.Data;
using System;
using System.IO;
using UnityEngine;

namespace Pinboard.Systems
{
    public static class FavouritesService
    {
        public static string SavePath => Path.Combine(
            Application.persistentDataPath, "ModsData", "Pinboard", "favourites.json");

        // The mod shipped as "PinIt" up to 1.0.1. Existing testers have their data
        // under the old folder; migrate it once so the rename doesn't wipe their pins.
        private static string LegacySavePath => Path.Combine(
            Application.persistentDataPath, "ModsData", "PinIt", "favourites.json");

        public static FavouritesData Load()
        {
            MigrateLegacyDataIfNeeded();
            var data = TryLoad(SavePath) ?? TryLoad(SavePath + ".bak");
            data = data ?? new FavouritesData();
            EnsureDefaults(data);
            return data;
        }

        // One-time copy of the old PinIt save (and its .bak) into the new Pinboard
        // folder. Only runs when the new file doesn't exist yet, so it never clobbers
        // newer data and is a no-op for fresh installs and on every subsequent launch.
        private static void MigrateLegacyDataIfNeeded()
        {
            try
            {
                if (File.Exists(SavePath)) return;          // already migrated or native install
                if (!File.Exists(LegacySavePath)) return;   // nothing to migrate

                Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
                File.Copy(LegacySavePath, SavePath, overwrite: false);
                if (File.Exists(LegacySavePath + ".bak"))
                    File.Copy(LegacySavePath + ".bak", SavePath + ".bak", overwrite: false);

                Mod.log.Info($"[Pinboard] Migrated favourites from legacy PinIt folder: {LegacySavePath} -> {SavePath}");
            }
            catch (Exception ex)
            {
                Mod.log.Warn($"[Pinboard] Legacy data migration failed: {ex.Message}");
            }
        }

        private static FavouritesData TryLoad(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                var result = JsonConvert.DeserializeObject<FavouritesData>(File.ReadAllText(path));
                if (result != null) return result;
            }
            catch (Exception ex)
            {
                Mod.log.Warn($"[Pinboard] FavouritesService.TryLoad({path}) failed: {ex.Message}");
            }
            return null;
        }

        private static void EnsureDefaults(FavouritesData data)
        {
            if (data.Collections.Count == 0)
            {
                var col = new CollectionData { Name = "New Collection" };
                data.Collections.Add(col);
                data.ActiveCollectionId = col.Id;
            }
            else if (data.ActiveCollectionId == null ||
                     !data.Collections.Exists(c => c.Id == data.ActiveCollectionId))
            {
                data.ActiveCollectionId = data.Collections[0].Id;
            }
        }

        public static void Save(FavouritesData data)
        {
            try
            {
                var path    = SavePath;
                var tmpPath = path + ".tmp";
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(tmpPath, JsonConvert.SerializeObject(data, Formatting.Indented));
                if (File.Exists(path))
                    File.Replace(tmpPath, path, path + ".bak");
                else
                    File.Move(tmpPath, path);
            }
            catch (Exception ex)
            {
                Mod.log.Error($"[Pinboard] FavouritesService.Save failed: {ex}");
            }
        }

        public static CollectionData GetActiveCollection(FavouritesData data)
            => data.Collections.Find(c => c.Id == data.ActiveCollectionId);

        // ── Collections ───────────────────────────────────────────────────────

        public static CollectionData CreateCollection(FavouritesData data, string name)
        {
            var col = new CollectionData { Name = name };
            data.Collections.Add(col);
            return col;
        }

        public static void DeleteCollection(FavouritesData data, string id)
        {
            data.Collections.RemoveAll(c => c.Id == id);
            if (data.ActiveCollectionId == id)
                data.ActiveCollectionId = data.Collections.Count > 0 ? data.Collections[0].Id : null;
        }

        public static void RenameCollection(FavouritesData data, string id, string name)
        {
            var col = data.Collections.Find(c => c.Id == id);
            if (col != null) col.Name = name;
        }

        // ── Pins within a collection ──────────────────────────────────────────

        public static void PinToCollection(FavouritesData data, string collectionId, FavouriteEntry entry)
        {
            var col = data.Collections.Find(c => c.Id == collectionId);
            if (col == null) return;
            if (!col.Pins.Exists(p => p.Name == entry.Name))
                col.Pins.Add(entry);
        }

        public static void UnpinFromCollection(FavouritesData data, string collectionId, string assetName)
        {
            var col = data.Collections.Find(c => c.Id == collectionId);
            if (col == null) return;
            col.Pins.RemoveAll(p => p.Name == assetName);
            foreach (var filter in col.Filters)
                filter.Assets.Remove(assetName);
        }

        // ── Filters within a collection ───────────────────────────────────────

        public static FilterData CreateFilter(FavouritesData data, string collectionId, string name)
        {
            var col = data.Collections.Find(c => c.Id == collectionId);
            if (col == null) return null;
            var filter = new FilterData { Name = name };
            col.Filters.Add(filter);
            return filter;
        }

        public static void DeleteFilter(FavouritesData data, string collectionId, string filterId)
        {
            var col = data.Collections.Find(c => c.Id == collectionId);
            col?.Filters.RemoveAll(f => f.Id == filterId);
        }

        public static void RenameFilter(FavouritesData data, string collectionId, string filterId, string name)
        {
            var col = data.Collections.Find(c => c.Id == collectionId);
            var filter = col?.Filters.Find(f => f.Id == filterId);
            if (filter != null) filter.Name = name;
        }

        public static void AddToFilter(FavouritesData data, string collectionId, string filterId, string assetName)
        {
            var col = data.Collections.Find(c => c.Id == collectionId);
            var filter = col?.Filters.Find(f => f.Id == filterId);
            if (filter != null && !filter.Assets.Contains(assetName))
                filter.Assets.Add(assetName);
        }

        public static void RemoveFromFilter(FavouritesData data, string collectionId, string filterId, string assetName)
        {
            var col = data.Collections.Find(c => c.Id == collectionId);
            col?.Filters.Find(f => f.Id == filterId)?.Assets.Remove(assetName);
        }
    }
}
