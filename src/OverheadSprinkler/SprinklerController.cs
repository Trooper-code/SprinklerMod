using System;
using System.Collections;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
#if IL2CPP
using Il2CppFishNet;
using Il2CppFishNet.Object;
using Il2CppScheduleOne;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.EntityFramework;
using Il2CppScheduleOne.Growing;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.ObjectScripts;
using Il2CppScheduleOne.Property;
using Il2CppScheduleOne.Storage;
using Il2CppScheduleOne.Tiles;
#else
using FishNet;
using FishNet.Object;
using ScheduleOne;
using ScheduleOne.DevUtilities;
using ScheduleOne.EntityFramework;
using ScheduleOne.Growing;
using ScheduleOne.ItemFramework;
using ScheduleOne.ObjectScripts;
using ScheduleOne.Property;
using ScheduleOne.Storage;
using ScheduleOne.Tiles;
#endif

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
/// Stock and eligibility snapshot for one additive type within a property.
/// </summary>
public readonly struct AdditiveStockEntry
{
    public AdditiveStockEntry(AdditiveDefinition definition, int storageCount, int eligibleCount)
    {
        Definition = definition;
        StorageCount = storageCount;
        EligibleCount = eligibleCount;
    }

    public AdditiveDefinition Definition { get; }
    public int StorageCount { get; }
    public int EligibleCount { get; }
}

/// <summary>
/// Snapshot of an owned property's installed overhead sprinklers, covered grow containers,
/// and available additive stock in property storage.
/// </summary>
public sealed class PropertySprinklerInfo
{
    public PropertySprinklerInfo(
        Property property,
        string propertyName,
        int headCount,
        int rackCount,
        int containerCount,
        float averageMoistureNormalized,
        IReadOnlyList<AdditiveStockEntry> additives)
    {
        Property = property;
        PropertyName = propertyName;
        HeadCount = headCount;
        RackCount = rackCount;
        ContainerCount = containerCount;
        AverageMoistureNormalized = averageMoistureNormalized;
        Additives = additives;
    }

    public Property Property { get; }
    public string PropertyName { get; }
    public int HeadCount { get; }
    public int RackCount { get; }
    public int ContainerCount { get; }
    public float AverageMoistureNormalized { get; }
    public IReadOnlyList<AdditiveStockEntry> Additives { get; }
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
        bool alreadyRunning,
        IReadOnlyList<ContainerWaterResult> results)
    {
        PropertyName = propertyName;
        HeadCount = headCount;
        RackCount = rackCount;
        ContainersFound = containersFound;
        ContainersWatered = containersWatered;
        SkippedNotServer = skippedNotServer;
        AlreadyRunning = alreadyRunning;
        Results = results;
    }

    public string PropertyName { get; }
    public int HeadCount { get; }
    public int RackCount { get; }
    public int ContainersFound { get; }
    public int ContainersWatered { get; }
    public bool SkippedNotServer { get; }
    public bool AlreadyRunning { get; }
    public IReadOnlyList<ContainerWaterResult> Results { get; }
}

/// <summary>
/// Summary of applying one additive type across a property's sprinkler-covered containers.
/// </summary>
public sealed class AdditiveApplySummary
{
    public AdditiveApplySummary(
        string propertyName,
        string additiveName,
        int appliedCount,
        int eligibleCount,
        int remainingInStorage,
        bool ranOutOfStock,
        bool skippedNotServer)
    {
        PropertyName = propertyName;
        AdditiveName = additiveName;
        AppliedCount = appliedCount;
        EligibleCount = eligibleCount;
        RemainingInStorage = remainingInStorage;
        RanOutOfStock = ranOutOfStock;
        SkippedNotServer = skippedNotServer;
    }

    public string PropertyName { get; }
    public string AdditiveName { get; }
    public int AppliedCount { get; }
    public int EligibleCount { get; }
    public int RemainingInStorage { get; }
    public bool RanOutOfStock { get; }
    public bool SkippedNotServer { get; }
}

