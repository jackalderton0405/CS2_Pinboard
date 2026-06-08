using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game;
using Game.Input;
using Game.Prefabs;
using Game.Rendering;
using Game.SceneFlow;
using Game.Tools;
using Game.UI;
using Newtonsoft.Json;
using PinIt.Data;
using System.Linq;
using Unity.Collections;
using Unity.Entities;

namespace PinIt.Systems
{
    public partial class PinItUISystem : UISystemBase
    {
        public static PinItUISystem Instance { get; private set; }

        private ToolSystem m_ToolSystem;
        private PrefabSystem m_PrefabSystem;
        private ProxyAction m_TogglePanelAction;

        private FavouritesData m_Favourites;
        private bool m_Searched;
        private bool m_PanelOpen;

        private string m_CurrentPrefabName = "";
        private string m_CurrentPrefabType = "";
        private string m_CurrentPrefabThumbnail = "";
        private string m_CurrentPrefabDisplayName = "";

        private ValueBinding<bool> m_PanelOpenBinding;
        private ValueBinding<string> m_CollectionsDataBinding;
        private ValueBinding<string> m_ActiveCollectionIdBinding;
        private ValueBinding<string> m_CurrentPrefabBinding;
        private ValueBinding<string> m_CurrentPrefabIdBinding;
        private ValueBinding<bool> m_IsPinnedBinding;

        private class RemoveAssetPayload
        {
            [JsonProperty("collectionId")] public string CollectionId { get; set; }
            [JsonProperty("assetName")] public string AssetName { get; set; }
        }

        private class RenamePayload
        {
            [JsonProperty("id")] public string Id { get; set; }
            [JsonProperty("name")] public string Name { get; set; }
        }

        private class FilterActionPayload
        {
            [JsonProperty("collectionId")] public string CollectionId { get; set; }
            [JsonProperty("filterId")] public string FilterId { get; set; }
            [JsonProperty("name")] public string Name { get; set; }
        }

        private class FilterMemberPayload
        {
            [JsonProperty("collectionId")] public string CollectionId { get; set; }
            [JsonProperty("filterId")] public string FilterId { get; set; }
            [JsonProperty("assetName")] public string AssetName { get; set; }
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
            Mod.log.Info("[PinIt] PinItUISystem created");

            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();

            m_ToolSystem.EventPrefabChanged += OnPrefabChanged;

            m_TogglePanelAction = Mod.Setting?.GetAction(Setting.kTogglePanelActionName);
            if (m_TogglePanelAction != null) m_TogglePanelAction.shouldBeEnabled = true;

            AddBinding(new TriggerBinding("pinIt", "togglePanel", OnTogglePanel));
            AddBinding(m_PanelOpenBinding = new ValueBinding<bool>("pinIt", "panelOpen", false));
            AddBinding(m_CollectionsDataBinding = new ValueBinding<string>("pinIt", "collectionsData", "[]"));
            AddBinding(m_ActiveCollectionIdBinding = new ValueBinding<string>("pinIt", "activeCollectionId", ""));
            AddBinding(m_CurrentPrefabBinding = new ValueBinding<string>("pinIt", "currentPrefabName", ""));
            AddBinding(m_CurrentPrefabIdBinding = new ValueBinding<string>("pinIt", "currentPrefabId", ""));
            AddBinding(m_IsPinnedBinding = new ValueBinding<bool>("pinIt", "isPinned", false));

            AddBinding(new TriggerBinding<string>("pinIt", "selectAsset", OnSelectAsset));
            AddBinding(new TriggerBinding("pinIt", "pinCurrentAsset", OnPinCurrentAsset));
            AddBinding(new TriggerBinding("pinIt", "togglePin", OnTogglePin));
            AddBinding(new TriggerBinding<string>("pinIt", "removeAsset", OnRemoveAsset));
            AddBinding(new TriggerBinding<string>("pinIt", "setActiveCollection", OnSetActiveCollection));
            AddBinding(new TriggerBinding<string>("pinIt", "createCollection", OnCreateCollection));
            AddBinding(new TriggerBinding<string>("pinIt", "deleteCollection", OnDeleteCollection));
            AddBinding(new TriggerBinding<string>("pinIt", "renameCollection", OnRenameCollection));
            AddBinding(new TriggerBinding<string>("pinIt", "createFilter", OnCreateFilter));
            AddBinding(new TriggerBinding<string>("pinIt", "deleteFilter", OnDeleteFilter));
            AddBinding(new TriggerBinding<string>("pinIt", "renameFilter", OnRenameFilter));
            AddBinding(new TriggerBinding<string>("pinIt", "addToFilter", OnAddToFilter));
            AddBinding(new TriggerBinding<string>("pinIt", "removeFromFilter", OnRemoveFromFilter));
        }

