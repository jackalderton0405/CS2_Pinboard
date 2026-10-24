import React, { useState, useRef, useEffect } from "react";
import { useValue } from "cs2/api";
import { Portal, Tooltip } from "cs2/ui";
import styles from "./pinboard-panel.module.css";
import "./pinboard-scrollbar.css";
import { useText, isTextKey } from "./text";
import { parseShared } from "./parse-cache";
import { CollectionData, FavouriteEntry, Notice, PanelLayout, RemovedPin, SortMode } from "./types";
import { detailsOf, matches, nameOf, parse, sortLabel, sortPins, SORT_MODES } from "./assets";
import {
    panelOpen$, collectionsData$, currentPrefab$, currentPrefabId$, recentlyPlaced$, mostUsed$,
    panelLayout$, notice$, quickPins$,
    togglePanel, pinCurrentAsset, removeAsset, restorePin, movePin,
    createCollection, deleteCollection, renameCollection, exportCollection, importCollection,
    createFilter, deleteFilter, renameFilter, addToFilter, removeFromFilter,
    clearRecent, clearMostUsed, savePanelPosition, setGridView, setSortMode,
    MAX_COLLECTION_NAME, MAX_FILTER_NAME,
} from "./game";
import {
    CheckboxChecked, CheckboxUnchecked, ChevronDownIcon, ChevronRightIcon, ChevronUpIcon, ClockIcon, CloseIcon,
    FilterIcon, FolderIcon, GridIcon, ListIcon, MoveIcon, PinIcon, ShareIcon, StarIcon,
} from "./icons";
import { AssetGrid, AssetRow, IconButton, NameInput, PinPickerButton, SearchBox, ShowMore } from "./panel/common";
import { CollectionRowItem, FilterTabItem } from "./panel/collection-rows";
import { MoveCopyPicker, PinPicker } from "./panel/pickers";
import { DistrictView } from "./panel/district-view";
import { QuickPinsView } from "./panel/quick-pins-view";
import { ConfirmDialog, MissingAssetDialog, ShareCodeDialog } from "./panel/dialogs";

// The Pinboard panel: the window with the Pins, Recent, Quick pins and More tabs.
// The smaller pieces it's built from are in the panel/ folder.

type View = "root" | "collection" | "recent" | "mostUsed" | "quick" | "more" | "district";
type Tab = "pins" | "recent" | "quick" | "more";

type PendingDelete =
  | { kind: "collection"; id: string; name: string }
  | { kind: "filter"; collectionId: string; filterId: string; name: string };

type Renaming =
  | { kind: "collection"; id: string; value: string }
  | { kind: "filter"; collectionId: string; filterId: string; value: string };

// The short message at the bottom of the panel.
interface Toast { id: number; text: string; undo?: RemovedPin; }

const TOAST_SECONDS = 6;
// Long lists are drawn this many at a time, so they open quickly.
const AUTO_PAGE = 25;

// ── PinboardPanel ────────────────────────────────────────────────────────────────

