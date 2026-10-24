// The game's accent colour, for the drawn icons (tick boxes, radio buttons).
// Icons are drawn shapes, so they need the colour itself rather than the CSS name.
// Read from the game's interface each time, so it follows the player's colour set.
export const accentColour = (): string => {
    try {
        const value = getComputedStyle(document.documentElement).getPropertyValue("--accentColorNormal").trim();
        if (value) return value;
    } catch { /* fall back below */ }
    return "#4bc3f1";
};
