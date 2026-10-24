using Newtonsoft.Json;
using Pinboard.Data;
using System.Collections.Generic;

namespace Pinboard.Systems
{
    // The shapes of the messages the panel sends (as JSON). The names in quotes must
    // match the panel code in UI/src/mods/game.ts.
    public partial class PinboardUISystem
    {
        // One asset in one collection: pin it there, or take it out.
        private class CollectionAssetPayload
        {
            [JsonProperty("collectionId")] public string CollectionId { get; set; }
            [JsonProperty("assetName")] public string AssetName { get; set; }
        }

        private class RenamePayload
        {
            [JsonProperty("id")] public string Id { get; set; }
            [JsonProperty("name")] public string Name { get; set; }
        }

        private class FilterActionPayload
        {
            [JsonProperty("collectionId")] public string CollectionId { get; set; }
            [JsonProperty("filterId")] public string FilterId { get; set; }
            [JsonProperty("name")] public string Name { get; set; }
        }

        private class FilterMemberPayload
        {
            [JsonProperty("collectionId")] public string CollectionId { get; set; }
            [JsonProperty("filterId")] public string FilterId { get; set; }
            [JsonProperty("assetName")] public string AssetName { get; set; }
        }

        private class RestorePinPayload
        {
            [JsonProperty("collectionId")] public string CollectionId { get; set; }
            [JsonProperty("entry")] public FavouriteEntry Entry { get; set; }
            [JsonProperty("index")] public int Index { get; set; }
            [JsonProperty("filterIds")] public List<string> FilterIds { get; set; }
        }

        private class QuickPinPayload
        {
            [JsonProperty("slot")] public int Slot { get; set; }
            [JsonProperty("assetName")] public string AssetName { get; set; }
            [JsonProperty("delta")] public int Delta { get; set; }
        }

        private class MovePinPayload
        {
            [JsonProperty("collectionId")] public string CollectionId { get; set; }
            [JsonProperty("assetName")] public string AssetName { get; set; }
            [JsonProperty("delta")] public int Delta { get; set; }
        }

        private class MoveCollectionPayload
        {
            [JsonProperty("id")] public string Id { get; set; }
            [JsonProperty("delta")] public int Delta { get; set; }
        }

        private class CopyPinPayload
        {
            [JsonProperty("fromCollectionId")] public string FromCollectionId { get; set; }
            [JsonProperty("toCollectionId")] public string ToCollectionId { get; set; }
            [JsonProperty("assetName")] public string AssetName { get; set; }
            [JsonProperty("move")] public bool Move { get; set; }
        }

        private class PanelPositionPayload
        {
            [JsonProperty("x")] public float X { get; set; }
            [JsonProperty("y")] public float Y { get; set; }
        }
    }
}
