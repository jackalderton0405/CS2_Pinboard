import { trigger, bindValue } from "cs2/api";
import { RemovedPin, SortMode } from "./types";

// Everything the panel, the tool options box and the info panel section say to the
// game side, in one place. The group and the names must match the C# code
// (Systems/PinboardUISystem.cs). "pinboard2" keeps Pinboard 2 apart from 1.x ("pinboard").
const GROUP = "pinboard2";

const send = (name: string, ...args: (string | number | boolean)[]) => trigger(GROUP, name, ...args);
// Messages with several parts go as JSON text, which the C# side reads back.
const sendJson = (name: string, message: object) => trigger(GROUP, name, JSON.stringify(message));

// ── What the game side shows us ───────────────────────────────────────────────
export const panelOpen$          = bindValue<boolean>(GROUP, "panelOpen", false);
export const collectionsData$    = bindValue<string>(GROUP, "collectionsData", "[]");
export const activeCollectionId$ = bindValue<string>(GROUP, "activeCollectionId", "");
export const currentPrefab$      = bindValue<string>(GROUP, "currentPrefabName", "");
export const currentPrefabId$    = bindValue<string>(GROUP, "currentPrefabId", "");
export const isPinned$           = bindValue<boolean>(GROUP, "isPinned", false);
export const recentlyPlaced$     = bindValue<string>(GROUP, "recentlyPlacedData", "[]");
export const mostUsed$           = bindValue<string>(GROUP, "mostUsedData", "[]");
export const quickPins$          = bindValue<string>(GROUP, "quickPinsData", "[]");
export const inspected$          = bindValue<string>(GROUP, "inspected", "");
export const missingAsset$       = bindValue<string>(GROUP, "missingAsset", "");
export const missingSource$      = bindValue<string>(GROUP, "missingAssetSource", "");
export const panelLayout$        = bindValue<string>(GROUP, "panelLayout", "");
export const notice$             = bindValue<string>(GROUP, "notice", "");
export const shareCode$          = bindValue<string>(GROUP, "shareCode", "");
export const districts$          = bindValue<string>(GROUP, "districts", "[]");
export const districtScan$       = bindValue<string>(GROUP, "districtScan", "");

// ── The panel and picking assets up ───────────────────────────────────────────
export const togglePanel         = () => send("togglePanel");
export const selectAsset         = (name: string) => send("selectAsset", name);
export const dismissMissingAsset = () => send("dismissMissingAsset");

// ── Pins ──────────────────────────────────────────────────────────────────────
// The held asset into the collection being viewed (the panel's "+ Pin").
export const pinCurrentAsset = (collectionId: string) => send("pinCurrentAsset", collectionId);
// The held asset in or out of the tool options box's collection.
export const togglePin       = () => send("togglePin");
// Any asset the panel knows about into a collection.
export const pinInto         = (collectionId: string, assetName: string) => sendJson("pinFromRecent", { collectionId, assetName });
export const removeAsset     = (collectionId: string, assetName: string) => sendJson("removeAsset", { collectionId, assetName });
export const restorePin      = (p: RemovedPin) => sendJson("restorePin", p);
export const movePin         = (collectionId: string, assetName: string, delta: number) => sendJson("movePin", { collectionId, assetName, delta });
export const copyPin         = (fromCollectionId: string, toCollectionId: string, assetName: string, move: boolean) =>
    sendJson("copyPin", { fromCollectionId, toCollectionId, assetName, move });

// ── Collections and filters ───────────────────────────────────────────────────
export const setActiveCollection = (id: string) => send("setActiveCollection", id);
export const createCollection    = (name: string) => send("createCollection", name);
export const deleteCollection    = (id: string) => send("deleteCollection", id);
export const renameCollection    = (id: string, name: string) => sendJson("renameCollection", { id, name });
export const moveCollection      = (id: string, delta: number) => sendJson("moveCollection", { id, delta });
export const createFilter        = (collectionId: string, name: string) => sendJson("createFilter", { collectionId, name });
export const deleteFilter        = (collectionId: string, filterId: string) => sendJson("deleteFilter", { collectionId, filterId });
export const renameFilter        = (collectionId: string, filterId: string, name: string) => sendJson("renameFilter", { collectionId, filterId, name });
// Adding to a filter also pins the asset in that collection (done on the game side).
export const addToFilter         = (collectionId: string, filterId: string, assetName: string) => sendJson("addToFilter", { collectionId, filterId, assetName });
export const removeFromFilter    = (collectionId: string, filterId: string, assetName: string) => sendJson("removeFromFilter", { collectionId, filterId, assetName });

// ── Recently Placed, Most Used, quick pins ────────────────────────────────────
export const clearRecent   = () => send("clearRecent");
export const clearMostUsed = () => send("clearMostUsed");
// assetName "" = the asset being held right now.
export const setQuickPin   = (slot: number, assetName: string) => sendJson("setQuickPin", { slot, assetName });
export const clearQuickPin = (slot: number) => send("clearQuickPin", slot);
export const moveQuickPin  = (slot: number, delta: number) => sendJson("moveQuickPin", { slot, delta });

// ── Sharing ───────────────────────────────────────────────────────────────────
export const exportCollection = (id: string) => send("exportCollection", id);
export const importCollection = (code: string) => send("importCollection", code);
export const dismissShareCode = () => send("dismissShareCode");

// ── Panel layout ──────────────────────────────────────────────────────────────
export const savePanelPosition = (x: number, y: number) => sendJson("savePanelPosition", { x, y });
export const setGridView       = (grid: boolean) => send("setGridView", grid);
export const setSortMode       = (mode: SortMode) => send("setSortMode", mode);

// ── Pin everything in a district ──────────────────────────────────────────────
export const listDistricts = () => send("listDistricts");
export const scanDistrict  = (districtId: string) => sendJson("scanDistrict", { districtId });
export const pinDistrict   = (p: {
    districtId: string; categories: string[];
    collectionId: string | null; newCollectionName: string;
    filterId: string | null; newFilterName: string;
}) => sendJson("pinDistrict", p);

// Longest names the game side keeps (the same limits as the C# code).
export const MAX_COLLECTION_NAME = 30;
export const MAX_FILTER_NAME = 20;
