import React, { useState, useRef } from "react";
import { trigger, useValue, bindValue } from "cs2/api";
import { Button } from "cs2/ui";
import { ModuleRegistryExtend } from "cs2/modding";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";
import styles from "./pinboard-add-button.module.css";

interface FilterData { id: string; name: string; assets: string[]; }
interface CollectionData { id: string; name: string; pins: { name: string }[]; filters: FilterData[]; }

// ── Icons ─────────────────────────────────────────────────────────────────────

const PinIcon = () => (
    <svg style={{ display: "block", width: "18rem", height: "18rem" }} viewBox="0 0 100 100" fill="none">
        <path fillRule="evenodd"
            d="M50 5 C27 5 10 22 10 43 C10 68 50 95 50 95 C50 95 90 68 90 43 C90 22 73 5 50 5 Z M50 59 C50 59 31 46 31 34 C31 26 37 20 44 20 C47 20 49 22 50 25 C51 22 53 20 56 20 C63 20 69 26 69 34 C69 46 50 59 50 59 Z"
            fill="white"/>
    </svg>
);

const FilterIcon = () => (
    <svg style={{ display: "block", width: "18rem", height: "18rem" }} viewBox="0 0 24 24" fill="none">
        <path d="M3 4h18l-7 9v6l-4-2V13L3 4z" stroke="white" strokeWidth="1.8" strokeLinejoin="round" strokeLinecap="round"/>
    </svg>
);

const CloseIcon = () => (
    <svg style={{ display: "block", width: "12rem", height: "12rem" }} viewBox="0 0 24 24" fill="none">
        <path d="M18 6L6 18M6 6l12 12" stroke="white" strokeWidth="2" strokeLinecap="round"/>
    </svg>
);

const CheckboxChecked = () => (
    <svg style={{ display: "block", flexShrink: 0, width: "14rem", height: "14rem" }} viewBox="0 0 24 24" fill="none">
        <rect x="3" y="3" width="18" height="18" rx="3" fill="rgba(200,168,75,0.25)" stroke="#c8a84b" strokeWidth="1.5"/>
        <path d="M7 12l4 4 6-7" stroke="#c8a84b" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
    </svg>
);

const CheckboxUnchecked = () => (
    <svg style={{ display: "block", flexShrink: 0, width: "14rem", height: "14rem" }} viewBox="0 0 24 24" fill="none">
        <rect x="3" y="3" width="18" height="18" rx="3" stroke="rgba(255,255,255,0.25)" strokeWidth="1.5"/>
    </svg>
);

const ChevronDownIcon = () => (
    <svg style={{ display: "block", width: "10rem", height: "10rem" }} viewBox="0 0 24 24" fill="none">
        <path d="M6 9l6 6 6-6" stroke="white" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round"/>
    </svg>
);

const ChevronRightIcon = () => (
    <svg style={{ display: "block", width: "10rem", height: "10rem" }} viewBox="0 0 24 24" fill="none">
        <path d="M9 6l6 6-6 6" stroke="white" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round"/>
    </svg>
);

// ── Bindings ──────────────────────────────────────────────────────────────────

const currentPrefabId$    = bindValue<string>("pinboard", "currentPrefabId", "");
const isPinned$           = bindValue<boolean>("pinboard", "isPinned", false);
const collectionsData$    = bindValue<string>("pinboard", "collectionsData", "[]");
const activeCollectionId$ = bindValue<string>("pinboard", "activeCollectionId", "");

const togglePin           = () => trigger("pinboard", "togglePin");
const setActiveCollection = (id: string) => trigger("pinboard", "setActiveCollection", id);
const addToFilter    = (colId: string, filterId: string, asset: string) =>
    trigger("pinboard", "addToFilter", JSON.stringify({ collectionId: colId, filterId, assetName: asset }));
const removeFromFilter = (colId: string, filterId: string, asset: string) =>
    trigger("pinboard", "removeFromFilter", JSON.stringify({ collectionId: colId, filterId, assetName: asset }));

// ── Collections overlay panel ─────────────────────────────────────────────────

interface CollectionsPanelProps {
    collections: CollectionData[];
    activeColId: string;
    currentPrefabId: string;
    bottom: number;
    left: number;
    onClose: () => void;
}

