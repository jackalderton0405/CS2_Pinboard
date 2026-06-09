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
    }
}
