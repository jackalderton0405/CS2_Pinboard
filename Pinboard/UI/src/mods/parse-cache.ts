// The game sends the lists (collections, Recently Placed...) as text. Turning that
// text back into lists takes time when there are hundreds of pins, and the panel,
// the tool box and the info section all redraw often (on every hover, drag or
// keypress). So each list is only read once per change and the result is shared.
// The lists must not be changed in place by the code that uses them.
const cache = new Map<string, { json: string; value: unknown }>();

export const parseShared = <T,>(key: string, json: string, fallback: T): T => {
    const known = cache.get(key);
    if (known && known.json === json) return known.value as T;
    let value: T;
    try { value = json ? JSON.parse(json) : fallback; } catch { value = fallback; }
    cache.set(key, { json, value });
    return value;
};
