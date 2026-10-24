import { Text, typeLabel } from "./text";
import { FavouriteEntry, SortMode } from "./types";

// Small helpers for showing, searching and sorting assets.

// Reads a list the game sent as text. Bad text gives the fallback instead of an error.
// (For the big lists that redraw often, use parseShared in parse-cache.ts.)
export const parse = <T,>(json: string, fallback: T): T => {
    try { return json ? JSON.parse(json) : fallback; } catch { return fallback; }
};

export const nameOf = (e: { name: string; displayName?: string }) => e.displayName || e.name;

// "From: Modern Architecture" or "From Paradox Mods, ID 12345"; empty for the base game.
export const sourceOf = (t: Text, e: { sourcePack?: string; sourceModId?: string }) =>
    e.sourcePack ? t("fromPack", e.sourcePack) : e.sourceModId ? t("fromMod", e.sourceModId) : "";

// The grey line under a name: its type, plus where it comes from when known.
export const detailsOf = (t: Text, e: FavouriteEntry, first?: string) =>
    [first ?? typeLabel(e.type), sourceOf(t, e)].filter(Boolean).join(" · ");

// Does an asset match what's typed in a search box? (query is already lower case)
export const matches = (e: FavouriteEntry, query: string) =>
    !query || nameOf(e).toLowerCase().includes(query) || e.name.toLowerCase().includes(query);

export const SORT_MODES: SortMode[] = ["own", "az", "za", "newest", "type"];

export const sortPins = (pins: FavouriteEntry[], mode: SortMode): FavouriteEntry[] => {
    const byName = (a: FavouriteEntry, b: FavouriteEntry) => nameOf(a).toLowerCase().localeCompare(nameOf(b).toLowerCase());
    switch (mode) {
        case "az": return [...pins].sort(byName);
        case "za": return [...pins].sort((a, b) => byName(b, a));
        // Pins from before this version have no date: they go to the bottom, in your order.
        case "newest": return [...pins].sort((a, b) => (b.pinnedAt ?? 0) - (a.pinnedAt ?? 0));
        case "type": return [...pins].sort((a, b) => typeLabel(a.type).localeCompare(typeLabel(b.type)) || byName(a, b));
        default: return pins;
    }
};

export const sortLabel = (t: Text, mode: SortMode) =>
    t(({ own: "sortOwn", az: "sortAz", za: "sortZa", newest: "sortNewest", type: "sortType" } as const)[mode]);
