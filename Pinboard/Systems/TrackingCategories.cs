using Game.Prefabs;
using System;

namespace Pinboard.Systems
{
    // Sorts what the player places into kinds (buildings, roads, fences...), so the
    // options page can say which kinds Recently Placed and Most Used keep track of.
    // The kinds come from the game's own asset classes (checked against the game's
    // code, October 2026). A class added by a later game update lands in "Everything else".
    public enum TrackingCategory
    {
        Buildings, Zones, Districts, Roads, Paths, Utilities, Fences,
        Areas, Trees, Props, Terrain, TransportLines, Other,
    }

    public static class TrackingCategories
    {
        public static bool IsTracked(PrefabBase prefab)
        {
            var s = Mod.Setting;
            if (prefab == null || s == null) return true;
            switch (Categorise(prefab))
            {
                case TrackingCategory.Buildings:      return s.TrackBuildings;
                case TrackingCategory.Zones:          return s.TrackZones;
                case TrackingCategory.Districts:      return s.TrackDistricts;
                case TrackingCategory.Roads:          return s.TrackRoads;
                case TrackingCategory.Paths:          return s.TrackPaths;
                case TrackingCategory.Utilities:      return s.TrackUtilities;
                case TrackingCategory.Fences:         return s.TrackFences;
                case TrackingCategory.Areas:          return s.TrackAreas;
                case TrackingCategory.Trees:          return s.TrackTrees;
                case TrackingCategory.Props:          return s.TrackProps;
                case TrackingCategory.Terrain:        return s.TrackTerrain;
                case TrackingCategory.TransportLines: return s.TrackTransportLines;
                default:                              return s.TrackOther;
            }
        }

        // Walks from the asset's own class up through the classes it's built on, and
        // stops at the first one we know. More specific classes are checked first.
        public static TrackingCategory Categorise(PrefabBase prefab)
        {
            try
            {
                for (var type = prefab.GetType(); type != null && type != typeof(object); type = type.BaseType)
                {
                    switch (type.Name)
                    {
                        case "DistrictPrefab": return TrackingCategory.Districts;
                        case "ZonePrefab": return TrackingCategory.Zones;
                        case "SurfacePrefab":
                        case "LotPrefab":
                        case "SpacePrefab":
                        case "AreaPrefab": return TrackingCategory.Areas;
                        case "RoadPrefab":
                        case "TrackPrefab":
                        case "TaxiwayPrefab":
                        case "WaterwayPrefab": return TrackingCategory.Roads;
                        case "PathwayPrefab": return TrackingCategory.Paths;
                        case "PowerLinePrefab":
                        case "PipelinePrefab": return TrackingCategory.Utilities;
                        case "FencePrefab":
                        case "NetFencePrefab":
                        case "NetLanePrefab": return TrackingCategory.Fences;
                        case "NetPrefab": return TrackingCategory.Roads;
                        case "TerraformingPrefab": return TrackingCategory.Terrain;
                        case "TransportLinePrefab": return TrackingCategory.TransportLines;
                        case "BuildingPrefab":
                        case "BuildingExtensionPrefab": return TrackingCategory.Buildings;
                        case "AssetStampPrefab":
                        case "MarkerObjectPrefab": return TrackingCategory.Other;
                        case "ObjectPrefab":
                            // Trees and plants are objects with a tree or plant part.
                            return prefab.components != null && prefab.components.Exists(c =>
                                c != null && (c.GetType().Name == "TreeObject" || c.GetType().Name == "PlantObject"))
                                ? TrackingCategory.Trees : TrackingCategory.Props;
                    }
                }
            }
            catch (Exception ex)
            {
                Mod.log.Warn($"[Pinboard] Couldn't tell what kind of asset {prefab.name} is: {ex.Message}");
            }
            return TrackingCategory.Other;
        }
    }
}
