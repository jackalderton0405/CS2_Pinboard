import React, { useState, useRef } from "react";
import { trigger, useValue, bindValue } from "cs2/api";
import { ConfirmationDialog, Portal } from "cs2/ui";
import styles from "./pinit-panel.module.css";
import "./pinit-scrollbar.css";

interface FavouriteEntry { name: string; type: string; thumbnail?: string; displayName?: string; }
interface FilterData { id: string; name: string; assets: string[]; }
interface CollectionData { id: string; name: string; pins: FavouriteEntry[]; filters: FilterData[]; }

type PendingDelete =
  | { kind: "collection"; id: string; name: string }
  | { kind: "filter"; collectionId: string; filterId: string; name: string };

type Renaming =
  | { kind: "collection"; id: string; value: string }
  | { kind: "filter"; collectionId: string; filterId: string; value: string };

// ── Bindings ──────────────────────────────────────────────────────────────────
const panelOpen$        = bindValue<boolean>("pinIt", "panelOpen", false);
const collectionsData$  = bindValue<string>("pinIt", "collectionsData", "[]");
const currentPrefab$    = bindValue<string>("pinIt", "currentPrefabName", "");
const currentPrefabId$  = bindValue<string>("pinIt", "currentPrefabId", "");
const isPinned$         = bindValue<boolean>("pinIt", "isPinned", false);

// ── Triggers ──────────────────────────────────────────────────────────────────
const close = () => trigger("pinIt", "togglePanel");
const selectAsset = (name: string) => trigger("pinIt", "selectAsset", name);
const pinCurrentAsset = () => trigger("pinIt", "pinCurrentAsset");
const removeAsset = (collectionId: string, assetName: string) =>
    trigger("pinIt", "removeAsset", JSON.stringify({ collectionId, assetName }));
const setActiveCollection = (id: string) => trigger("pinIt", "setActiveCollection", id);
const createCollection = (name: string) => trigger("pinIt", "createCollection", name);
const deleteCollection = (id: string) => trigger("pinIt", "deleteCollection", id);
const renameCollection = (id: string, name: string) =>
    trigger("pinIt", "renameCollection", JSON.stringify({ id, name }));
const createFilter = (collectionId: string, name: string) =>
    trigger("pinIt", "createFilter", JSON.stringify({ collectionId, name }));
const deleteFilter = (collectionId: string, filterId: string) =>
    trigger("pinIt", "deleteFilter", JSON.stringify({ collectionId, filterId }));
const renameFilter = (collectionId: string, filterId: string, name: string) =>
    trigger("pinIt", "renameFilter", JSON.stringify({ collectionId, filterId, name }));
const addToFilter = (collectionId: string, filterId: string, assetName: string) =>
    trigger("pinIt", "addToFilter", JSON.stringify({ collectionId, filterId, assetName }));
const removeFromFilter = (collectionId: string, filterId: string, assetName: string) =>
    trigger("pinIt", "removeFromFilter", JSON.stringify({ collectionId, filterId, assetName }));

// ── Brand colour (no game CSS variable equivalent) ────────────────────────────
const GOLD     = "#c8a84b";
const GOLD_BG  = "rgba(200, 168, 75, 0.18)";

// ── SVG icon helpers ──────────────────────────────────────────────────────────
const svgStyle = (size = "14rem", opacity = 1): React.CSSProperties => ({
    display: "block", flexShrink: 0, width: size, height: size, opacity,
});

// ── Icons ─────────────────────────────────────────────────────────────────────

const CloseIcon = ({ size = "14rem", opacity = 1 }: { size?: string; opacity?: number }) => (
    <svg style={svgStyle(size, opacity)} viewBox="0 0 24 24" fill="none">
        <path d="M18 6L6 18M6 6l12 12" stroke="white" strokeWidth="2.2" strokeLinecap="round"/>
    </svg>
);

const ChevronDownIcon = ({ size = "14rem", opacity = 1 }: { size?: string; opacity?: number }) => (
    <svg style={svgStyle(size, opacity)} viewBox="0 0 24 24" fill="none">
        <path d="M6 9l6 6 6-6" stroke="white" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round"/>
    </svg>
);

