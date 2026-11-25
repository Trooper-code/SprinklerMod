using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Object;
using ScheduleOne.EntityFramework;
using ScheduleOne.Growing;
using ScheduleOne.ObjectScripts;
using ScheduleOne.Property;
using ScheduleOne.Tiles;

namespace OverheadSprinkler;

/// <summary>
/// Per-container before/after moisture snapshot produced by a watering pass.
/// </summary>
public readonly struct ContainerWaterResult
{
    public ContainerWaterResult(string name, float beforeNormalized, float afterNormalized, float capacity)
    {
        Name = name;
        BeforeNormalized = beforeNormalized;
        AfterNormalized = afterNormalized;
        Capacity = capacity;
    }

    public string Name { get; }
    public float BeforeNormalized { get; }
    public float AfterNormalized { get; }
    public float Capacity { get; }
}

/// <summary>
/// Summary of a single property-wide watering pass.
/// </summary>
public sealed class WaterSummary
{
    public WaterSummary(
        string propertyName,
        int headCount,
        int rackCount,
        int containersFound,
        int containersWatered,
        bool skippedNotServer,
        IReadOnlyList<ContainerWaterResult> results)
    {
        PropertyName = propertyName;
        HeadCount = headCount;
        RackCount = rackCount;
        ContainersFound = containersFound;
        ContainersWatered = containersWatered;
        SkippedNotServer = skippedNotServer;
        Results = results;
    }

    public string PropertyName { get; }
    public int HeadCount { get; }
    public int RackCount { get; }
    public int ContainersFound { get; }
    public int ContainersWatered { get; }
    public bool SkippedNotServer { get; }
    public IReadOnlyList<ContainerWaterResult> Results { get; }
}

/// <summary>
/// Pure lookup and watering controller decoupled from hotkeys and UI triggers.
/// Shared by M1 hotkeys, M2 PhoneApp triggers, and M3 additive delivery.
/// </summary>
public static class SprinklerController
{
    /// <summary>
    /// Item ID registered for the Overhead Sprinkler Head.
    /// </summary>
    public const string ItemId = "overhead_sprinkler_head";

    /// <summary>
    /// Finds all placed, non-ghost Overhead Sprinkler Heads in the given property.
    /// </summary>
    public static List<ProceduralGridItem> FindHeadsInProperty(Property property)
    {
        var heads = new List<ProceduralGridItem>();
        if (property == null)
        {
            return heads;
        }

        List<ProceduralGridItem> gridItems = property.GetBuildablesOfType<ProceduralGridItem>();
        for (int i = 0; i < gridItems.Count; i++)
        {
            ProceduralGridItem item = gridItems[i];
            if (item == null || item.isGhost)
            {
                continue;
            }

            if (item.ItemInstance != null && string.Equals(item.ItemInstance.ID, ItemId, StringComparison.Ordinal))
            {
                heads.Add(item);
            }
        }

        return heads;
    }

    /// <summary>
    /// Resolves the parent Suspension Rack (FloorRack) that an attached head hangs from.
    /// </summary>
    public static FloorRack? GetParentRack(ProceduralGridItem head)
    {
        if (head == null)
        {
            return null;
        }

        List<CoordinateProceduralTilePair>? matches = head.sync___get_value_footprintTileMatches();
        if (matches == null || matches.Count == 0)
        {
            return null;
        }

        for (int i = 0; i < matches.Count; i++)
        {
            NetworkObject? tileParent = matches[i].tileParent;
            if (tileParent == null)
            {
                continue;
            }

            FloorRack? rack = tileParent.GetComponent<FloorRack>();
            if (rack != null && !rack.isGhost)
            {
                return rack;
            }
        }

        return null;
    }

    /// <summary>
    /// Collects every non-ghost GrowContainer (Pot, grow tent, MushroomBed) placed under a Suspension Rack.
    /// </summary>
    public static HashSet<GrowContainer> GetContainersUnderRack(FloorRack rack)
    {
        var containers = new HashSet<GrowContainer>();
        if (rack == null || rack.isGhost)
        {
            return containers;
        }

        if (rack.ProceduralTiles != null)
        {
            for (int i = 0; i < rack.ProceduralTiles.Count; i++)
            {
                ProceduralTile procTile = rack.ProceduralTiles[i];
                Tile? stdTile = procTile?.MatchedFootprintTile?.MatchedStandardTile;
                CollectContainersFromTile(stdTile, containers);
            }
        }

        // Fallback/union via OwnerGrid + CoordinatePairs in case any ProceduralTile has a null MatchedFootprintTile.
        if (rack.OwnerGrid != null && rack.CoordinatePairs != null)
        {
            for (int i = 0; i < rack.CoordinatePairs.Count; i++)
            {
                Tile stdTile = rack.OwnerGrid.GetTile(rack.CoordinatePairs[i].coord2);
                CollectContainersFromTile(stdTile, containers);
            }
        }

        return containers;
    }

    /// <summary>
    /// Waters every GrowContainer under every Overhead Sprinkler Head in the given property.
    /// </summary>
    public static WaterSummary WaterProperty(Property property)
    {
        if (property == null)
        {
            throw new ArgumentNullException(nameof(property));
        }

        List<ProceduralGridItem> heads = FindHeadsInProperty(property);
        var racks = new HashSet<FloorRack>();
        for (int i = 0; i < heads.Count; i++)
        {
            FloorRack? rack = GetParentRack(heads[i]);
            if (rack != null)
            {
                racks.Add(rack);
            }
        }

        var uniqueContainers = new HashSet<GrowContainer>();
        foreach (FloorRack rack in racks)
        {
            foreach (GrowContainer container in GetContainersUnderRack(rack))
            {
                uniqueContainers.Add(container);
            }
        }

        var results = new List<ContainerWaterResult>(uniqueContainers.Count);
        if (!InstanceFinder.IsServer)
        {
            return new WaterSummary(
                property.PropertyName,
                heads.Count,
                racks.Count,
                uniqueContainers.Count,
                0,
                skippedNotServer: true,
                results);
        }

        int wateredCount = 0;
        foreach (GrowContainer container in uniqueContainers)
        {
            float before = container.NormalizedMoistureAmount;
            container.ChangeMoistureAmount(container.MoistureCapacity);
            container.SyncMoistureData();
            float after = container.NormalizedMoistureAmount;

            wateredCount++;
            string name = container.ItemInstance != null ? container.ItemInstance.Name : container.GetType().Name;
            results.Add(new ContainerWaterResult(name, before, after, container.MoistureCapacity));
        }

        return new WaterSummary(
            property.PropertyName,
            heads.Count,
            racks.Count,
            uniqueContainers.Count,
            wateredCount,
            skippedNotServer: false,
            results);
    }

    private static void CollectContainersFromTile(Tile? tile, HashSet<GrowContainer> results)
    {
        if (tile == null || tile.BuildableOccupants == null)
        {
            return;
        }

        for (int i = 0; i < tile.BuildableOccupants.Count; i++)
        {
            if (tile.BuildableOccupants[i] is GrowContainer container && !container.isGhost)
            {
                results.Add(container);
            }
        }
    }
}
