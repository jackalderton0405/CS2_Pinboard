import React, { useState } from "react";
import { useValue } from "cs2/api";
import { Tooltip } from "cs2/ui";
import styles from "./in-game-sections.module.css";
import { useText } from "./text";
import { parseShared } from "./parse-cache";
import { CollectionData, InspectedAsset } from "./types";
import { collectionsData$, inspected$, addToFilter, pinInto, removeAsset, removeFromFilter } from "./game";
import { CheckboxChecked, CheckboxUnchecked } from "./icons";

// The game's own arrow, as used by its info panel rows (Transform Tool, for example).
const GameArrow = ({ open }: { open: boolean }) => (
    <img
        className={styles.infoArrow}
        src={open ? "Media/Glyphs/ThickStrokeArrowDown.svg" : "Media/Glyphs/ThickStrokeArrowRight.svg"}
    />
);

// A smaller, bolder cross than the panel's, coloured like the text around it.
const CloseIcon = () => (
    <svg style={{ display: "block", width: "9rem", height: "9rem" }} viewBox="0 0 24 24" fill="none">
        <path d="M6 6l12 12M18 6L6 18" stroke="currentColor" strokeWidth="3" strokeLinecap="round"/>
    </svg>
);

// ── Section in the game's side info panel ─────────────────────────────────────
// Shown when you click something in the city (a school, a tree, a road...). Folds
// open like the game's own sections, and lists every collection with a pin switch.

// Must match PinboardInfoSection.kGroup in the C# code.
export const INFO_SECTION_GROUP = "Pinboard.Systems.PinboardInfoSection";

// Remembered while the game is running, so the section stays the way the player left it.
let infoOpenRemembered = false;

export const PinboardInfoSection = () => {
    const t = useText();
    const [open, setOpenState] = useState(infoOpenRemembered);
    const setOpen = (v: boolean) => { infoOpenRemembered = v; setOpenState(v); };
    const [adding, setAdding] = useState(false);

    const inspectedJson   = useValue(inspected$);
    const collectionsJson = useValue(collectionsData$);

    const inspected = parseShared<InspectedAsset | null>("inspected", inspectedJson, null);
    const collections = parseShared<CollectionData[]>("collections", collectionsJson, []);

    if (!inspected) return null;
    const asset = inspected.name;
    // Only the collections this asset is in are listed; the rest sit in the "Add to" list.
    const pinnedIn = collections.filter(c => c.pins.some(p => p.name === asset));
    const notIn    = collections.filter(c => !c.pins.some(p => p.name === asset));
    const pinnedCount = pinnedIn.length;

    return (
        <div className={styles.infoSection}>
            <div className={styles.infoHeader} onClick={() => { setOpen(!open); setAdding(false); }}>
                <span className={styles.infoTitle}>{t("title")}</span>
                <span className={styles.infoSummary}>
                    {pinnedCount === 0 ? t("infoNotPinned") : pinnedCount === 1 ? t("infoPinnedIn1") : t("infoPinnedInN", pinnedCount)}
                </span>
                <GameArrow open={open} />
            </div>
            {open && (
                <div className={styles.infoList}>
                    {inspected.zoned && <div className={styles.infoEmpty}>{t("zonedNote")}</div>}
                    {pinnedCount === 0 && <div className={styles.infoEmpty}>{t("infoNoneYet")}</div>}
                    {pinnedIn.map(col => (
                        <div key={col.id} className={styles.infoColGroup}>
                            <div className={styles.infoColHead}>
                                <span className={styles.infoColName}>{col.name}</span>
                                <Tooltip tooltip={t("tipInfoUnpin")}>
                                    <button className={styles.infoUnpin} onClick={() => removeAsset(col.id, asset)}>
                                        {t("pinned")} <CloseIcon />
                                    </button>
                                </Tooltip>
                            </div>
                            {/* Its filters as tick boxes. */}
                            {col.filters.length > 0 && (
                                <div className={styles.infoFilters}>
                                    {col.filters.map(filter => {
                                        const inFilter = filter.assets.includes(asset);
                                        return (
                                            <div
                                                key={filter.id}
                                                className={`${styles.tbFilter} ${inFilter ? styles.tbFilterOn : ""}`}
                                                onClick={() => inFilter
                                                    ? removeFromFilter(col.id, filter.id, asset)
                                                    : addToFilter(col.id, filter.id, asset)}
                                            >
                                                {inFilter ? <CheckboxChecked size="14rem" fillOpacity={0.25} /> : <CheckboxUnchecked size="14rem" />}
                                                <span>{filter.name}</span>
                                            </div>
                                        );
                                    })}
                                </div>
                            )}
                        </div>
                    ))}
                    {collections.length === 0 && <div className={styles.infoEmpty}>{t("noCollectionsToPin")}</div>}
                    {notIn.length > 0 && (
                        <Tooltip tooltip={t("tipInfoAdd")}>
                            <button
                                className={`${styles.infoAdd} ${adding ? styles.infoAddOpen : ""}`}
                                onClick={() => setAdding(a => !a)}
                            >
                                <span className={styles.infoColName}>{t("infoAddTo")}</span>
                                <GameArrow open={adding} />
                            </button>
                        </Tooltip>
                    )}
                    {adding && notIn.length > 0 && (
                        <div className={styles.tbColList}>
                            {notIn.map(col => (
                                <div
                                    key={col.id}
                                    className={styles.infoAddOption}
                                    onClick={() => { pinInto(col.id, asset); setAdding(false); }}
                                >
                                    <span className={styles.infoColName}>{col.name}</span>
                                    <span className={styles.infoColState}>{t("pin")}</span>
                                </div>
                            ))}
                        </div>
                    )}
                </div>
            )}
        </div>
    );
};
