using System;

namespace Pinboard.Systems
{
    // Turning a collection into a share code, and adding one someone else shared.
    // The code itself is made and read in ShareCodes.cs.
    public partial class PinboardUISystem
    {
        private void OnExportCollection(string id)
        {
            var col = FavouritesService.FindCollection(m_Favourites, id);
            if (col == null) return;
            string code;
            try
            {
                code = ShareCodes.Export(col);
            }
            catch (Exception ex)
            {
                Mod.log.Warn($"[Pinboard] Couldn't make a share code: {ex.Message}");
                Notify("exportFailed");
                return;
            }
            // The panel shows the start of the code as a reminder, and we also try to
            // put it on the clipboard straight away.
            m_ShareCodeBinding.Update(code);
            Notify(TryCopyToClipboard(code) ? "exportCopied" : "exportShown", col.Name);
        }

        // Unity's clipboard lives in a part of the engine this mod doesn't otherwise
        // use, so it's looked up by name. If it's ever missing, the code box still works.
        private static bool TryCopyToClipboard(string text)
        {
            try
            {
                var type = Type.GetType("UnityEngine.GUIUtility, UnityEngine.IMGUIModule");
                var property = type?.GetProperty("systemCopyBuffer");
                if (property == null) return false;
                property.SetValue(null, text, null);
                return true;
            }
            catch (Exception ex)
            {
                Mod.log.Warn($"[Pinboard] Couldn't copy to the clipboard: {ex.Message}");
                return false;
            }
        }

        private void OnImportCollection(string code)
        {
            if (m_Favourites == null) return;
            var col = ShareCodes.Import(code, kMaxCollectionNameLength, kMaxFilterNameLength, out var error);
            if (col == null)
            {
                Notify("importFailed_" + error);
                return;
            }

            // Don't end up with two collections of the same name.
            var baseName = col.Name;
            for (var n = 2; m_Favourites.Collections.Exists(c => c.Name == col.Name); n++)
            {
                var suffix = " (" + n + ")";
                col.Name = (baseName.Length + suffix.Length > kMaxCollectionNameLength
                    ? baseName.Substring(0, kMaxCollectionNameLength - suffix.Length).TrimEnd()
                    : baseName) + suffix;
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            foreach (var pin in col.Pins) pin.PinnedAt = now;

            var missing = FillInFromGame(col.Pins);
            m_Favourites.Collections.Add(col);
            RequestSave();
            PushCollectionsData();
            Notify(missing > 0 ? "importedSomeMissing" : "imported",
                col.Name, col.Pins.Count.ToString(), missing.ToString());
        }
    }
}
