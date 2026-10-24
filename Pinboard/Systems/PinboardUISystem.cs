using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game;
using Game.Input;
using Game.Prefabs;
using Game.Tools;
using Game.UI;
using Newtonsoft.Json;
using Pinboard.Data;
using System;
using System.Collections.Generic;

namespace Pinboard.Systems
{
    // The game side of Pinboard. It holds the player's pins, talks to the panel
    // through bindings, and watches what the player picks up and places.
    //
    // The class is split over several files, one per job:
    //   PinboardUISystem.cs              this file: state, bindings, loading and saving
    //   PinboardUISystem.Messages.cs     the shapes of the messages the panel sends
    //   PinboardUISystem.Assets.cs       finding assets in the game, picking them up
    //   PinboardUISystem.Tracking.cs     the held asset, Recently Placed, Most Used
    //   PinboardUISystem.Collections.cs  collections and filters
    //   PinboardUISystem.Pins.cs         pinning, quick pins, the info panel section
    //   PinboardUISystem.Sharing.cs      share codes
    //   PinboardUISystem.Layout.cs       the panel's position, view and sort order
    //   PinboardUISystem.Districts.cs    "pin everything in a district"
    public partial class PinboardUISystem : UISystemBase
    {
        public static PinboardUISystem Instance { get; private set; }

        // Every binding lives in this group. It must match the panel code (UI/src/mods/game.ts),
        // and is different from Pinboard 1.x ("pinboard") so the two can never mix.
        private const string kGroup = "pinboard2";

        // Name length limits, enforced here as well as in the UI so the save file
        // stays sane whatever sends us a message.
        private const int kMaxCollectionNameLength = 30;
        private const int kMaxFilterNameLength = 20;

        private ToolSystem m_ToolSystem;
        private PrefabSystem m_PrefabSystem;
        private ProxyAction m_TogglePanelAction;
        private ProxyAction m_PinCurrentAction;
        private readonly List<ProxyAction> m_QuickPickActions = new List<ProxyAction>();

        // The player's pins, collections and lists. Null until a city or the editor has loaded.
        private FavouritesData m_Favourites;
        private bool m_PanelOpen;

        private ValueBinding<bool> m_PanelOpenBinding;
        private ValueBinding<string> m_CollectionsDataBinding;
        private ValueBinding<string> m_ActiveCollectionIdBinding;
        private ValueBinding<string> m_CurrentPrefabBinding;
        private ValueBinding<string> m_CurrentPrefabIdBinding;
        private ValueBinding<bool> m_IsPinnedBinding;
        private ValueBinding<string> m_RecentlyPlacedBinding;
        private ValueBinding<string> m_MostUsedBinding;
        private ValueBinding<string> m_QuickPinsBinding;
        private ValueBinding<string> m_MissingAssetBinding;
        private ValueBinding<string> m_MissingAssetSourceBinding;
        private ValueBinding<string> m_InspectedBinding;
        private ValueBinding<string> m_PanelLayoutBinding;
        private ValueBinding<string> m_NoticeBinding;
        private ValueBinding<string> m_ShareCodeBinding;

        // Each notice gets a new number so the panel can tell two identical ones apart.
        private int m_NoticeCounter;

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;

            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();

            m_ToolSystem.EventPrefabChanged += OnPrefabChanged;
            ToolBaseSystem.EventToolActionPerformed += OnToolActionPerformed;

            m_TogglePanelAction = EnableAction(Setting.kTogglePanelActionName);
            m_PinCurrentAction = EnableAction(Setting.kPinCurrentActionName);
            foreach (var name in Setting.kQuickPickActions)
                m_QuickPickActions.Add(EnableAction(name));

            // What the panel shows.
            m_PanelOpenBinding = AddValue("panelOpen", false);
            m_CollectionsDataBinding = AddValue("collectionsData", "[]");
            m_ActiveCollectionIdBinding = AddValue("activeCollectionId", "");
            m_CurrentPrefabBinding = AddValue("currentPrefabName", "");
            m_CurrentPrefabIdBinding = AddValue("currentPrefabId", "");
            m_IsPinnedBinding = AddValue("isPinned", false);
            m_RecentlyPlacedBinding = AddValue("recentlyPlacedData", "[]");
            m_MostUsedBinding = AddValue("mostUsedData", "[]");
            m_QuickPinsBinding = AddValue("quickPinsData", "[]");
            m_MissingAssetBinding = AddValue("missingAsset", "");
            m_MissingAssetSourceBinding = AddValue("missingAssetSource", "");
            m_InspectedBinding = AddValue("inspected", "");
            m_PanelLayoutBinding = AddValue("panelLayout", PanelLayoutJson());
            m_NoticeBinding = AddValue("notice", "");
            m_ShareCodeBinding = AddValue("shareCode", "");

