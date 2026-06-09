using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Input;
using Game.Modding;
using Game.Settings;
using Game.UI.Localization;
using Game.UI.Menu;
using Game.UI.Widgets;
using Pinboard.Data;
using Pinboard.Systems;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;

namespace Pinboard
{
    [FileLocation(nameof(Pinboard))]
    [SettingsUIKeyboardAction(kTogglePanelActionName, ActionType.Button, usages: new[] { "Pinboard", Usages.kDefaultUsage })]
    [SettingsUIGroupOrder(kKeybindGroup, kCollectionsGroup, kFiltersGroup)]
    [SettingsUIShowGroupName(kKeybindGroup, kCollectionsGroup, kFiltersGroup)]
    public class Setting : ModSetting
    {
        public const string kTogglePanelActionName = "PinboardTogglePanel";

        public const string kSection = "Main";
        public const string kKeybindGroup = "Keybinding";
        public const string kCollectionsGroup = "Collections";
        public const string kFiltersGroup = "Filters";

        public Setting(IMod mod) : base(mod) { }

        // ── Keybinding ────────────────────────────────────────────────────────
        [SettingsUIKeyboardBinding(BindingKeyboard.B, kTogglePanelActionName, ctrl: true)]
        [SettingsUISection(kSection, kKeybindGroup)]
        public ProxyBinding TogglePanelKey { get; set; }

        // ── Collections ───────────────────────────────────────────────────────
        private string m_SelectedCollectionId = "";
        private string m_SelectedFilterId = "";

        [SettingsUIMultilineText]
        [SettingsUISection(kSection, kCollectionsGroup)]
        public string InfoText => string.Empty;

        [SettingsUIButton]
        [SettingsUISection(kSection, kCollectionsGroup)]
        public bool RefreshButton { set { Refresh(); } }

        [SettingsUIDropdown(typeof(Setting), nameof(GetCollectionItems))]
        [SettingsUISection(kSection, kCollectionsGroup)]
        public string SelectedCollectionId
        {
            get { NormalizeSelection(); return m_SelectedCollectionId; }
            set
            {
                m_SelectedCollectionId = value ?? "";
                // Switching collection: snap the filter selection to that collection's first filter.
                var data = FavouritesService.Load();
                var col = data.Collections.FirstOrDefault(c => c.Id == m_SelectedCollectionId);
                m_SelectedFilterId = col?.Filters.FirstOrDefault()?.Id ?? "";
                Mod.log.Info($"[Pinboard][Settings] Collection selected: {m_SelectedCollectionId}");
                RefreshUI(); // rebuild the page so the Filter dropdown re-queries this collection
            }
        }

        [SettingsUIButton]
        [SettingsUIConfirmation]
        [SettingsUISection(kSection, kCollectionsGroup)]
        public bool ClearCollectionButton { set { ClearCollection(); } }

        [SettingsUIButton]
        [SettingsUIConfirmation]
        [SettingsUISection(kSection, kCollectionsGroup)]
        public bool DeleteCollectionButton { set { DeleteCollection(); } }

        // ── Filters ───────────────────────────────────────────────────────────
        [SettingsUIDropdown(typeof(Setting), nameof(GetFilterItems))]
        [SettingsUISection(kSection, kFiltersGroup)]
        public string SelectedFilterId
        {
            get { NormalizeSelection(); return m_SelectedFilterId; }
            set
            {
                m_SelectedFilterId = value ?? "";
                Mod.log.Info($"[Pinboard][Settings] Filter selected: {m_SelectedFilterId}");
            }
        }

        [SettingsUIButton]
        [SettingsUIConfirmation]
        [SettingsUISection(kSection, kFiltersGroup)]
        public bool ClearFilterButton { set { ClearFilter(); } }

        [SettingsUIButton]
        [SettingsUIConfirmation]
        [SettingsUISection(kSection, kFiltersGroup)]
        public bool DeleteFilterButton { set { DeleteFilter(); } }

        // ── Dropdown item providers ───────────────────────────────────────────
        public DropdownItem<string>[] GetCollectionItems()
        {
            var data = FavouritesService.Load();
            var items = data.Collections
                .Select(c => new DropdownItem<string> { value = c.Id, displayName = LocalizedString.Value(c.Name) })
                .ToList();
            if (items.Count == 0)
                items.Add(new DropdownItem<string> { value = "", displayName = LocalizedString.Value("(no collections)") });
            Mod.log.Info($"[Pinboard][Settings] GetCollectionItems -> {items.Count}");
            return items.ToArray();
        }