const ChevronRightIcon = ({ size = "14rem", opacity = 1 }: { size?: string; opacity?: number }) => (
    <svg style={svgStyle(size, opacity)} viewBox="0 0 24 24" fill="none">
        <path d="M9 6l6 6-6 6" stroke="white" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round"/>
    </svg>
);

const SearchIcon = ({ size = "14rem", opacity = 1 }: { size?: string; opacity?: number }) => (
    <svg style={svgStyle(size, opacity)} viewBox="0 0 24 24" fill="none">
        <circle cx="10.5" cy="10.5" r="6.5" stroke="white" strokeWidth="2"/>
        <path d="M15.5 15.5L20 20" stroke="white" strokeWidth="2.2" strokeLinecap="round"/>
    </svg>
);

const FolderIcon = ({ size = "14rem", opacity = 1 }: { size?: string; opacity?: number }) => (
    <svg style={svgStyle(size, opacity)} viewBox="0 0 24 24" fill="none">
        <path d="M3 8a2 2 0 0 1 2-2h4l2 2h8a2 2 0 0 1 2 2v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8z"
              stroke="white" strokeWidth="1.8" strokeLinejoin="round"/>
    </svg>
);

const PinIcon = ({ size = "14rem" }: { size?: string }) => (
    <svg style={svgStyle(size)} viewBox="0 0 100 100" fill="none">
        <path fillRule="evenodd"
            d="M50 5 C27 5 10 22 10 43 C10 68 50 95 50 95 C50 95 90 68 90 43 C90 22 73 5 50 5 Z M50 59 C50 59 31 46 31 34 C31 26 37 20 44 20 C47 20 49 22 50 25 C51 22 53 20 56 20 C63 20 69 26 69 34 C69 46 50 59 50 59 Z"
            fill="white"/>
    </svg>
);

const RemoveIcon = ({ size = "14rem" }: { size?: string }) => (
    <svg style={svgStyle(size)} viewBox="0 0 24 24" fill="none">
        <path d="M18 6L6 18M6 6l12 12" stroke="white" strokeWidth="2.2" strokeLinecap="round"/>
    </svg>
);

const FilterIcon = ({ size = "13rem" }: { size?: string }) => (
    <svg style={svgStyle(size)} viewBox="0 0 24 24" fill="none">
        <path d="M3 4h18l-7 9v6l-4-2V13L3 4z" stroke="white" strokeWidth="1.8" strokeLinejoin="round" strokeLinecap="round"/>
    </svg>
);

const PencilIcon = ({ size = "12rem" }: { size?: string }) => (
    <svg style={svgStyle(size)} viewBox="0 0 24 24" fill="none">
        <path d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 0 1 3.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z"
              stroke="white" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"/>
    </svg>
);

const CheckboxChecked = ({ size = "15rem" }: { size?: string }) => (
    <svg style={svgStyle(size)} viewBox="0 0 24 24" fill="none">
        <rect x="3" y="3" width="18" height="18" rx="3" fill={GOLD_BG} stroke={GOLD} strokeWidth="1.5"/>
        <path d="M7 12l4 4 6-7" stroke={GOLD} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
    </svg>
);

const CheckboxUnchecked = ({ size = "15rem" }: { size?: string }) => (
    <svg style={svgStyle(size)} viewBox="0 0 24 24" fill="none">
        <rect x="3" y="3" width="18" height="18" rx="3" stroke="rgba(255,255,255,0.25)" strokeWidth="1.5"/>
    </svg>
);

// ── CollectionRowItem ─────────────────────────────────────────────────────────

interface CollectionRowProps {
    col: CollectionData;
    isRenaming: boolean;
    renamingValue: string;
    onEnter: () => void;
    onRenameStart: () => void;
    onRenameChange: (v: string) => void;
    onRenameSubmit: () => void;
    onRenameCancel: () => void;
    onDelete: () => void;
}

