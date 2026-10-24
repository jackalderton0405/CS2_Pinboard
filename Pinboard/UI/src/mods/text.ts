import { useLocalization } from "cs2/l10n";

// Every word the panel shows, in English. Each one is looked up in the game's
// translations first as "Pinboard.UI[key]", so a translation can be added later
// (one file per language on the game side) without touching the panel. When the
// game has no translation, the English here is used. {0}, {1}... are filled in
// with names and numbers.
const EN = {
    title: "Pinboard",
    collections: "Collections",

    // Tabs along the top of the panel
    tabPins: "Pins",
    tabRecent: "Recent",
    tabQuick: "Quick pins",
    tabMore: "More",
    tipTabPins: "Your collections, and search across all your pins",
    tipTabRecent: "What you placed lately, and what you place most",
    tipTabQuick: "Nine pins you can pick up with Alt + 1 to 9",
    tipTabMore: "Import a shared collection, pin a whole district",

    // Quick pins tab
    quickExplain: "Press Alt + 1 to Alt + 9 to pick these up, even with the panel closed. You can change the keys in the game's Options.",
    quickKey: "Alt + {0}",
    quickEmpty: "Empty",
    quickUseHolding: "Use what I'm holding: {0}",
    quickHoldHint: "Tip: pick up an asset from the game's menu to put it here, or choose one of your pins below.",
    quickSearch: "Search your pins...",
    quickNoCandidates: "No pins yet. Pin something first, or pick it up and use the button above.",
    tipQuickSet: "Choose what this key picks up",
    tipQuickClear: "Empty this slot",
    recentlyPlaced: "Recently Placed",
    mostUsed: "Most Used",
    allPins: "All Pins",
    pinCount1: "1 pin",
    pinCountN: "{0} pins",
    newCollection: "+ New Collection",
    collectionNamePlaceholder: "Collection name...",
    importCollection: "Import a shared collection",
    importPlaceholder: "Paste a share code, then press Enter...",
    newFilter: "+ New Filter",
    filterNamePlaceholder: "Filter name...",
    searchPins: "Search pins...",
    searchEverywhere: "Search all your pins...",
    searchList: "Search this list...",
    pin: "+ Pin",
    pinned: "Pinned",
    pinShort: "Pin",
    unpin: "Unpin",
    clear: "Clear",
    showMore: "Show more ({0} left)",
    cancel: "Cancel",
    delete: "Delete",
    ok: "OK",
    close: "Close",
    move: "Move",
    copy: "Copy",
    undo: "Undo",

    // Hover labels (tooltips)
    tipCollapse: "Fold the panel up",
    tipExpand: "Unfold the panel",
    tipClose: "Close the panel",
    tipDrag: "Drag to move the panel. Double-click to put it back.",
    tipRename: "Rename",
    tipDeleteCollection: "Delete this collection",
    tipDeleteFilter: "Delete this filter",
    tipShare: "Share this collection (makes a share code)",
    tipMoveUp: "Move up",
    tipMoveDown: "Move down",
    tipFilters: "Choose which filters this pin is in",
    tipMoveCopy: "Move or copy to another collection",
    tipRemovePin: "Remove from this collection",
    tipPinToCollection: "Pin to a collection or filter",
    tipGrid: "Show as a grid of pictures",
    tipList: "Show as a list",
    tipSort: "Change the order",
    tipPinButton: "Pin the asset you're holding into this collection",
    tipUnpinButton: "Remove the asset you're holding from this collection",

    // Sorting
    sortOwn: "Your order",
    sortAz: "A to Z",
    sortZa: "Z to A",
    sortNewest: "Newest pinned",
    sortType: "By type",

    // Empty screens
    emptyCollections: "No collections yet. Press \"+ New Collection\" below to make one.",
    emptyPins: "No pins yet. Pick an asset from the game's menu, then press \"+ Pin\" at the top of this panel.",
    emptyFilter: "No pins in this filter yet. Press the funnel button on a pin in \"All Pins\" to add it here.",
    emptyRecent: "Nothing placed yet. Assets you build with will appear here.",
    emptyMostUsed: "Nothing placed yet. The assets you place most often will appear here.",
    noMatches: "No matches.",
    noOtherCollections: "There are no other collections. Make one first.",
    noCollectionsToPin: "Create a collection first (+ New Collection in the Pinboard panel).",
    noFiltersInCollection: "No filters in this collection",
    wholeCollection: "Whole collection",
    inCollections: "In: {0}",
    placedTimes1: "Placed once",
    placedTimesN: "Placed {0} times",
    fromPack: "From: {0}",
    fromMod: "From Paradox Mods, ID {0}",
    missingFromPack: "It comes from the {0} content pack.",
    missingFromMod: "It comes from Paradox Mods, ID {0}. Search for that number on the Paradox Mods website.",
    reorderHint: "Tip: choose \"Your order\" and clear the search to move pins up and down.",

    // Dialogs
    deleteCollectionTitle: "Delete Collection",
    deleteCollectionMessage: "Delete collection '{0}'? All pins and filters inside will be removed.",
    deleteFilterTitle: "Delete Filter",
    deleteFilterMessage: "Delete filter '{0}'? Pins will remain in the collection.",
    missingTitle: "Asset not found",
    missingMessage: "'{0}' isn't available in this game. If it's a custom asset or from a content pack, check it's installed and switched on, then reload your city. Your pin is kept.",
    clearRecentTitle: "Clear Recently Placed",
    clearRecentMessage: "Clear the entire Recently Placed list? Your collections and pins are not affected.",
    clearMostUsedTitle: "Clear Most Used",
    clearMostUsedMessage: "Reset the Most Used list and all its counts? Your collections and pins are not affected.",
    shareTitle: "Share code",
    shareMessage: "The share code is on your clipboard. Paste it to another player, and they can add it with \"Import a shared collection\" in their Pinboard panel.",

    // Short messages at the bottom of the panel
    removedPin: "Removed '{0}'.",
    pinnedTo: "Pinned '{0}' to {1}.",
    unpinnedFrom: "Removed '{0}' from {1}.",
    nothingSelected: "Pick an asset first, then press the shortcut to pin it.",
    noQuickPick: "Quick pin {0} is empty. Choose one in the Quick pins tab.",
    exportCopied: "Share code for '{0}' copied to the clipboard.",
    exportShown: "Share code for '{0}' is ready to copy.",
    exportFailed: "Couldn't make a share code. See the game log for details.",
    imported: "Added '{0}' with {1} pins.",
    importedSomeMissing: "Added '{0}' with {1} pins. Not installed in your game: {2}.",
    importFailed_empty: "Paste a share code first.",
    importFailed_tooLong: "That share code is too long.",
    importFailed_notACode: "That doesn't look like a Pinboard share code. It should start with PBV2:",
    importFailed_damaged: "That share code is damaged or incomplete. Try copying it again.",

    // Pin everything in a district
    districtOpen: "Pin everything in a district",
    districtTitle: "Pin a district",
    districtChoose: "1. Choose a district",
    districtWhat: "2. What to pin",
    districtInto: "3. Into which collection",
    districtFilter: "4. Into a filter (optional)",
    districtNone: "This city has no districts yet. Draw one with the game's District tool first.",
    districtEverything: "Everything",
    catBuildings: "Buildings you placed",
    catZoned: "Zoned buildings (grown by the game)",
    zonedNote: "Zoned buildings only stay on the matching zone colour, unless you use a mod like Plop the Growables.",
    catObjects: "Props, decals and other objects",
    catTrees: "Trees and plants",
    catNetworks: "Roads, paths and tracks",
    districtNewCollection: "New collection",
    districtNoFilter: "No filter",
    districtNewFilter: "New filter",
    districtLooking: "Looking inside the district...",
    districtPinButton: "Pin {0} assets",
    districtPinButton1: "Pin 1 asset",
    districtExplain: "Each different asset is pinned once. Things that come built into another asset, like the trees on a building, are left out.",
    districtPinned: "Pinned {0} new assets to '{1}' ({2} were already there).",
    districtNothing: "Nothing to pin: there's nothing of those kinds in this district.",
    districtFailed: "Couldn't look inside that district. See the game log for details.",

    // Tool options section
    none: "None",
    tipToolPin: "Pin or unpin the asset you're holding",
    tipInfoUnpin: "Remove this asset from this collection",
    tipInfoCollection: "Choose which collection to pin into",
    tipInfoAdd: "Pin this asset into another collection",
    tipQuickEmptySlot: "Quick pin {0} is empty. Click to put what you're holding here.",
    infoNotPinned: "Not pinned",
    infoNoneYet: "Not in any collection yet.",
    infoAddTo: "+ Add to a collection",
    infoPinnedIn1: "In 1 collection",
    infoPinnedInN: "In {0} collections",
};

