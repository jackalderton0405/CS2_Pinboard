import React from "react";
import styles from "../pinboard-panel.module.css";
import { Text } from "../text";
import { CollectionData, FilterData } from "../types";
import { exportCollection, moveCollection, MAX_COLLECTION_NAME, MAX_FILTER_NAME } from "../game";
import { BinIcon, ChevronDownIcon, ChevronRightIcon, ChevronUpIcon, FolderIcon, PencilIcon, ShareIcon } from "../icons";
import { IconButton, NameInput } from "./common";

// What a collection row and a filter tab need to be renamed in place.
interface RenameProps {
    isRenaming: boolean;
    renamingValue: string;
    onRenameStart: () => void;
    onRenameChange: (v: string) => void;
    onRenameSubmit: () => void;
    onRenameCancel: () => void;
    onDelete: () => void;
}

// One collection on the main screen: open it, reorder, rename, share or delete.
export const CollectionRowItem = ({
    col, t, isFirst, isLast, onEnter,
    isRenaming, renamingValue, onRenameStart, onRenameChange, onRenameSubmit, onRenameCancel, onDelete,
}: RenameProps & {
    col: CollectionData;
    t: Text;
    isFirst: boolean;
    isLast: boolean;
    onEnter: () => void;
}) => (
    <div
        className={`${styles.collectionRow} ${isRenaming ? styles.collectionRowRenaming : ""}`}
        onClick={isRenaming ? undefined : onEnter}
    >
        <FolderIcon size="15rem" opacity={0.7} />

        {isRenaming ? (
            <NameInput
                maxLength={MAX_COLLECTION_NAME}
                value={renamingValue}
                onChange={onRenameChange}
                onSubmit={onRenameSubmit}
                onCancel={onRenameCancel}
                className={styles.renameInput}
            />
        ) : (
            <span className={styles.collectionName}>{col.name}</span>
        )}

        {!isRenaming && (
            <div className={styles.rowActions}>
                {/* The first/last row keeps an invisible arrow so the buttons line up. */}
                <IconButton tip={t("tipMoveUp")} onClick={() => !isFirst && moveCollection(col.id, -1)} className={`${styles.iconBtn} ${isFirst ? styles.hiddenBtn : ""}`}><ChevronUpIcon size="13rem" /></IconButton>
                <IconButton tip={t("tipMoveDown")} onClick={() => !isLast && moveCollection(col.id, 1)} className={`${styles.iconBtn} ${isLast ? styles.hiddenBtn : ""}`}><ChevronDownIcon size="13rem" /></IconButton>
                <IconButton tip={t("tipRename")} onClick={onRenameStart}><PencilIcon /></IconButton>
                <IconButton tip={t("tipShare")} onClick={() => exportCollection(col.id)}><ShareIcon /></IconButton>
                <IconButton tip={t("tipDeleteCollection")} onClick={onDelete} className={`${styles.iconBtn} ${styles.iconBtnDelete}`}><BinIcon size="14rem" /></IconButton>
            </div>
        )}

        <span className={styles.collectionCount}>
            {col.pins.length === 1 ? t("pinCount1") : t("pinCountN", col.pins.length)}
        </span>

        {!isRenaming && <ChevronRightIcon size="12rem" opacity={0.35} />}
    </div>
);

// One filter tab inside a collection: show it, rename or delete it.
export const FilterTabItem = ({
    filter, t, isActive, onSelect,
    isRenaming, renamingValue, onRenameStart, onRenameChange, onRenameSubmit, onRenameCancel, onDelete,
}: RenameProps & {
    filter: FilterData;
    t: Text;
    isActive: boolean;
    onSelect: () => void;
}) => (
    <div className={`${styles.tab} ${isActive ? styles.tabActive : ""}`} onClick={isRenaming ? undefined : onSelect}>
        {isRenaming ? (
            <NameInput
                maxLength={MAX_FILTER_NAME}
                value={renamingValue}
                onChange={onRenameChange}
                onSubmit={onRenameSubmit}
                onCancel={onRenameCancel}
                className={styles.tabRenameInput}
            />
        ) : (
            <span>{filter.name}</span>
        )}
        {!isRenaming && (
            <div className={styles.tabActions}>
                <IconButton tip={t("tipRename")} onClick={onRenameStart} className={`${styles.iconBtn} ${styles.iconBtnSm}`}>
                    <PencilIcon size="13rem" />
                </IconButton>
                <IconButton tip={t("tipDeleteFilter")} onClick={onDelete} className={`${styles.iconBtn} ${styles.iconBtnSm} ${styles.iconBtnDelete}`}>
                    <BinIcon size="13rem" />
                </IconButton>
            </div>
        )}
    </div>
);
