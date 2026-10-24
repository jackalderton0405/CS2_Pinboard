using Game.Prefabs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pinboard.Data;
using Unity.Entities;

namespace Pinboard.Systems
{
    // Pinning and unpinning, from every place the player can do it: the panel, the
    // tool options box, the side info panel and the keyboard. Also the quick pins.
    public partial class PinboardUISystem
    {
        // The asset behind whatever the player clicked in the city (shown in the
        // game's side info panel), so it can be pinned from there.
        private FavouriteEntry m_Inspected;
        public bool HasInspected => m_Inspected != null;

        // ── Pins ──────────────────────────────────────────────────────────────

        // The panel's "+ Pin" button: pins the held asset into the collection the
        // player is looking at (passed in), not a hidden "current" one.
        private void OnPinCurrentAsset(string collectionId)
        {
            if (!IsHolding || FavouritesService.FindCollection(m_Favourites, collectionId) == null) return;
            FavouritesService.PinToCollection(m_Favourites, collectionId, m_Current.Copy());
            PinsChanged();
        }

        // Any asset the panel knows about (from Recently Placed, Most Used, search
        // results, the info panel...) into a collection.
        private void OnPinInto(string payload)
        {
            if (m_Favourites == null) return;
            var p = Parse<CollectionAssetPayload>(payload);
            var entry = FindKnownEntry(p?.AssetName);
            if (entry == null) return;
            FavouritesService.PinToCollection(m_Favourites, p.CollectionId, entry);
            PinsChanged();
        }

        private void OnRemoveAsset(string payload)
        {
            if (m_Favourites == null) return;
            var p = Parse<CollectionAssetPayload>(payload);
            if (p == null) return;
            FavouritesService.UnpinFromCollection(m_Favourites, p.CollectionId, p.AssetName);
            PinsChanged();
        }

        // The panel's "Undo" after removing a pin.
        private void OnRestorePin(string payload)
        {
            if (m_Favourites == null) return;
            var p = Parse<RestorePinPayload>(payload);
            if (p?.Entry == null || string.IsNullOrEmpty(p.Entry.Name)) return;
            FavouritesService.RestorePin(m_Favourites, p.CollectionId, p.Entry, p.Index, p.FilterIds);
            PinsChanged();
        }

        private void OnMovePin(string payload)
        {
            if (m_Favourites == null) return;
            var p = Parse<MovePinPayload>(payload);
            if (p == null || !FavouritesService.MovePin(m_Favourites, p.CollectionId, p.AssetName, p.Delta)) return;
            PinsChanged();
        }

        private void OnCopyPin(string payload)
        {
            if (m_Favourites == null) return;
            var p = Parse<CopyPinPayload>(payload);
            if (p == null || !FavouritesService.CopyPin(m_Favourites, p.FromCollectionId, p.ToCollectionId, p.AssetName, p.Move)) return;
            PinsChanged();
        }

        // ── The tool options pin button and the pin shortcut ──────────────────

        // The tool-options pin button: toggles the held asset in the target
        // collection, which the tool options section shows by name.
        private void OnTogglePin()
        {
            if (!IsHolding || m_Favourites == null) return;
            TogglePinInTarget();
        }

        // The "pin selected asset" shortcut: same as the tool-options pin button,
        // plus a short message, since the player may not be looking at either.
        private void OnPinCurrentShortcut()
        {
            if (m_Favourites == null) return;
            if (!IsHolding)
            {
                Notify("nothingSelected");
                return;
            }
            var pinned = TogglePinInTarget();
            var target = FavouritesService.GetActiveCollection(m_Favourites);
            Notify(pinned ? "pinnedTo" : "unpinnedFrom", m_Current.DisplayName, target?.Name ?? "");
        }

        // Returns true when the asset ends up pinned.
        private bool TogglePinInTarget()
        {
            var target = GetOrCreateTargetCollection();
            var name = m_Current.Name;

            if (target.Pins.Exists(p => p.Name == name))
                FavouritesService.UnpinFromCollection(m_Favourites, target.Id, name);
            else
                FavouritesService.PinToCollection(m_Favourites, target.Id, m_Current.Copy());

            PinsChanged();
            return target.Pins.Exists(p => p.Name == name);
        }

        // ── The side info panel ───────────────────────────────────────────────

        // Called by PinboardInfoSection when the player clicks something in the city.
        public void SetInspectedPrefab(Entity prefabEntity)
        {
            m_Inspected = null;
            if (prefabEntity != Entity.Null && m_PrefabSystem.TryGetPrefab(prefabEntity, out PrefabBase prefab) && prefab != null)
                m_Inspected = EntryFromPrefab(prefab);

            if (m_Inspected == null)
            {
                m_InspectedBinding.Update("");
                return;
            }
            // Zoned (growable) buildings get a note in the info section: they only stay
            // on matching zoning unless the player uses a mod like Plop the Growables.
            var inspected = JObject.FromObject(m_Inspected);
            inspected["zoned"] = EntityManager.HasComponent<SpawnableBuildingData>(prefabEntity);
            m_InspectedBinding.Update(inspected.ToString(Formatting.None));
        }

        // ── Quick pins (Alt + 1 to 9) ─────────────────────────────────────────

        // Quick-pick key n (0-based): picks up quick pin n (set in the panel's Quick pins tab).
        private void OnQuickPick(int index)
        {
            if (m_Favourites == null) return;
            var entry = index < m_Favourites.QuickPins.Count ? m_Favourites.QuickPins[index] : null;
            if (entry == null)
            {
                Notify("noQuickPick", (index + 1).ToString());
                return;
            }
            OnSelectAsset(entry.Name);
        }

        // Payload: slot, and the asset's name ("" = the asset being held right now).
        private void OnSetQuickPin(string payload)
        {
            if (m_Favourites == null) return;
            var p = Parse<QuickPinPayload>(payload);
            if (p == null) return;
            var entry = string.IsNullOrEmpty(p.AssetName) ? m_Current?.Copy() : FindKnownEntry(p.AssetName);
            if (entry == null) return;
            FavouritesService.SetQuickPin(m_Favourites, p.Slot, entry);
            RequestSave();
            PushQuickPins();
        }

        private void OnClearQuickPin(int slot)
        {
            if (m_Favourites == null) return;
            FavouritesService.ClearQuickPin(m_Favourites, slot);
            RequestSave();
            PushQuickPins();
        }

        private void OnMoveQuickPin(string payload)
        {
            if (m_Favourites == null) return;
            var p = Parse<QuickPinPayload>(payload);
            if (p == null) return;
            FavouritesService.MoveQuickPin(m_Favourites, p.Slot, p.Delta);
            RequestSave();
            PushQuickPins();
        }
    }
}
