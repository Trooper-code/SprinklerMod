using System;
using System.Collections.Generic;
using MelonLoader;
using ScheduleOne.EntityFramework;
using ScheduleOne.Growing;
using ScheduleOne.ObjectScripts;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Property;
using ScheduleOne.Tiles;
using UnityEngine;
using UnityEngine.InputSystem;

[assembly: MelonInfo(typeof(RackProbe.ProbeMod), "RackProbe", "0.1.0", "Trooper-code")]

namespace RackProbe;

/// <summary>
/// Read-only M0 diagnostic mod that logs Suspension Racks (FloorRack), their attached
/// ProceduralGridItems (e.g. GrowLights), and the pots/grow containers underneath them on F9.
/// </summary>
public class ProbeMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        LoggerInstance.Msg("RackProbe loaded. Press F9 in-game to log Suspension Racks and pots under them.");
    }

    public override void OnUpdate()
    {
        if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame)
        {
            DumpRacksInCurrentProperty();
        }
    }

    private void DumpRacksInCurrentProperty()
    {
        try
        {
            Property? currentProperty = Player.Local != null ? Player.Local.CurrentProperty : null;
            string propName = currentProperty != null ? currentProperty.PropertyName : "(none / outdoors)";
            LoggerInstance.Msg($"=== RackProbe Snapshot | Property: {propName} ===");

            FloorRack[] allRacks = UnityEngine.Object.FindObjectsOfType<FloorRack>();
            int rackCount = 0;

            foreach (FloorRack rack in allRacks)
            {
                if (rack == null || rack.isGhost)
                {
                    continue;
                }

                if (currentProperty != null && rack.ParentProperty != currentProperty)
                {
                    continue;
                }

                rackCount++;
                string rackId = rack.ItemInstance != null ? rack.ItemInstance.ID : "(null)";
                string rackName = rack.ItemInstance != null ? rack.ItemInstance.Name : "(null)";
                LoggerInstance.Msg(
                    $"[Rack #{rackCount}] Name={rackName} ID={rackId} GUID={rack.GUID} Pos={rack.transform.position} " +
                    $"ProcTiles={rack.ProceduralTiles?.Count ?? 0} CoordPairs={rack.CoordinatePairs?.Count ?? 0}");

                var containersUnderRack = new HashSet<GrowContainer>();
                var loggedOccupants = new HashSet<ProceduralGridItem>();

                if (rack.ProceduralTiles != null)
                {
                    for (int i = 0; i < rack.ProceduralTiles.Count; i++)
                    {
                        ProceduralTile procTile = rack.ProceduralTiles[i];
                        if (procTile == null)
                        {
                            continue;
                        }

                        int occCount = procTile.Occupants != null ? procTile.Occupants.Count : 0;
                        LoggerInstance.Msg($"  ProcTile[{i}] Type={procTile.TileType} Occupants={occCount}");

                        if (procTile.Occupants != null)
                        {
                            foreach (ProceduralGridItem occ in procTile.Occupants)
                            {
                                if (occ == null || !loggedOccupants.Add(occ))
                                {
                                    continue;
                                }

                                string occId = occ.ItemInstance != null ? occ.ItemInstance.ID : "(null)";
                                string occName = occ.ItemInstance != null ? occ.ItemInstance.Name : "(null)";
                                LoggerInstance.Msg(
                                    $"    Attached: {occ.GetType().Name} Name={occName} ID={occId} GUID={occ.GUID} " +
                                    $"IsGrowLight={occ is GrowLight}");
                            }
                        }

                        Tile? stdTile = procTile.MatchedFootprintTile != null
                            ? procTile.MatchedFootprintTile.MatchedStandardTile
                            : null;
                        CollectContainersFromTile(stdTile, containersUnderRack);
                    }
                }

                // Cross-check FloorRack.CoordinatePairs to confirm both paths yield the same containers.
                var coordPairContainers = new HashSet<GrowContainer>();
                if (rack.OwnerGrid != null && rack.CoordinatePairs != null)
                {
                    foreach (CoordinatePair pair in rack.CoordinatePairs)
                    {
                        Tile stdTile = rack.OwnerGrid.GetTile(pair.coord2);
                        CollectContainersFromTile(stdTile, coordPairContainers);
                    }
                }

                LoggerInstance.Msg(
                    $"  Containers under rack: {containersUnderRack.Count} (via ProcTiles), " +
                    $"{coordPairContainers.Count} (via CoordinatePairs)");

                foreach (GrowContainer container in containersUnderRack)
                {
                    string cId = container.ItemInstance != null ? container.ItemInstance.ID : "(null)";
                    string cName = container.ItemInstance != null ? container.ItemInstance.Name : "(null)";
                    float currentWater = container.NormalizedMoistureAmount * container.MoistureCapacity;
                    float pct = container.NormalizedMoistureAmount * 100f;
                    LoggerInstance.Msg(
                        $"    Container: {container.GetType().Name} Name={cName} ID={cId} GUID={container.GUID} " +
                        $"Water={currentWater:F2}/{container.MoistureCapacity:F2} ({pct:F0}%)");
                }
            }

            if (rackCount == 0)
            {
                LoggerInstance.Msg("No FloorRacks found in current property.");
            }

            LoggerInstance.Msg("=== End RackProbe Snapshot ===");
        }
        catch (Exception ex)
        {
            LoggerInstance.Error($"RackProbe failed: {ex}");
        }
    }

    private static void CollectContainersFromTile(Tile? tile, HashSet<GrowContainer> results)
    {
        if (tile == null || tile.BuildableOccupants == null)
        {
            return;
        }

        foreach (GridItem occupant in tile.BuildableOccupants)
        {
            if (occupant is GrowContainer container && !container.isGhost)
            {
                results.Add(container);
            }
        }
    }
}