        // ── Collections ───────────────────────────────────────────────────────

        private void OnSetActiveCollection(string id)
        {
            if (m_Favourites == null) return;
            m_Favourites.ActiveCollectionId = id;
            FavouritesService.Save(m_Favourites);
            m_ActiveCollectionIdBinding.Update(id ?? "");
            UpdateIsPinned();
            Mod.log.Info($"[PinIt] Active collection: {id}");
        }

        private void OnCreateCollection(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || m_Favourites == null) return;
            FavouritesService.CreateCollection(m_Favourites, name.Trim());
            FavouritesService.Save(m_Favourites);
            PushCollectionsData();
            Mod.log.Info($"[PinIt] Collection created: {name}");
        }

        private void OnDeleteCollection(string id)
        {
            if (m_Favourites == null) return;
            FavouritesService.DeleteCollection(m_Favourites, id);
            FavouritesService.Save(m_Favourites);
            PushAll();
            Mod.log.Info($"[PinIt] Collection deleted: {id}");
        }

        private void OnRenameCollection(string payload)
        {
            if (m_Favourites == null) return;
            var p = JsonConvert.DeserializeObject<RenamePayload>(payload);
            if (p == null || string.IsNullOrWhiteSpace(p.Name)) return;
            FavouritesService.RenameCollection(m_Favourites, p.Id, p.Name.Trim());
            FavouritesService.Save(m_Favourites);
            PushCollectionsData();
        }

        // ── Filters ───────────────────────────────────────────────────────────

        private void OnCreateFilter(string payload)
        {
            if (m_Favourites == null) return;
            var p = JsonConvert.DeserializeObject<FilterActionPayload>(payload);
            if (p == null) return;
            FavouritesService.CreateFilter(m_Favourites, p.CollectionId, (p.Name ?? "New Filter").Trim());
            FavouritesService.Save(m_Favourites);
            PushCollectionsData();
            Mod.log.Info($"[PinIt] Filter created in {p.CollectionId}: {p.Name}");
        }

        private void OnDeleteFilter(string payload)
        {
            if (m_Favourites == null) return;
            var p = JsonConvert.DeserializeObject<FilterActionPayload>(payload);
            if (p == null) return;
            FavouritesService.DeleteFilter(m_Favourites, p.CollectionId, p.FilterId);
            FavouritesService.Save(m_Favourites);
            PushCollectionsData();
        }

        private void OnRenameFilter(string payload)
        {
            if (m_Favourites == null) return;
            var p = JsonConvert.DeserializeObject<FilterActionPayload>(payload);
            if (p == null || string.IsNullOrWhiteSpace(p.Name)) return;
            FavouritesService.RenameFilter(m_Favourites, p.CollectionId, p.FilterId, p.Name.Trim());
            FavouritesService.Save(m_Favourites);
            PushCollectionsData();
        }

        private void OnAddToFilter(string payload)
        {
            if (m_Favourites == null) return;
            var p = JsonConvert.DeserializeObject<FilterMemberPayload>(payload);
            if (p == null) return;
            FavouritesService.AddToFilter(m_Favourites, p.CollectionId, p.FilterId, p.AssetName);
            FavouritesService.Save(m_Favourites);
            PushCollectionsData();
        }

        private void OnRemoveFromFilter(string payload)
        {
            if (m_Favourites == null) return;
            var p = JsonConvert.DeserializeObject<FilterMemberPayload>(payload);
            if (p == null) return;
            FavouritesService.RemoveFromFilter(m_Favourites, p.CollectionId, p.FilterId, p.AssetName);
            FavouritesService.Save(m_Favourites);
            PushCollectionsData();
        }

        // ── Pins ──────────────────────────────────────────────────────────────

