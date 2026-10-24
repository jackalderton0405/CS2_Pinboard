using Pinboard.Data;
using System.Linq;

namespace Pinboard.Systems
{
    // Creating, renaming, reordering and deleting collections and their filters.
    public partial class PinboardUISystem
    {
        // Trims a name and cuts it to the limit. Returns null when nothing is left.
        private static string CleanName(string name, int maxLength) => TextLimits.Clean(name, maxLength);

        // The collection the tool-options pin button adds to. If the player deleted
        // every collection, a fresh one is made so the button never silently does nothing.
        private CollectionData GetOrCreateTargetCollection()
        {
            var target = FavouritesService.GetActiveCollection(m_Favourites)
                         ?? m_Favourites.Collections.FirstOrDefault();
            if (target == null)
            {
                target = FavouritesService.CreateCollection(m_Favourites, "New Collection");
                Mod.log.Info("[Pinboard] No collections left, created \"New Collection\" to pin into");
            }
            if (m_Favourites.ActiveCollectionId != target.Id)
            {
                m_Favourites.ActiveCollectionId = target.Id;
                m_ActiveCollectionIdBinding.Update(target.Id);
            }
            return target;
        }

        // ── Collections ───────────────────────────────────────────────────────

        // Only the tool-options collection picker calls this. Browsing the panel
        // doesn't change where the pin button puts things.
        private void OnSetActiveCollection(string id)
        {
            if (FavouritesService.FindCollection(m_Favourites, id) == null) return;
            m_Favourites.ActiveCollectionId = id;
            RequestSave();
            m_ActiveCollectionIdBinding.Update(id);
            UpdateIsPinned();
        }

        private void OnCreateCollection(string name)
        {
            if (m_Favourites == null) return;
            var clean = CleanName(name, kMaxCollectionNameLength);
            if (clean == null) return;
            FavouritesService.CreateCollection(m_Favourites, clean);
            PinsChanged();
        }

        private void OnRenameCollection(string payload)
        {
            if (m_Favourites == null) return;
            var p = Parse<RenamePayload>(payload);
            var clean = CleanName(p?.Name, kMaxCollectionNameLength);
            if (clean == null) return;
            FavouritesService.RenameCollection(m_Favourites, p.Id, clean);
            PinsChanged();
        }

        private void OnMoveCollection(string payload)
        {
            if (m_Favourites == null) return;
            var p = Parse<MoveCollectionPayload>(payload);
            if (p == null || !FavouritesService.MoveCollection(m_Favourites, p.Id, p.Delta)) return;
            PinsChanged();
        }

        private void OnDeleteCollection(string id)
        {
            if (m_Favourites == null) return;
            FavouritesService.DeleteCollection(m_Favourites, id);
            RequestSave();
            PushAll();
        }

        // ── Filters ───────────────────────────────────────────────────────────

        private void OnCreateFilter(string payload)
        {
            if (m_Favourites == null) return;
            var p = Parse<FilterActionPayload>(payload);
            if (p == null) return;
            var clean = CleanName(p.Name, kMaxFilterNameLength) ?? "New Filter";
            FavouritesService.CreateFilter(m_Favourites, p.CollectionId, clean);
            PinsChanged();
        }

        private void OnRenameFilter(string payload)
        {
            if (m_Favourites == null) return;
            var p = Parse<FilterActionPayload>(payload);
            var clean = CleanName(p?.Name, kMaxFilterNameLength);
            if (clean == null) return;
            FavouritesService.RenameFilter(m_Favourites, p.CollectionId, p.FilterId, clean);
            PinsChanged();
        }

        private void OnDeleteFilter(string payload)
        {
            if (m_Favourites == null) return;
            var p = Parse<FilterActionPayload>(payload);
            if (p == null) return;
            FavouritesService.DeleteFilter(m_Favourites, p.CollectionId, p.FilterId);
            PinsChanged();
        }

        // A filter only shows assets that are pinned in its collection, so adding
        // to a filter also pins the asset there first. Every button follows this rule.
        private void OnAddToFilter(string payload)
        {
            if (m_Favourites == null) return;
            var p = Parse<FilterMemberPayload>(payload);
            if (p == null || string.IsNullOrEmpty(p.AssetName)) return;

            var col = FavouritesService.FindCollection(m_Favourites, p.CollectionId);
            if (col == null || !col.Filters.Exists(f => f.Id == p.FilterId)) return;

            if (!col.Pins.Exists(e => e.Name == p.AssetName))
            {
                var entry = FindKnownEntry(p.AssetName);
                if (entry == null)
                {
                    Mod.log.Warn($"[Pinboard] Can't add unknown asset '{p.AssetName}' to a filter");
                    return;
                }
                FavouritesService.PinToCollection(m_Favourites, col.Id, entry);
            }

            FavouritesService.AddToFilter(m_Favourites, p.CollectionId, p.FilterId, p.AssetName);
            PinsChanged();
        }

        private void OnRemoveFromFilter(string payload)
        {
            if (m_Favourites == null) return;
            var p = Parse<FilterMemberPayload>(payload);
            if (p == null) return;
            FavouritesService.RemoveFromFilter(m_Favourites, p.CollectionId, p.FilterId, p.AssetName);
            PinsChanged();
        }
    }
}
