import { ModRegistrar } from "cs2/modding";
import { PinboardButton } from "mods/pinboard-button";
import { PinboardPanel } from "mods/pinboard-panel";
import { PinboardToolOptionsWrapper } from "mods/tool-options-section";
import { PinboardInfoSection, INFO_SECTION_GROUP } from "mods/info-section";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";

const register: ModRegistrar = (moduleRegistry) => {
    // Required before any VanillaComponentResolver.instance usage.
    VanillaComponentResolver.setRegistry(moduleRegistry);

    // Pinboard's highlight colour is the game's own accent colour, so it follows the
    // game's interface style. If a game version doesn't define it, use the game's usual blue.
    try {
        const accent = getComputedStyle(document.documentElement).getPropertyValue("--accentColorNormal");
        if (!accent || !accent.trim()) document.documentElement.style.setProperty("--accentColorNormal", "rgba(75, 193, 241, 1)");
    } catch { /* leave it to the game */ }

    moduleRegistry.append("GameTopRight", PinboardButton);
    moduleRegistry.append("Game", PinboardPanel);

    // Inject into the tool options panel — same hook point as Anarchy.
    moduleRegistry.extend(
        "game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx",
        "MouseToolOptions",
        PinboardToolOptionsWrapper
    );

    // Our section in the side info panel (the one that opens when you click a
    // building, tree, road...). If a game update moves that panel, skip it quietly
    // rather than breaking the rest of the mod.
    try {
        moduleRegistry.extend(
            "game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx",
            "selectedInfoSectionComponents",
            ((components: any) => {
                components[INFO_SECTION_GROUP] = PinboardInfoSection;
                return components;
            }) as any
        );
    } catch (e) {
        console.warn("[Pinboard] Couldn't add the info panel section", e);
    }
};

export default register;