            // What the panel asks for. The handlers are in the file for each job.
            AddBinding(new TriggerBinding(kGroup, "togglePanel", OnTogglePanel));
            AddBinding(new TriggerBinding<string>(kGroup, "selectAsset", OnSelectAsset));
            AddBinding(new TriggerBinding(kGroup, "dismissMissingAsset", () => m_MissingAssetBinding.Update("")));

            AddBinding(new TriggerBinding<string>(kGroup, "pinCurrentAsset", OnPinCurrentAsset));
            AddBinding(new TriggerBinding(kGroup, "togglePin", OnTogglePin));
            AddBinding(new TriggerBinding<string>(kGroup, "pinFromRecent", OnPinInto));
            AddBinding(new TriggerBinding<string>(kGroup, "removeAsset", OnRemoveAsset));
            AddBinding(new TriggerBinding<string>(kGroup, "restorePin", OnRestorePin));
            AddBinding(new TriggerBinding<string>(kGroup, "movePin", OnMovePin));
            AddBinding(new TriggerBinding<string>(kGroup, "copyPin", OnCopyPin));

            AddBinding(new TriggerBinding<string>(kGroup, "setActiveCollection", OnSetActiveCollection));
            AddBinding(new TriggerBinding<string>(kGroup, "createCollection", OnCreateCollection));
            AddBinding(new TriggerBinding<string>(kGroup, "deleteCollection", OnDeleteCollection));
            AddBinding(new TriggerBinding<string>(kGroup, "renameCollection", OnRenameCollection));
            AddBinding(new TriggerBinding<string>(kGroup, "moveCollection", OnMoveCollection));

            AddBinding(new TriggerBinding<string>(kGroup, "createFilter", OnCreateFilter));
            AddBinding(new TriggerBinding<string>(kGroup, "deleteFilter", OnDeleteFilter));
            AddBinding(new TriggerBinding<string>(kGroup, "renameFilter", OnRenameFilter));
            AddBinding(new TriggerBinding<string>(kGroup, "addToFilter", OnAddToFilter));
            AddBinding(new TriggerBinding<string>(kGroup, "removeFromFilter", OnRemoveFromFilter));

            AddBinding(new TriggerBinding(kGroup, "clearRecent", OnClearRecent));
            AddBinding(new TriggerBinding(kGroup, "clearMostUsed", OnClearMostUsed));

            AddBinding(new TriggerBinding<string>(kGroup, "setQuickPin", OnSetQuickPin));
            AddBinding(new TriggerBinding<int>(kGroup, "clearQuickPin", OnClearQuickPin));
            AddBinding(new TriggerBinding<string>(kGroup, "moveQuickPin", OnMoveQuickPin));

            AddBinding(new TriggerBinding<string>(kGroup, "exportCollection", OnExportCollection));
            AddBinding(new TriggerBinding<string>(kGroup, "importCollection", OnImportCollection));
            AddBinding(new TriggerBinding(kGroup, "dismissShareCode", () => m_ShareCodeBinding.Update("")));

            AddBinding(new TriggerBinding<string>(kGroup, "savePanelPosition", OnSavePanelPosition));
            AddBinding(new TriggerBinding<bool>(kGroup, "setGridView", OnSetGridView));
            AddBinding(new TriggerBinding<string>(kGroup, "setSortMode", OnSetSortMode));

            CreateDistrictBindings();
        }

        private ValueBinding<T> AddValue<T>(string name, T initial)
        {
            var binding = new ValueBinding<T>(kGroup, name, initial);
            AddBinding(binding);
            return binding;
        }

        private static ProxyAction EnableAction(string name)
        {
            var action = Mod.Setting?.GetAction(name);
            if (action != null) action.shouldBeEnabled = true;
            return action;
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        // The one place pins are loaded: every time a city or the editor finishes loading.
        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            if (mode != GameMode.Game && mode != GameMode.Editor) return;

            // Keep any changes from the city we're leaving before reloading.
            SaveNow();
            m_PrefabsByName = null;
            AssetSources.ClearCache();
            m_Favourites = FavouritesService.Load();
            RefreshStoredAssetInfo();
            PushAll();
            Mod.log.Info($"[Pinboard] Loaded {m_Favourites.Collections.Count} collections");
        }

        protected override void OnUpdate()
        {
            if (WasPressed(m_TogglePanelAction)) OnTogglePanel();
            if (WasPressed(m_PinCurrentAction)) OnPinCurrentShortcut();
            for (var i = 0; i < m_QuickPickActions.Count; i++)
                if (WasPressed(m_QuickPickActions[i])) OnQuickPick(i);

            SaveUsesIfDue(false);
            SaveIfDue();
            PollBrushOrZone();
        }

