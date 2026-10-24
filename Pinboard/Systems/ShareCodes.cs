using Newtonsoft.Json;
using Pinboard.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Pinboard.Systems
{
    // Turns a collection into a short "share code" (text players can paste into
    // Discord or a comment) and back. The code is the collection as compact JSON,
    // zipped and written as letters and numbers, with "PBV2:" in front so a pasted
    // code is easy to recognise. Pictures are left out to keep codes short; the game
    // fills them in when the collection is imported.
    public static class ShareCodes
    {
        private const string kPrefix = "PBV2:";
        private const int kMaxPins = 1000;
        private const int kMaxFilters = 50;
        private const int kMaxCodeLength = 200000;

        // A real code with the most pins allowed unzips to roughly 0.25 MB. Anything far
        // bigger is broken or made to freeze the game, so reading stops here.
        private const int kMaxUnzippedLength = 2 * 1024 * 1024;

        private class SharedPin
        {
            [JsonProperty("n")] public string Name { get; set; }
            [JsonProperty("t")] public string Type { get; set; }
            [JsonProperty("d")] public string DisplayName { get; set; }
            [JsonProperty("p", NullValueHandling = NullValueHandling.Ignore)] public string SourcePack { get; set; }
            [JsonProperty("m", NullValueHandling = NullValueHandling.Ignore)] public string SourceModId { get; set; }
        }

        private class SharedFilter
        {
            [JsonProperty("name")] public string Name { get; set; }
            [JsonProperty("assets")] public List<string> Assets { get; set; }
        }

        private class SharedCollection
        {
            [JsonProperty("v")] public int Version { get; set; } = 1;
            [JsonProperty("name")] public string Name { get; set; }
            [JsonProperty("pins")] public List<SharedPin> Pins { get; set; }
            [JsonProperty("filters")] public List<SharedFilter> Filters { get; set; }
        }

        public static string Export(CollectionData col)
        {
            var shared = new SharedCollection
            {
                Name = col.Name,
                Pins = col.Pins.Select(p => new SharedPin
                {
                    Name = p.Name,
                    Type = p.Type,
                    DisplayName = p.DisplayName,
                    SourcePack = p.SourcePack,
                    SourceModId = p.SourceModId,
                }).ToList(),
                Filters = col.Filters.Select(f => new SharedFilter { Name = f.Name, Assets = f.Assets.ToList() }).ToList(),
            };
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(shared));
            using (var output = new MemoryStream())
            {
                using (var zip = new GZipStream(output, CompressionMode.Compress))
                    zip.Write(bytes, 0, bytes.Length);
                return kPrefix + Convert.ToBase64String(output.ToArray());
            }
        }

        // Reads a share code into a new, unsaved collection (fresh IDs, so importing
        // the same code twice gives two separate collections). Returns null and an
        // explanation for the player when the code can't be read.
        public static CollectionData Import(string code, int maxCollectionName, int maxFilterName, out string error)
        {
            error = null;
            var text = new string((code ?? "").Where(c => !char.IsWhiteSpace(c)).ToArray());
            if (text.Length == 0) { error = "empty"; return null; }
            if (text.Length > kMaxCodeLength) { error = "tooLong"; return null; }
            if (!text.StartsWith(kPrefix, StringComparison.OrdinalIgnoreCase)) { error = "notACode"; return null; }

            SharedCollection shared;
            try
            {
                var zipped = Convert.FromBase64String(text.Substring(kPrefix.Length));
                using (var input = new GZipStream(new MemoryStream(zipped), CompressionMode.Decompress))
                using (var reader = new StreamReader(input, Encoding.UTF8))
                    shared = JsonConvert.DeserializeObject<SharedCollection>(ReadLimited(reader));
            }
            catch (Exception ex)
            {
                Mod.log.Warn($"[Pinboard] Couldn't read a share code: {ex.Message}");
                error = "damaged";
                return null;
            }
            if (shared?.Pins == null) { error = "damaged"; return null; }

            var col = new CollectionData { Name = TextLimits.Clean(shared.Name, maxCollectionName) ?? "Shared Collection" };
            var pinned = new HashSet<string>();
            foreach (var pin in shared.Pins.Take(kMaxPins))
            {
                if (pin == null || string.IsNullOrEmpty(pin.Name) || !pinned.Add(pin.Name)) continue;
                col.Pins.Add(new FavouriteEntry
                {
                    Name = pin.Name,
                    Type = pin.Type ?? "",
                    DisplayName = string.IsNullOrEmpty(pin.DisplayName) ? pin.Name.Replace('_', ' ') : pin.DisplayName,
                    Thumbnail = "",
                    SourcePack = TextLimits.Clean(pin.SourcePack, 80),
                    // Only digits, so a code can't smuggle in anything odd.
                    SourceModId = pin.SourceModId != null && pin.SourceModId.Length <= 12 && pin.SourceModId.All(char.IsDigit)
                        ? pin.SourceModId : null,
                });
            }
            foreach (var filter in (shared.Filters ?? new List<SharedFilter>()).Take(kMaxFilters))
            {
                if (filter == null) continue;
                col.Filters.Add(new FilterData
                {
                    Name = TextLimits.Clean(filter.Name, maxFilterName) ?? "New Filter",
                    // Only names that are actually pinned in the collection.
                    Assets = (filter.Assets ?? new List<string>()).Where(pinned.Contains).Distinct().ToList(),
                });
            }
            return col;
        }

        private static string ReadLimited(TextReader reader)
        {
            var text = new StringBuilder();
            var buffer = new char[8192];
            int read;
            while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                text.Append(buffer, 0, read);
                if (text.Length > kMaxUnzippedLength)
                    throw new InvalidDataException("share code unzips to more than " + kMaxUnzippedLength + " characters");
            }
            return text.ToString();
        }
    }
}