        private void OnPinCurrentAsset()
        {
            if (string.IsNullOrEmpty(m_CurrentPrefabName) || m_Favourites == null) return;
            var active = FavouritesService.GetActiveCollection(m_Favourites);
            if (active == null)
            {
                Mod.log.Info("[PinIt] No active collection to pin into");
                return;
            }
            if (active.Pins.Any(p => p.Name == m_CurrentPrefabName)) return;

            FavouritesService.PinToCollection(m_Favourites, active.Id, new FavouriteEntry
            {
                Name = m_CurrentPrefabName,
                Type = m_CurrentPrefabType,
                Thumbnail = m_CurrentPrefabThumbnail,
                DisplayName = m_CurrentPrefabDisplayName,
            });

            FavouritesService.Save(m_Favourites);
            PushCollectionsData();
            m_IsPinnedBinding.Update(true);
            Mod.log.Info($"[PinIt] Pinned: {m_CurrentPrefabName} into '{active.Name}'");
        }

        private void OnTogglePin()
        {
            if (string.IsNullOrEmpty(m_CurrentPrefabName) || m_Favourites == null) return;
            var active = FavouritesService.GetActiveCollection(m_Favourites);
            if (active == null) return;

            if (active.Pins.Any(p => p.Name == m_CurrentPrefabName))
            {
                FavouritesService.UnpinFromCollection(m_Favourites, active.Id, m_CurrentPrefabName);
                Mod.log.Info($"[PinIt] Unpinned: {m_CurrentPrefabName} from '{active.Name}'");
            }
            else
            {
                FavouritesService.PinToCollection(m_Favourites, active.Id, new FavouriteEntry
                {
                    Name = m_CurrentPrefabName,
                    Type = m_CurrentPrefabType,
                    Thumbnail = m_CurrentPrefabThumbnail,
                    DisplayName = m_CurrentPrefabDisplayName,
                });
                Mod.log.Info($"[PinIt] Pinned: {m_CurrentPrefabName} into '{active.Name}'");
            }

            FavouritesService.Save(m_Favourites);
            PushCollectionsData();
            UpdateIsPinned();
        }

        private void OnRemoveAsset(string payload)
        {
            if (m_Favourites == null) return;
            var p = JsonConvert.DeserializeObject<RemoveAssetPayload>(payload);
            if (p == null) return;
            FavouritesService.UnpinFromCollection(m_Favourites, p.CollectionId, p.AssetName);
            FavouritesService.Save(m_Favourites);
            PushCollectionsData();
            UpdateIsPinned();
            Mod.log.Info($"[PinIt] Removed: {p.AssetName} from {p.CollectionId}");
        }

        // ── Prefab tracking ───────────────────────────────────────────────────

        private string GetDisplayName(PrefabBase prefab)
        {
            var dict = GameManager.instance.localizationManager.activeDictionary;
            if (dict.TryGetValue("Assets.NAME[" + prefab.name + "]", out var name) && !string.IsNullOrEmpty(name))
                return name;
            return prefab.name.Replace('_', ' ');
        }

        private void OnPrefabChanged(PrefabBase prefab)
        {
            if (prefab == null)
            {
                m_CurrentPrefabName = "";
                m_CurrentPrefabType = "";
                m_CurrentPrefabThumbnail = "";
                m_CurrentPrefabDisplayName = "";
            }
            else
            {
                m_CurrentPrefabName = prefab.name;
                m_CurrentPrefabType = prefab.GetType().Name;
                m_CurrentPrefabThumbnail = ImageSystem.GetThumbnail(prefab) ?? "";
                m_CurrentPrefabDisplayName = GetDisplayName(prefab);
                Mod.log.Info($"[PinIt] Prefab selected: {m_CurrentPrefabName} [{m_CurrentPrefabType}] -> \"{m_CurrentPrefabDisplayName}\"");
            }

            m_CurrentPrefabBinding.Update(m_CurrentPrefabDisplayName);
            m_CurrentPrefabIdBinding.Update(m_CurrentPrefabName);
            UpdateIsPinned();
        }

        private void UpdateIsPinned()
        {
            if (m_Favourites == null || string.IsNullOrEmpty(m_CurrentPrefabName))
            {
                m_IsPinnedBinding.Update(false);
                return;
            }
            var active = FavouritesService.GetActiveCollection(m_Favourites);
            m_IsPinnedBinding.Update(active != null && active.Pins.Any(p => p.Name == m_CurrentPrefabName));
        }

        // ── Asset selection ───────────────────────────────────────────────────

        private void OnTogglePanel()
        {
            m_PanelOpen = !m_PanelOpen;
            m_PanelOpenBinding.Update(m_PanelOpen);
            Mod.log.Info($"[PinIt] PANEL {(m_PanelOpen ? "OPENED" : "CLOSED")}");
        }

