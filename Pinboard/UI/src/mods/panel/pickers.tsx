import React, { useState } from "react";
import styles from "../pinboard-panel.module.css";
import { Text } from "../text";
import { CollectionData, FavouriteEntry, FilterData } from "../types";
import { addToFilter, copyPin, pinInto, removeAsset, removeFromFilter } from "../game";
import { CheckboxChecked, CheckboxUnchecked, ChevronDownIcon, ChevronRightIcon, FolderIcon } from "../icons";

// Pick a collection (or one of its filters) to pin an asset into. Used under a row
// in Recently Placed, Most Used and the search results. Lists every collection;
// open one to tick "Whole collection" or one of its filters.

export const PinPicker = ({ entry, collections, t }: { entry: FavouriteEntry; collections: CollectionData[]; t: Text }) => {
    // Which collection's filters are expanded inside the picker (one at a time).
    const [expandedCol, setExpandedCol] = useState<string | null>(null);

    // Adding to a filter pins the asset to the collection too (done on the game side).
    // Unticking only removes it from the filter (it stays pinned).
    const toggleFilter = (col: CollectionData, filter: FilterData, inFilter: boolean) => {
        if (inFilter) removeFromFilter(col.id, filter.id, entry.name);
        else addToFilter(col.id, filter.id, entry.name);
    };

    return (
        <div className={styles.checkboxList}>
            {collections.length === 0 ? (
                <div className={styles.pickNoLists}>{t("noCollectionsToPin")}</div>
            ) : collections.map(col => {
                const inCol       = col.pins.some(p => p.name === entry.name);
                const isColOpen   = expandedCol === col.id;
                return (
                    <div key={col.id} className={styles.pickCol}>
                        <div className={styles.pickColHeader} onClick={() => setExpandedCol(isColOpen ? null : col.id)}>
                            {isColOpen
                                ? <ChevronDownIcon size="11rem" opacity={0.55} />
                                : <ChevronRightIcon size="11rem" opacity={0.55} />}
                            <span className={styles.pickColName}>{col.name}</span>
                            {/* One click pins to (or unpins from) the whole collection.
                                Opening the collection shows its filters as tick boxes. */}
                            <button
                                className={inCol ? styles.pickPinBtnOn : styles.pickPinBtn}
                                onClick={e => {
                                    e.stopPropagation();
                                    if (inCol) removeAsset(col.id, entry.name);
                                    else pinInto(col.id, entry.name);
                                }}
                            >
                                {inCol ? t("pinned") : t("pin")}
                            </button>
                        </div>

                        {isColOpen && (
                            <div className={styles.pickSubList}>
                                <div
                                    onClick={() => inCol ? removeAsset(col.id, entry.name) : pinInto(col.id, entry.name)}
                                    className={`${styles.checkboxRow} ${inCol ? styles.checkboxRowChecked : ""}`}
                                >
                                    {inCol ? <CheckboxChecked /> : <CheckboxUnchecked />}
                                    <span>{t("wholeCollection")}</span>
                                </div>

                                {col.filters.map(filter => {
                                    const inFilter = filter.assets.includes(entry.name);
                                    return (
                                        <div
                                            key={filter.id}
                                            onClick={() => toggleFilter(col, filter, inFilter)}
                                            className={`${styles.checkboxRow} ${inFilter ? styles.checkboxRowChecked : ""}`}
                                        >
                                            {inFilter ? <CheckboxChecked /> : <CheckboxUnchecked />}
                                            <span>{filter.name}</span>
                                        </div>
                                    );
                                })}

                                {col.filters.length === 0 && (
                                    <div className={styles.pickNoLists}>{t("noFiltersInCollection")}</div>
                                )}
                            </div>
                        )}
                    </div>
                );
            })}
        </div>
    );
};

// Move or copy a pin into another collection.

export const MoveCopyPicker = ({ entry, from, collections, t, onDone }: {
    entry: FavouriteEntry; from: CollectionData; collections: CollectionData[]; t: Text; onDone: () => void;
}) => {
    const others = collections.filter(c => c.id !== from.id);
    return (
        <div className={styles.checkboxList}>
            {others.length === 0 && <div className={styles.pickNoLists}>{t("noOtherCollections")}</div>}
            {others.map(col => {
                const already = col.pins.some(p => p.name === entry.name);
                return (
                    <div key={col.id} className={styles.moveRow}>
                        <FolderIcon size="13rem" opacity={0.6} />
                        <span className={styles.pickColName}>{col.name}</span>
                        {already && <span className={styles.pickColDot} />}
                        <button className={styles.smallBtn} onClick={e => { e.stopPropagation(); copyPin(from.id, col.id, entry.name, true); onDone(); }}>{t("move")}</button>
                        <button className={styles.smallBtn} onClick={e => { e.stopPropagation(); copyPin(from.id, col.id, entry.name, false); onDone(); }}>{t("copy")}</button>
                    </div>
                );
            })}
        </div>
    );
};
