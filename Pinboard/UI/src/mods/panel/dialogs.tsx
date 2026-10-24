import React from "react";
import { useValue } from "cs2/api";
import { ConfirmationDialog, Portal } from "cs2/ui";
import styles from "../pinboard-panel.module.css";
import { Text } from "../text";
import { parse } from "../assets";
import { dismissMissingAsset, dismissShareCode, missingAsset$, missingSource$, shareCode$ } from "../game";

// The game's own pop-up dialogs, drawn outside the panel (the panel cuts off
// anything that sticks out of it).

// Characters per line in the share code preview.
const SHARE_LINE = 40;

// "Are you sure?" before deleting a collection or filter, or clearing a list.
export const ConfirmDialog = ({ title, message, confirm, cancel, onConfirm, onCancel }: {
    title: string;
    message: string;
    confirm: string;
    cancel: string;
    onConfirm: () => void;
    onCancel: () => void;
}) => (
    <Portal>
        <ConfirmationDialog
            title={title}
            message={message}
            confirm={confirm}
            cancel={cancel}
            multiline={true}
            onConfirm={onConfirm}
            onCancel={onCancel}
        />
    </Portal>
);

// Asset not found: usually a custom asset or content pack that isn't installed.
// Shown even with the panel closed, since a quick-pick key can cause it.
export const MissingAssetDialog = ({ t }: { t: Text }) => {
    const missingAsset  = useValue(missingAsset$);
    const missingSource = useValue(missingSource$);
    if (missingAsset === "") return null;

    const source = parse<{ pack?: string; modId?: string } | null>(missingSource, null);
    const from = source?.pack ? " " + t("missingFromPack", source.pack)
        : source?.modId ? " " + t("missingFromMod", source.modId) : "";
    return (
        <Portal>
            <ConfirmationDialog
                title={t("missingTitle")}
                message={t("missingMessage", missingAsset) + from}
                confirm={t("ok")}
                cancellable={false}
                multiline={true}
                onConfirm={() => dismissMissingAsset()}
            />
        </Portal>
    );
};

// A share code was made. It's already on the clipboard, so the dialog only shows
// the start of it as a reminder. A text box here stretched across the whole screen
// in the game's screen engine, and long codes can't wrap, so it's split into short lines.
export const ShareCodeDialog = ({ t }: { t: Text }) => {
    const shareCode = useValue(shareCode$);
    if (shareCode === "") return null;
    return (
        <Portal>
            <ConfirmationDialog
                title={t("shareTitle")}
                message={t("shareMessage")}
                confirm={t("close")}
                cancellable={false}
                multiline={true}
                onConfirm={() => dismissShareCode()}
            >
                <div className={styles.shareBody}>
                    <div className={styles.sharePreview}>
                        {[0, 1].map(i => shareCode.slice(i * SHARE_LINE, (i + 1) * SHARE_LINE))
                            .filter(Boolean)
                            .map((line, i) => <div key={i}>{line}</div>)}
                        {shareCode.length > 2 * SHARE_LINE && <div>…</div>}
                    </div>
                </div>
            </ConfirmationDialog>
        </Portal>
    );
};
