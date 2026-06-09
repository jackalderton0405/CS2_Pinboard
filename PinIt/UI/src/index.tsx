import { ModRegistrar } from "cs2/modding";
import { PinboardButton } from "mods/pinboard-button";
import { PinboardPanel } from "mods/pinboard-panel";
import { PinboardToolOptionsWrapper } from "mods/pinboard-add-button";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";

const register: ModRegistrar = (moduleRegistry) => {
    // Required before any VanillaComponentResolver.instance usage.
    VanillaComponentResolver.setRegistry(moduleRegistry);

    moduleRegistry.append("GameTopRight", PinboardButton);
    moduleRegistry.append("Game", PinboardPanel);

    // Inject into the tool options panel — same hook point as Anarchy.
    moduleRegistry.extend(
        "game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx",
        "MouseToolOptions",
        PinboardToolOptionsWrapper
    );
};

export default register;
