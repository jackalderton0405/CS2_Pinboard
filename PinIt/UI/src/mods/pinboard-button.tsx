import React from "react";
import { trigger, useValue, bindValue } from "cs2/api";
import { Button, Tooltip } from "cs2/ui";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";

const panelOpen$ = bindValue<boolean>("pinboard", "panelOpen", false);
const togglePanel = () => trigger("pinboard", "togglePanel");

const PinIcon = () => (
    <svg style={{ display: "block", width: "36rem", height: "36rem" }} viewBox="0 0 690 690" fill="none">
        <path
            fillRule="evenodd"
            clipRule="evenodd"
            d="M340,80 C220,80 130,170 130,290 C130,410 340,600 340,600 C340,600 550,410 550,290 C550,170 460,80 340,80 Z
               M340,370 C340,370 255,305 255,255 C255,225 278,205 305,205 C320,205 333,213 340,225 C347,213 360,205 375,205 C402,205 425,225 425,255 C425,305 340,370 340,370 Z"
            fill="white"
        />
    </svg>
);

export const PinboardButton = () => {
    const isOpen = useValue(panelOpen$);
    const vcr = VanillaComponentResolver.instance;
    return (
        <Tooltip tooltip="Pinboard">
            <Button
                variant="floating"
                selected={isOpen}
                onSelect={togglePanel}
                focusKey={vcr.FOCUS_DISABLED}
            >
                <PinIcon />
            </Button>
        </Tooltip>
    );
};