        private static readonly string[] s_CommonPrefabTypes =
        {
            "StaticObjectPrefab", "BuildingPrefab", "VegetationPrefab",
            "PropPrefab", "ObjectGeometryPrefab", "MarkerObjectPrefab",
        };

        private void OnSelectAsset(string name)
        {
            FavouriteEntry entry = null;
            if (m_Favourites != null)
            {
                foreach (var col in m_Favourites.Collections)
                {
                    entry = col.Pins.FirstOrDefault(p => p.Name == name);
                    if (entry != null) break;
                }
            }

            // 1. Fast path — the type we stored when the asset was pinned.
            if (entry != null && !string.IsNullOrEmpty(entry.Type) &&
                m_PrefabSystem.TryGetPrefab(new PrefabID(entry.Type, name), out PrefabBase byStored))
            {
                m_ToolSystem.ActivatePrefabTool(byStored);
                Mod.log.Info($"[PinIt] Selected: {name} ({entry.Type})");
                return;
            }

            // 2. Guess from the common placeable prefab types.
            foreach (var typeName in s_CommonPrefabTypes)
            {
                if (m_PrefabSystem.TryGetPrefab(new PrefabID(typeName, name), out PrefabBase byGuess))
                {
                    HealStoredType(entry, typeName);
                    m_ToolSystem.ActivatePrefabTool(byGuess);
                    Mod.log.Info($"[PinIt] Selected (type fallback): {name} ({typeName})");
                    return;
                }
            }

            // 3. Definitive — scan every registered prefab and match by name,
            //    so assets of any type (sculptures, custom ploppables, etc.) resolve.
            var found = FindPrefabByName(name);
            if (found != null)
            {
                HealStoredType(entry, found.GetType().Name);
                m_ToolSystem.ActivatePrefabTool(found);
                Mod.log.Info($"[PinIt] Selected (name scan): {name} ({found.GetType().Name})");
                return;
            }

            Mod.log.Info($"[PinIt] Asset not found: {name}");
        }

        // Scans all prefab entities and returns the one whose name matches.
        private PrefabBase FindPrefabByName(string name)
        {
            var query = GetEntityQuery(ComponentType.ReadOnly<PrefabData>());
            using var entities = query.ToEntityArray(Allocator.Temp);
            foreach (var e in entities)
            {
                if (m_PrefabSystem.TryGetPrefab(e, out PrefabBase prefab) && prefab.name == name)
                    return prefab;
            }
            return null;
        }

        // Persists the resolved type back onto the pin so future clicks take the fast path.
        private void HealStoredType(FavouriteEntry entry, string type)
        {
            if (entry == null || entry.Type == type || string.IsNullOrEmpty(type)) return;
            entry.Type = type;
            FavouritesService.Save(m_Favourites);
        }

        // ── Push helpers ──────────────────────────────────────────────────────

        private void PushCollectionsData()
        {
            m_CollectionsDataBinding.Update(JsonConvert.SerializeObject(m_Favourites.Collections));
        }

        private void PushAll()
        {
            PushCollectionsData();
            m_ActiveCollectionIdBinding.Update(m_Favourites.ActiveCollectionId ?? "");
            UpdateIsPinned();
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            if (mode != GameMode.Game && mode != GameMode.Editor) return;
            LoadFavourites();
        }

        private void LoadFavourites()
        {
            m_Favourites = FavouritesService.Load();
            PushAll();
            Mod.log.Info($"[PinIt] Loaded — {m_Favourites.Collections.Count} collections");
        }

        // Called by the settings page after it edits favourites.json directly,
        // so the live in-game panel reflects the change immediately.
        public void ReloadFromDisk()
        {
            m_Favourites = FavouritesService.Load();
            PushAll();
            Mod.log.Info("[PinIt] Reloaded favourites from disk (settings edit)");
        }

        protected override void OnUpdate()
        {
            if (!m_Searched && m_Favourites == null &&
                (GameManager.instance?.gameMode == GameMode.Game ||
                 GameManager.instance?.gameMode == GameMode.Editor))
            {
                m_Searched = true;
                LoadFavourites();
            }

            if (m_TogglePanelAction != null && m_TogglePanelAction.WasPerformedThisFrame())
                OnTogglePanel();
        }

        protected override void OnDestroy()
        {
            m_ToolSystem.EventPrefabChanged -= OnPrefabChanged;
            if (m_TogglePanelAction != null) m_TogglePanelAction.shouldBeEnabled = false;
            if (m_Favourites != null) FavouritesService.Save(m_Favourites);
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }
    }
}
