using System;
using System.Collections.Generic;
using MelonLoader;
using S1API.Items;
using S1API.Lifecycle;
using S1API.Shops;
using ScheduleOne;
using ScheduleOne.DevUtilities;
using ScheduleOne.ObjectScripts;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Property;
#if DEBUG
using UnityEngine.InputSystem;
#endif
using GameBuildableItemDefinition = ScheduleOne.ItemFramework.BuildableItemDefinition;
using GameItemDefinition = ScheduleOne.ItemFramework.ItemDefinition;
using GameItemInstance = ScheduleOne.ItemFramework.ItemInstance;
using S1BuildableItemCreator = S1API.Items.Buildable.BuildableItemCreator;
using S1BuildableItemDefinition = S1API.Items.Buildable.BuildableItemDefinition;
using S1ItemDefinition = S1API.Items.ItemDefinition;

[assembly: MelonInfo(typeof(OverheadSprinkler.OverheadSprinklerMod), "OverheadSprinkler", "0.4.0", "Trooper-code")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace OverheadSprinkler;

/// <summary>
/// Main MelonMod entrypoint for Overhead Sprinkler:
/// - Loads MelonPreferences config (price, spray duration, particles, sound, volume).
/// - Clones the best vanilla overhead grow light ("fullspectrumgrowlight") into "overhead_sprinkler_head".
/// - Lists the item in Oscar's Warehouse shop (and any shop selling the Full Spectrum Grow Light).
/// - Leaves M1 debug hotkeys (F7/F8) active only in DEBUG builds.
/// </summary>
public sealed class OverheadSprinklerMod : MelonMod
{
    private static readonly string[] PreferredGrowLightIds =
    {
        "fullspectrumgrowlight",
        "ledgrowlight",
        "halogengrowlight",
    };

    private string? _clonedSourceId;

    public override void OnInitializeMelon()
    {
        SprinklerConfig.Initialize();
        GameLifecycle.OnPreLoad += OnPreLoad;
        GameLifecycle.OnLoadComplete += OnLoadComplete;
        LoggerInstance.Msg("OverheadSprinkler v0.4.0 loaded.");
    }

    public override void OnSceneWasInitialized(int buildIndex, string sceneName)
    {
        if (string.Equals(sceneName, "Main", StringComparison.OrdinalIgnoreCase))
        {
            EnsureItemRegistered();
        }
    }

#if DEBUG
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
#endif

    private void OnPreLoad()
    {
        EnsureItemRegistered();
    }

    private void OnLoadComplete()
    {
        EnsureItemRegistered();
        RegisterInShops();
    }

    private void EnsureItemRegistered()
    {
        try
        {
            if (!Singleton<Registry>.InstanceExists)
            {
                return;
            }

            float price = SprinklerConfig.HeadPurchasePrice;

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

            // CloneFrom shares sourceDef.BuiltItem; do not mutate BuiltItem so vanilla grow lights stay intact.
            S1BuildableItemDefinition created = S1BuildableItemCreator.CloneFrom(sourceDef.ID)
                .WithBasicInfo(
                    SprinklerController.ItemId,
                    "Overhead Sprinkler Head",
                    "High-end overhead grow light with an integrated sprinkler head. Hangs on a Suspension Rack and waters every pot underneath via the Sprinklers phone app.",
                    ItemCategory.Growing)
                .WithPricing(price, 0.5f)
                .Build();

            ItemManager.PreserveRuntimeItem(created);
            _clonedSourceId = sourceDef.ID;
            LoggerInstance.Msg(
                $"Registered '{SprinklerController.ItemId}' (${price:F0}) by cloning '{sourceDef.ID}' ({sourceDef.Name}).");
        }
        catch (Exception ex)
        {
            LoggerInstance.Error($"Failed to register '{SprinklerController.ItemId}': {ex}");
        }
    }

    private void RegisterInShops()
    {
        try
        {
            S1ItemDefinition? itemDef = ItemManager.GetDefinition(SprinklerController.ItemId);
            if (itemDef == null)
            {
                LoggerInstance.Warning($"Cannot add '{SprinklerController.ItemId}' to shops because its definition is not registered.");
                return;
            }

            float price = SprinklerConfig.HeadPurchasePrice;
            var targetShops = new List<Shop>();
            string sourceLookupId = _clonedSourceId ?? "fullspectrumgrowlight";
            Shop[] sourceShops = ShopManager.FindShopsByItem(sourceLookupId);
            for (int i = 0; i < sourceShops.Length; i++)
            {
                if (sourceShops[i] != null && !targetShops.Contains(sourceShops[i]))
                {
                    targetShops.Add(sourceShops[i]);
                }
            }

            if (targetShops.Count == 0)
            {
                Shop? oscarShop = ShopManager.GetShopByName("Oscar's Store")
                                  ?? ShopManager.GetShopByName("Warehouse");
                if (oscarShop != null)
                {
                    targetShops.Add(oscarShop);
                }
                else
                {
                    Shop[] growingShops = ShopManager.FindShopsByCategory(ItemCategory.Growing);
                    for (int i = 0; i < growingShops.Length; i++)
                    {
                        if (growingShops[i] != null && !targetShops.Contains(growingShops[i]))
                        {
                            targetShops.Add(growingShops[i]);
                        }
                    }
                }
            }

            if (targetShops.Count == 0)
            {
                LoggerInstance.Warning("No matching shop found to list Overhead Sprinkler Head.");
                return;
            }

            for (int i = 0; i < targetShops.Count; i++)
            {
                Shop shop = targetShops[i];
                if (shop.HasItem(SprinklerController.ItemId))
                {
                    continue;
                }

                if (shop.AddItem(itemDef, price))
                {
                    LoggerInstance.Msg(
                        $"Added '{SprinklerController.ItemId}' to shop '{shop.Name}' for ${price:F0}.");
                }
                else
                {
                    LoggerInstance.Warning($"Shop.AddItem returned false for shop '{shop.Name}'.");
                }
            }
        }
        catch (Exception ex)
        {
            LoggerInstance.Error($"Failed to register '{SprinklerController.ItemId}' in shops: {ex}");
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

#if DEBUG
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
                    $"({summary.ContainersFound} pot/bed(s)), but skipped watering because this client is not the host/server.");
                return;
            }

            LoggerInstance.Msg(
                $"Started watering property '{summary.PropertyName}': {summary.HeadCount} head(s), " +
                $"{summary.RackCount} rack(s), {summary.ContainersWatered} container(s).");
        }
        catch (Exception ex)
        {
            LoggerInstance.Error($"F8 water failed: {ex}");
        }
    }
#endif
}
