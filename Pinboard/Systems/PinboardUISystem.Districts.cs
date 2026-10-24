using Colossal.UI.Binding;
using Game.Prefabs;
using Game.UI;
using Newtonsoft.Json;
using Pinboard.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Pinboard.Systems
{
    // "Pin everything in a district": finds every asset placed inside one of the
    // city's districts and pins each kind once into a collection (and optionally a
    // filter). Things that come built into another asset (for example the trees and
    // props that are part of a building) belong to that asset, so they're skipped.
    public partial class PinboardUISystem
    {
        // The kinds of things a player can choose from.
        private const string kCatBuildings = "buildings"; // placed by the player (services, landmarks, ...)
        private const string kCatZoned = "zoned";         // grown by the game in zones
        private const string kCatObjects = "objects";     // props, decals and other objects
        private const string kCatTrees = "trees";         // trees and plants
        private const string kCatNetworks = "networks";   // roads, paths, tracks, fences...
        private static readonly string[] s_DistrictCategories = { kCatBuildings, kCatZoned, kCatObjects, kCatTrees, kCatNetworks };

        private const int kMaxDistrictPins = 2000;

        private ValueBinding<string> m_DistrictsBinding;
        private ValueBinding<string> m_DistrictScanBinding;

        private class DistrictInfo
        {
            [JsonProperty("id")] public string Id { get; set; }
            [JsonProperty("name")] public string Name { get; set; }
        }

        private class DistrictScanPayload
        {
            [JsonProperty("districtId")] public string DistrictId { get; set; }
        }

        private class DistrictPinPayload
        {
            [JsonProperty("districtId")] public string DistrictId { get; set; }
            [JsonProperty("categories")] public List<string> Categories { get; set; }
            [JsonProperty("collectionId")] public string CollectionId { get; set; }
            [JsonProperty("newCollectionName")] public string NewCollectionName { get; set; }
            [JsonProperty("filterId")] public string FilterId { get; set; }
            [JsonProperty("newFilterName")] public string NewFilterName { get; set; }
        }

        private void CreateDistrictBindings()
        {
            m_DistrictsBinding = AddValue("districts", "[]");
            m_DistrictScanBinding = AddValue("districtScan", "");
            AddBinding(new TriggerBinding(kGroup, "listDistricts", PushDistricts));
            AddBinding(new TriggerBinding<string>(kGroup, "scanDistrict", OnScanDistrict));
            AddBinding(new TriggerBinding<string>(kGroup, "pinDistrict", OnPinDistrict));
        }

        // ── Districts in this city ────────────────────────────────────────────

        private static string DistrictKey(Entity e) => e.Index + ":" + e.Version;

        private bool TryParseDistrict(string key, out Entity district)
        {
            district = Entity.Null;
            var parts = (key ?? "").Split(':');
            if (parts.Length != 2 || !int.TryParse(parts[0], out var index) || !int.TryParse(parts[1], out var version)) return false;
            district = new Entity { Index = index, Version = version };
            return EntityManager.Exists(district) && EntityManager.HasComponent<Game.Areas.District>(district);
        }

        private string DistrictName(Entity district, int number)
        {
            try
            {
                // The name the player gave the district (or the game's made-up one).
                // Looked up by name so a game update can't stop the mod from building.
                var names = World.GetOrCreateSystemManaged<NameSystem>();
                var method = names.GetType().GetMethod("GetRenderedLabelName", new[] { typeof(Entity) });
                var name = method?.Invoke(names, new object[] { district }) as string;
                if (!string.IsNullOrWhiteSpace(name)) return name;
            }
            catch (Exception ex)
            {
                Mod.log.Warn($"[Pinboard] Couldn't read a district name: {ex.Message}");
            }
            return "District " + number;
        }

        private void PushDistricts()
        {
            var list = new List<DistrictInfo>();
            try
            {
                var query = GetEntityQuery(
                    ComponentType.ReadOnly<Game.Areas.District>(),
                    ComponentType.ReadOnly<Game.Areas.Node>(),
                    ComponentType.Exclude<Game.Common.Deleted>(),
                    ComponentType.Exclude<Game.Tools.Temp>());
                using (var districts = query.ToEntityArray(Allocator.TempJob))
                {
                    var n = 0;
                    foreach (var d in districts)
                        list.Add(new DistrictInfo { Id = DistrictKey(d), Name = DistrictName(d, ++n) });
                }
            }
            catch (Exception ex)
            {
                Mod.log.Warn($"[Pinboard] Couldn't list districts: {ex.Message}");
            }
            m_DistrictsBinding.Update(JsonConvert.SerializeObject(list.OrderBy(d => d.Name)));
        }

        // ── Finding what's inside ─────────────────────────────────────────────

        // Each kind of thing in the district, with every different asset once.
        private Dictionary<string, Dictionary<Entity, FavouriteEntry>> ScanDistrict(Entity district)
        {
            var found = s_DistrictCategories.ToDictionary(c => c, c => new Dictionary<Entity, FavouriteEntry>());

            // The district's outline, flat on the ground (x and z).
            var nodes = EntityManager.GetBuffer<Game.Areas.Node>(district, true);
            var outline = new float2[nodes.Length];
            for (var i = 0; i < nodes.Length; i++) outline[i] = nodes[i].m_Position.xz;
            if (outline.Length < 3) return found;
            var min = outline[0];
            var max = outline[0];
            foreach (var p in outline) { min = math.min(min, p); max = math.max(max, p); }

            // Buildings, objects and trees placed on their own (not part of another asset).
            var objects = GetEntityQuery(
                ComponentType.ReadOnly<Game.Objects.Transform>(),
                ComponentType.ReadOnly<PrefabRef>(),
                ComponentType.ReadOnly<Game.Objects.Static>(),
                ComponentType.Exclude<Game.Common.Owner>(),
                ComponentType.Exclude<Game.Common.Deleted>(),
                ComponentType.Exclude<Game.Tools.Temp>());
            using (var entities = objects.ToEntityArray(Allocator.TempJob))
            using (var transforms = objects.ToComponentDataArray<Game.Objects.Transform>(Allocator.TempJob))
            using (var prefabs = objects.ToComponentDataArray<PrefabRef>(Allocator.TempJob))
            {
                for (var i = 0; i < entities.Length; i++)
                {
                    if (!IsInside(outline, min, max, transforms[i].m_Position)) continue;
                    var e = entities[i];
                    var prefab = prefabs[i].m_Prefab;
                    string category;
                    if (EntityManager.HasComponent<Game.Buildings.Building>(e))
                        category = EntityManager.HasComponent<SpawnableBuildingData>(prefab) ? kCatZoned : kCatBuildings;
                    else if (EntityManager.HasComponent<Game.Objects.Tree>(e))
                        category = kCatTrees;
                    else
                        category = kCatObjects;
                    AddFound(found[category], prefab);
                }
            }

            // Roads, paths, tracks and the like: a piece counts when its middle is inside.
            var edges = GetEntityQuery(
                ComponentType.ReadOnly<Game.Net.Edge>(),
                ComponentType.ReadOnly<PrefabRef>(),
                ComponentType.Exclude<Game.Common.Owner>(),
                ComponentType.Exclude<Game.Common.Deleted>(),
                ComponentType.Exclude<Game.Tools.Temp>());
            using (var entities = edges.ToEntityArray(Allocator.TempJob))
            using (var edgeData = edges.ToComponentDataArray<Game.Net.Edge>(Allocator.TempJob))
            using (var prefabs = edges.ToComponentDataArray<PrefabRef>(Allocator.TempJob))
            {
                for (var i = 0; i < entities.Length; i++)
                {
                    var start = edgeData[i].m_Start;
                    var end = edgeData[i].m_End;
                    if (!EntityManager.HasComponent<Game.Net.Node>(start) || !EntityManager.HasComponent<Game.Net.Node>(end)) continue;
                    var middle = (EntityManager.GetComponentData<Game.Net.Node>(start).m_Position
                                + EntityManager.GetComponentData<Game.Net.Node>(end).m_Position) * 0.5f;
                    if (IsInside(outline, min, max, middle)) AddFound(found[kCatNetworks], prefabs[i].m_Prefab);
                }
            }
            return found;
        }

        // Is a point inside the district's outline? (The usual "count the edges you
        // cross" test, with a quick box check first.)
        private static bool IsInside(float2[] outline, float2 min, float2 max, float3 pos)
        {
            var p = pos.xz;
            if (p.x < min.x || p.y < min.y || p.x > max.x || p.y > max.y) return false;
            var inside = false;
            for (int i = 0, j = outline.Length - 1; i < outline.Length; j = i++)
            {
                var a = outline[i];
                var b = outline[j];
                if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }
            return inside;
        }

        private void AddFound(Dictionary<Entity, FavouriteEntry> into, Entity prefabEntity)
        {
            if (into.ContainsKey(prefabEntity) || !m_PrefabSystem.TryGetPrefab(prefabEntity, out PrefabBase prefab) || prefab == null) return;
            into[prefabEntity] = EntryFromPrefab(prefab);
        }

        private void OnScanDistrict(string payload)
        {
            var p = Parse<DistrictScanPayload>(payload);
            if (p == null || !TryParseDistrict(p.DistrictId, out var district))
            {
                m_DistrictScanBinding.Update("");
                return;
            }
            try
            {
                var found = ScanDistrict(district);
                m_DistrictScanBinding.Update(JsonConvert.SerializeObject(new
                {
                    districtId = p.DistrictId,
                    counts = found.ToDictionary(kv => kv.Key, kv => kv.Value.Count),
                }));
            }
            catch (Exception ex)
            {
                Mod.log.Warn($"[Pinboard] Couldn't look inside a district: {ex}");
                m_DistrictScanBinding.Update("");
                Notify("districtFailed");
            }
        }

        // ── Pinning it all ────────────────────────────────────────────────────

        private void OnPinDistrict(string payload)
        {
            if (m_Favourites == null) return;
            var p = Parse<DistrictPinPayload>(payload);
            if (p?.Categories == null || !TryParseDistrict(p.DistrictId, out var district)) return;

            Dictionary<string, Dictionary<Entity, FavouriteEntry>> found;
            try { found = ScanDistrict(district); }
            catch (Exception ex)
            {
                Mod.log.Warn($"[Pinboard] Couldn't look inside a district: {ex}");
                Notify("districtFailed");
                return;
            }

            var entries = p.Categories.Where(found.ContainsKey)
                .SelectMany(c => found[c].Values)
                .GroupBy(e => e.Name).Select(g => g.First())
                .OrderBy(e => e.DisplayName)
                .Take(kMaxDistrictPins)
                .ToList();
            if (entries.Count == 0)
            {
                Notify("districtNothing");
                return;
            }

            // Where to put them: an existing collection, or a new one.
            var col = FavouritesService.FindCollection(m_Favourites, p.CollectionId);
            if (col == null)
            {
                var name = CleanName(p.NewCollectionName, kMaxCollectionNameLength);
                if (name == null) return;
                col = FavouritesService.CreateCollection(m_Favourites, name);
            }

            // And optionally into a filter: an existing one, or a new one.
            FilterData filter = col.Filters.Find(f => f.Id == p.FilterId);
            if (filter == null)
            {
                var filterName = CleanName(p.NewFilterName, kMaxFilterNameLength);
                if (filterName != null) filter = FavouritesService.CreateFilter(m_Favourites, col.Id, filterName);
            }

            var added = 0;
            foreach (var entry in entries)
            {
                if (!col.Pins.Exists(e => e.Name == entry.Name))
                {
                    FavouritesService.PinToCollection(m_Favourites, col.Id, entry);
                    added++;
                }
                if (filter != null) FavouritesService.AddToFilter(m_Favourites, col.Id, filter.Id, entry.Name);
            }

            PinsChanged();
            Notify("districtPinned", added.ToString(), col.Name, (entries.Count - added).ToString());
        }
    }
}
