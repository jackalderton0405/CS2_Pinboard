// The shapes of the lists the game side sends (as JSON). They match the C# classes
// in Data/FavouritesData.cs.

export interface FavouriteEntry {
    name: string;
    type: string;
    thumbnail?: string;
    displayName?: string;
    pinnedAt?: number;
    uses?: number;
    sourcePack?: string;
    sourceModId?: string;
}

export interface FilterData { id: string; name: string; assets: string[]; }

export interface CollectionData { id: string; name: string; pins: FavouriteEntry[]; filters: FilterData[]; }

export type SortMode = "own" | "az" | "za" | "newest" | "type";

export interface PanelLayout { x: number; y: number; grid: boolean; sort: SortMode; }

// A short message from the game side, shown for a few seconds.
export interface Notice { id: number; key: string; args?: string[]; }

// A pin that was just removed, kept for a few seconds so "Undo" can put it back.
export interface RemovedPin { collectionId: string; entry: FavouriteEntry; index: number; filterIds: string[]; }

export interface DistrictInfo { id: string; name: string; }
export interface DistrictScan { districtId: string; counts: Record<string, number>; }

// The asset behind whatever the player clicked in the city.
export interface InspectedAsset extends FavouriteEntry { zoned?: boolean; }
