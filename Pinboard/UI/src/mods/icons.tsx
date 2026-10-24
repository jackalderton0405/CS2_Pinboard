import React from "react";
import { accentColour } from "./accent";

// The small drawn icons used across Pinboard. Each takes a size (and some an
// opacity) so the same icon can be used in the panel and in the tool options box.

// ── SVG icon helpers ──────────────────────────────────────────────────────────
export const svgStyle = (size = "14rem", opacity = 1): React.CSSProperties => ({
    display: "block", flexShrink: 0, width: size, height: size, opacity,
});

export type IconProps = { size?: string; opacity?: number };

// ── Icons ─────────────────────────────────────────────────────────────────────

export const CloseIcon = ({ size = "14rem", opacity = 1 }: IconProps) => (
    <svg style={svgStyle(size, opacity)} viewBox="0 0 24 24" fill="none">
        <path d="M18 6L6 18M6 6l12 12" stroke="white" strokeWidth="2.2" strokeLinecap="round"/>
    </svg>
);

export const BinIcon = ({ size = "14rem", opacity = 1 }: IconProps) => (
    <svg style={svgStyle(size, opacity)} viewBox="0 0 24 24" fill="none">
        <path d="M4 7h16M9 7V4.5h6V7M6.5 7l1 13h9l1-13M10 11v5.5M14 11v5.5"
              stroke="white" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"/>
    </svg>
);

export const ChevronDownIcon = ({ size = "14rem", opacity = 1 }: IconProps) => (
    <svg style={svgStyle(size, opacity)} viewBox="0 0 24 24" fill="none">
        <path d="M6 9l6 6 6-6" stroke="white" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round"/>
    </svg>
);

export const ChevronUpIcon = ({ size = "14rem", opacity = 1 }: IconProps) => (
    <svg style={svgStyle(size, opacity)} viewBox="0 0 24 24" fill="none">
        <path d="M6 15l6-6 6 6" stroke="white" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round"/>
    </svg>
);

export const ChevronRightIcon = ({ size = "14rem", opacity = 1 }: IconProps) => (
    <svg style={svgStyle(size, opacity)} viewBox="0 0 24 24" fill="none">
        <path d="M9 6l6 6-6 6" stroke="white" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round"/>
    </svg>
);

export const SearchIcon = ({ size = "14rem", opacity = 1 }: IconProps) => (
    <svg style={svgStyle(size, opacity)} viewBox="0 0 24 24" fill="none">
        <circle cx="10.5" cy="10.5" r="6.5" stroke="white" strokeWidth="2"/>
        <path d="M15.5 15.5L20 20" stroke="white" strokeWidth="2.2" strokeLinecap="round"/>
    </svg>
);

export const ClockIcon = ({ size = "14rem", opacity = 1 }: IconProps) => (
    <svg style={svgStyle(size, opacity)} viewBox="0 0 24 24" fill="none">
        <circle cx="12" cy="12" r="9" stroke="white" strokeWidth="1.8"/>
        <path d="M12 7v5l3.5 2" stroke="white" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"/>
    </svg>
);

export const StarIcon = ({ size = "14rem", opacity = 1 }: IconProps) => (
    <svg style={svgStyle(size, opacity)} viewBox="0 0 24 24" fill="none">
        <path d="M12 3.5l2.6 5.3 5.9.9-4.25 4.1 1 5.8L12 16.9l-5.25 2.7 1-5.8L3.5 9.7l5.9-.9L12 3.5z"
              stroke="white" strokeWidth="1.7" strokeLinejoin="round"/>
    </svg>
);

export const FolderIcon = ({ size = "14rem", opacity = 1 }: IconProps) => (
    <svg style={svgStyle(size, opacity)} viewBox="0 0 24 24" fill="none">
        <path d="M3 8a2 2 0 0 1 2-2h4l2 2h8a2 2 0 0 1 2 2v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8z"
              stroke="white" strokeWidth="1.8" strokeLinejoin="round"/>
    </svg>
);

export const PinIcon = ({ size = "14rem" }: IconProps) => (
    <svg style={svgStyle(size)} viewBox="0 0 100 100" fill="none">
        <path fillRule="evenodd"
            d="M50 5 C27 5 10 22 10 43 C10 68 50 95 50 95 C50 95 90 68 90 43 C90 22 73 5 50 5 Z M50 59 C50 59 31 46 31 34 C31 26 37 20 44 20 C47 20 49 22 50 25 C51 22 53 20 56 20 C63 20 69 26 69 34 C69 46 50 59 50 59 Z"
            fill="white"/>
    </svg>
);