const CollectionRowItem = ({
    col, isRenaming, renamingValue,
    onEnter, onRenameStart, onRenameChange, onRenameSubmit, onRenameCancel, onDelete,
}: CollectionRowProps) => (
    <div className={`${styles.collectionRow} ${isRenaming ? styles.collectionRowRenaming : ""}`}>
        <FolderIcon size="15rem" opacity={0.7} />

        {isRenaming ? (
            <input
                autoFocus
                maxLength={30}
                value={renamingValue}
                onChange={e => onRenameChange(e.target.value)}
                onKeyDown={e => {
                    if (e.key === "Enter") onRenameSubmit();
                    if (e.key === "Escape") onRenameCancel();
                }}
                onBlur={onRenameSubmit}
                className={styles.renameInput}
                onClick={e => e.stopPropagation()}
            />
        ) : (
            <span onClick={onEnter} className={styles.collectionName}>{col.name}</span>
        )}

        <span className={styles.collectionCount}>
            {col.pins.length} {col.pins.length === 1 ? "pin" : "pins"}
        </span>

        <div className={styles.rowActions}>
            <button
                onClick={e => { e.stopPropagation(); onRenameStart(); }}
                className={styles.iconBtn}
            ><PencilIcon /></button>
            <button
                onClick={e => { e.stopPropagation(); onDelete(); }}
                className={`${styles.iconBtn} ${styles.iconBtnDelete}`}
            ><CloseIcon size="12rem" /></button>
        </div>

        {!isRenaming && <ChevronRightIcon size="12rem" opacity={0.35} />}
    </div>
);

// ── FilterTabItem ─────────────────────────────────────────────────────────────

interface FilterTabProps {
    filter: FilterData;
    isActive: boolean;
    isRenaming: boolean;
    renamingValue: string;
    onSelect: () => void;
    onRenameStart: () => void;
    onRenameChange: (v: string) => void;
    onRenameSubmit: () => void;
    onRenameCancel: () => void;
    onDelete: () => void;
}

const FilterTabItem = ({
    filter, isActive, isRenaming, renamingValue,
    onSelect, onRenameStart, onRenameChange, onRenameSubmit, onRenameCancel, onDelete,
}: FilterTabProps) => (
    <div className={`${styles.tab} ${isActive ? styles.tabActive : ""}`}>
        {isRenaming ? (
            <input
                autoFocus
                maxLength={20}
                value={renamingValue}
                onChange={e => onRenameChange(e.target.value)}
                onKeyDown={e => {
                    if (e.key === "Enter") onRenameSubmit();
                    if (e.key === "Escape") onRenameCancel();
                }}
                onBlur={onRenameSubmit}
                className={styles.tabRenameInput}
                onClick={e => e.stopPropagation()}
            />
        ) : (
            <span onClick={onSelect}>{filter.name}</span>
        )}
        {!isRenaming && (
            <div className={styles.tabActions}>
                <button
                    onClick={e => { e.stopPropagation(); onRenameStart(); }}
                    className={`${styles.iconBtn} ${styles.iconBtnSm}`}
                ><PencilIcon size="10rem" /></button>
                <button
                    onClick={e => { e.stopPropagation(); onDelete(); }}
                    className={`${styles.iconBtn} ${styles.iconBtnSm} ${styles.iconBtnDelete}`}
                ><CloseIcon size="10rem" /></button>
            </div>
        )}
    </div>
);

// ── FavouriteRow ──────────────────────────────────────────────────────────────

interface RowProps {
    name: string;
    displayName?: string;
    thumbnail?: string;
    collectionId: string;
    filters: FilterData[];
    isExpanded: boolean;
    onToggleExpand: () => void;
}

const FavouriteRow = ({ name, displayName, thumbnail, collectionId, filters, isExpanded, onToggleExpand }: RowProps) => (
    <div className={styles.favRow}>
        <div className={styles.favRowInner}>
            {thumbnail ? (
                <img src={thumbnail} className={styles.favThumb} />
            ) : (
                <div className={styles.favThumbEmpty}>{"?"}</div>
            )}
            <span onClick={() => selectAsset(name)} className={styles.favName}>
                {displayName || name}
            </span>

            {filters.length > 0 && (
                <button
                    onClick={e => { e.stopPropagation(); onToggleExpand(); }}
                    title="Assign to filters"
                    className={`${styles.favActionBtn} ${isExpanded ? styles.favActionBtnActive : ""}`}
                >
                    <FilterIcon size="13rem" />
                </button>
            )}

            <button
                onClick={e => { e.stopPropagation(); removeAsset(collectionId, name); }}
                className={styles.favRemoveBtn}
            ><RemoveIcon size="13rem" /></button>
        </div>

        {isExpanded && (
            <div className={styles.checkboxList}>
                {filters.map(filter => {
                    const checked = filter.assets.includes(name);
                    return (
                        <div
                            key={filter.id}
                            onClick={() => checked
                                ? removeFromFilter(collectionId, filter.id, name)
                                : addToFilter(collectionId, filter.id, name)}
                            className={`${styles.checkboxRow} ${checked ? styles.checkboxRowChecked : ""}`}
                        >
                            {checked ? <CheckboxChecked /> : <CheckboxUnchecked />}
                            <span>{filter.name}</span>
                        </div>
                    );
                })}
            </div>
        )}
    </div>
);

