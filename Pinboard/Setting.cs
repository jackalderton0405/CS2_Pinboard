using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Input;
using Game.Modding;
using Game.Settings;
using System.Collections.Generic;

namespace Pinboard
{
    // The options page only holds the keyboard shortcut. Collections and filters are
    // managed in the in-game panel, so only PinboardUISystem ever reads or writes the
    // save file and the two can never overwrite each other's changes.
    [FileLocation("Pinboard")]
    [SettingsUIKeyboardAction(kTogglePanelActionName, ActionType.Button, usages: new[] { "Pinboard", Usages.kDefaultUsage })]
    [SettingsUIKeyboardAction(kPinCurrentActionName, ActionType.Button, usages: new[] { "Pinboard", Usages.kDefaultUsage })]
    [SettingsUIKeyboardAction(kQuickPick1, ActionType.Button, usages: new[] { "Pinboard", Usages.kDefaultUsage })]
    [SettingsUIKeyboardAction(kQuickPick2, ActionType.Button, usages: new[] { "Pinboard", Usages.kDefaultUsage })]
    [SettingsUIKeyboardAction(kQuickPick3, ActionType.Button, usages: new[] { "Pinboard", Usages.kDefaultUsage })]
    [SettingsUIKeyboardAction(kQuickPick4, ActionType.Button, usages: new[] { "Pinboard", Usages.kDefaultUsage })]
    [SettingsUIKeyboardAction(kQuickPick5, ActionType.Button, usages: new[] { "Pinboard", Usages.kDefaultUsage })]
    [SettingsUIKeyboardAction(kQuickPick6, ActionType.Button, usages: new[] { "Pinboard", Usages.kDefaultUsage })]
    [SettingsUIKeyboardAction(kQuickPick7, ActionType.Button, usages: new[] { "Pinboard", Usages.kDefaultUsage })]
    [SettingsUIKeyboardAction(kQuickPick8, ActionType.Button, usages: new[] { "Pinboard", Usages.kDefaultUsage })]
    [SettingsUIKeyboardAction(kQuickPick9, ActionType.Button, usages: new[] { "Pinboard", Usages.kDefaultUsage })]
    [SettingsUIGroupOrder(kKeybindGroup, kQuickPickGroup, kTrackingGroup)]
    [SettingsUIShowGroupName(kKeybindGroup, kQuickPickGroup, kTrackingGroup)]
    public class Setting : ModSetting
    {
        public const string kTogglePanelActionName = "PinboardTogglePanel";
        public const string kPinCurrentActionName = "PinboardPinCurrent";

        // Quick-pick keys: pick up quick pin 1 to 9 (chosen in the panel's Quick pins tab).
        public const string kQuickPick1 = "PinboardQuickPick1";
        public const string kQuickPick2 = "PinboardQuickPick2";
        public const string kQuickPick3 = "PinboardQuickPick3";
        public const string kQuickPick4 = "PinboardQuickPick4";
        public const string kQuickPick5 = "PinboardQuickPick5";
        public const string kQuickPick6 = "PinboardQuickPick6";
        public const string kQuickPick7 = "PinboardQuickPick7";
        public const string kQuickPick8 = "PinboardQuickPick8";
        public const string kQuickPick9 = "PinboardQuickPick9";
        public static readonly string[] kQuickPickActions =
        {
            kQuickPick1, kQuickPick2, kQuickPick3, kQuickPick4, kQuickPick5,
            kQuickPick6, kQuickPick7, kQuickPick8, kQuickPick9,
        };

        public const string kSection = "Main";
        public const string kKeybindGroup = "Keybinding";
        public const string kQuickPickGroup = "QuickPick";
        public const string kTrackingGroup = "Tracking";

        public Setting(IMod mod) : base(mod) { }

        // ── Keybinding ────────────────────────────────────────────────────────
        [SettingsUIKeyboardBinding(BindingKeyboard.B, kTogglePanelActionName, ctrl: true)]
        [SettingsUISection(kSection, kKeybindGroup)]
        public ProxyBinding TogglePanelKey { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.B, kPinCurrentActionName, ctrl: true, shift: true)]
        [SettingsUISection(kSection, kKeybindGroup)]
        public ProxyBinding PinCurrentKey { get; set; }

