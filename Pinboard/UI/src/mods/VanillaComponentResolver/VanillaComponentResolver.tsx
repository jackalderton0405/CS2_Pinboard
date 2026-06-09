// Adapted from yenyang/Anarchy — pulls native CS2 tool-options components out of the registry.
import { ModuleRegistry } from "cs2/modding";

type PropsSection = {
    title?: string | null;
    children: any;
};

const registryIndex = {
    Section:        ["game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx", "Section"],
    toolButtonTheme:["game-ui/game/components/tool-options/tool-button/tool-button.module.scss",       "classes"],
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

    private get<K extends keyof typeof registryIndex>(key: K) {
        if (this.cache[key]) return this.cache[key];
        const [path, name] = registryIndex[key];
        return (this.cache[key] = (this.reg.registry.get(path) as any)[name]);
    }

    public get Section():         (props: PropsSection)    => JSX.Element { return this.get("Section"); }
    public get toolButtonTheme(): any { return this.get("toolButtonTheme"); }
    public get FOCUS_DISABLED():  any { return this.get("FOCUS_DISABLED"); }
}
