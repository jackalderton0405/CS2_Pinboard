import React, { useState } from "react";
import { useValue } from "cs2/api";
import { Tooltip } from "cs2/ui";
import { ModuleRegistryExtend } from "cs2/modding";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";
import styles from "./in-game-sections.module.css";
import { useText } from "./text";
import { parseShared } from "./parse-cache";
import { CollectionData, FavouriteEntry } from "./types";
import { nameOf } from "./assets";
import {
    activeCollectionId$, collectionsData$, currentPrefabId$, isPinned$, quickPins$,
    addToFilter, removeFromFilter, selectAsset, setActiveCollection, setQuickPin, togglePin,
} from "./game";
import { CheckboxChecked, CheckboxUnchecked, ChevronDownIcon, PinIcon } from "./icons";

const QUICK_SLOTS = 9;

// ── Tool options section ──────────────────────────────────────────────────────
// Everything sits inside the game's tool options box: the pin button and the
// collection to pin into, the filters as tick boxes, and the nine quick pins.

export const PinboardToolOptionsWrapper: ModuleRegistryExtend = (Component: any) => {
    return (props) => {
        const t = useText();
        const [colOpen, setColOpen] = useState(false);

        const currentPrefabId = useValue(currentPrefabId$);
        const isPinned        = useValue(isPinned$);
        const collectionsJson = useValue(collectionsData$);
        const activeColId     = useValue(activeCollectionId$);
        const quickJson       = useValue(quickPins$);

        const collections = parseShared<CollectionData[]>("collections", collectionsJson, []);
        const quick = parseShared<(FavouriteEntry | null)[]>("quickPins", quickJson, []);

        const result: JSX.Element = Component(props);
        const vcr = VanillaComponentResolver.instance;

        // Nothing selected, or a game update removed the pieces we borrow:
        // show the game's tool options exactly as they are.
        if (!currentPrefabId || !vcr?.isAvailable) return result;

        const target = collections.find(c => c.id === activeColId) ?? collections[0];

        const section = (
            <div key="pinboard-section-wrap">
                <vcr.Section title={t("title")}>
                    <Tooltip tooltip={t("tipToolPin")}>
                        <button className={`${styles.tbPinBtn} ${isPinned ? styles.tbPinBtnOn : ""}`} onClick={togglePin}>
                            <PinIcon size="18rem" />
                            <span>{isPinned ? t("pinned") : t("pinShort")}</span>
                        </button>
                    </Tooltip>
                    {target && (
                        <Tooltip tooltip={t("tipInfoCollection")}>
                            <button
                                className={`${styles.tbColBtn} ${colOpen ? styles.tbColBtnOpen : ""}`}
                                onClick={() => collections.length > 1 && setColOpen(o => !o)}
                            >
                                <span className={styles.tbColName}>{shorten(target.name, 16)}</span>
                                {collections.length > 1 && <ChevronDownIcon size="10rem" />}
                            </button>
                        </Tooltip>
                    )}
                </vcr.Section>

                <div className={styles.tbBlock}>
                    {colOpen && (
                        <div className={styles.tbColList}>
                            {collections.map(col => (
                                <div
                                    key={col.id}
                                    className={`${styles.tbColOption} ${col.id === target?.id ? styles.tbColOptionActive : ""}`}
                                    onClick={() => { setActiveCollection(col.id); setColOpen(false); }}
                                >{col.name}</div>
                            ))}
                        </div>
                    )}

                    {target && target.filters.length > 0 && (
                        <div className={styles.tbFilters}>
                            {target.filters.map(filter => {
                                const inFilter = filter.assets.includes(currentPrefabId);
                                return (
                                    <div
                                        key={filter.id}
                                        className={`${styles.tbFilter} ${inFilter ? styles.tbFilterOn : ""}`}
                                        onClick={() => inFilter
                                            ? removeFromFilter(target.id, filter.id, currentPrefabId)
                                            : addToFilter(target.id, filter.id, currentPrefabId)}
                                    >
                                        {inFilter ? <CheckboxChecked size="14rem" fillOpacity={0.25} /> : <CheckboxUnchecked size="14rem" />}
                                        <span>{filter.name}</span>
                                    </div>
                                );
                            })}
                        </div>
                    )}

                    {/* Quick pins 1 to 9: click a filled one to pick it up, an empty one to put what you're holding there. */}
                    <div className={styles.tbQuick}>
                        {Array.from({ length: QUICK_SLOTS }, (_, i) => {
                            const q = quick[i] ?? null;
                            return (
                                <Tooltip key={i} tooltip={q ? `${i + 1}. ${nameOf(q)}` : t("tipQuickEmptySlot", i + 1)}>
                                    <button
                                        className={`${styles.tbQuickSlot} ${q ? "" : styles.tbQuickEmpty} ${q && q.name === currentPrefabId ? styles.tbQuickCurrent : ""}`}
                                        onClick={() => q ? selectAsset(q.name) : setQuickPin(i, "")}
                                    >
                                        {q?.thumbnail && <img src={q.thumbnail} className={styles.tbQuickImg} />}
                                        <span className={styles.tbQuickNum}>{i + 1}</span>
                                    </button>
                                </Tooltip>
                            );
                        })}
                    </div>
                </div>
            </div>
        );

        // Put our section first in the game's tool options box without changing the
        // game's own element. If the box isn't laid out the way we expect (for example
        // after a game update), place our section just before it instead of failing.
        try {
            const children = result?.props?.children;
            return Array.isArray(children)
                ? React.cloneElement(result, undefined, section, ...children)
                : <>{section}{result}</>;
        } catch {
            return result;
        }
    };
};

// Keeps names short enough to fit the tool options column.
const shorten = (text: string, max = 16) => text.length > max ? text.slice(0, max - 1) + "…" : text;