export const PinboardPanel = () => {
    const t = useText();

    const isOpen           = useValue(panelOpen$);
    const collectionsJson  = useValue(collectionsData$);
    const currentPrefab    = useValue(currentPrefab$);
    const currentPrefabId  = useValue(currentPrefabId$);
    const recentJson       = useValue(recentlyPlaced$);
    const mostUsedJson     = useValue(mostUsed$);
    const layoutJson       = useValue(panelLayout$);
    const noticeJson       = useValue(notice$);
    const quickPinsJson    = useValue(quickPins$);

    const [panelView, setPanelView]         = useState<View>("root");
    const [viewCollectionId, setViewColId]  = useState<string | null>(null);
    const [viewFilterId, setViewFilterId]   = useState<string | null>(null);
    const [expandedRow, setExpandedRow]     = useState<string | null>(null);
    const [movingRow, setMovingRow]         = useState<string | null>(null);
    const [pickingRow, setPickingRow]       = useState<string | null>(null);
    const [pendingClear, setPendingClear]   = useState<"recent" | "mostUsed" | null>(null);
    const [searchText, setSearchText]       = useState("");
    // Recently Placed and Most Used can hold hundreds of assets. Drawing them all
    // at once (each with its picture) made the tab slow to open, so show a page at a time.
    const [autoLimit, setAutoLimit]         = useState(AUTO_PAGE);
    const [rootSearch, setRootSearch]       = useState("");
    const [collapsed, setCollapsed]         = useState(false);
    const [sortOpen, setSortOpen]           = useState(false);
    const [pendingDelete, setPendingDelete] = useState<PendingDelete | null>(null);
    const [renaming, setRenaming]           = useState<Renaming | null>(null);
    const [newColMode, setNewColMode]       = useState(false);
    const [newColName, setNewColName]       = useState("");
    const [importMode, setImportMode]       = useState(false);
    const [importText, setImportText]       = useState("");
    const [newFilterMode, setNewFilterMode] = useState(false);
    const [newFilterName, setNewFilterName] = useState("");
    const [hoverPinned, setHoverPinned]     = useState(false);
    const [toast, setToast]                 = useState<Toast | null>(null);
    // Where the panel is while being dragged (px). null = use the saved spot.
    const [dragPos, setDragPos]             = useState<{ x: number; y: number } | null>(null);

    const listRef    = useRef<HTMLDivElement>(null);
    const colListRef = useRef<HTMLDivElement>(null);
    const panelRef   = useRef<HTMLDivElement>(null);
    const toastIdRef = useRef(0);
    const lastNoticeRef = useRef(0);
    const dragRef    = useRef<{ startX: number; startY: number; panelX: number; panelY: number; moved: boolean } | null>(null);

    // Set once a name box has been saved or cancelled (Enter or Escape). The box
    // loses focus straight after, and that "clicked away" would otherwise save
    // again: re-saving a cancelled name, or creating the same collection twice.
    const handledRef = useRef(false);

    const layout = parse<PanelLayout>(layoutJson, { x: -1, y: -1, grid: false, sort: "own" });
    const sortMode: SortMode = SORT_MODES.includes(layout.sort) ? layout.sort : "own";

    const showToast = (text: string, undo?: RemovedPin) => {
        toastIdRef.current += 1;
        setToast({ id: toastIdRef.current, text, undo });
    };

    // Messages from the game side (pin shortcut, sharing, quick-pick keys).
    useEffect(() => {
        const notice = parse<Notice | null>(noticeJson, null);
        if (!notice || notice.id === lastNoticeRef.current) return;
        lastNoticeRef.current = notice.id;
        if (isTextKey(notice.key)) showToast(t(notice.key, ...(notice.args ?? [])));
    }, [noticeJson]);

    // Each message disappears after a few seconds.
    useEffect(() => {
        if (!toast) return;
        const timer = setTimeout(() => setToast(current => current?.id === toast.id ? null : current), TOAST_SECONDS * 1000);
        return () => clearTimeout(timer);
    }, [toast?.id]);

    // ── Dragging the panel by its title bar ──
    useEffect(() => {
        const onMove = (e: MouseEvent) => {
            const d = dragRef.current;
            if (!d) return;
            const dx = e.clientX - d.startX, dy = e.clientY - d.startY;
            if (!d.moved && Math.abs(dx) + Math.abs(dy) < 4) return;
            d.moved = true;
            setDragPos(clampToScreen(d.panelX + dx, d.panelY + dy));
        };
        const onUp = () => {
            const d = dragRef.current;
            dragRef.current = null;
            if (!d || !d.moved) return;
            setDragPos(pos => {
                if (pos) savePanelPosition(Math.round(pos.x), Math.round(pos.y));
                return pos;
            });
        };
        window.addEventListener("mousemove", onMove);
        window.addEventListener("mouseup", onUp);
        return () => {
            window.removeEventListener("mousemove", onMove);
            window.removeEventListener("mouseup", onUp);
        };
    }, []);

    // When the saved spot changes (or is reset), stop using the dragged position.
    useEffect(() => { if (!dragRef.current) setDragPos(null); }, [layout.x, layout.y]);

    const clampToScreen = (x: number, y: number) => {
        const w = panelRef.current?.offsetWidth ?? 400;
        return {
            x: Math.max(0, Math.min(x, window.innerWidth - w)),
            y: Math.max(0, Math.min(y, window.innerHeight - 60)),
        };
    };

    const onHeaderMouseDown = (e: React.MouseEvent) => {
        if (e.button !== 0 || !panelRef.current) return;
        const rect = panelRef.current.getBoundingClientRect();
        dragRef.current = { startX: e.clientX, startY: e.clientY, panelX: rect.left, panelY: rect.top, moved: false };
    };

    // ── Toast shown even while the panel is closed (shortcut keys) ──
    const toastView = toast && (
        <div className={isOpen ? styles.toast : `${styles.toast} ${styles.toastFloating}`}>
            <span className={styles.toastText}>{toast.text}</span>
            {toast.undo && (
                <button
                    className={styles.toastUndo}
                    onClick={() => { restorePin(toast.undo!); setToast(null); }}
                >{t("undo")}</button>
            )}
        </div>
    );

    if (!isOpen) return (
        <>
            {toastView && <Portal>{toastView}</Portal>}
            <MissingAssetDialog t={t} />
        </>
    );

    const collections   = parseShared<CollectionData[]>("collections", collectionsJson, []);
    const recentEntries = parseShared<FavouriteEntry[]>("recent", recentJson, []);
    const mostUsed      = parseShared<FavouriteEntry[]>("mostUsed", mostUsedJson, []);
    const quickPins     = parseShared<(FavouriteEntry | null)[]>("quickPins", quickPinsJson, []);

    // Everything that can go in a quick pin: all pins, then Recently Placed and Most Used, once each.
    const quickCandidates: FavouriteEntry[] = [];
    {
        const seen = new Set<string>();
        const add = (e: FavouriteEntry) => { if (e && !seen.has(e.name)) { seen.add(e.name); quickCandidates.push(e); } };
        collections.forEach(c => c.pins.forEach(add));
        recentEntries.forEach(add);
        mostUsed.forEach(add);
    }

    const viewCollection = panelView === "collection"
        ? (collections.find(c => c.id === viewCollectionId) ?? null)
        : null;

    const effectiveView: View = panelView === "collection" && !viewCollection ? "root" : panelView;

    const resetRowState = () => {
        setExpandedRow(null);
        setMovingRow(null);
        setPickingRow(null);
        setSearchText("");
        setSortOpen(false);
        setAutoLimit(AUTO_PAGE);
    };

    const navigateInto = (col: CollectionData) => {
        setViewColId(col.id);
        setPanelView("collection");
        setViewFilterId(null);
        resetRowState();
    };

    const navigateRoot = () => {
        setPanelView("root");
        setViewFilterId(null);
        resetRowState();
    };

    const navigateTo = (view: "recent" | "mostUsed" | "quick" | "more" | "district") => {
        setPanelView(view);
        setViewColId(null);
        setViewFilterId(null);
        resetRowState();
    };

    // Which tab a view belongs to.
    const activeTab: Tab =
        effectiveView === "recent" || effectiveView === "mostUsed" ? "recent"
        : effectiveView === "quick" ? "quick"
        : effectiveView === "more" || effectiveView === "district" ? "more"
        : "pins";

    const openTab = (tab: Tab) => {
        setImportMode(false);
        setImportText("");
        if (tab === "pins") navigateRoot();
        else if (tab === "recent") navigateTo(effectiveView === "mostUsed" ? "mostUsed" : "recent");
        else navigateTo(tab);
    };

    const startRename = (r: Renaming) => { handledRef.current = false; setRenaming(r); };
    const cancelRename = () => { handledRef.current = true; setRenaming(null); };

    const submitRename = () => {
        if (handledRef.current) { handledRef.current = false; return; }
        if (!renaming) return;
        const v = renaming.value.trim();
        if (v) {
            if (renaming.kind === "collection")
                renameCollection(renaming.id, v);
            else
                renameFilter(renaming.collectionId, renaming.filterId, v);
        }
        handledRef.current = true;
        setRenaming(null);
    };

    const startNewCollection = () => { handledRef.current = false; setImportMode(false); setNewColMode(true); };
    const cancelNewCollection = () => { handledRef.current = true; setNewColName(""); setNewColMode(false); };

    const submitNewCollection = () => {
        if (handledRef.current) { handledRef.current = false; return; }
        const name = newColName.trim();
        if (name) createCollection(name);
        handledRef.current = true;
        setNewColName("");
        setNewColMode(false);
    };

    const startImport = () => { setNewColMode(false); setImportMode(true); };
    const cancelImport = () => { setImportText(""); setImportMode(false); };
    const submitImport = () => {
        importCollection(importText);
        setImportText("");
        setImportMode(false);
    };

    const startNewFilter = () => { handledRef.current = false; setNewFilterMode(true); };
    const cancelNewFilter = () => { handledRef.current = true; setNewFilterName(""); setNewFilterMode(false); };

    const submitNewFilter = () => {
        if (handledRef.current) { handledRef.current = false; return; }
        const name = newFilterName.trim();
        if (name && viewCollectionId) createFilter(viewCollectionId, name);
        handledRef.current = true;
        setNewFilterName("");
        setNewFilterMode(false);
    };

    const confirmDelete = () => {
        if (!pendingDelete) return;
        if (pendingDelete.kind === "collection") {
            deleteCollection(pendingDelete.id);
            if (viewCollectionId === pendingDelete.id) navigateRoot();
        } else {
            deleteFilter(pendingDelete.collectionId, pendingDelete.filterId);
            if (viewFilterId === pendingDelete.filterId) setViewFilterId(null);
        }
        setPendingDelete(null);
    };

    // Removes a pin and offers "Undo" for a few seconds.
    const removeWithUndo = (col: CollectionData, assetName: string) => {
        const index = col.pins.findIndex(p => p.name === assetName);
        if (index < 0) return;
        const entry = col.pins[index];
        const filterIds = col.filters.filter(f => f.assets.includes(assetName)).map(f => f.id);
        removeAsset(col.id, assetName);
        showToast(t("removedPin", nameOf(entry)), { collectionId: col.id, entry, index, filterIds });
    };

    const togglePicker = (name: string) => setPickingRow(pickingRow === name ? null : name);

    const handleWheel    = (e: React.WheelEvent) => { if (listRef.current)    listRef.current.scrollTop    += e.deltaY; };
    const handleColWheel = (e: React.WheelEvent) => { if (colListRef.current) colListRef.current.scrollTop += e.deltaY; };

    // ── Collection view: what to show ──
    const activeFilter = viewCollection?.filters.find(f => f.id === viewFilterId) ?? null;
    const basePins     = viewCollection?.pins ?? [];
    const filtered     = activeFilter ? basePins.filter(p => activeFilter.assets.includes(p.name)) : basePins;
    const query        = searchText.trim().toLowerCase();
    const searched     = filtered.filter(p => matches(p, query));
    const sorted       = sortPins(searched, sortMode);
    // Big collections are drawn a page at a time so they open quickly.
    const visiblePins  = sorted.slice(0, autoLimit);
    const showMore     = () => setAutoLimit(l => l + AUTO_PAGE);
    const morePins     = <ShowMore t={t} hidden={sorted.length - visiblePins.length} onClick={showMore} />;
    // Up/down arrows only make sense when you're looking at the whole collection in your order.
    const canReorder   = sortMode === "own" && !query && !activeFilter;
    // The "+ Pin" button works on the collection being viewed, so check that one.
    const isPinnedHere = !!viewCollection && viewCollection.pins.some(p => p.name === currentPrefabId);

    // ── Search everywhere (main screen) ──
    const rootQuery = rootSearch.trim().toLowerCase();
    const searchResults: { entry: FavouriteEntry; where: string[] }[] = [];
    if (rootQuery) {
        const byName = new Map<string, { entry: FavouriteEntry; where: string[] }>();
        const add = (entry: FavouriteEntry, where: string) => {
            if (!matches(entry, rootQuery)) return;
            const found = byName.get(entry.name);
            if (found) { if (!found.where.includes(where)) found.where.push(where); }
            else { const item = { entry, where: [where] }; byName.set(entry.name, item); searchResults.push(item); }
        };
        collections.forEach(c => c.pins.forEach(p => add(p, c.name)));
        recentEntries.forEach(p => add(p, t("recentlyPlaced")));
        mostUsed.forEach(p => add(p, t("mostUsed")));
    }

    const panelStyle: React.CSSProperties | undefined = dragPos
        ? { left: `${dragPos.x}px`, top: `${dragPos.y}px`, right: "auto" }
        : layout.x >= 0 && layout.y >= 0
            ? (() => { const p = clampToScreen(layout.x, layout.y); return { left: `${p.x}px`, top: `${p.y}px`, right: "auto" }; })()
            : undefined;

    // ── Pieces shared by the list views ──
    const gridToggle = (
        <IconButton
            tip={layout.grid ? t("tipList") : t("tipGrid")}
            onClick={() => setGridView(!layout.grid)}
            className={`${styles.viewBtn} ${layout.grid ? styles.viewBtnActive : ""}`}
        >{layout.grid ? <ListIcon /> : <GridIcon />}</IconButton>
    );

    // A list of auto-managed assets (Recently Placed, Most Used) with a pin picker per row.
    const autoList = (entries: FavouriteEntry[], subtitle: (e: FavouriteEntry) => string, emptyText: string) => {
        const matching = entries.filter(e => matches(e, query));
        const shown = matching.slice(0, autoLimit);
        if (entries.length === 0) return <div className={styles.emptyState}>{emptyText}</div>;
        if (matching.length === 0) return <div className={styles.emptyState}>{t("noMatches")}</div>;
        const more = <ShowMore t={t} hidden={matching.length - shown.length} onClick={showMore} />;
        if (layout.grid) return <><AssetGrid entries={shown} currentId={currentPrefabId} />{more}</>;
        return (
            <div className={styles.pinList}>
                {shown.map(entry => (
                    <AssetRow
                        key={entry.name}
                        entry={entry}
                        subtitle={subtitle(entry)}
                        isCurrent={entry.name === currentPrefabId}
                        actions={
                            <PinPickerButton t={t} open={pickingRow === entry.name} onClick={() => togglePicker(entry.name)} />
                        }
                        expanded={pickingRow === entry.name && <PinPicker entry={entry} collections={collections} t={t} />}
                    />
                ))}
                {more}
            </div>
        );
    };

    return (
        <>
        <div
            ref={panelRef}
            style={panelStyle}
            className={`${styles.panel} ${collapsed ? styles.panelCollapsed : styles.panelExpanded}`}
        >

            {/* ── Header (drag it to move the panel, double-click to put it back) ── */}
            <div
                className={`${styles.header} ${collapsed ? styles.headerCollapsed : ""}`}
                onMouseDown={onHeaderMouseDown}
                onDoubleClick={() => { dragRef.current = null; setDragPos(null); savePanelPosition(-1, -1); }}
            >
                <PinIcon size="18rem" />
                <Tooltip tooltip={t("tipDrag")}>
                    <span className={styles.headerTitle}>{t("title")}</span>
                </Tooltip>

                {effectiveView === "collection" && currentPrefab && (
                    <div
                        onMouseDown={e => e.stopPropagation()}
                        onDoubleClick={e => e.stopPropagation()}
                        onMouseEnter={() => setHoverPinned(true)}
                        onMouseLeave={() => setHoverPinned(false)}
                    >
                        <Tooltip tooltip={isPinnedHere ? t("tipUnpinButton") : t("tipPinButton")}>
                            <div
                                className={isPinnedHere ? styles.pinBtnPinned : styles.pinBtn}
                                onClick={isPinnedHere
                                    ? () => removeWithUndo(viewCollection!, currentPrefabId)
                                    : () => pinCurrentAsset(viewCollectionId!)}
                            >
                                {isPinnedHere ? (hoverPinned ? t("unpin") : t("pinned")) : t("pin")}
                            </div>
                        </Tooltip>
                    </div>
                )}

                <div onMouseDown={e => e.stopPropagation()} onDoubleClick={e => e.stopPropagation()} className={styles.headerButtons}>
                    <IconButton tip={collapsed ? t("tipExpand") : t("tipCollapse")} onClick={() => setCollapsed(c => !c)} className={styles.ctrlBtn}>
                        {collapsed ? <ChevronRightIcon /> : <ChevronDownIcon />}
                    </IconButton>
                    <IconButton tip={t("tipClose")} onClick={togglePanel} className={styles.ctrlBtn}>
                        <CloseIcon />
                    </IconButton>
                </div>
            </div>

            {/* ── Tabs ── */}
            {!collapsed && (
                <div className={styles.mainTabBar}>
                    {([
                        ["pins", t("tabPins"), t("tipTabPins"), <FolderIcon size="13rem" />],
                        ["recent", t("tabRecent"), t("tipTabRecent"), <ClockIcon size="13rem" />],
                        ["quick", t("tabQuick"), t("tipTabQuick"), <StarIcon size="13rem" />],
                        ["more", t("tabMore"), t("tipTabMore"), <ShareIcon size="12rem" />],
                    ] as [Tab, string, string, JSX.Element][]).map(([tab, label, tip, icon]) => (
                        <Tooltip key={tab} tooltip={tip}>
                            <div
                                className={`${styles.mainTab} ${activeTab === tab ? styles.mainTabActive : ""}`}
                                onClick={() => openTab(tab)}
                            >
                                {icon}
                                <span>{label}</span>
                            </div>
                        </Tooltip>
                    ))}
                </div>
            )}

            {/* ── Root view ── */}
            {!collapsed && effectiveView === "root" && (
                <>
                    <div className={styles.searchRow}>
                        <SearchBox value={rootSearch} onChange={setRootSearch} placeholder={t("searchEverywhere")} />
                    </div>

                    <div ref={colListRef} onWheel={handleColWheel} className={`${styles.scrollArea} pinboard-scroll`}>
                        {rootQuery ? (
                            searchResults.length === 0 ? (
                                <div className={styles.emptyState}>{t("noMatches")}</div>
                            ) : (
                                <div className={styles.pinList}>
                                    {searchResults.map(({ entry, where }) => (
                                        <AssetRow
                                            key={entry.name}
                                            entry={entry}
                                            subtitle={t("inCollections", where.join(", "))}
                                            isCurrent={entry.name === currentPrefabId}
                                            actions={
                                                <PinPickerButton t={t} open={pickingRow === entry.name} onClick={() => togglePicker(entry.name)} />
                                            }
                                            expanded={pickingRow === entry.name && <PinPicker entry={entry} collections={collections} t={t} />}
                                        />
                                    ))}
                                </div>
                            )
                        ) : (
                            <>
                                <div className={styles.sectionBar}>{t("collections")}</div>

                                {collections.length === 0 && (
                                    <div className={styles.emptyState}>{t("emptyCollections")}</div>
                                )}

                                {collections.map((col, i) => {
                                    const isRenamingThis = renaming?.kind === "collection" && renaming.id === col.id;
                                    return (
                                        <CollectionRowItem
                                            key={col.id}
                                            col={col}
                                            t={t}
                                            isFirst={i === 0}
                                            isLast={i === collections.length - 1}
                                            isRenaming={isRenamingThis}
                                            renamingValue={isRenamingThis ? renaming!.value : col.name}
                                            onEnter={() => navigateInto(col)}
                                            onRenameStart={() => startRename({ kind: "collection", id: col.id, value: col.name })}
                                            onRenameChange={v => setRenaming(r => r ? { ...r, value: v } : null)}
                                            onRenameSubmit={submitRename}
                                            onRenameCancel={cancelRename}
                                            onDelete={() => setPendingDelete({ kind: "collection", id: col.id, name: col.name })}
                                        />
                                    );
                                })}

                                <div className={styles.newItemRow}>
                                    {newColMode ? (
                                        <NameInput
                                            maxLength={MAX_COLLECTION_NAME}
                                            value={newColName}
                                            onChange={setNewColName}
                                            onSubmit={submitNewCollection}
                                            onCancel={cancelNewCollection}
                                            placeholder={t("collectionNamePlaceholder")}
                                            className={`${styles.renameInput} ${styles.renameInputFull}`}
                                        />
                                    ) : (
                                        <div className={styles.newItemTrigger} onClick={startNewCollection}>
                                            {t("newCollection")}
                                        </div>
                                    )}
                                </div>
                            </>
                        )}
                    </div>
                </>
            )}

            {/* ── Collection view ── */}
            {!collapsed && effectiveView === "collection" && viewCollection && (
                <>
                    {/* Breadcrumb + share */}
                    <div className={`${styles.sectionBarFlex} ${styles.sectionBarFlexBetween}`}>
                        <div className={styles.breadcrumbGroup}>
                            <span className={styles.breadcrumbLink} onClick={navigateRoot}>{t("collections")}</span>
                            <ChevronRightIcon size="9rem" opacity={0.4} />
                            <span className={styles.breadcrumbCurrent}>{viewCollection.name}</span>
                        </div>
                        <IconButton tip={t("tipShare")} onClick={() => exportCollection(viewCollection.id)}><ShareIcon size="14rem" /></IconButton>
                    </div>

                    {/* Filter tabs */}
                    <div className={styles.tabsBar}>
                        <div
                            className={`${styles.tab} ${viewFilterId === null ? styles.tabActive : ""}`}
                            onClick={() => setViewFilterId(null)}
                        >
                            {t("allPins")}
                        </div>

                        {viewCollection.filters.map(filter => {
                            const isRenamingThis = renaming?.kind === "filter" && renaming.filterId === filter.id;
                            return (
                                <FilterTabItem
                                    key={filter.id}
                                    filter={filter}
                                    t={t}
                                    isActive={viewFilterId === filter.id}
                                    isRenaming={isRenamingThis}
                                    renamingValue={isRenamingThis ? renaming!.value : filter.name}
                                    onSelect={() => setViewFilterId(filter.id)}
                                    onRenameStart={() => startRename({ kind: "filter", collectionId: viewCollection.id, filterId: filter.id, value: filter.name })}
                                    onRenameChange={v => setRenaming(r => r ? { ...r, value: v } : null)}
                                    onRenameSubmit={submitRename}
                                    onRenameCancel={cancelRename}
                                    onDelete={() => setPendingDelete({ kind: "filter", collectionId: viewCollection.id, filterId: filter.id, name: filter.name })}
                                />
                            );
                        })}

                        {newFilterMode ? (
                            <NameInput
                                maxLength={MAX_FILTER_NAME}
                                value={newFilterName}
                                onChange={setNewFilterName}
                                onSubmit={submitNewFilter}
                                onCancel={cancelNewFilter}
                                placeholder={t("filterNamePlaceholder")}
                                className={`${styles.tabRenameInput} ${styles.tabRenameInputWide}`}
                            />
                        ) : (
                            <div className={`${styles.tab} ${styles.tabNew}`} onClick={startNewFilter}>{t("newFilter")}</div>
                        )}
                    </div>

                    {/* Search + sort + grid/list */}
                    <div className={styles.searchRow}>
                        <SearchBox value={searchText} onChange={setSearchText} placeholder={t("searchPins")} />
                        <div className={styles.sortWrap}>
                            <Tooltip tooltip={t("tipSort")}>
                                <button
                                    onClick={() => setSortOpen(o => !o)}
                                    className={`${styles.sortBtn} ${sortMode !== "own" ? styles.sortBtnActive : ""}`}
                                >
                                    <span>{sortLabel(t, sortMode)}</span>
                                    <ChevronDownIcon size="10rem" opacity={0.6} />
                                </button>
                            </Tooltip>
                        </div>
                        {gridToggle}
                    </div>
                    {/* Sort choices open as a row under the search bar, so a short panel never cuts them off. */}
                    {sortOpen && (
                        <div className={styles.sortMenu}>
                            {SORT_MODES.map(mode => (
                                <div
                                    key={mode}
                                    className={`${styles.sortOption} ${mode === sortMode ? styles.sortOptionActive : ""}`}
                                    onClick={() => { setSortMode(mode); setSortOpen(false); }}
                                >{sortLabel(t, mode)}</div>
                            ))}
                        </div>
                    )}

                    {/* Pin list */}
                    <div ref={listRef} onWheel={handleWheel} className={`${styles.scrollArea} pinboard-scroll`}>
                        {sorted.length === 0 ? (
                            <div className={styles.emptyState}>
                                {query ? t("noMatches") : viewFilterId ? t("emptyFilter") : t("emptyPins")}
                            </div>
                        ) : layout.grid ? (
                            <><AssetGrid entries={visiblePins} currentId={currentPrefabId} />{morePins}</>
                        ) : (
                            <div className={styles.pinList}>
                                {visiblePins.map((entry, i) => {
                                    const isExpanded = expandedRow === entry.name;
                                    const isMoving = movingRow === entry.name;
                                    return (
                                        <AssetRow
                                            key={entry.name}
                                            entry={entry}
                                            subtitle={detailsOf(t, entry)}
                                            isCurrent={entry.name === currentPrefabId}
                                            actions={<>
                                                {canReorder && (
                                                    <IconButton
                                                        tip={t("tipMoveUp")}
                                                        onClick={() => i > 0 && movePin(viewCollection.id, entry.name, -1)}
                                                        className={`${styles.favActionBtn} ${i === 0 ? styles.hiddenBtn : ""}`}
                                                    ><ChevronUpIcon size="13rem" /></IconButton>
                                                )}
                                                {canReorder && (
                                                    <IconButton
                                                        tip={t("tipMoveDown")}
                                                        onClick={() => i < sorted.length - 1 && movePin(viewCollection.id, entry.name, 1)}
                                                        className={`${styles.favActionBtn} ${i === sorted.length - 1 ? styles.hiddenBtn : ""}`}
                                                    ><ChevronDownIcon size="13rem" /></IconButton>
                                                )}
                                                {viewCollection.filters.length > 0 && (
                                                    <IconButton
                                                        tip={t("tipFilters")}
                                                        onClick={() => { setMovingRow(null); setExpandedRow(isExpanded ? null : entry.name); }}
                                                        className={`${styles.favActionBtn} ${isExpanded ? styles.favActionBtnActive : ""}`}
                                                    ><FilterIcon size="13rem" /></IconButton>
                                                )}
                                                <IconButton
                                                    tip={t("tipMoveCopy")}
                                                    onClick={() => { setExpandedRow(null); setMovingRow(isMoving ? null : entry.name); }}
                                                    className={`${styles.favActionBtn} ${isMoving ? styles.favActionBtnActive : ""}`}
                                                ><MoveIcon size="13rem" /></IconButton>
                                                <IconButton
                                                    tip={t("tipRemovePin")}
                                                    onClick={() => removeWithUndo(viewCollection, entry.name)}
                                                    className={styles.favRemoveBtn}
                                                ><CloseIcon size="13rem" /></IconButton>
                                            </>}
                                            expanded={isExpanded ? (
                                                <div className={styles.checkboxList}>
                                                    {viewCollection.filters.map(filter => {
                                                        const checked = filter.assets.includes(entry.name);
                                                        return (
                                                            <div
                                                                key={filter.id}
                                                                onClick={() => checked
                                                                    ? removeFromFilter(viewCollection.id, filter.id, entry.name)
                                                                    : addToFilter(viewCollection.id, filter.id, entry.name)}
                                                                className={`${styles.checkboxRow} ${checked ? styles.checkboxRowChecked : ""}`}
                                                            >
                                                                {checked ? <CheckboxChecked /> : <CheckboxUnchecked />}
                                                                <span>{filter.name}</span>
                                                            </div>
                                                        );
                                                    })}
                                                </div>
                                            ) : isMoving ? (
                                                <MoveCopyPicker entry={entry} from={viewCollection} collections={collections} t={t} onDone={() => setMovingRow(null)} />
                                            ) : undefined}
                                        />
                                    );
                                })}
                                {morePins}
                                {!canReorder && sortMode !== "own" && !query && (
                                    <div className={styles.hint}>{t("reorderHint")}</div>
                                )}
                            </div>
                        )}
                    </div>
                </>
            )}

            {/* ── Recently Placed and Most Used views ── */}
            {!collapsed && (effectiveView === "recent" || effectiveView === "mostUsed") && (
                <>
                    <div className={`${styles.sectionBarFlex} ${styles.sectionBarFlexBetween}`}>
                        <div className={styles.segmented}>
                            {(["recent", "mostUsed"] as const).map(v => (
                                <div
                                    key={v}
                                    className={`${styles.segment} ${effectiveView === v ? styles.segmentActive : ""}`}
                                    onClick={() => effectiveView !== v && navigateTo(v)}
                                >
                                    {v === "recent" ? t("recentlyPlaced") : t("mostUsed")}
                                    <span className={styles.segmentCount}>{(v === "recent" ? recentEntries : mostUsed).length}</span>
                                </div>
                            ))}
                        </div>
                        {(effectiveView === "recent" ? recentEntries : mostUsed).length > 0 && (
                            <button className={styles.clearRecentBtn} onClick={() => setPendingClear(effectiveView)}>{t("clear")}</button>
                        )}
                    </div>

                    <div className={styles.searchRow}>
                        <SearchBox value={searchText} onChange={setSearchText} placeholder={t("searchList")} />
                        {gridToggle}
                    </div>

                    <div ref={listRef} onWheel={handleWheel} className={`${styles.scrollArea} pinboard-scroll`}>
                        {effectiveView === "recent"
                            ? autoList(recentEntries, e => detailsOf(t, e), t("emptyRecent"))
                            : autoList(mostUsed, e => detailsOf(t, e, (e.uses ?? 0) === 1 ? t("placedTimes1") : t("placedTimesN", e.uses ?? 0)), t("emptyMostUsed"))}
                    </div>
                </>
            )}

            {/* ── Quick pins ── */}
            {!collapsed && effectiveView === "quick" && (
                <div ref={listRef} onWheel={handleWheel} className={`${styles.scrollArea} pinboard-scroll`}>
                    <QuickPinsView
                        t={t}
                        slots={quickPins}
                        candidates={quickCandidates}
                        currentId={currentPrefabId}
                        currentName={currentPrefab}
                    />
                </div>
            )}

            {/* ── More: the things you don't need every day ── */}
            {!collapsed && effectiveView === "more" && (
                <div className={`${styles.scrollArea} pinboard-scroll`}>
                    <div className={styles.newItemRow}>
                        {importMode ? (
                            <NameInput
                                value={importText}
                                onChange={setImportText}
                                onSubmit={submitImport}
                                onCancel={cancelImport}
                                onBlur={cancelImport}
                                placeholder={t("importPlaceholder")}
                                className={`${styles.renameInput} ${styles.renameInputFull}`}
                            />
                        ) : (
                            <div className={styles.newItemTrigger} onClick={startImport}>
                                <ShareIcon size="12rem" />
                                <span>{t("importCollection")}</span>
                            </div>
                        )}
                        <div className={styles.newItemTrigger} onClick={() => navigateTo("district")}>
                            <FolderIcon size="12rem" />
                            <span>{t("districtOpen")}</span>
                        </div>
                    </div>
                </div>
            )}

            {/* ── Pin everything in a district ── */}
            {!collapsed && effectiveView === "district" && (
                <>
                    <div className={styles.sectionBarFlex}>
                        <span className={styles.breadcrumbLink} onClick={() => navigateTo("more")}>{t("tabMore")}</span>
                        <ChevronRightIcon size="9rem" opacity={0.4} />
                        <span className={styles.breadcrumbCurrent}>{t("districtTitle")}</span>
                    </div>
                    <div ref={listRef} onWheel={handleWheel} className={`${styles.scrollArea} pinboard-scroll`}>
                        <DistrictView
                            t={t}
                            collections={collections}
                            onDone={id => {
                                const col = id ? collections.find(c => c.id === id) : undefined;
                                if (col) navigateInto(col); else navigateRoot();
                            }}
                        />
                    </div>
                </>
            )}

            {toastView}
        </div>

        {/* ── Pop-up dialogs ── */}
        {pendingDelete !== null && (
            <ConfirmDialog
                title={pendingDelete.kind === "collection" ? t("deleteCollectionTitle") : t("deleteFilterTitle")}
                message={pendingDelete.kind === "collection"
                    ? t("deleteCollectionMessage", pendingDelete.name)
                    : t("deleteFilterMessage", pendingDelete.name)}
                confirm={t("delete")}
                cancel={t("cancel")}
                onConfirm={confirmDelete}
                onCancel={() => setPendingDelete(null)}
            />
        )}

        {pendingClear !== null && (
            <ConfirmDialog
                title={pendingClear === "recent" ? t("clearRecentTitle") : t("clearMostUsedTitle")}
                message={pendingClear === "recent" ? t("clearRecentMessage") : t("clearMostUsedMessage")}
                confirm={t("clear")}
                cancel={t("cancel")}
                onConfirm={() => { if (pendingClear === "recent") clearRecent(); else clearMostUsed(); setPendingClear(null); }}
                onCancel={() => setPendingClear(null)}
            />
        )}

        <MissingAssetDialog t={t} />
        <ShareCodeDialog t={t} />
        </>
    );
};
