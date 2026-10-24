import React, { useState } from "react";
import styles from "../pinboard-panel.module.css";
import { Text } from "../text";
import { FavouriteEntry } from "../types";
import { detailsOf, matches, nameOf } from "../assets";
import { clearQuickPin, moveQuickPin, selectAsset, setQuickPin } from "../game";
import { ChevronDownIcon, ChevronUpIcon, CloseIcon, PencilIcon, SearchIcon } from "../icons";
import { IconButton, Thumb } from "./common";

// The Quick pins tab: the nine assets Alt + 1 to 9 pick up, even with the panel closed.

const QUICK_SLOTS = 9;

export const QuickPinsView = ({ t, slots, candidates, currentId, currentName }: {
    t: Text;
    slots: (FavouriteEntry | null)[];
    candidates: FavouriteEntry[];
    currentId: string;
    currentName: string;
}) => {
    // The slot whose "choose an asset" list is open.
    const [choosing, setChoosing] = useState<number | null>(null);
    const [search, setSearch]     = useState("");

    const query = search.trim().toLowerCase();
    const shown = candidates.filter(e => matches(e, query)).slice(0, 40);

    const choose = (slot: number, assetName: string) => {
        setQuickPin(slot, assetName);
        setChoosing(null);
        setSearch("");
    };

    return (
        <div className={styles.pinList}>
            <div className={styles.quickExplain}>{t("quickExplain")}</div>
            {Array.from({ length: QUICK_SLOTS }, (_, slot) => {
                const entry  = slots[slot] ?? null;
                const isOpen = choosing === slot;
                return (
                    <div key={slot} className={`${styles.favRow} ${entry && entry.name === currentId ? styles.favRowCurrent : ""}`}>
                        <div
                            className={styles.favRowInner}
                            onClick={() => entry ? selectAsset(entry.name) : setChoosing(isOpen ? null : slot)}
                        >
                            <span className={styles.quickKey}>{t("quickKey", slot + 1)}</span>
                            {entry ? <Thumb src={entry.thumbnail} /> : <div className={styles.quickEmptyThumb} />}
                            <div className={styles.favText}>
                                {entry ? (
                                    <>
                                        <span className={styles.favName}>{nameOf(entry)}</span>
                                        <span className={styles.favSubtitle}>{detailsOf(t, entry)}</span>
                                    </>
                                ) : (
                                    <span className={styles.quickEmptyText}>{t("quickEmpty")}</span>
                                )}
                            </div>
                            <div className={styles.favActions}>
                                <IconButton
                                    tip={t("tipMoveUp")}
                                    onClick={() => slot > 0 && moveQuickPin(slot, -1)}
                                    className={`${styles.favActionBtn} ${slot === 0 ? styles.hiddenBtn : ""}`}
                                ><ChevronUpIcon size="13rem" /></IconButton>
                                <IconButton
                                    tip={t("tipMoveDown")}
                                    onClick={() => slot < QUICK_SLOTS - 1 && moveQuickPin(slot, 1)}
                                    className={`${styles.favActionBtn} ${slot === QUICK_SLOTS - 1 ? styles.hiddenBtn : ""}`}
                                ><ChevronDownIcon size="13rem" /></IconButton>
                                <IconButton
                                    tip={t("tipQuickSet")}
                                    onClick={() => { setSearch(""); setChoosing(isOpen ? null : slot); }}
                                    className={`${styles.favActionBtn} ${isOpen ? styles.favActionBtnActive : ""}`}
                                ><PencilIcon size="13rem" /></IconButton>
                                <IconButton
                                    tip={t("tipQuickClear")}
                                    onClick={() => entry && clearQuickPin(slot)}
                                    className={`${styles.favRemoveBtn} ${entry ? "" : styles.hiddenBtn}`}
                                ><CloseIcon size="13rem" /></IconButton>
                            </div>
                        </div>
                        {isOpen && (
                            <div className={styles.quickChooser}>
                                {currentId ? (
                                    <button className={styles.primaryBtn} onClick={() => choose(slot, "")}>
                                        {t("quickUseHolding", currentName || currentId)}
                                    </button>
                                ) : (
                                    <div className={styles.hint}>{t("quickHoldHint")}</div>
                                )}
                                {candidates.length === 0 ? (
                                    <div className={styles.hint}>{t("quickNoCandidates")}</div>
                                ) : (
                                    <>
                                        <div className={styles.searchWrap}>
                                            <div className={styles.searchIconWrap}><SearchIcon size="13rem" opacity={0.5} /></div>
                                            <input
                                                value={search}
                                                onChange={e => setSearch(e.target.value)}
                                                placeholder={t("quickSearch")}
                                                className={styles.searchInput}
                                            />
                                        </div>
                                        <div
                                            className={styles.quickCandidates}
                                            onWheel={e => { e.stopPropagation(); e.currentTarget.scrollTop += e.deltaY; }}
                                        >
                                            {shown.length === 0 && <div className={styles.hint}>{t("noMatches")}</div>}
                                            {shown.map(c => (
                                                <div key={c.name} className={styles.quickCandidate} onClick={() => choose(slot, c.name)}>
                                                    <Thumb src={c.thumbnail} />
                                                    <span className={styles.favName}>{nameOf(c)}</span>
                                                </div>
                                            ))}
                                        </div>
                                    </>
                                )}
                            </div>
                        )}
                    </div>
                );
            })}
        </div>
    );
};
