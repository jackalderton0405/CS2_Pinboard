// Adapted from yenyang/Anarchy — pulls native CS2 tool-options components out of the registry.
import { ModuleRegistry } from "cs2/modding";

type PropsSection = {
    title?: string | null;
    children: any;
};

const registryIndex = {
    Section:        ["game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx", "Section"],
    FOCUS_DISABLED: ["game-ui/common/focus/focus-key.ts",                                             "FOCUS_DISABLED"],
} as const;

export class VanillaComponentResolver {
    public static get instance(): VanillaComponentResolver { return this._instance!; }
    private static _instance?: VanillaComponentResolver;

    public static setRegistry(registry: ModuleRegistry) {
        this._instance = new VanillaComponentResolver(registry);
    }

    private reg: ModuleRegistry;
    private cache: Partial<Record<keyof typeof registryIndex, any>> = {};

    constructor(registry: ModuleRegistry) { this.reg = registry; }

    // Returns undefined (instead of throwing) if a game update moved or renamed
    // the piece we're looking for, so callers can skip their extra UI safely.
    private get<K extends keyof typeof registryIndex>(key: K) {
        if (this.cache[key]) return this.cache[key];
        const [path, name] = registryIndex[key];
        const module = this.reg.registry.get(path) as any;
        return (this.cache[key] = module ? module[name] : undefined);
    }

    // True when everything the tool-options section needs is still in the game.
    public get isAvailable(): boolean {
        return !!this.Section;
    }

    public get Section():         (props: PropsSection)    => JSX.Element { return this.get("Section"); }
    public get FOCUS_DISABLED():  any { return this.get("FOCUS_DISABLED"); }
}