export const FilterIcon = ({ size = "13rem" }: IconProps) => (
    <svg style={svgStyle(size)} viewBox="0 0 24 24" fill="none">
        <path d="M3 4h18l-7 9v6l-4-2V13L3 4z" stroke="white" strokeWidth="1.8" strokeLinejoin="round" strokeLinecap="round"/>
    </svg>
);

export const PencilIcon = ({ size = "13rem" }: IconProps) => (
    <svg style={svgStyle(size)} viewBox="0 0 24 24" fill="none">
        <path d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 0 1 3.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z"
              stroke="white" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"/>
    </svg>
);

export const ShareIcon = ({ size = "13rem" }: IconProps) => (
    <svg style={svgStyle(size)} viewBox="0 0 24 24" fill="none">
        <circle cx="18" cy="5.5" r="2.5" stroke="white" strokeWidth="1.8"/>
        <circle cx="6" cy="12" r="2.5" stroke="white" strokeWidth="1.8"/>
        <circle cx="18" cy="18.5" r="2.5" stroke="white" strokeWidth="1.8"/>
        <path d="M8.2 10.8l7.6-4.1M8.2 13.2l7.6 4.1" stroke="white" strokeWidth="1.8"/>
    </svg>
);

export const MoveIcon = ({ size = "13rem" }: IconProps) => (
    <svg style={svgStyle(size)} viewBox="0 0 24 24" fill="none">
        <path d="M4 12h13M13 7l5 5-5 5" stroke="white" strokeWidth="1.9" strokeLinecap="round" strokeLinejoin="round"/>
        <path d="M20 4v16" stroke="white" strokeWidth="1.9" strokeLinecap="round"/>
    </svg>
);

export const GridIcon = ({ size = "14rem" }: IconProps) => (
    <svg style={svgStyle(size)} viewBox="0 0 24 24" fill="none">
        <rect x="4" y="4" width="6.5" height="6.5" rx="1" stroke="white" strokeWidth="1.7"/>
        <rect x="13.5" y="4" width="6.5" height="6.5" rx="1" stroke="white" strokeWidth="1.7"/>
        <rect x="4" y="13.5" width="6.5" height="6.5" rx="1" stroke="white" strokeWidth="1.7"/>
        <rect x="13.5" y="13.5" width="6.5" height="6.5" rx="1" stroke="white" strokeWidth="1.7"/>
    </svg>
);

export const ListIcon = ({ size = "14rem" }: IconProps) => (
    <svg style={svgStyle(size)} viewBox="0 0 24 24" fill="none">
        <path d="M9 6h11M9 12h11M9 18h11" stroke="white" strokeWidth="1.8" strokeLinecap="round"/>
        <circle cx="5" cy="6" r="1.3" fill="white"/><circle cx="5" cy="12" r="1.3" fill="white"/><circle cx="5" cy="18" r="1.3" fill="white"/>
    </svg>
);

export const CheckboxChecked = ({ size = "15rem", fillOpacity = 0.2 }: IconProps & { fillOpacity?: number }) => (
    <svg style={svgStyle(size)} viewBox="0 0 24 24" fill="none">
        <rect x="3" y="3" width="18" height="18" rx="3" fill={accentColour()} fillOpacity={fillOpacity} stroke={accentColour()} strokeWidth="1.5"/>
        <path d="M7 12l4 4 6-7" stroke={accentColour()} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
    </svg>
);

export const CheckboxUnchecked = ({ size = "15rem" }: IconProps) => (
    <svg style={svgStyle(size)} viewBox="0 0 24 24" fill="none">
        <rect x="3" y="3" width="18" height="18" rx="3" stroke="rgba(255,255,255,0.25)" strokeWidth="1.5"/>
    </svg>
);

// A round "choose one" button.
export const Radio = ({ on }: { on: boolean }) => (
    <svg style={svgStyle("15rem")} viewBox="0 0 24 24" fill="none">
        <circle cx="12" cy="12" r="8" stroke={on ? accentColour() : "rgba(255,255,255,0.3)"} strokeWidth="1.6"/>
        {on && <circle cx="12" cy="12" r="4" fill={accentColour()}/>}
    </svg>
);
