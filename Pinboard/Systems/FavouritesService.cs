using Newtonsoft.Json;
using Pinboard.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Pinboard.Systems
{
    public static class FavouritesService
    {
        public static string SavePath => Path.Combine(
            Application.persistentDataPath, "ModsData", "PinboardV2", "favourites.json");

        // Pinboard 2.0 keeps its pins in a folder of its own ("PinboardV2", the name it had
        // while being built). On first launch it copies the 1.x pins across once and never
        // writes to the old file, so that file stays as a backup and going back to 1.x
        // still finds the player's pins.
        private static string LegacySavePath => Path.Combine(
            Application.persistentDataPath, "ModsData", "Pinboard", "favourites.json");

        // Before 1.0.1 the mod was called PinIt. Players who never ran a later version
        // only have this one.
        private static string PinItSavePath => Path.Combine(
            Application.persistentDataPath, "ModsData", "PinIt", "favourites.json");

        public static FavouritesData Load()
        {
            MigrateLegacyDataIfNeeded();
            var data = TryLoad(SavePath) ?? TryLoad(SavePath + ".bak");
            if (data == null) KeepDamagedFiles();
            data = data ?? new FavouritesData();
            // If a default collection had to be made, save it straight away so its
            // hidden ID stays the same next time instead of a new one being invented.
            if (EnsureDefaults(data)) Save(data);
            return data;
        }

        // One-time copy of the 1.x Pinboard save (or the older PinIt one), with its .bak,
        // into the 2.0 folder. Only runs when the new file doesn't exist yet, so it never clobbers
        // newer data and is a no-op for fresh installs and on every subsequent launch.
        private static void MigrateLegacyDataIfNeeded()
        {
            try
            {
                if (File.Exists(SavePath)) return;          // already migrated or native install
                var from = File.Exists(LegacySavePath) ? LegacySavePath
                         : File.Exists(PinItSavePath) ? PinItSavePath
                         : null;
                if (from == null) return;                   // nothing to migrate

                Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
                File.Copy(from, SavePath, overwrite: false);
                if (File.Exists(from + ".bak"))
                    File.Copy(from + ".bak", SavePath + ".bak", overwrite: false);

                Mod.log.Info($"[Pinboard] Copied favourites from the earlier version: {from} -> {SavePath}");
            }
            catch (Exception ex)
            {
                Mod.log.Warn($"[Pinboard] Legacy data migration failed: {ex.Message}");
            }
        }

        // Both the save and its backup are unreadable, so the player is about to start
        // with an empty pinboard and the files would be saved over. Copy them to new
        // names first, so the pins can still be rescued by hand. Only ever runs in that
        // rare case, while a city loads.
        private static void KeepDamagedFiles()
        {
            var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            foreach (var path in new[] { SavePath, SavePath + ".bak" })
            {
                try
                {
                    if (!File.Exists(path)) continue;
                    var copy = Path.Combine(Path.GetDirectoryName(path),
                        Path.GetFileName(path).Replace("favourites", "favourites-damaged-" + stamp));
                    File.Copy(path, copy, overwrite: false);
                    Mod.log.Warn($"[Pinboard] Couldn't read the pins file, so a copy was kept at {copy} and Pinboard started fresh.");
                }
                catch (Exception ex)
                {
                    Mod.log.Warn($"[Pinboard] Couldn't keep a copy of the damaged pins file {path}: {ex.Message}");
                }
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

        // Fills in anything missing. Returns true when something changed that should be saved.
        private static bool EnsureDefaults(FavouritesData data)
        {
            var changed = false;

            // Older save files (pre-1.2.0) have no "recentlyPlaced" key; the property
            // initializer covers a missing key, this guards an explicit null in the JSON.
            if (data.RecentlyPlaced == null) data.RecentlyPlaced = new List<FavouriteEntry>();
            if (data.Collections == null) data.Collections = new List<CollectionData>();
            if (data.MostUsed == null) data.MostUsed = new List<FavouriteEntry>();
            if (data.QuickPins == null) data.QuickPins = new List<FavouriteEntry>();
            while (data.QuickPins.Count < QuickPinSlots) data.QuickPins.Add(null);
            if (data.QuickPins.Count > QuickPinSlots) data.QuickPins.RemoveRange(QuickPinSlots, data.QuickPins.Count - QuickPinSlots);

            if (data.Collections.Count == 0)
            {
                var col = new CollectionData { Name = "New Collection" };
                data.Collections.Add(col);
                data.ActiveCollectionId = col.Id;
                changed = true;
            }
            else if (data.ActiveCollectionId == null ||
                     !data.Collections.Exists(c => c.Id == data.ActiveCollectionId))
            {
                data.ActiveCollectionId = data.Collections[0].Id;
                changed = true;
            }

            return changed;
        }

        // ── Saving ────────────────────────────────────────────────────────────
        //
        // Every save is turned into text on the main thread and given the next number.
        // Writing the file is the slow part, and can happen on a background thread. A
        // file write never replaces the file with an older save than the one already
        // written, so a background save that was still waiting can't undo a newer one.

        private static readonly object s_PendingLock = new object();
        private static readonly object s_FileLock = new object();
        private static long s_LastNumber;        // the number given to the newest save (main thread)
        private static long s_WrittenNumber;     // the newest save written to disk (under s_FileLock)
        private static string s_PendingJson;     // the newest save still waiting for the background thread
        private static string s_PendingPath;
        private static long s_PendingNumber;
        private static bool s_Writing;

        // Saves straight away and waits for the file to be written. Used when the
        // game closes or a city is about to load, so nothing is lost.
        public static void Save(FavouritesData data)
        {
            var json = Serialize(data);
            if (json == null) return;
            var number = ++s_LastNumber;
            // Anything still waiting to be written in the background is older than this.
            lock (s_PendingLock) s_PendingJson = null;
            WriteFile(SavePath, json, number);
        }

        // Saves without making the game wait: the file is written on a background
        // thread while the game carries on. If several saves come in quickly, only
        // the newest one is written.
        public static void SaveInBackground(FavouritesData data)
        {
            var json = Serialize(data);
            if (json == null) return;
            var number = ++s_LastNumber;
            // Unity only lets the main thread ask for the save folder, so look it up here.
            var path = SavePath;
            lock (s_PendingLock)
            {
                s_PendingJson = json;
                s_PendingPath = path;
                s_PendingNumber = number;
                if (s_Writing) return;
                s_Writing = true;
            }
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                while (true)
                {
                    string next, nextPath;
                    long nextNumber;
                    lock (s_PendingLock)
                    {
                        next = s_PendingJson;
                        nextPath = s_PendingPath;
                        nextNumber = s_PendingNumber;
                        s_PendingJson = null;
                        if (next == null) { s_Writing = false; return; }
                    }
                    WriteFile(nextPath, next, nextNumber);
                }
            });
        }

        private static string Serialize(FavouritesData data)
        {
            try
            {
                return JsonConvert.SerializeObject(data, Formatting.Indented);
            }
            catch (Exception ex)
            {
                Mod.log.Error($"[Pinboard] FavouritesService.Save failed: {ex}");
                return null;
            }
        }

        // Writes to a temporary file first, then swaps it in and keeps the previous
        // file as the .bak, so a crash halfway through can never leave a half-written file.
        private static void WriteFile(string path, string json, long number)
        {
            lock (s_FileLock)
            {
                if (number <= s_WrittenNumber) return;   // a newer save is already on disk
                try
                {
                    var tmpPath = path + ".tmp";
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllText(tmpPath, json);
                    if (File.Exists(path))
                        File.Replace(tmpPath, path, path + ".bak");
                    else
                        File.Move(tmpPath, path);
                    s_WrittenNumber = number;
                }
                catch (Exception ex)
                {
                    Mod.log.Error($"[Pinboard] FavouritesService.Save failed: {ex}");
                }
            }
        }

        // ── Finding things ────────────────────────────────────────────────────

        public static CollectionData FindCollection(FavouritesData data, string id)
            => data?.Collections.Find(c => c.Id == id);

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
            var col = FindCollection(data, id);
            if (col != null) col.Name = name;
        }

        // ── Pins within a collection ──────────────────────────────────────────

        public static void PinToCollection(FavouritesData data, string collectionId, FavouriteEntry entry)
        {
            var col = FindCollection(data, collectionId);
            if (col == null) return;
            if (col.Pins.Exists(p => p.Name == entry.Name)) return;
            if (entry.PinnedAt == null) entry.PinnedAt = NowMs();
            col.Pins.Add(entry);
        }

        private static long NowMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Puts a removed pin back where it was (the panel's "Undo"), including the
        // filters it was in. Does nothing if it has been pinned again in the meantime.
        public static void RestorePin(FavouritesData data, string collectionId, FavouriteEntry entry, int index, List<string> filterIds)
        {
            var col = FindCollection(data, collectionId);
            if (col == null || entry == null || string.IsNullOrEmpty(entry.Name)) return;
            if (col.Pins.Exists(p => p.Name == entry.Name)) return;

            if (index < 0 || index > col.Pins.Count) index = col.Pins.Count;
            col.Pins.Insert(index, entry);
            if (filterIds == null) return;
            foreach (var filter in col.Filters)
            {
                if (filterIds.Contains(filter.Id) && !filter.Assets.Contains(entry.Name))
                    filter.Assets.Add(entry.Name);
            }
        }

        // Moves a pin up (delta -1) or down (delta +1) in its collection's own order.
        public static bool MovePin(FavouritesData data, string collectionId, string assetName, int delta)
        {
            var col = FindCollection(data, collectionId);
            return col != null && MoveItem(col.Pins, col.Pins.FindIndex(p => p.Name == assetName), delta);
        }

        public static bool MoveCollection(FavouritesData data, string id, int delta)
            => MoveItem(data.Collections, data.Collections.FindIndex(c => c.Id == id), delta);

        private static bool MoveItem<T>(List<T> list, int from, int delta)
        {
            if (from < 0) return false;
            var to = from + Math.Sign(delta);
            if (to < 0 || to >= list.Count) return false;
            var item = list[from];
            list.RemoveAt(from);
            list.Insert(to, item);
            return true;
        }

        // Copies (or moves) a pin into another collection. Filters belong to one
        // collection, so the pin arrives without any filter ticks.
        public static bool CopyPin(FavouritesData data, string fromId, string toId, string assetName, bool move)
        {
            if (fromId == toId) return false;
            var from = FindCollection(data, fromId);
            var to = FindCollection(data, toId);
            var entry = from?.Pins.Find(p => p.Name == assetName);
            if (entry == null || to == null) return false;

            PinToCollection(data, toId, entry.Copy());
            if (move) UnpinFromCollection(data, fromId, assetName);
            return true;
        }

        public static void UnpinFromCollection(FavouritesData data, string collectionId, string assetName)
        {
            var col = FindCollection(data, collectionId);
            if (col == null) return;
            col.Pins.RemoveAll(p => p.Name == assetName);
            foreach (var filter in col.Filters)
                filter.Assets.Remove(assetName);
        }

        // ── Filters within a collection ───────────────────────────────────────

        public static FilterData CreateFilter(FavouritesData data, string collectionId, string name)
        {
            var col = FindCollection(data, collectionId);
            if (col == null) return null;
            var filter = new FilterData { Name = name };
            col.Filters.Add(filter);
            return filter;
        }

        public static void DeleteFilter(FavouritesData data, string collectionId, string filterId)
        {
            var col = FindCollection(data, collectionId);
            col?.Filters.RemoveAll(f => f.Id == filterId);
        }

        public static void RenameFilter(FavouritesData data, string collectionId, string filterId, string name)
        {
            var col = FindCollection(data, collectionId);
            var filter = col?.Filters.Find(f => f.Id == filterId);
            if (filter != null) filter.Name = name;
        }

        public static void AddToFilter(FavouritesData data, string collectionId, string filterId, string assetName)
        {
            var col = FindCollection(data, collectionId);
            var filter = col?.Filters.Find(f => f.Id == filterId);
            if (filter != null && !filter.Assets.Contains(assetName))
                filter.Assets.Add(assetName);
        }

        public static void RemoveFromFilter(FavouritesData data, string collectionId, string filterId, string assetName)
        {
            var col = FindCollection(data, collectionId);
            col?.Filters.Find(f => f.Id == filterId)?.Assets.Remove(assetName);
        }

        // ── Recently placed (auto-managed, capped) ────────────────────────────

        public const int RecentlyPlacedCap = 100;

        // Records a placement: dedupes by name (so re-placing bumps to the top
        // rather than duplicating) and trims the list to the cap, newest first.
        public static void RecordPlacement(FavouritesData data, FavouriteEntry entry)
        {
            if (data == null || entry == null || string.IsNullOrEmpty(entry.Name)) return;
            data.RecentlyPlaced.RemoveAll(p => p.Name == entry.Name);
            data.RecentlyPlaced.Insert(0, entry);
            if (data.RecentlyPlaced.Count > RecentlyPlacedCap)
                data.RecentlyPlaced.RemoveRange(RecentlyPlacedCap, data.RecentlyPlaced.Count - RecentlyPlacedCap);
        }

        public static void ClearRecentlyPlaced(FavouritesData data)
        {
            data?.RecentlyPlaced.Clear();
        }

        // ── Most used (auto-managed, capped) ──────────────────────────────────

        public const int MostUsedCap = 200;

        // Adds one use. When the list is full, the least-used asset makes room.
        public static void RecordUse(FavouritesData data, FavouriteEntry entry)
        {
            if (data == null || entry == null || string.IsNullOrEmpty(entry.Name)) return;
            var known = data.MostUsed.Find(p => p.Name == entry.Name);
            if (known != null)
            {
                known.Uses = (known.Uses ?? 0) + 1;
                known.DisplayName = entry.DisplayName;
                known.Thumbnail = entry.Thumbnail;
                known.Type = entry.Type;
            }
            else
            {
                var added = entry.Copy();
                added.Uses = 1;
                data.MostUsed.Add(added);
            }

            // Most used first. A stable sort keeps ties in the order they were first used.
            var ranked = data.MostUsed
                .Select((e, i) => new { e, i })
                .OrderByDescending(x => x.e.Uses ?? 0).ThenBy(x => x.i)
                .Select(x => x.e)
                .Take(MostUsedCap)
                .ToList();
            data.MostUsed.Clear();
            data.MostUsed.AddRange(ranked);
        }

        public static void ClearMostUsed(FavouritesData data)
        {
            data?.MostUsed.Clear();
        }

        // ── Quick pins (Alt + 1 to 9) ─────────────────────────────────────────

        public const int QuickPinSlots = 9;

        // Puts a copy of the entry in a slot. If it was already in another slot, it
        // moves, so each asset has one quick key.
        public static void SetQuickPin(FavouritesData data, int slot, FavouriteEntry entry)
        {
            if (data == null || entry == null || string.IsNullOrEmpty(entry.Name)) return;
            if (slot < 0 || slot >= QuickPinSlots) return;
            for (var i = 0; i < data.QuickPins.Count; i++)
                if (data.QuickPins[i]?.Name == entry.Name) data.QuickPins[i] = null;
            data.QuickPins[slot] = entry.Copy();
        }

        public static void ClearQuickPin(FavouritesData data, int slot)
        {
            if (data == null || slot < 0 || slot >= data.QuickPins.Count) return;
            data.QuickPins[slot] = null;
        }

        // Swaps a slot with the one above (-1) or below (+1).
        public static void MoveQuickPin(FavouritesData data, int slot, int delta)
        {
            if (data == null) return;
            var other = slot + delta;
            if (slot < 0 || other < 0 || slot >= data.QuickPins.Count || other >= data.QuickPins.Count) return;
            var held = data.QuickPins[slot];
            data.QuickPins[slot] = data.QuickPins[other];
            data.QuickPins[other] = held;
        }
    }
}
