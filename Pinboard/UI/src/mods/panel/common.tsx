import React from "react";
import { Tooltip } from "cs2/ui";
import styles from "../pinboard-panel.module.css";
import { Text } from "../text";
import { FavouriteEntry } from "../types";
import { nameOf } from "../assets";
import { selectAsset } from "../game";
import { CloseIcon, PinIcon, SearchIcon } from "../icons";

// Small building blocks shared by the panel's screens.

// A small button with the game's own hover label.
export const IconButton = ({ tip, onClick, className, children }: {
    tip: string;
    onClick: () => void;
    className?: string;
    children: React.ReactNode;
}) => (
    <Tooltip tooltip={tip}>
        <button
            className={className ?? styles.iconBtn}
            onClick={e => { e.stopPropagation(); onClick(); }}
        >{children}</button>
    </Tooltip>
);

// An asset's picture, or a "?" when it has none.
export const Thumb = ({ src, big }: { src?: string; big?: boolean }) => src
    ? <img src={src} className={big ? styles.gridThumb : styles.favThumb} />
    : <div className={big ? styles.gridThumbEmpty : styles.favThumbEmpty}>{"?"}</div>;

// One asset in any list (pins, Recently Placed, Most Used, search). Clicking
// anywhere on the row (picture, name or empty space) picks up the asset.
export const AssetRow = ({ entry, subtitle, isCurrent, actions, expanded }: {
    entry: FavouriteEntry;
    subtitle?: string;
    isCurrent: boolean;
    actions?: React.ReactNode;
    expanded?: React.ReactNode;
}) => (
    <div className={`${styles.favRow} ${isCurrent ? styles.favRowCurrent : ""}`}>
        <div className={styles.favRowInner} onClick={() => selectAsset(entry.name)}>
            <Thumb src={entry.thumbnail} />
            <div className={styles.favText}>
                <span className={styles.favName}>{nameOf(entry)}</span>
                {subtitle && <span className={styles.favSubtitle}>{subtitle}</span>}
            </div>
            {actions && <div className={styles.favActions}>{actions}</div>}
        </div>
        {expanded}
    </div>
);

// The same assets as a grid of pictures.
export const AssetGrid = ({ entries, currentId }: { entries: FavouriteEntry[]; currentId: string }) => (
    <div className={styles.grid}>
        {entries.map(entry => (
            <Tooltip key={entry.name} tooltip={nameOf(entry)}>
                <div
                    className={`${styles.gridTile} ${entry.name === currentId ? styles.gridTileCurrent : ""}`}
                    onClick={() => selectAsset(entry.name)}
                >
                    <Thumb src={entry.thumbnail} big />
                    <span className={styles.gridName}>{nameOf(entry)}</span>
                </div>
            </Tooltip>
        ))}
    </div>
);

// The pin button on a row that opens the "pin into a collection" picker under it.
export const PinPickerButton = ({ t, open, onClick }: { t: Text; open: boolean; onClick: () => void }) => (
    <IconButton
        tip={t("tipPinToCollection")}
        onClick={onClick}
        className={`${styles.favActionBtn} ${open ? styles.favActionBtnActive : ""}`}
    ><PinIcon size="13rem" /></IconButton>
);

// "Show 25 more" under a long list.
export const ShowMore = ({ t, hidden, onClick }: { t: Text; hidden: number; onClick: () => void }) => hidden > 0 ? (
    <div className={styles.newItemRow}>
        <div className={styles.newItemTrigger} onClick={onClick}>{t("showMore", hidden)}</div>
    </div>
) : null;

export const SearchBox = ({ value, onChange, placeholder }: {
    value: string;
    onChange: (v: string) => void;
    placeholder: string;
}) => (
    <div className={styles.searchWrap}>
        <div className={styles.searchIconWrap}>
            <SearchIcon size="13rem" opacity={0.5} />
        </div>
        <input
            value={value}
            onChange={e => onChange(e.target.value)}
            placeholder={placeholder}
            className={styles.searchInput}
        />
        {value && (
            <div className={styles.searchClear} onClick={() => onChange("")}>
                <CloseIcon size="12rem" />
            </div>
        )}
    </div>
);

// A box for typing a name: Enter saves, Escape cancels, clicking away saves
// (or does onBlur when given). Clicks inside it don't reach the row behind it.
export const NameInput = ({ value, onChange, onSubmit, onCancel, onBlur, maxLength, placeholder, className }: {
    value: string;
    onChange: (v: string) => void;
    onSubmit: () => void;
    onCancel: () => void;
    onBlur?: () => void;
    maxLength?: number;
    placeholder?: string;
    className: string;
}) => (
    <input
        autoFocus
        maxLength={maxLength}
        value={value}
        onChange={e => onChange(e.target.value)}
        onKeyDown={e => {
            if (e.key === "Enter") onSubmit();
            if (e.key === "Escape") onCancel();
        }}
        onBlur={onBlur ?? onSubmit}
        placeholder={placeholder}
        className={className}
        onClick={e => e.stopPropagation()}
    />
);