        public DropdownItem<string>[] GetFilterItems()
        {
            var data = FavouritesService.Load();
            var col = data.Collections.FirstOrDefault(c => c.Id == m_SelectedCollectionId)
                      ?? data.Collections.FirstOrDefault();
            var items = (col?.Filters ?? new List<FilterData>())
                .Select(f => new DropdownItem<string> { value = f.Id, displayName = LocalizedString.Value(f.Name) })
                .ToList();
            if (items.Count == 0)
                items.Add(new DropdownItem<string> { value = "", displayName = LocalizedString.Value("(no filters)") });
            Mod.log.Info($"[Pinboard][Settings] GetFilterItems(col={col?.Name}) -> {items.Count}");
            return items.ToArray();
        }

        // ── Button actions ────────────────────────────────────────────────────
        private void ClearCollection()
        {
            var data = FavouritesService.Load();
            var col = data.Collections.FirstOrDefault(c => c.Id == m_SelectedCollectionId);
            if (col == null) { Mod.log.Info("[Pinboard][Settings] ClearCollection: none selected"); return; }
            int pins = col.Pins.Count, filters = col.Filters.Count;
            col.Pins.Clear();
            col.Filters.Clear();
            Commit(data);
            Mod.log.Info($"[Pinboard][Settings] Cleared collection '{col.Name}' ({pins} pins, {filters} filters)");
        }

        private void DeleteCollection()
        {
            var data = FavouritesService.Load();
            var col = data.Collections.FirstOrDefault(c => c.Id == m_SelectedCollectionId);
            if (col == null) { Mod.log.Info("[Pinboard][Settings] DeleteCollection: none selected"); return; }
            FavouritesService.DeleteCollection(data, m_SelectedCollectionId);
            // Re-point selection at whatever remains.
            m_SelectedCollectionId = data.Collections.FirstOrDefault()?.Id ?? "";
            m_SelectedFilterId = data.Collections.FirstOrDefault(c => c.Id == m_SelectedCollectionId)?
                .Filters.FirstOrDefault()?.Id ?? "";
            Commit(data);
            Mod.log.Info($"[Pinboard][Settings] Deleted collection '{col.Name}'");
        }

        private void ClearFilter()
        {
            var data = FavouritesService.Load();
            var col = data.Collections.FirstOrDefault(c => c.Id == m_SelectedCollectionId);
            var filter = col?.Filters.FirstOrDefault(f => f.Id == m_SelectedFilterId);
            if (filter == null) { Mod.log.Info("[Pinboard][Settings] ClearFilter: none selected"); return; }
            int assets = filter.Assets.Count;
            filter.Assets.Clear();
            Commit(data);
            Mod.log.Info($"[Pinboard][Settings] Cleared filter '{filter.Name}' ({assets} assets)");
        }

