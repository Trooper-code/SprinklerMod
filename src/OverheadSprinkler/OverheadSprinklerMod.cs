using System;
using System.Collections.Generic;
using MelonLoader;
using S1API.Items;
using S1API.Items.Buildable;
using S1API.Lifecycle;
using ScheduleOne;
using ScheduleOne.DevUtilities;
using ScheduleOne.ItemFramework;
using ScheduleOne.ObjectScripts;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Property;
using UnityEngine.InputSystem;
using GameBuildableItemDefinition = ScheduleOne.ItemFramework.BuildableItemDefinition;
using GameItemDefinition = ScheduleOne.ItemFramework.ItemDefinition;
using GameItemInstance = ScheduleOne.ItemFramework.ItemInstance;
using S1BuildableItemCreator = S1API.Items.Buildable.BuildableItemCreator;
using S1BuildableItemDefinition = S1API.Items.Buildable.BuildableItemDefinition;
using S1ItemDefinition = S1API.Items.ItemDefinition;

[assembly: MelonInfo(typeof(OverheadSprinkler.OverheadSprinklerMod), "OverheadSprinkler", "0.1.0", "Trooper-code")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace OverheadSprinkler;

/// <summary>
/// M1 Proof-of-Concept MelonMod:
/// - Clones a vanilla grow light via S1API into "overhead_sprinkler_head" without mutating the shared prefab.
/// - F7 spawns an Overhead Sprinkler Head into the player's inventory.
/// - F8 waters every pot/grow container under every Suspension Rack with a head in the current property.
/// </summary>
public sealed class OverheadSprinklerMod : MelonMod
{
    private static readonly string[] PreferredGrowLightIds =
    {
        "fullspectrumgrowlight",
        "ledgrowlight",
        "halogengrowlight",
    };

    public override void OnInitializeMelon()
    {
        GameLifecycle.OnPreLoad += OnPreLoad;
        LoggerInstance.Msg("OverheadSprinkler M1 loaded. Hotkeys in Main scene: F7 = spawn head, F8 = water current property.");
    }

    public override void OnSceneWasInitialized(int buildIndex, string sceneName)
    {
        if (string.Equals(sceneName, "Main", StringComparison.OrdinalIgnoreCase))
        {
            EnsureItemRegistered();
        }
    }

    public override void OnUpdate()
    {
        Keyboard? keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.f7Key.wasPressedThisFrame)
        {
            SpawnHeadInInventory();
        }
        else if (keyboard.f8Key.wasPressedThisFrame)
        {
            TriggerWaterCurrentProperty();
        }
    }

    private void OnPreLoad()
    {
        EnsureItemRegistered();
    }

    private void EnsureItemRegistered()
    {
        try
        {
            if (!Singleton<Registry>.InstanceExists)
            {
                return;
            }

            if (Registry.ItemExists(SprinklerController.ItemId))
            {
                S1ItemDefinition? existing = ItemManager.GetDefinition(SprinklerController.ItemId);
                if (existing != null)
                {
                    ItemManager.PreserveRuntimeItem(existing);
                }
                return;
            }

            GameBuildableItemDefinition? sourceDef = FindSourceGrowLightDefinition();
            if (sourceDef == null)
            {
                LoggerInstance.Error("Could not find any vanilla GrowLight BuildableItemDefinition in Registry to clone.");
                return;
            }

            // CloneFrom shares sourceDef.BuiltItem; do not mutate BuiltItem in M1 so vanilla grow lights stay intact.
            S1BuildableItemDefinition created = S1BuildableItemCreator.CloneFrom(sourceDef.ID)
                .WithBasicInfo(
                    SprinklerController.ItemId,
                    "Overhead Sprinkler Head",
                    "Hangs from a Suspension Rack and waters every pot underneath.",
                    ItemCategory.Growing)
                .WithPricing(300f, 0.5f)
                .Build();

            ItemManager.PreserveRuntimeItem(created);
            LoggerInstance.Msg(
                $"Registered '{SprinklerController.ItemId}' by cloning '{sourceDef.ID}' ({sourceDef.Name}).");
        }
        catch (Exception ex)
        {
            LoggerInstance.Error($"Failed to register '{SprinklerController.ItemId}': {ex}");
        }
    }

    private static GameBuildableItemDefinition? FindSourceGrowLightDefinition()
    {
        for (int i = 0; i < PreferredGrowLightIds.Length; i++)
        {
            GameBuildableItemDefinition? candidate = Registry.GetItem<GameBuildableItemDefinition>(PreferredGrowLightIds[i]);
            if (candidate != null && candidate.BuiltItem is GrowLight)
            {
                return candidate;
            }
        }

        List<GameItemDefinition> allItems = Singleton<Registry>.Instance.GetAllItems();
        for (int i = 0; i < allItems.Count; i++)
        {
            if (allItems[i] is GameBuildableItemDefinition buildable && buildable.BuiltItem is GrowLight)
            {
                return buildable;
            }
        }

        return null;
    }

    private void SpawnHeadInInventory()
    {
        try
        {
            if (Player.Local == null || !PlayerSingleton<PlayerInventory>.InstanceExists)
            {
                LoggerInstance.Warning("F7 pressed before player/inventory is ready. Load into a save first.");
                return;
            }

            EnsureItemRegistered();

            GameItemDefinition? def = Registry.GetItem(SprinklerController.ItemId);
            if (def == null)
            {
                LoggerInstance.Error($"Item '{SprinklerController.ItemId}' is not registered.");
                return;
            }

            GameItemInstance instance = def.GetDefaultInstance(1);
            PlayerInventory inventory = PlayerSingleton<PlayerInventory>.Instance;
            if (!inventory.CanItemFitInInventory(instance, 1))
            {
                LoggerInstance.Warning("Inventory is full; free a hotbar slot and press F7 again.");
                return;
            }

            inventory.AddItemToInventory(instance);
            LoggerInstance.Msg("Added 1x Overhead Sprinkler Head to inventory.");
        }
        catch (Exception ex)
        {
            LoggerInstance.Error($"F7 spawn failed: {ex}");
        }
    }

    private void TriggerWaterCurrentProperty()
    {
        try
        {
            Property? currentProperty = Player.Local != null ? Player.Local.CurrentProperty : null;
            if (currentProperty == null)
            {
                LoggerInstance.Warning("F8 pressed outside a property. Stand inside an owned property and press F8.");
                return;
            }

            WaterSummary summary = SprinklerController.WaterProperty(currentProperty);
            if (summary.SkippedNotServer)
            {
                LoggerInstance.Warning(
                    $"Property '{summary.PropertyName}': found {summary.HeadCount} head(s) across {summary.RackCount} rack(s) " +
                    $"({summary.ContainersFound} pot/bed(s)), but skipped watering because this client is not the host/server (M4 adds client RPC).");
                return;
            }

            LoggerInstance.Msg(
                $"Watered property '{summary.PropertyName}': {summary.HeadCount} head(s), " +
                $"{summary.RackCount} rack(s), {summary.ContainersWatered} container(s).");

            for (int i = 0; i < summary.Results.Count; i++)
            {
                ContainerWaterResult r = summary.Results[i];
                LoggerInstance.Msg(
                    $"  - {r.Name}: {r.BeforeNormalized * 100f:F0}% -> {r.AfterNormalized * 100f:F0}% (cap {r.Capacity:F1})");
            }
        }
        catch (Exception ex)
        {
            LoggerInstance.Error($"F8 water failed: {ex}");
        }
    }
}
