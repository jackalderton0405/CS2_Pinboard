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
        public static ILog log = LogManager.GetLogger($"{nameof(Pinboard)}.{nameof(Mod)}").SetShowsErrorsInUI(false);

        public static Setting Setting { get; private set; }

        private Setting m_Setting;

        public void OnLoad(UpdateSystem updateSystem)
        {
            Console.WriteLine("[Pinboard] OnLoad entered");
            try
            {
                log.Info("[Pinboard] OnLoad started");

                try
                {
                    m_Setting = new Setting(this);
                    m_Setting.RegisterInOptionsUI();
                    m_Setting.RegisterKeyBindings();
                    Setting = m_Setting;
                    log.Info("[Pinboard] Settings registered");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Pinboard] Settings registration failed: {ex}");
                    log.Error($"[Pinboard] Settings registration failed: {ex}");
                }

                try
                {
                    GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(m_Setting));
                    log.Info("[Pinboard] Locale added");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Pinboard] Locale failed: {ex}");
                    log.Error($"[Pinboard] Locale failed: {ex}");
                }

                try
                {
                    AssetDatabase.global.LoadSettings(nameof(Pinboard), m_Setting, new Setting(this));
                    log.Info("[Pinboard] Asset settings loaded");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Pinboard] AssetDatabase.LoadSettings failed: {ex}");
                    log.Error($"[Pinboard] AssetDatabase.LoadSettings failed: {ex}");
                }

                try
                {
                    updateSystem.UpdateAt<PinboardUISystem>(SystemUpdatePhase.UIUpdate);
                    log.Info("[Pinboard] UI system registered");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Pinboard] UISystem registration failed: {ex}");
                    log.Error($"[Pinboard] UISystem registration failed: {ex}");
                }

                log.Info("[Pinboard] OnLoad complete");
                Console.WriteLine("[Pinboard] OnLoad complete");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Pinboard] OnLoad fatal exception: {ex}");
                log.Error($"[Pinboard] OnLoad fatal exception: {ex}");
            }
        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
            m_Setting?.UnregisterInOptionsUI();
            m_Setting = null;
            Setting = null;
        }
    }
}