        // ── Quick-pick keys (Alt + 1 to 9) ────────────────────────────────────
        [SettingsUIKeyboardBinding(BindingKeyboard.Digit1, kQuickPick1, alt: true)]
        [SettingsUISection(kSection, kQuickPickGroup)]
        public ProxyBinding QuickPick1Key { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.Digit2, kQuickPick2, alt: true)]
        [SettingsUISection(kSection, kQuickPickGroup)]
        public ProxyBinding QuickPick2Key { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.Digit3, kQuickPick3, alt: true)]
        [SettingsUISection(kSection, kQuickPickGroup)]
        public ProxyBinding QuickPick3Key { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.Digit4, kQuickPick4, alt: true)]
        [SettingsUISection(kSection, kQuickPickGroup)]
        public ProxyBinding QuickPick4Key { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.Digit5, kQuickPick5, alt: true)]
        [SettingsUISection(kSection, kQuickPickGroup)]
        public ProxyBinding QuickPick5Key { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.Digit6, kQuickPick6, alt: true)]
        [SettingsUISection(kSection, kQuickPickGroup)]
        public ProxyBinding QuickPick6Key { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.Digit7, kQuickPick7, alt: true)]
        [SettingsUISection(kSection, kQuickPickGroup)]
        public ProxyBinding QuickPick7Key { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.Digit8, kQuickPick8, alt: true)]
        [SettingsUISection(kSection, kQuickPickGroup)]
        public ProxyBinding QuickPick8Key { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.Digit9, kQuickPick9, alt: true)]
        [SettingsUISection(kSection, kQuickPickGroup)]
        public ProxyBinding QuickPick9Key { get; set; }

        // ── What Recently Placed and Most Used keep track of ──────────────────
        // Buildings, trees, fences/lane details, surfaces and props are on to start with;
        // the rest are off. Switching one off only stops new placements of that kind
        // being added; what's already in the lists stays until cleared.
        [SettingsUISection(kSection, kTrackingGroup)]
        public bool TrackBuildings { get; set; } = true;

        [SettingsUISection(kSection, kTrackingGroup)]
        public bool TrackZones { get; set; }

        [SettingsUISection(kSection, kTrackingGroup)]
        public bool TrackDistricts { get; set; }

        [SettingsUISection(kSection, kTrackingGroup)]
        public bool TrackRoads { get; set; }

        [SettingsUISection(kSection, kTrackingGroup)]
        public bool TrackPaths { get; set; }

        [SettingsUISection(kSection, kTrackingGroup)]
        public bool TrackUtilities { get; set; }

        [SettingsUISection(kSection, kTrackingGroup)]
        public bool TrackFences { get; set; } = true;

        [SettingsUISection(kSection, kTrackingGroup)]
        public bool TrackAreas { get; set; } = true;

        [SettingsUISection(kSection, kTrackingGroup)]
        public bool TrackTrees { get; set; } = true;

        [SettingsUISection(kSection, kTrackingGroup)]
        public bool TrackProps { get; set; } = true;

        [SettingsUISection(kSection, kTrackingGroup)]
        public bool TrackTerrain { get; set; }

        [SettingsUISection(kSection, kTrackingGroup)]
        public bool TrackTransportLines { get; set; }

        [SettingsUISection(kSection, kTrackingGroup)]
        public bool TrackOther { get; set; }

        // ── Panel layout (not shown on the options page) ──────────────────────
        // Remembered here rather than in the pins file, so the pins file only holds pins.
        // A position below zero means "the default spot".
        [SettingsUIHidden]
        public float PanelX { get; set; } = -1f;

        [SettingsUIHidden]
        public float PanelY { get; set; } = -1f;

        [SettingsUIHidden]
        public bool GridView { get; set; }

        [SettingsUIHidden]
        public string SortMode { get; set; } = "own";

        public override void SetDefaults()
        {
            PanelX = -1f;
            PanelY = -1f;
            GridView = false;
            SortMode = "own";
            TrackBuildings = true;
            TrackZones = false;
            TrackDistricts = false;
            TrackRoads = false;
            TrackPaths = false;
            TrackUtilities = false;
            TrackFences = true;
            TrackAreas = true;
            TrackTrees = true;
            TrackProps = true;
            TrackTerrain = false;
            TrackTransportLines = false;
            TrackOther = false;
        }
    }

    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocaleEN(Setting setting) { m_Setting = setting; }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            var entries = new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "Pinboard" },

                // Keybinding
                { m_Setting.GetBindingMapLocaleID(), "Pinboard" },
                { m_Setting.GetBindingKeyLocaleID(Setting.kTogglePanelActionName), "Toggle Pinboard panel" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.TogglePanelKey)), "Toggle panel" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.TogglePanelKey)), "Keyboard shortcut to open and close the Pinboard panel." },
                { m_Setting.GetBindingKeyLocaleID(Setting.kPinCurrentActionName), "Pin the selected asset" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.PinCurrentKey)), "Pin selected asset" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.PinCurrentKey)), "Pins (or unpins) the asset you have selected into the collection shown in the tool options, without opening the panel." },

                // Groups
                { m_Setting.GetOptionGroupLocaleID(Setting.kKeybindGroup), "Keybinding" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kQuickPickGroup), "Quick pins" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kTrackingGroup), "Recently Placed and Most Used" },
            };

            // What Recently Placed and Most Used keep track of.
            void Track(string property, string label, string what)
            {
                entries[m_Setting.GetOptionLabelLocaleID(property)] = label;
                entries[m_Setting.GetOptionDescLocaleID(property)] =
                    "When on, " + what + " you place are added to Recently Placed and Most Used. Turning this off doesn't remove anything already in the lists.";
            }
            Track(nameof(Setting.TrackBuildings), "Buildings", "buildings (services, landmarks, ploppable and zoned buildings, and their add-ons)");
            Track(nameof(Setting.TrackZones), "Zones", "zone types");
            Track(nameof(Setting.TrackDistricts), "Districts", "districts");
            Track(nameof(Setting.TrackRoads), "Roads and tracks", "roads, train and tram tracks, taxiways and waterways");
            Track(nameof(Setting.TrackPaths), "Paths", "footpaths, bike paths and other pathways");
            Track(nameof(Setting.TrackUtilities), "Power lines and pipes", "power lines, water and sewage pipes");
            Track(nameof(Setting.TrackFences), "Fences, hedges and lane details", "fences, hedges, walls and other details drawn along a line (net lanes)");
            Track(nameof(Setting.TrackAreas), "Surfaces and areas", "surfaces, lots and other painted areas");
            Track(nameof(Setting.TrackTrees), "Trees and plants", "trees and plants");
            Track(nameof(Setting.TrackProps), "Props and decals", "props, decals and other placed objects");
            Track(nameof(Setting.TrackTerrain), "Terrain tools", "terrain brushes (raise, lower, level, slope, soften) and material or resource painting");
            Track(nameof(Setting.TrackTransportLines), "Transport lines", "bus, tram, train and other transport lines");
            entries[m_Setting.GetOptionLabelLocaleID(nameof(Setting.TrackOther))] = "Everything else";
            entries[m_Setting.GetOptionDescLocaleID(nameof(Setting.TrackOther))] =
                "Anything that doesn't fit the groups above, such as asset stamps and markers. When on, these are added to Recently Placed and Most Used. Turning this off doesn't remove anything already in the lists.";

            // Quick-pick keys 1 to 9 share the same wording.
            for (var i = 0; i < Setting.kQuickPickActions.Length; i++)
            {
                var n = i + 1;
                var property = "QuickPick" + n + "Key";
                entries[m_Setting.GetBindingKeyLocaleID(Setting.kQuickPickActions[i])] = "Pick up quick pin " + n;
                entries[m_Setting.GetOptionLabelLocaleID(property)] = "Pick up quick pin " + n;
                entries[m_Setting.GetOptionDescLocaleID(property)] =
                    "Picks up quick pin " + n + ". Choose your nine quick pins in the Quick pins tab of the Pinboard panel.";
            }
            return entries;
        }

        public void Unload() { }
    }
}