        private void DeleteFilter()
        {
            var data = FavouritesService.Load();
            var col = data.Collections.FirstOrDefault(c => c.Id == m_SelectedCollectionId);
            var filter = col?.Filters.FirstOrDefault(f => f.Id == m_SelectedFilterId);
            if (col == null || filter == null) { Mod.log.Info("[Pinboard][Settings] DeleteFilter: none selected"); return; }
            FavouritesService.DeleteFilter(data, col.Id, m_SelectedFilterId);
            m_SelectedFilterId = col.Filters.FirstOrDefault(f => f.Id != filter.Id)?.Id ?? "";
            Commit(data);
            Mod.log.Info($"[Pinboard][Settings] Deleted filter '{filter.Name}' from '{col.Name}'");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        // Persist to disk and refresh the live in-game panel if a game is running.
        private void Commit(FavouritesData data)
        {
            FavouritesService.Save(data);
            PinboardUISystem.Instance?.ReloadFromDisk();
            RefreshUI(); // rebuild the page so dropdowns re-query their (now changed) items
        }

        // Re-pull collections/filters from disk into the dropdowns. Clicking any
        // settings button triggers a page rebuild, which re-runs the getters, so
        // this picks up collections/filters created in the in-game panel.
        private void Refresh()
        {
            NormalizeSelection();
            Mod.log.Info("[Pinboard][Settings] Manual refresh requested");
            RefreshUI();
        }

        // Force the options page to rebuild. ApplyAndSave only refreshes widget
        // *values*; a dropdown's *item list* is built once at registration, so the
        // only way to re-run the item getters is to re-register the whole page.
        // Guarded against re-entrancy.
        private bool m_Refreshing;
        private void RefreshUI()
        {
            if (m_Refreshing) return;
            m_Refreshing = true;
            try
            {
                UnregisterInOptionsUI();
                RegisterInOptionsUI();
                Mod.log.Info("[Pinboard][Settings] Options page re-registered (rebuild)");

                // Re-registering bounces the options menu off our page; navigate back.
                try
                {
                    var ou = World.DefaultGameObjectInjectionWorld?.GetExistingSystemManaged<OptionsUISystem>();
                    ou?.OpenPage(id, kSection, false);
                    Mod.log.Info($"[Pinboard][Settings] Reopened page id='{id}' section='{kSection}'");
                }
                catch (System.Exception ex)
                {
                    Mod.log.Warn($"[Pinboard][Settings] OpenPage failed: {ex.Message}");
                }
            }
            finally { m_Refreshing = false; }
        }

        // Keep the stored selections pointing at things that still exist.
        private void NormalizeSelection()
        {
            var data = FavouritesService.Load();
            var col = data.Collections.FirstOrDefault(c => c.Id == m_SelectedCollectionId);
            if (col == null)
            {
                col = data.Collections.FirstOrDefault();
                m_SelectedCollectionId = col?.Id ?? "";
            }
            if (col == null || col.Filters.All(f => f.Id != m_SelectedFilterId))
                m_SelectedFilterId = col?.Filters.FirstOrDefault()?.Id ?? "";
        }

        public override void SetDefaults()
        {
            var data = FavouritesService.Load();
            m_SelectedCollectionId = data.Collections.FirstOrDefault()?.Id ?? "";
            m_SelectedFilterId = data.Collections.FirstOrDefault()?.Filters.FirstOrDefault()?.Id ?? "";
        }
    }

    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocaleEN(Setting setting) { m_Setting = setting; }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "Pinboard" },

                // Keybinding
                { m_Setting.GetBindingMapLocaleID(), "Pinboard" },
                { m_Setting.GetBindingKeyLocaleID(Setting.kTogglePanelActionName), "Toggle Pinboard panel" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.TogglePanelKey)), "Toggle panel" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.TogglePanelKey)), "Keyboard shortcut to open and close the Pinboard panel." },

                // Groups
                { m_Setting.GetOptionGroupLocaleID(Setting.kKeybindGroup), "Keybinding" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kCollectionsGroup), "Collections" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kFiltersGroup), "Filters" },

                // Collection widgets
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InfoText)), "Note: clearing, deleting or refreshing reloads this page, which briefly returns you to the top of the options menu. This is normal — your change has been applied." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RefreshButton)), "Refresh lists" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RefreshButton)), "Reload collections and filters to reflect changes made in the in-game panel." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.SelectedCollectionId)), "Collection" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.SelectedCollectionId)), "Choose a collection to manage." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ClearCollectionButton)), "Clear collection" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ClearCollectionButton)), "Remove all pins and filters from the selected collection, keeping the collection itself." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.ClearCollectionButton)), "Clear all pins and filters from this collection?" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DeleteCollectionButton)), "Delete collection" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DeleteCollectionButton)), "Delete the selected collection entirely." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.DeleteCollectionButton)), "Delete this collection and everything in it?" },

                // Filter widgets
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.SelectedFilterId)), "Filter" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.SelectedFilterId)), "Choose a filter within the selected collection." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ClearFilterButton)), "Clear filter" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ClearFilterButton)), "Remove all assets from the selected filter, keeping the filter itself." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.ClearFilterButton)), "Clear all assets from this filter?" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DeleteFilterButton)), "Delete filter" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DeleteFilterButton)), "Delete the selected filter entirely." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.DeleteFilterButton)), "Delete this filter?" },
            };
        }

        public void Unload() { }
    }
}