export type TextKey = keyof typeof EN;
export type Text = (key: TextKey, ...args: (string | number)[]) => string;

const fill = (text: string, args: (string | number)[]) =>
    text.replace(/\{(\d+)\}/g, (all, i) => (args[+i] !== undefined ? String(args[+i]) : all));

// The game's translation lookup. If it's ever missing (for example after a game
// update), the panel quietly uses the English above instead of breaking.
export const useText = (): Text => {
    let translate: ((id: string, fallback?: string | null) => string | null) | undefined;
    try { translate = useLocalization().translate; } catch { translate = undefined; }
    return (key, ...args) => {
        let text: string = EN[key];
        if (translate) {
            try { text = translate("Pinboard.UI[" + key + "]", EN[key]) || EN[key]; } catch { /* keep English */ }
        }
        return fill(text, args);
    };
};

// Text for keys sent by the game side (the short messages), checked first.
export const isTextKey = (key: string): key is TextKey => Object.prototype.hasOwnProperty.call(EN, key);

// A friendly word for the game's asset types. Unknown types are tidied up
// automatically, e.g. "MarkerObjectPrefab" becomes "Marker Object".
const TYPE_LABELS: Record<string, string> = {
    BuildingPrefab: "Building",
    BuildingExtensionPrefab: "Building upgrade",
    StaticObjectPrefab: "Object",
    ObjectGeometryPrefab: "Object",
    MarkerObjectPrefab: "Marker",
    VegetationPrefab: "Plant",
    RoadPrefab: "Road",
    TrackPrefab: "Track",
    PathwayPrefab: "Path",
    FencePrefab: "Fence",
    SurfacePrefab: "Surface",
    AreaPrefab: "Area",
    TransportLinePrefab: "Transport line",
};

export const typeLabel = (type?: string): string => {
    if (!type) return "";
    if (TYPE_LABELS[type]) return TYPE_LABELS[type];
    return type.replace(/Prefab$/, "").replace(/([a-z])([A-Z])/g, "$1 $2");
};