// ── PinItPanel ────────────────────────────────────────────────────────────────

export const PinItPanel = () => {
    const isOpen           = useValue(panelOpen$);
    const collectionsJson  = useValue(collectionsData$);
    const currentPrefab    = useValue(currentPrefab$);
    const currentPrefabId  = useValue(currentPrefabId$);
    const isPinned         = useValue(isPinned$);

    const [panelView, setPanelView]         = useState<"root" | "collection">("root");
    const [viewCollectionId, setViewColId]  = useState<string | null>(null);
    const [viewFilterId, setViewFilterId]   = useState<string | null>(null);
    const [expandedRow, setExpandedRow]     = useState<string | null>(null);
    const [searchText, setSearchText]       = useState("");
    const [collapsed, setCollapsed]         = useState(false);
    const [sortDir, setSortDir]             = useState<"asc" | "desc" | null>(null);
    const [pendingDelete, setPendingDelete] = useState<PendingDelete | null>(null);
    const [renaming, setRenaming]           = useState<Renaming | null>(null);
    const [newColMode, setNewColMode]       = useState(false);
    const [newColName, setNewColName]       = useState("");
    const [newFilterMode, setNewFilterMode] = useState(false);
    const [newFilterName, setNewFilterName] = useState("");

    const listRef    = useRef<HTMLDivElement>(null);
    const colListRef = useRef<HTMLDivElement>(null);

    if (!isOpen) return null;

    let collections: CollectionData[] = [];
    try { collections = JSON.parse(collectionsJson); } catch {}

    const viewCollection = panelView === "collection"
        ? (collections.find(c => c.id === viewCollectionId) ?? null)
        : null;

    const effectiveView = panelView === "collection" && !viewCollection ? "root" : panelView;

    const navigateInto = (col: CollectionData) => {
        setViewColId(col.id);
        setPanelView("collection");
        setViewFilterId(null);
        setExpandedRow(null);
        setSearchText("");
        setActiveCollection(col.id);
    };

    const navigateRoot = () => {
        setPanelView("root");
        setViewFilterId(null);
        setExpandedRow(null);
        setSearchText("");
        setSortDir(null);
    };

    const submitRename = () => {
        if (!renaming) return;
        const v = renaming.value.trim();
        if (v) {
            if (renaming.kind === "collection")
                renameCollection(renaming.id, v);
            else
                renameFilter(renaming.collectionId, renaming.filterId, v);
        }
        setRenaming(null);
    };

    const submitNewCollection = () => {
        const t = newColName.trim();
        if (t) createCollection(t);
        setNewColName("");
        setNewColMode(false);
    };

    const submitNewFilter = () => {
        const t = newFilterName.trim();
        if (t && viewCollectionId) createFilter(viewCollectionId, t);
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

    const handleWheel    = (e: React.WheelEvent) => { if (listRef.current)    listRef.current.scrollTop    += e.deltaY; };
    const handleColWheel = (e: React.WheelEvent) => { if (colListRef.current) colListRef.current.scrollTop += e.deltaY; };

    const activeFilter = viewCollection?.filters.find(f => f.id === viewFilterId) ?? null;
    const basePins     = viewCollection?.pins ?? [];
    const filtered     = activeFilter ? basePins.filter(p => activeFilter.assets.includes(p.name)) : basePins;
    const query      = searchText.trim().toLowerCase();
    const searched   = query ? filtered.filter(p => (p.displayName || p.name).toLowerCase().includes(query)) : filtered;
    const sorted     = sortDir === null ? searched : [...searched].sort((a, b) => {
        const na = (a.displayName || a.name).toLowerCase();
        const nb = (b.displayName || b.name).toLowerCase();
        return sortDir === "asc" ? na.localeCompare(nb) : nb.localeCompare(na);
    });

    return (
        <>
        <div className={`${styles.panel} ${collapsed ? styles.panelCollapsed : styles.panelExpanded}`}>

            {/* ── Header ── */}
            <div className={`${styles.header} ${collapsed ? styles.headerCollapsed : ""}`}>
                <PinIcon size="18rem" />
                <span className={styles.headerTitle}>{"PinIt"}</span>

                {effectiveView === "collection" && currentPrefab && (
                    <div
                        className={isPinned ? styles.pinBtnPinned : styles.pinBtn}
                        onClick={isPinned
                            ? () => removeAsset(viewCollectionId!, currentPrefabId)
                            : pinCurrentAsset}
                    >
                        {isPinned ? "Pinned" : "+ Pin"}
                    </div>
                )}

                <button className={styles.ctrlBtn} onClick={() => setCollapsed(c => !c)}>
                    {collapsed ? <ChevronRightIcon /> : <ChevronDownIcon />}
                </button>
                <button className={styles.ctrlBtn} onClick={close}>
                    <CloseIcon />
                </button>
            </div>

            {/* ── Root view ── */}
            {!collapsed && effectiveView === "root" && (
                <>
                    <div className={styles.sectionBar}>{"COLLECTIONS"}</div>

                    <div ref={colListRef} onWheel={handleColWheel} className={`${styles.scrollArea} pinit-scroll`}>
                        {collections.length === 0 && (
                            <div className={styles.emptyState}>{"No collections yet."}</div>
                        )}

                        {collections.map(col => {
                            const isRenamingThis = renaming?.kind === "collection" && renaming.id === col.id;
                            return (
                                <CollectionRowItem
                                    key={col.id}
                                    col={col}
                                    isRenaming={isRenamingThis}
                                    renamingValue={isRenamingThis ? (renaming as any).value : col.name}
                                    onEnter={() => navigateInto(col)}
                                    onRenameStart={() => setRenaming({ kind: "collection", id: col.id, value: col.name })}
                                    onRenameChange={v => setRenaming(r => r ? { ...r, value: v } : null)}
                                    onRenameSubmit={submitRename}
                                    onRenameCancel={() => setRenaming(null)}
                                    onDelete={() => setPendingDelete({ kind: "collection", id: col.id, name: col.name })}
                                />
                            );
                        })}

                        <div className={styles.newItemRow}>
                            {newColMode ? (
                                <input
                                    autoFocus
                                    maxLength={30}
                                    value={newColName}
                                    onChange={e => setNewColName(e.target.value)}
                                    onKeyDown={e => {
                                        if (e.key === "Enter") submitNewCollection();
                                        if (e.key === "Escape") { setNewColName(""); setNewColMode(false); }
                                    }}
                                    onBlur={submitNewCollection}
                                    placeholder="Collection name..."
                                    className={`${styles.renameInput} ${styles.renameInputFull}`}
                                />
                            ) : (
                                <div className={styles.newItemTrigger} onClick={() => setNewColMode(true)}>
                                    {"+ New Collection"}
                                </div>
                            )}
                        </div>
                    </div>
                </>
            )}

            {/* ── Collection view ── */}
            {!collapsed && effectiveView === "collection" && (
                <>
                    {/* Breadcrumb */}
                    <div className={styles.sectionBarFlex}>
                        <span
                            className={styles.breadcrumbLink}
                            onClick={navigateRoot}
                        >{"Collections"}</span>
                        <ChevronRightIcon size="9rem" opacity={0.4} />
                        <span className={styles.breadcrumbCurrent}>{viewCollection?.name ?? ""}</span>
                    </div>

                    {/* Filter tabs */}
                    <div className={styles.tabsBar}>
                        <div
                            className={`${styles.tab} ${viewFilterId === null ? styles.tabActive : ""}`}
                            onClick={() => setViewFilterId(null)}
                        >
                            {"All Pins"}
                        </div>

                        {viewCollection?.filters.map(filter => {
                            const isRenamingThis = renaming?.kind === "filter" && renaming.filterId === filter.id;
                            return (
                                <FilterTabItem
                                    key={filter.id}
                                    filter={filter}
                                    isActive={viewFilterId === filter.id}
                                    isRenaming={isRenamingThis}
                                    renamingValue={isRenamingThis ? (renaming as any).value : filter.name}
                                    onSelect={() => setViewFilterId(filter.id)}
                                    onRenameStart={() => setRenaming({ kind: "filter", collectionId: viewCollectionId!, filterId: filter.id, value: filter.name })}
                                    onRenameChange={v => setRenaming(r => r ? { ...r, value: v } : null)}
                                    onRenameSubmit={submitRename}
                                    onRenameCancel={() => setRenaming(null)}
                                    onDelete={() => setPendingDelete({ kind: "filter", collectionId: viewCollectionId!, filterId: filter.id, name: filter.name })}
                                />
                            );
                        })}

                        {newFilterMode ? (
                            <input
                                autoFocus
                                maxLength={20}
                                value={newFilterName}
                                onChange={e => setNewFilterName(e.target.value)}
                                onKeyDown={e => {
                                    if (e.key === "Enter") submitNewFilter();
                                    if (e.key === "Escape") { setNewFilterName(""); setNewFilterMode(false); }
                                }}
                                onBlur={submitNewFilter}
                                placeholder="Filter name..."
                                className={`${styles.tabRenameInput} ${styles.tabRenameInputWide}`}
                            />
                        ) : (
                            <div
                                className={`${styles.tab} ${styles.tabNew}`}
                                onClick={() => setNewFilterMode(true)}
                            >{"+ New Filter"}</div>
                        )}
                    </div>

                    {/* Search + Sort */}
                    <div className={styles.searchRow}>
                        <div className={styles.searchWrap}>
                            <div className={styles.searchIconWrap}>
                                <SearchIcon size="13rem" opacity={0.5} />
                            </div>
                            <input
                                value={searchText}
                                onChange={e => setSearchText(e.target.value)}
                                placeholder="Search pins..."
                                className={styles.searchInput}
                            />
                            {searchText && (
                                <div className={styles.searchClear} onClick={() => setSearchText("")}>
                                    <CloseIcon size="12rem" />
                                </div>
                            )}
                        </div>
                        <button
                            onClick={() => setSortDir(d => d === null ? "asc" : d === "asc" ? "desc" : null)}
                            className={`${styles.sortBtn} ${sortDir !== null ? styles.sortBtnActive : ""}`}
                        >
                            {sortDir === "desc" ? "Z→A" : "A→Z"}
                        </button>
                    </div>

                    {/* Pin list */}
                    <div ref={listRef} onWheel={handleWheel} className={`${styles.scrollArea} pinit-scroll`}>
                        {sorted.length === 0 ? (
                            <div className={styles.emptyState}>
                                {query ? "No matches." : viewFilterId ? "No pins in this filter." : "No pins yet."}
                            </div>
                        ) : (
                            <div className={styles.pinList}>
                                {sorted.map(entry => (
                                    <FavouriteRow
                                        key={entry.name}
                                        name={entry.name}
                                        displayName={entry.displayName}
                                        thumbnail={entry.thumbnail}
                                        collectionId={viewCollectionId!}
                                        filters={viewCollection?.filters ?? []}
                                        isExpanded={expandedRow === entry.name}
                                        onToggleExpand={() => setExpandedRow(expandedRow === entry.name ? null : entry.name)}
                                    />
                                ))}
                            </div>
                        )}
                    </div>
                </>
            )}
        </div>

        {/* ── Confirmation dialog — native game dialog, portaled out of the overflow:hidden panel ── */}
        {pendingDelete !== null && (
            <Portal>
                <ConfirmationDialog
                    title={pendingDelete.kind === "collection" ? "Delete Collection" : "Delete Filter"}
                    message={pendingDelete.kind === "collection"
                        ? `Delete collection '${pendingDelete.name}'? All pins and filters inside will be removed.`
                        : `Delete filter '${pendingDelete.name}'? Pins will remain in the collection.`}
                    confirm={"Delete"}
                    cancel={"Cancel"}
                    multiline={true}
                    onConfirm={() => confirmDelete()}
                    onCancel={() => setPendingDelete(null)}
                />
            </Portal>
        )}
        </>
    );
};
