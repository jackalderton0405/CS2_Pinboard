using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using System.Collections.Generic;

namespace PinIt
{
    [FileLocation(nameof(PinIt))]
    public class Setting : ModSetting
    {
        public Setting(IMod mod) : base(mod) { }

        public override void SetDefaults() { }
    }

    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocaleEN(Setting setting) { m_Setting = setting; }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "PinIt" },
            };
        }

        public void Unload() { }
    }
}
