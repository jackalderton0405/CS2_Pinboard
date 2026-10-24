using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Pinboard.Systems;
using System;

namespace Pinboard
{
    public class Mod : IMod
    {
        public static ILog log = LogManager.GetLogger("Pinboard.Mod").SetShowsErrorsInUI(false);

        public static Setting Setting { get; private set; }

        private Setting m_Setting;

        public void OnLoad(UpdateSystem updateSystem)
        {
            // Settings, shortcut and text come first. If any of it fails, stop here
            // with one clear error instead of carrying on and causing follow-on errors.
            try
            {
                m_Setting = new Setting(this);
                m_Setting.RegisterInOptionsUI();
                m_Setting.RegisterKeyBindings();
                GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(m_Setting));
                AssetDatabase.global.LoadSettings("Pinboard", m_Setting, new Setting(this));
                Setting = m_Setting;
            }
            catch (Exception ex)
            {
                log.Error($"[Pinboard] Start-up failed, the mod is switched off for this session: {ex}");
                try { m_Setting?.UnregisterInOptionsUI(); } catch { /* it may never have registered */ }
                m_Setting = null;
                Setting = null;
                return;
            }

            updateSystem.UpdateAt<PinboardUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<PinboardInfoSection>(SystemUpdatePhase.UIUpdate);
            log.Info("[Pinboard] Loaded");
        }

        public void OnDispose()
        {
            m_Setting?.UnregisterInOptionsUI();
            m_Setting = null;
            Setting = null;
        }
    }
}
