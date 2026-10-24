using Game.Prefabs;
using Game.SceneFlow;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Pinboard.Systems
{
    // Works out where an asset comes from, so a shared collection can tell the
    // other player what they need to install:
    //  - an official content pack or DLC: the game gives such assets a
    //    "ContentPrerequisite" naming the pack;
    //  - a Paradox Mods asset pack: its files sit in a folder named after the mod's
    //    ID number, e.g. ".../mods_subscribed/12345_3/...".
    // These parts of the game aren't documented for mods, so everything is looked up
    // by name and wrapped in checks: if a game update renames something, the source
    // is simply left blank and nothing else is affected.
    public static class AssetSources
    {
        private static readonly Regex s_ModFolder = new Regex(@"[\\/](\d{2,})_\d+[\\/]", RegexOptions.Compiled);
        private const BindingFlags kMembers = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        // Looking this up is slow (it digs through the asset by name), and the answer
        // never changes while the game runs, so each asset is only looked up once.
        private static readonly Dictionary<PrefabBase, Source> s_Cache = new Dictionary<PrefabBase, Source>();

        private struct Source
        {
            public string Pack, ModId, Where;
        }

        // Pack names are in the player's language, so this is emptied when the language changes.
        public static void ClearCache() => s_Cache.Clear();

        public static void Detect(PrefabBase prefab, out string pack, out string modId, out string where)
        {
            pack = null;
            modId = null;
            where = null;
            if (prefab == null) return;
            if (!s_Cache.TryGetValue(prefab, out var known))
            {
                DetectUncached(prefab, out known.Pack, out known.ModId, out known.Where);
                s_Cache[prefab] = known;
            }
            pack = known.Pack;
            modId = known.ModId;
            where = known.Where;
        }

        private static void DetectUncached(PrefabBase prefab, out string pack, out string modId, out string where)
        {
            pack = null;
            modId = null;
            where = null;

            try { pack = FindContentPack(prefab); }
            catch (Exception ex) { Mod.log.Warn($"[Pinboard] Couldn't read the content pack of {prefab.name}: {ex.Message}"); }

            try
            {
                where = FindAssetPath(prefab);
                var match = where == null ? null : s_ModFolder.Match(where);
                if (match != null && match.Success) modId = match.Groups[1].Value;
            }
            catch (Exception ex) { Mod.log.Warn($"[Pinboard] Couldn't read the file location of {prefab.name}: {ex.Message}"); }
        }

        // PrefabBase.components holds the asset's parts. A ContentPrerequisite part
        // points at the pack (a ContentPrefab) the asset needs.
        private static string FindContentPack(PrefabBase prefab)
        {
            if (!(GetMember(prefab, "components") is IEnumerable components)) return null;
            foreach (var component in components)
            {
                if (component == null || component.GetType().Name != "ContentPrerequisite") continue;
                var content = GetMember(component, "m_ContentPrerequisite") as PrefabBase;
                if (content == null) return null;
                return LocalizedName(content);
            }
            return null;
        }

        // The asset's file on disk, as text. Tried a few ways because the game's
        // asset classes name this differently in different places.
        private static string FindAssetPath(PrefabBase prefab)
        {
            var asset = GetMember(prefab, "asset");
            if (asset == null) return null;
            foreach (var name in new[] { "path", "uri", "identifier" })
            {
                var value = GetMember(asset, name)?.ToString();
                if (!string.IsNullOrEmpty(value)) return value;
            }
            return asset.ToString();
        }

        private static string LocalizedName(PrefabBase content)
        {
            var dict = GameManager.instance?.localizationManager?.activeDictionary;
            if (dict != null)
            {
                foreach (var key in new[] { "Assets.NAME[" + content.name + "]", "Content.NAME[" + content.name + "]" })
                {
                    if (dict.TryGetValue(key, out var text) && !string.IsNullOrEmpty(text)) return text;
                }
            }
            return content.name.Replace('_', ' ');
        }

        private static object GetMember(object target, string name)
        {
            var type = target.GetType();
            var property = type.GetProperty(name, kMembers);
            if (property != null && property.GetIndexParameters().Length == 0) return property.GetValue(target, null);
            return type.GetField(name, kMembers)?.GetValue(target);
        }
    }
}
