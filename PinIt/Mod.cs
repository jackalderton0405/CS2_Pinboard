using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using PinIt.Systems;
using System;

namespace PinIt
{
    public class Mod : IMod
    {
        public static ILog log = LogManager.GetLogger($"{nameof(PinIt)}.{nameof(Mod)}").SetShowsErrorsInUI(false);

        public static Setting Setting { get; private set; }

        private Setting m_Setting;

        public void OnLoad(UpdateSystem updateSystem)
        {
            Console.WriteLine("[PinIt] OnLoad entered");
            try
            {
                log.Info("[PinIt] OnLoad started");

                try
                {
                    m_Setting = new Setting(this);
                    m_Setting.RegisterInOptionsUI();
                    m_Setting.RegisterKeyBindings();
                    Setting = m_Setting;
                    log.Info("[PinIt] Settings registered");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PinIt] Settings registration failed: {ex}");
                    log.Error($"[PinIt] Settings registration failed: {ex}");
                }

                try
                {
                    GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(m_Setting));
                    log.Info("[PinIt] Locale added");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PinIt] Locale failed: {ex}");
                    log.Error($"[PinIt] Locale failed: {ex}");
                }

                try
                {
                    AssetDatabase.global.LoadSettings(nameof(PinIt), m_Setting, new Setting(this));
                    log.Info("[PinIt] Asset settings loaded");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PinIt] AssetDatabase.LoadSettings failed: {ex}");
                    log.Error($"[PinIt] AssetDatabase.LoadSettings failed: {ex}");
                }

                try
                {
                    updateSystem.UpdateAt<PinItUISystem>(SystemUpdatePhase.UIUpdate);
                    log.Info("[PinIt] UI system registered");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PinIt] UISystem registration failed: {ex}");
                    log.Error($"[PinIt] UISystem registration failed: {ex}");
                }

                log.Info("[PinIt] OnLoad complete");
                Console.WriteLine("[PinIt] OnLoad complete");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PinIt] OnLoad fatal exception: {ex}");
                log.Error($"[PinIt] OnLoad fatal exception: {ex}");
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