/// <summary>
/// Pure lookup, watering, and additive-delivery controller decoupled from hotkeys and UI triggers.
/// Shared by debug hotkeys and the Sprinklers PhoneApp.
/// Deduplicates Unity objects by GetInstanceID() so IL2CPP wrapper instances never double-count.
/// </summary>
public static class SprinklerController
{
    /// <summary>
    /// Item ID registered for the Overhead Sprinkler Head.
    /// </summary>
    public const string ItemId = "overhead_sprinkler_head";

    private const int WaterSegments = 5;

    private static readonly Color WaterMistColor = new(0.62f, 0.88f, 1.0f, 0.85f);
    private static readonly Color FertilizerMistColor = new(0.32f, 0.88f, 0.48f, 0.85f);
    private static readonly Color PgrMistColor = new(0.95f, 0.72f, 0.25f, 0.85f);
    private static readonly Color SpeedGrowMistColor = new(0.68f, 0.48f, 0.96f, 0.85f);

    private static readonly string[] PreferredAdditiveOrder =
    {
        "fertilizer",
        "pgr",
        "speedgrow",
    };

    private static readonly HashSet<int> ActiveWateringPropertyIds = new();

    /// <summary>
    /// Raised whenever a property's watering state or container moisture changes during a spray cycle.
    /// </summary>
    public static event Action? OnSprinklerStateChanged;

    /// <summary>
    /// Returns true while a multi-segment watering cycle is actively running on the given property.
    /// </summary>
    public static bool IsPropertyWatering(Property? property)
    {
        return property != null && ActiveWateringPropertyIds.Contains(property.GetInstanceID());
    }

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

