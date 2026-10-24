using Newtonsoft.Json;
using System;
using System.Linq;

namespace Pinboard.Systems
{
    // Opening and closing the panel, and remembering where it sits, list or grid,
    // and the sort order. Those are kept in the mod's settings, not the pins file.
    public partial class PinboardUISystem
    {
        private static readonly string[] s_SortModes = { "own", "az", "za", "newest", "type" };

        private void OnTogglePanel()
        {
            m_PanelOpen = !m_PanelOpen;
            // The lists are sent to the panel whenever they change, so opening it
            // has little to do. Names and pictures are only looked up again if the
            // language changed since the last time, so a language switch still shows
            // straight away without reloading the city.
            if (m_PanelOpen && m_Favourites != null)
            {
                if (CurrentLocale() != m_RefreshedLocale)
                {
                    RefreshStoredAssetInfo();
                    PushLists();
                }
                else if (m_UsesDirty)
                {
                    PushMostUsed();
                }
            }
            m_PanelOpenBinding.Update(m_PanelOpen);
        }

        private string PanelLayoutJson()
        {
            var s = Mod.Setting;
            return JsonConvert.SerializeObject(new
            {
                x = s?.PanelX ?? -1f,
                y = s?.PanelY ?? -1f,
                grid = s?.GridView ?? false,
                sort = s?.SortMode ?? "own",
            });
        }

        private void SaveLayout()
        {
            try { Mod.Setting?.ApplyAndSave(); }
            catch (Exception ex) { Mod.log.Warn($"[Pinboard] Couldn't save the panel layout: {ex.Message}"); }
            m_PanelLayoutBinding.Update(PanelLayoutJson());
        }

        private void OnSavePanelPosition(string payload)
        {
            var p = Parse<PanelPositionPayload>(payload);
            if (p == null || Mod.Setting == null) return;
            Mod.Setting.PanelX = p.X;
            Mod.Setting.PanelY = p.Y;
            SaveLayout();
        }

        private void OnSetGridView(bool grid)
        {
            if (Mod.Setting == null) return;
            Mod.Setting.GridView = grid;
            SaveLayout();
        }

        private void OnSetSortMode(string mode)
        {
            if (Mod.Setting == null || !s_SortModes.Contains(mode)) return;
            Mod.Setting.SortMode = mode;
            SaveLayout();
        }
    }
}