const CollectionsPanel = ({ collections, activeColId, currentPrefabId, bottom, left, onClose }: CollectionsPanelProps) => {
    const [localActiveId, setLocalActiveId] = useState(activeColId);
    const [colOpen, setColOpen] = useState(false);

    const activeCol = collections.find(c => c.id === localActiveId) ?? collections[0];

    const handleCollectionSelect = (id: string) => {
        setLocalActiveId(id);
        setActiveCollection(id);
        setColOpen(false);
    };

    return (
        <div
            className={styles.panel}
            style={{ position: "fixed", bottom: `${bottom}px`, left: `${left}px`, zIndex: 9999, pointerEvents: "auto" }}
        >
            {/* Header */}
            <div className={styles.header}>
                <span className={styles.headerLabel}>PINBOARD</span>
                <div className={styles.closeBtn} onClick={onClose}><CloseIcon /></div>
            </div>

            {/* Collection selector — inline expand, only if multiple collections */}
            {collections.length > 1 && (
                <div className={styles.colSelectorWrap}>
                    <div
                        className={`${styles.colSelectorTrigger} ${colOpen ? styles.colSelectorTriggerOpen : ""}`}
                        onClick={() => setColOpen(o => !o)}
                    >
                        <span className={styles.colSelectorName}>{activeCol?.name ?? "None"}</span>
                        {colOpen ? <ChevronDownIcon /> : <ChevronRightIcon />}
                    </div>

                    {colOpen && collections.map(col => (
                        <div
                            key={col.id}
                            className={`${styles.colOption} ${col.id === localActiveId ? styles.colOptionActive : ""}`}
                            onClick={() => handleCollectionSelect(col.id)}
                        >
                            {col.name}
                        </div>
                    ))}
                </div>
            )}

            {/* Filter checkboxes */}
            <div>
                {!activeCol || activeCol.filters.length === 0 ? (
                    <div className={styles.listEmpty}>No filters in this collection</div>
                ) : (
                    activeCol.filters.map(filter => {
                        const inFilter = filter.assets.includes(currentPrefabId);
                        return (
                            <div
                                key={filter.id}
                                className={`${styles.listItem} ${inFilter ? styles.listItemInList : ""}`}
                                onClick={() => inFilter
                                    ? removeFromFilter(activeCol.id, filter.id, currentPrefabId)
                                    : addToFilter(activeCol.id, filter.id, currentPrefabId)}
                            >
                                {inFilter ? <CheckboxChecked /> : <CheckboxUnchecked />}
                                <span className={styles.listItemName}>{filter.name}</span>
                            </div>
                        );
                    })
                )}
            </div>
        </div>
    );
};

// ── HOC ───────────────────────────────────────────────────────────────────────

export const PinboardToolOptionsWrapper: ModuleRegistryExtend = (Component: any) => {
    return (props) => {
        const [overlayOpen, setOverlayOpen] = useState(false);
        const [panelBottom, setPanelBottom] = useState(0);
        const [panelLeft,   setPanelLeft]   = useState(0);
        const sectionRef                    = useRef<HTMLDivElement>(null);

        const currentPrefabId = useValue(currentPrefabId$);
        const isPinned        = useValue(isPinned$);
        const collectionsJson = useValue(collectionsData$);
        const activeColId     = useValue(activeCollectionId$);

        let collections: CollectionData[] = [];
        try { collections = JSON.parse(collectionsJson); } catch {}

        const handleFolderClick = () => {
            if (!overlayOpen && sectionRef.current) {
                const rect = sectionRef.current.getBoundingClientRect();
                setPanelBottom(window.innerHeight - rect.top + 24);
                setPanelLeft(rect.left);
            }
            setOverlayOpen(o => !o);
        };

        const vcr    = VanillaComponentResolver.instance;
        const result: JSX.Element = Component();

        if (currentPrefabId) {
            result.props.children?.unshift(
                <div key="pinboard-section-wrap" ref={sectionRef}>
                    <vcr.Section title="Pinboard">
                        <Button
                            variant="icon"
                            selected={isPinned}
                            onSelect={togglePin}
                            focusKey={vcr.FOCUS_DISABLED}
                            className={vcr.toolButtonTheme.button}
                        >
                            <PinIcon />
                        </Button>
                        {collections.length > 0 && (
                            <Button
                                variant="icon"
                                selected={overlayOpen}
                                onSelect={handleFolderClick}
                                focusKey={vcr.FOCUS_DISABLED}
                                className={vcr.toolButtonTheme.button}
                            >
                                <FilterIcon />
                            </Button>
                        )}
                    </vcr.Section>
                </div>
            );
        }

        return (
            <>
                {result}
                {overlayOpen && currentPrefabId && collections.length > 0 && (
                    <CollectionsPanel
                        collections={collections}
                        activeColId={activeColId}
                        currentPrefabId={currentPrefabId}
                        bottom={panelBottom}
                        left={panelLeft}
                        onClose={() => setOverlayOpen(false)}
                    />
                )}
            </>
        );
    };
};