        var seenIds = new HashSet<int>();
        var gridItems = property.GetBuildablesOfType<ProceduralGridItem>();
        for (int i = 0; i < gridItems.Count; i++)
        {
            ProceduralGridItem item = gridItems[i];
            if (item == null || item.isGhost)
            {
                continue;
            }

            if (item.ItemInstance != null
                && string.Equals(item.ItemInstance.ID, ItemId, StringComparison.Ordinal)
                && seenIds.Add(item.GetInstanceID()))
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

#if IL2CPP
        var matches = head.footprintTileMatches;
#else
        var matches = head.sync___get_value_footprintTileMatches();
#endif
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
    /// Collects every unique non-ghost GrowContainer (Pot, grow tent, MushroomBed) placed under a Suspension Rack.
    /// </summary>
    public static List<GrowContainer> GetContainersUnderRack(FloorRack rack)
    {
        var byId = new Dictionary<int, GrowContainer>();
        if (rack == null || rack.isGhost)
        {
            return new List<GrowContainer>();
        }

        if (rack.ProceduralTiles != null)
        {
            for (int i = 0; i < rack.ProceduralTiles.Count; i++)
            {
                ProceduralTile procTile = rack.ProceduralTiles[i];
                Tile? stdTile = procTile?.MatchedFootprintTile?.MatchedStandardTile;
                CollectContainersFromTile(stdTile, byId);
            }
        }

        // Fallback/union via OwnerGrid + CoordinatePairs in case any ProceduralTile has a null MatchedFootprintTile.
        if (rack.OwnerGrid != null && rack.CoordinatePairs != null)
        {
            for (int i = 0; i < rack.CoordinatePairs.Count; i++)
            {
                Tile stdTile = rack.OwnerGrid.GetTile(rack.CoordinatePairs[i].coord2);
                CollectContainersFromTile(stdTile, byId);
            }
        }

        return new List<GrowContainer>(byId.Values);
    }

    /// <summary>
    /// Returns all registered AdditiveDefinition items ordered Fertilizer, PGR, Speed Grow, then alphabetically.
    /// </summary>
    public static List<AdditiveDefinition> GetAllAdditives()
    {
        var additives = new List<AdditiveDefinition>();
        if (!Singleton<Registry>.InstanceExists)
        {
            return additives;
        }

        var allItems = Singleton<Registry>.Instance.GetAllItems();
        for (int i = 0; i < allItems.Count; i++)
        {
            AdditiveDefinition? additiveDef = GameCompat.TryCast<AdditiveDefinition>(allItems[i]);
            if (additiveDef != null)
            {
                additives.Add(additiveDef);
            }
        }

        additives.Sort((a, b) =>
        {
            int rankA = GetAdditiveSortRank(a.ID);
            int rankB = GetAdditiveSortRank(b.ID);
            if (rankA != rankB)
            {
                return rankA.CompareTo(rankB);
            }

            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        });

        return additives;
    }

    /// <summary>
    /// Returns all non-ghost StorageEntity containers belonging to placeable storage shelves in the property.
    /// </summary>
    public static List<StorageEntity> GetPropertyStorageEntities(Property property)
    {
        var storages = new List<StorageEntity>();
        if (property == null)
        {
            return storages;
        }

        var seenIds = new HashSet<int>();
        var placeables = property.GetBuildablesOfType<PlaceableStorageEntity>();
        for (int i = 0; i < placeables.Count; i++)
        {
            PlaceableStorageEntity placeable = placeables[i];
            if (placeable == null || placeable.isGhost || placeable.StorageEntity == null)
            {
                continue;
            }

            if (seenIds.Add(placeable.StorageEntity.GetInstanceID()))
            {
                storages.Add(placeable.StorageEntity);
            }
        }

        return storages;
    }

    /// <summary>
    /// Counts how many bottles/units of an item ID are available across storage shelves in the property.
    /// </summary>
    public static int CountItemInPropertyStorage(Property property, string itemId)
    {
        if (property == null || string.IsNullOrEmpty(itemId))
        {
            return 0;
        }

        int total = 0;
        List<StorageEntity> storages = GetPropertyStorageEntities(property);
        for (int i = 0; i < storages.Count; i++)
        {
            var slots = storages[i].ItemSlots;
            if (slots == null)
            {
                continue;
            }

            for (int s = 0; s < slots.Count; s++)
            {
                ItemSlot slot = slots[s];
                if (slot == null || slot.IsRemovalLocked || slot.ItemInstance == null || slot.Quantity <= 0)
                {
                    continue;
                }

                if (string.Equals(slot.ItemInstance.ID, itemId, StringComparison.Ordinal))
                {
                    total += slot.Quantity;
                }
            }
        }

        return total;
    }

    /// <summary>
    /// Consumes 1 unit of an item ID from storage shelves in the property, returning true on success.
    /// </summary>
    public static bool TryConsumeOneFromPropertyStorage(Property property, string itemId)
    {
        if (property == null || string.IsNullOrEmpty(itemId))
        {
            return false;
        }

        List<StorageEntity> storages = GetPropertyStorageEntities(property);
        for (int i = 0; i < storages.Count; i++)
        {
            var slots = storages[i].ItemSlots;
            if (slots == null)
            {
                continue;
            }

            for (int s = 0; s < slots.Count; s++)
            {
                ItemSlot slot = slots[s];
                if (slot == null || slot.IsRemovalLocked || slot.ItemInstance == null || slot.Quantity <= 0)
                {
                    continue;
                }

                if (string.Equals(slot.ItemInstance.ID, itemId, StringComparison.Ordinal))
                {
                    slot.ChangeQuantity(-1);
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks whether a grow container can accept the given additive right now.
    /// </summary>
    public static bool IsContainerEligibleForAdditive(GrowContainer container, AdditiveDefinition additiveDef)
    {
        if (container == null || container.isGhost || additiveDef == null)
        {
            return false;
        }

        if (!container.ContainsGrowable() || container.GetGrowthProgressNormalized() >= 1f)
        {
            return false;
        }

        if (!IsAdditiveAllowed(container, additiveDef.ID))
        {
            return false;
        }

        return container.CanApplyAdditive(additiveDef, out _);
    }

    /// <summary>
    /// Returns every owned property that has at least one Overhead Sprinkler Head installed, sorted by name.
    /// </summary>
    public static List<PropertySprinklerInfo> GetOwnedPropertiesWithSprinklers()
    {
        var infos = new List<PropertySprinklerInfo>();
        var owned = Property.OwnedProperties;
        if (owned == null)
        {
            return infos;
        }

        List<AdditiveDefinition> allAdditives = GetAllAdditives();

        for (int i = 0; i < owned.Count; i++)
        {
            Property prop = owned[i];
            if (prop == null)
            {
                continue;
            }

            CollectRacksAndContainers(
                prop,
                out List<ProceduralGridItem> heads,
                out Dictionary<int, FloorRack> racksById,
                out Dictionary<int, GrowContainer> containersById);
            if (heads.Count == 0)
            {
                continue;
            }

            float avgMoisture = 1f;
            if (containersById.Count > 0)
            {
                float sum = 0f;
                foreach (GrowContainer container in containersById.Values)
                {
                    sum += container.NormalizedMoistureAmount;
                }
                avgMoisture = sum / containersById.Count;
            }

            var stockEntries = new List<AdditiveStockEntry>(allAdditives.Count);
            for (int a = 0; a < allAdditives.Count; a++)
            {
                AdditiveDefinition additiveDef = allAdditives[a];
                int storageCount = CountItemInPropertyStorage(prop, additiveDef.ID);
                int eligibleCount = 0;
                foreach (GrowContainer container in containersById.Values)
                {
                    if (IsContainerEligibleForAdditive(container, additiveDef))
                    {
                        eligibleCount++;
                    }
                }

                stockEntries.Add(new AdditiveStockEntry(additiveDef, storageCount, eligibleCount));
            }

            infos.Add(new PropertySprinklerInfo(
                prop,
                prop.PropertyName,
                heads.Count,
                racksById.Count,
                containersById.Count,
                avgMoisture,
                stockEntries));
        }

        infos.Sort((a, b) => string.Compare(a.PropertyName, b.PropertyName, StringComparison.OrdinalIgnoreCase));
        return infos;
    }

    /// <summary>
    /// Waters every GrowContainer under every Overhead Sprinkler Head in the given property.
    /// Uses a smooth 5-segment spray cycle when SprayDurationSeconds > 0.05s, or fills immediately otherwise.
    /// </summary>
    public static WaterSummary WaterProperty(Property property)
    {
        if (property == null)
        {
            throw new ArgumentNullException(nameof(property));
        }

        CollectRacksAndContainers(
            property,
            out List<ProceduralGridItem> heads,
            out Dictionary<int, FloorRack> racksById,
            out Dictionary<int, GrowContainer> containersById);

        var results = new List<ContainerWaterResult>(containersById.Count);
        if (IsPropertyWatering(property))
        {
            return new WaterSummary(
                property.PropertyName,
                heads.Count,
                racksById.Count,
                containersById.Count,
                0,
                skippedNotServer: false,
                alreadyRunning: true,
                results);
        }

        if (!InstanceFinder.IsServer)
        {
            return new WaterSummary(
                property.PropertyName,
                heads.Count,
                racksById.Count,
                containersById.Count,
                0,
                skippedNotServer: true,
                alreadyRunning: false,
                results);
        }

        if (heads.Count == 0)
        {
            return new WaterSummary(
                property.PropertyName,
                0,
                0,
                0,
                0,
                skippedNotServer: false,
                alreadyRunning: false,
                results);
        }

        float duration = SprinklerConfig.SprayDurationSeconds;
        if (duration <= 0.05f)
        {
            SprinklerEffects.PlaySprayOnHeads(heads, 1.2f, WaterMistColor, isAdditiveBurst: false);

            int wateredCount = 0;
            foreach (GrowContainer container in containersById.Values)
            {
                float before = container.NormalizedMoistureAmount;
                container.ChangeMoistureAmount(container.MoistureCapacity);
                container.SyncMoistureData();
                float after = container.NormalizedMoistureAmount;

                wateredCount++;
                string name = container.ItemInstance != null ? container.ItemInstance.Name : container.GetType().Name;
                results.Add(new ContainerWaterResult(name, before, after, container.MoistureCapacity));
            }

            OnSprinklerStateChanged?.Invoke();
            return new WaterSummary(
                property.PropertyName,
                heads.Count,
                racksById.Count,
                containersById.Count,
                wateredCount,
                skippedNotServer: false,
                alreadyRunning: false,
                results);
        }

        int propertyId = property.GetInstanceID();
        ActiveWateringPropertyIds.Add(propertyId);
        SprinklerEffects.PlaySprayOnHeads(heads, duration, WaterMistColor, isAdditiveBurst: false);
        OnSprinklerStateChanged?.Invoke();
        MelonCoroutines.Start(RunWateringRoutine(propertyId, new List<GrowContainer>(containersById.Values), duration));

        return new WaterSummary(
            property.PropertyName,
            heads.Count,
            racksById.Count,
            containersById.Count,
            containersById.Count,
            skippedNotServer: false,
            alreadyRunning: false,
            results);
    }

    /// <summary>
    /// Waters every owned property that has at least one Overhead Sprinkler Head installed.
    /// </summary>
    public static List<WaterSummary> WaterAllOwnedProperties()
    {
        List<PropertySprinklerInfo> props = GetOwnedPropertiesWithSprinklers();
        var summaries = new List<WaterSummary>(props.Count);
        for (int i = 0; i < props.Count; i++)
        {
            summaries.Add(WaterProperty(props[i].Property));
        }

        return summaries;
    }

    /// <summary>
    /// Consumes additive bottles from property storage and applies the additive to eligible
    /// sprinkler-covered GrowContainers in the property.
    /// </summary>
    public static AdditiveApplySummary ApplyAdditiveToProperty(Property property, AdditiveDefinition additiveDef)
    {
        if (property == null)
        {
            throw new ArgumentNullException(nameof(property));
        }
        if (additiveDef == null)
        {
            throw new ArgumentNullException(nameof(additiveDef));
        }

        CollectRacksAndContainers(
            property,
            out List<ProceduralGridItem> heads,
            out _,
            out Dictionary<int, GrowContainer> containersById);

        var eligible = new List<GrowContainer>();
        foreach (GrowContainer container in containersById.Values)
        {
            if (IsContainerEligibleForAdditive(container, additiveDef))
            {
                eligible.Add(container);
            }
        }

        if (!InstanceFinder.IsServer)
        {
            int currentStock = CountItemInPropertyStorage(property, additiveDef.ID);
            return new AdditiveApplySummary(
                property.PropertyName,
                additiveDef.Name,
                0,
                eligible.Count,
                currentStock,
                ranOutOfStock: false,
                skippedNotServer: true);
        }

        int appliedCount = 0;
        bool ranOutOfStock = false;
        for (int i = 0; i < eligible.Count; i++)
        {
            GrowContainer target = eligible[i];
            if (!IsContainerEligibleForAdditive(target, additiveDef))
            {
                continue;
            }

            if (!TryConsumeOneFromPropertyStorage(property, additiveDef.ID))
            {
                ranOutOfStock = true;
                break;
            }

            target.ApplyAdditive_Server(additiveDef.ID);
            appliedCount++;
        }

        if (appliedCount > 0)
        {
            SprinklerEffects.PlaySprayOnHeads(heads, 1.8f, GetAdditiveMistColor(additiveDef.ID), isAdditiveBurst: true);
        }

        int remaining = CountItemInPropertyStorage(property, additiveDef.ID);
        return new AdditiveApplySummary(
            property.PropertyName,
            additiveDef.Name,
            appliedCount,
            eligible.Count,
            remaining,
            ranOutOfStock,
            skippedNotServer: false);
    }

    private static IEnumerator RunWateringRoutine(int propertyInstanceId, List<GrowContainer> containers, float totalDuration)
    {
        try
        {
            float segmentDelay = totalDuration / WaterSegments;
            float normalizedIncrement = 1f / WaterSegments;

            for (int seg = 0; seg < WaterSegments; seg++)
            {
                yield return new WaitForSeconds(segmentDelay);

                for (int i = 0; i < containers.Count; i++)
                {
                    GrowContainer container = containers[i];
                    if (container == null || container.isGhost)
                    {
                        continue;
                    }

                    container.ChangeMoistureAmount(container.MoistureCapacity * normalizedIncrement);
                    container.SyncMoistureData();
                }

                OnSprinklerStateChanged?.Invoke();
            }
        }
        finally
        {
            ActiveWateringPropertyIds.Remove(propertyInstanceId);
            OnSprinklerStateChanged?.Invoke();
        }
    }

    private static Color GetAdditiveMistColor(string? additiveId)
    {
        if (string.Equals(additiveId, "fertilizer", StringComparison.OrdinalIgnoreCase))
        {
            return FertilizerMistColor;
        }
        if (string.Equals(additiveId, "pgr", StringComparison.OrdinalIgnoreCase))
        {
            return PgrMistColor;
        }
        if (string.Equals(additiveId, "speedgrow", StringComparison.OrdinalIgnoreCase))
        {
            return SpeedGrowMistColor;
        }
        return WaterMistColor;
    }

    private static int GetAdditiveSortRank(string? additiveId)
    {
        if (string.IsNullOrEmpty(additiveId))
        {
            return PreferredAdditiveOrder.Length;
        }

        for (int i = 0; i < PreferredAdditiveOrder.Length; i++)
        {
            if (string.Equals(additiveId, PreferredAdditiveOrder[i], StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return PreferredAdditiveOrder.Length;
    }

    private static bool IsAdditiveAllowed(GrowContainer container, string additiveId)
    {
        if (container.AllowedAdditives == null)
        {
            return false;
        }

        for (int i = 0; i < container.AllowedAdditives.Length; i++)
        {
            AdditiveDefinition allowed = container.AllowedAdditives[i];
            if (allowed != null && string.Equals(allowed.ID, additiveId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static void CollectRacksAndContainers(
        Property property,
        out List<ProceduralGridItem> heads,
        out Dictionary<int, FloorRack> racksById,
        out Dictionary<int, GrowContainer> containersById)
    {
        heads = FindHeadsInProperty(property);
        racksById = new Dictionary<int, FloorRack>();
        for (int i = 0; i < heads.Count; i++)
        {
            FloorRack? rack = GetParentRack(heads[i]);
            if (rack != null)
            {
                racksById[rack.GetInstanceID()] = rack;
            }
        }

        containersById = new Dictionary<int, GrowContainer>();
        foreach (FloorRack rack in racksById.Values)
        {
            List<GrowContainer> underRack = GetContainersUnderRack(rack);
            for (int i = 0; i < underRack.Count; i++)
            {
                GrowContainer container = underRack[i];
                containersById[container.GetInstanceID()] = container;
            }
        }
    }

    private static void CollectContainersFromTile(Tile? tile, Dictionary<int, GrowContainer> resultsById)
    {
        if (tile == null || tile.BuildableOccupants == null)
        {
            return;
        }

        for (int i = 0; i < tile.BuildableOccupants.Count; i++)
        {
            GrowContainer? container = GameCompat.TryCast<GrowContainer>(tile.BuildableOccupants[i]);
            if (container != null && !container.isGhost)
            {
                resultsById[container.GetInstanceID()] = container;
            }
        }
    }
}
