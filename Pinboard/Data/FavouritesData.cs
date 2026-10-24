using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Pinboard.Data
{
    public class FavouriteEntry
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("thumbnail")]
        public string Thumbnail { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        // When the asset was pinned (milliseconds since 1970, UTC), for "Newest first".
        // Added in V2 as a new optional key: pins saved before this have none and
        // sort to the bottom. Left out of the file when empty.
        [JsonProperty("pinnedAt", NullValueHandling = NullValueHandling.Ignore)]
        public long? PinnedAt { get; set; }

        // How many times the asset has been placed. Only used by the Most Used list.
        [JsonProperty("uses", NullValueHandling = NullValueHandling.Ignore)]
        public int? Uses { get; set; }

        // Where the asset comes from, so a shared collection can tell other players
        // what to install: an official content pack's name, or a Paradox Mods ID.
        // Optional keys added in V2; empty for the base game.
        [JsonProperty("sourcePack", NullValueHandling = NullValueHandling.Ignore)]
        public string SourcePack { get; set; }

        [JsonProperty("sourceModId", NullValueHandling = NullValueHandling.Ignore)]
        public string SourceModId { get; set; }

        public FavouriteEntry Copy() => new FavouriteEntry
        {
            Name = Name,
            Type = Type,
            Thumbnail = Thumbnail,
            DisplayName = DisplayName,
            SourcePack = SourcePack,
            SourceModId = SourceModId,
        };
    }

    public class FilterData
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("name")]
        public string Name { get; set; } = "New Filter";

        [JsonProperty("assets")]
        public List<string> Assets { get; set; } = new List<string>();
    }

    public class CollectionData
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("name")]
        public string Name { get; set; } = "New Collection";

        [JsonProperty("pins")]
        public List<FavouriteEntry> Pins { get; set; } = new List<FavouriteEntry>();

        [JsonProperty("filters")]
        public List<FilterData> Filters { get; set; } = new List<FilterData>();
    }

    public class FavouritesData
    {
        [JsonProperty("collections")]
        public List<CollectionData> Collections { get; set; } = new List<CollectionData>();

        [JsonProperty("activeCollectionId")]
        public string ActiveCollectionId { get; set; }

        // Auto-managed list of the most recently placed assets (newest first, capped).
        // Added in 1.2.0 — a new key, so older save files load untouched.
        [JsonProperty("recentlyPlaced")]
        public List<FavouriteEntry> RecentlyPlaced { get; set; } = new List<FavouriteEntry>();

        // Auto-managed list of the assets placed most often, each with a "uses" count.
        // Added in V2 — a new key, so older save files load untouched.
        [JsonProperty("mostUsed")]
        public List<FavouriteEntry> MostUsed { get; set; } = new List<FavouriteEntry>();

        // The player's nine quick pins, picked up with Alt + 1 to 9. An empty slot is null.
        // Added in V2 — a new key, so older save files load untouched.
        [JsonProperty("quickPins")]
        public List<FavouriteEntry> QuickPins { get; set; } = new List<FavouriteEntry>();
    }
}