        private static bool WasPressed(ProxyAction action) => action != null && action.WasPerformedThisFrame();

        protected override void OnDestroy()
        {
            m_ToolSystem.EventPrefabChanged -= OnPrefabChanged;
            ToolBaseSystem.EventToolActionPerformed -= OnToolActionPerformed;
            if (m_TogglePanelAction != null) m_TogglePanelAction.shouldBeEnabled = false;
            if (m_PinCurrentAction != null) m_PinCurrentAction.shouldBeEnabled = false;
            foreach (var action in m_QuickPickActions)
                if (action != null) action.shouldBeEnabled = false;
            SaveNow();
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        // ── Saving ────────────────────────────────────────────────────────────

        // Most Used counts change on every placement. They are saved at most every
        // few seconds (and when the game closes), not on every click.
        private const float kUsesSaveDelay = 10f;
        private bool m_UsesDirty;
        private float m_UsesDirtySince;

        // Other changes (pinning, renaming...) are saved a moment later, in the
        // background, so the game never pauses while the file is written. Several
        // changes in a row become one save.
        private const float kSaveDelay = 1f;
        private bool m_SaveDirty;
        private float m_SaveDueAt;

        private static float Now => UnityEngine.Time.realtimeSinceStartup;

        private void RequestSave()
        {
            if (m_SaveDirty) return;
            m_SaveDirty = true;
            m_SaveDueAt = Now + kSaveDelay;
        }

        private void MarkUsesChanged()
        {
            if (m_UsesDirty) return;
            m_UsesDirty = true;
            m_UsesDirtySince = Now;
        }

        private void SaveIfDue()
        {
            if (!m_SaveDirty || m_Favourites == null || Now < m_SaveDueAt) return;
            m_SaveDirty = false;
            FavouritesService.SaveInBackground(m_Favourites);
        }

        private void SaveUsesIfDue(bool force)
        {
            if (!m_UsesDirty || m_Favourites == null) return;
            if (!force && Now - m_UsesDirtySince < kUsesSaveDelay) return;
            m_UsesDirty = false;
            m_SaveDirty = false;
            FavouritesService.SaveInBackground(m_Favourites);
            PushMostUsed();
        }

        // Writes everything now and waits for it. Before a city loads and when the game closes.
        private void SaveNow()
        {
            m_SaveDirty = false;
            m_UsesDirty = false;
            if (m_Favourites != null) FavouritesService.Save(m_Favourites);
        }

        // Most changes to the pins end the same way: save soon, show the new lists,
        // and refresh the tool-options pin button.
        private void PinsChanged()
        {
            RequestSave();
            PushCollectionsData();
            UpdateIsPinned();
        }

        // ── Talking to the panel ──────────────────────────────────────────────

        // Short messages for the player, shown for a few seconds at the bottom of the
        // panel (or on their own when it's closed). The panel has the wording for each
        // key, so it can be translated; "args" fill in names and numbers.
        private void Notify(string key, params string[] args)
        {
            m_NoticeCounter++;
            m_NoticeBinding.Update(JsonConvert.SerializeObject(new { id = m_NoticeCounter, key, args }));
        }

        // Reads a JSON message from the UI. A garbled message is logged and ignored
        // instead of throwing inside the game's UI update.
        private static T Parse<T>(string payload) where T : class
        {
            if (string.IsNullOrEmpty(payload)) return null;
            try
            {
                return JsonConvert.DeserializeObject<T>(payload);
            }
            catch (Exception ex)
            {
                Mod.log.Warn($"[Pinboard] Ignored unreadable message ({typeof(T).Name}): {ex.Message}");
                return null;
            }
        }

        private void PushCollectionsData() => m_CollectionsDataBinding.Update(JsonConvert.SerializeObject(m_Favourites.Collections));
        private void PushRecentlyPlaced() => m_RecentlyPlacedBinding.Update(JsonConvert.SerializeObject(m_Favourites.RecentlyPlaced));
        private void PushMostUsed() => m_MostUsedBinding.Update(JsonConvert.SerializeObject(m_Favourites.MostUsed));
        private void PushQuickPins() => m_QuickPinsBinding.Update(JsonConvert.SerializeObject(m_Favourites.QuickPins));

        private void PushLists()
        {
            PushCollectionsData();
            PushRecentlyPlaced();
            PushMostUsed();
            PushQuickPins();
        }

        private void PushAll()
        {
            PushLists();
            m_ActiveCollectionIdBinding.Update(m_Favourites.ActiveCollectionId ?? "");
            UpdateIsPinned();
        }
    }
}
