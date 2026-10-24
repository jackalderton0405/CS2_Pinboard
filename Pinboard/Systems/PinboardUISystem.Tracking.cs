using Game.Input;
using Game.Prefabs;
using Game.Tools;
using Pinboard.Data;
using System.Collections.Generic;

namespace Pinboard.Systems
{
    // Keeps track of the asset the player is holding, and adds what they place to
    // Recently Placed and Most Used.
    public partial class PinboardUISystem
    {
        // The asset the player is holding right now (picked up from the game's menus
        // or the panel), or null when they aren't holding one.
        private FavouriteEntry m_Current;

        private string CurrentName => m_Current?.Name ?? "";
        private bool IsHolding => m_Current != null;

        // The terrain brushes and the zone tool don't tell the game's "asset changed"
        // event when you pick one, so OnUpdate watches those two tools' asset itself.
        private PrefabBase m_LastPolledPrefab;

        // Area tools (surfaces, for example) report a click for every corner of the
        // shape. For those, Most Used counts one use per pick-up instead of per click.
        private bool m_AreaUseCounted;

        // ── The held asset ────────────────────────────────────────────────────

        private void OnPrefabChanged(PrefabBase prefab)
        {
            m_AreaUseCounted = false;
            m_Current = prefab == null ? null : EntryFromPrefab(prefab);
            m_CurrentPrefabBinding.Update(m_Current?.DisplayName ?? "");
            m_CurrentPrefabIdBinding.Update(CurrentName);
            UpdateIsPinned();
        }

        // Picked up a brush or zone (which don't announce it themselves).
        private void PollBrushOrZone()
        {
            var tool = m_ToolSystem.activeTool;
            var toolName = tool?.GetType().Name;
            var polled = toolName == "TerrainToolSystem" || toolName == "ZoneToolSystem" ? tool.GetPrefab() : null;
            if (polled == m_LastPolledPrefab) return;
            m_LastPolledPrefab = polled;
            if (polled != null && polled.name != CurrentName) OnPrefabChanged(polled);
        }

        // "Is pinned" for the tool-options pin button, which works on the target collection.
        private void UpdateIsPinned()
        {
            if (m_Favourites == null || !IsHolding)
            {
                m_IsPinnedBinding.Update(false);
                return;
            }
            var active = FavouritesService.GetActiveCollection(m_Favourites);
            m_IsPinnedBinding.Update(active != null && active.Pins.Exists(p => p.Name == m_Current.Name));
        }

        // ── Placements ────────────────────────────────────────────────────────

        // The click that commits a placement. Most tools call it "Apply"; the terrain
        // brushes and the zone tool have their own names for it.
        private static readonly HashSet<string> s_PlaceActions = new HashSet<string>
        {
            "Apply", "Apply Zone",
            "Raise Terrain", "Lower Terrain", "Level Terrain", "Slope Terrain",
            "Soften Terrain", "Fast Soften", "Paint Material", "Paint Resource",
        };

        // Fired (a static event) whenever any tool input action reaches Performed.
        // We act only on a placing click, and only while the active tool is placing
        // the asset we are tracking.
        private void OnToolActionPerformed(ProxyAction action)
        {
            if (action == null || !s_PlaceActions.Contains(action.name)) return;
            if (m_Favourites == null || !IsHolding) return;

            // The bulldozer, the plain select tool and others also use "Apply".
            // Only count the click when the active tool is holding our asset.
            var activeTool = m_ToolSystem.activeTool;
            if (activeTool == null || activeTool is DefaultToolSystem || activeTool is BulldozeToolSystem) return;
            var toolPrefab = activeTool.GetPrefab();
            if (toolPrefab == null || toolPrefab.name != m_Current.Name) return;

            // The player can switch off kinds of things (districts, zones...) in the options.
            if (!TrackingCategories.IsTracked(toolPrefab)) return;

            // Every placement counts towards Most Used. Saved a little later, not per click.
            // A surface or other area is one use however many corners it has.
            var isAreaTool = activeTool.GetType().Name == "AreaToolSystem";
            if (!isAreaTool || !m_AreaUseCounted)
            {
                m_AreaUseCounted = isAreaTool;
                FavouritesService.RecordUse(m_Favourites, m_Current.Copy());
                MarkUsesChanged();
            }

            // Skip if this asset is already the most recent — avoids redundant
            // saves while dragging out a brush of the same prefab.
            var recent = m_Favourites.RecentlyPlaced;
            if (recent.Count > 0 && recent[0].Name == m_Current.Name) return;

            FavouritesService.RecordPlacement(m_Favourites, m_Current.Copy());
            RequestSave();
            PushRecentlyPlaced();
        }

        private void OnClearRecent()
        {
            if (m_Favourites == null) return;
            FavouritesService.ClearRecentlyPlaced(m_Favourites);
            RequestSave();
            PushRecentlyPlaced();
        }

        private void OnClearMostUsed()
        {
            if (m_Favourites == null) return;
            FavouritesService.ClearMostUsed(m_Favourites);
            RequestSave();
            m_UsesDirty = false;
            PushMostUsed();
        }
    }
}
