using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared.Enums;
using Ninja_Price.API.PoeNinja;
using Ninja_Price.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.FilesInMemory;
using Color = SharpDX.Color;

namespace Ninja_Price.Main;

public partial class Main
{
    private CustomItem _inspectedItem;

    private static readonly Dictionary<string, string> ShardMapping = new()
    {
        { "Transmutation Shard", "Orb of Transmutation" },
        { "Alteration Shard", "Orb of Alteration" },
        { "Annulment Shard", "Orb of Annulment" },
        { "Exalted Shard", "Exalted Orb" },
        { "Mirror Shard", "Mirror of Kalandra" },
        { "Regal Shard", "Regal Orb" },
        { "Alchemy Shard", "Orb of Alchemy" },
        { "Chaos Shard", "Chaos Orb" },
        { "Ancient Shard", "Ancient Orb" },
        { "Engineer's Shard", "Engineer's Orb" },
        { "Harbinger's Shard", "Harbinger's Orb" },
        { "Horizon Shard", "Orb of Horizons" },
        { "Binding Shard", "Orb of Binding" },
        { "Scroll Fragment", "Scroll of Wisdom" },
        { "Ritual Splinter", "Ritual Vessel" },
        { "Crescent Splinter", "The Maven's Writ" },
        { "Timeless Vaal Splinter", "Timeless Vaal Emblem" },
        { "Timeless Templar Splinter", "Timeless Templar Emblem" },
        { "Timeless Eternal Empire Splinter", "Timeless Eternal Emblem" },
        { "Timeless Maraketh Splinter", "Timeless Maraketh Emblem" },
        { "Timeless Karui Splinter", "Timeless Karui Emblem" },
        { "Splinter of Xoph", "Xoph's Breachstone" },
        { "Splinter of Tul", "Tul's Breachstone" },
        { "Splinter of Esh", "Esh's Breachstone" },
        { "Splinter of Uul-Netol", "Uul-Netol's Breachstone" },
        { "Splinter of Chayula", "Chayula's Breachstone" },
        { "Simulacrum Splinter", "Simulacrum" },
    };

    private double DivinePrice =>
        CollectedData.Currency.LinesByName.GetValueOrDefault("Divine Orb") switch { (null, null, 0) => throw new Exception("Divine price is missing"), var o => o.ChaosEquivalent };

    private List<NormalInventoryItem> GetInventoryItems()
    {
        var inventory = GameController.Game.IngameState.IngameUi.InventoryPanel;
        return !inventory.IsVisible ? null : inventory[InventoryIndex.PlayerInventory].VisibleInventoryItems.ToList();
    }

    private static List<CustomItem> FormatItems(IEnumerable<NormalInventoryItem> itemList)
    {
        return itemList.Where(x => x?.Item?.IsValid == true).Select(inventoryItem => new CustomItem(inventoryItem)).ToList();
    }

    private static bool TryGetShardParent(string shardBaseName, out string shardParent)
    {
        return ShardMapping.TryGetValue(shardBaseName, out shardParent);
    }

    private void GetHoveredItem()
    {
        try
        {
            var uiHover = GameController.Game.IngameState.UIHover;
            if (uiHover.Address != 0 && uiHover.AsObject<HoverItemIcon>().ToolTipType != ToolTipType.ItemInChat)
            {
                var inventoryItemIcon = uiHover.AsObject<NormalInventoryItem>();
                var tooltip = inventoryItemIcon.Tooltip;
                var poeEntity = inventoryItemIcon.Item;
                if (tooltip != null && poeEntity.Address != 0 && poeEntity.IsValid)
                {
                    var item = inventoryItemIcon.Item;
                    var baseItemType = GameController.Files.BaseItemTypes.Translate(item.Path);
                    if (baseItemType != null)
                    {
                        HoveredItem = new CustomItem(inventoryItemIcon);
                        if (Settings.DebugSettings.InspectHoverHotkey.PressedOnce())
                        {
                            _inspectedItem = HoveredItem;
                        }
                        if (HoveredItem.ItemType != ItemTypes.None)
                            GetValue(HoveredItem);
                    }
                }
            }

            HoveredItemTooltipRect = HoveredItem?.Element?.Tooltip?.GetClientRectCache;
        }
        catch (Exception ex)
        {
            DebugWindow.LogError($"Failed to get the hovered item: {ex}");
        }
    }

    private void GetValue(IEnumerable<CustomItem> items)
    {
        foreach (var customItem in items)
        {
            GetValue(customItem);
        }
    }

    private T GetValue<T>(T items) where T : IReadOnlyCollection<CustomItem>
    {
        foreach (var customItem in items)
        {
            GetValue(customItem);
        }

        return items;
    }

    private ItemTypes? FindUniqueType(string name)
    {
        return name switch
        {
            _ when CollectedData.UniqueAccessories.Lines.Find(x => x.Name == name) is { } => ItemTypes.UniqueAccessory,
            _ when CollectedData.UniqueArmours.Lines.Find(x => x.Name == name) is { } => ItemTypes.UniqueArmour,
            _ when CollectedData.UniqueFlasks.Lines.Find(x => x.Name == name) is { } => ItemTypes.UniqueFlask,
            _ when CollectedData.UniqueJewels.Lines.Find(x => x.Name == name) is { } => ItemTypes.UniqueJewel,
            _ when CollectedData.UniqueWeapons.Lines.Find(x => x.Name == name) is { } => ItemTypes.UniqueWeapon,
            _ when CollectedData.UniqueMaps.Lines.Find(x => x.Name == name) is { } => ItemTypes.UniqueMap,
            _ => null,
        };
    }

    private void GetValue(CustomItem item)
    {
        try
        {
            if(item.BaseName.Contains("Rogue's Marker"))
            {
                item.PriceData.MinChaosValue = 0;
            }
            else
            {
                switch (item.ItemType) // easier to get data for each item type and handle logic based on that
                {
                    // TODO: Complete
                    case ItemTypes.Currency:
                    {
                        if (item.BaseName.StartsWith("Chaos ")) // Chaos Orb or Shard
                        {
                            switch (item.CurrencyInfo.IsShard)
                            {
                                case false:
                                    item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize;
                                    break;
                                case true:
                                    item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize / 20.0;
                                    break;
                            }

                            break;
                        }

                        var (pricedStack, pricedItem) = item.CurrencyInfo.IsShard && TryGetShardParent(item.BaseName, out var shardParent)
                            ? (item.CurrencyInfo.MaxStackSize > 0 ? item.CurrencyInfo.MaxStackSize : 20, shardParent)
                            : (1, item.BaseName);
                        var currencySearch = CollectedData.Currency.LinesByName.GetValueOrDefault(pricedItem);
                        if (currencySearch != default)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * currencySearch.ChaosEquivalent / pricedStack;
                            item.PriceData.ChangeInLast7Days = currencySearch.Line.sparkline.totalChange ?? 0;
                            item.PriceData.DetailsId = currencySearch.Item.detailsId;
                        }

                        break;
                    }
                    case ItemTypes.Catalyst:
                        var catalystSearch = CollectedData.Currency.LinesByName.GetValueOrDefault(item.BaseName);
                        if (catalystSearch != default)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * catalystSearch.ChaosEquivalent;
                            item.PriceData.ChangeInLast7Days = catalystSearch.Line.sparkline.totalChange ?? 0;
                            item.PriceData.DetailsId = catalystSearch.Item.detailsId;
                        }

                        break;
                    case ItemTypes.DivinationCard:
                        var divinationSearch = CollectedData.DivinationCards.LinesByName.GetValueOrDefault(item.BaseName);
                        if (divinationSearch != default)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * divinationSearch.ChaosEquivalent;
                            item.PriceData.ChangeInLast7Days = divinationSearch.Line.sparkline.totalChange ?? 0;
                            item.PriceData.DetailsId = divinationSearch.Item.detailsId;
                        }

                        break;
                    case ItemTypes.Essence:
                        var essenceSearch = CollectedData.Essences.LinesByName.GetValueOrDefault(item.BaseName);
                        if (essenceSearch != default)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * essenceSearch.ChaosEquivalent;
                            item.PriceData.ChangeInLast7Days = essenceSearch.Line.sparkline.totalChange ?? 0;
                            item.PriceData.DetailsId = essenceSearch.Item.detailsId;
                        }

                        break;
                    case ItemTypes.Oil:
                        var oilSearch = CollectedData.Oils.LinesByName.GetValueOrDefault(item.BaseName);
                        if (oilSearch != default)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * oilSearch.ChaosEquivalent;
                            item.PriceData.ChangeInLast7Days = oilSearch.Line.sparkline.totalChange ?? 0;
                            item.PriceData.DetailsId = oilSearch.Item.detailsId;
                        }
                        break;
                    case ItemTypes.Tattoo:
                        var tattooSearch = CollectedData.Tattoos.LinesByName.GetValueOrDefault(item.BaseName);
                        if (tattooSearch != default)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * tattooSearch.ChaosEquivalent;
                            item.PriceData.ChangeInLast7Days = tattooSearch.Line.sparkline.totalChange ?? 0;
                            item.PriceData.DetailsId = tattooSearch.Item.detailsId;
                        }
                        break;
                    case ItemTypes.Omen:
                        var omenSearch = CollectedData.Omens.LinesByName.GetValueOrDefault(item.BaseName);
                        if (omenSearch != default)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * omenSearch.ChaosEquivalent;
                            item.PriceData.ChangeInLast7Days = omenSearch.Line.sparkline.totalChange ?? 0;
                            item.PriceData.DetailsId = omenSearch.Item.detailsId;
                        }
                        break;
                    case ItemTypes.Artifact:
                        var artifactSearch = CollectedData.Artifacts.LinesByName.GetValueOrDefault(item.BaseName);
                        if (artifactSearch != default)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * artifactSearch.ChaosEquivalent;
                            item.PriceData.ChangeInLast7Days = artifactSearch.Line.sparkline.totalChange ?? 0;
                            item.PriceData.DetailsId = artifactSearch.Item.detailsId;
                        }

                        break;
                    case ItemTypes.Fragment:
                    {
                        var (pricedStack, pricedItem) = item.CurrencyInfo.IsShard && TryGetShardParent(item.BaseName, out var shardParent)
                            ? (item.CurrencyInfo.MaxStackSize > 0 ? item.CurrencyInfo.MaxStackSize : 20, shardParent)
                            : (1, item.BaseName);
                        var fragmentSearch = CollectedData.Fragments.LinesByName.GetValueOrDefault(pricedItem);
                        if (fragmentSearch != default)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * fragmentSearch.ChaosEquivalent / pricedStack;
                            item.PriceData.ChangeInLast7Days = fragmentSearch.Line.sparkline.totalChange ?? 0;
                            item.PriceData.DetailsId = fragmentSearch.Item.detailsId;
                        }

                        break;
                    }
                    case ItemTypes.SkillGem:
                        var displayText = !string.IsNullOrEmpty(item.GemName) ? item.GemName : item.BaseName;
                        var fittingGems = CollectedData.SkillGems.Lines
                           .Where(x => x.Name == displayText).ToList();
                        var gemSearch = MoreLinq.MoreEnumerable.MaxBy(fittingGems,
                            x => (x.GemLevel == item.GemLevel,
                                  x.Corrupted == item.IsCorrupted,
                                  x.GemQuality == item.Quality,
                                  x.GemQuality == item.Quality switch { > 15 and < 21 => 20, var o => o },
                                  x.GemQuality <= item.Quality,
                                  x.GemLevel > item.GemLevel ? -x.GemLevel : 0,
                                  x.GemLevel + x.GemQuality)).ToList();

                        if (gemSearch.Any())
                        {
                            var minValueRecord = gemSearch.MinBy(x => x.ChaosValue)!;
                            item.PriceData.MinChaosValue = minValueRecord.ChaosValue;
                            item.PriceData.ChangeInLast7Days = minValueRecord.Sparkline.Data?.Any() == true
                                                                   ? minValueRecord.Sparkline.TotalChange
                                                                   : minValueRecord.LowConfidenceSparkline.TotalChange;
                            item.PriceData.DetailsId = minValueRecord.DetailsId;
                        }

                        break;
                    case ItemTypes.ClusterJewel:
                        var passivesText = $"{item.ClusterJewelData.PassiveCount} passives";
                        var fittingJewels = CollectedData.ClusterJewels.Lines.Where(x =>
                            x.Name == item.ClusterJewelData.Name &&
                            x.Variant == passivesText &&
                            x.LevelRequired <= item.ItemLevel).ToList();
                        if (fittingJewels.Any())
                        {
                            var bestFit = fittingJewels.MaxBy(x => x.LevelRequired);
                            item.PriceData.MinChaosValue = bestFit.ChaosValue;
                            item.PriceData.ChangeInLast7Days = bestFit.Sparkline.Data?.Any() == true
                                ? bestFit.Sparkline.TotalChange
                                : bestFit.LowConfidenceSparkline.TotalChange;
                            item.PriceData.DetailsId = bestFit.DetailsId;
                        }

                        break;
                    case ItemTypes.Invitation:
                        var invitationSearch = CollectedData.Invitations.Lines.Find(x => x.Name == item.BaseName);
                        if (invitationSearch != null)
                        {
                            item.PriceData.MinChaosValue = invitationSearch.ChaosValue ?? 0;
                            item.PriceData.ChangeInLast7Days = invitationSearch.Sparkline.TotalChange ?? 0;
                            item.PriceData.DetailsId = invitationSearch.DetailsId;
                        }

                        break;
                    case ItemTypes.DeliriumOrbs:
                        var deliriumOrbsSearch = CollectedData.DeliriumOrb.LinesByName.GetValueOrDefault(item.BaseName);
                        if (deliriumOrbsSearch != default)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * deliriumOrbsSearch.ChaosEquivalent;
                            item.PriceData.ChangeInLast7Days = deliriumOrbsSearch.Line.sparkline.totalChange ?? 0;
                            item.PriceData.DetailsId = deliriumOrbsSearch.Item.detailsId;
                        }

                        break;
                    case ItemTypes.Vials:
                        var vialCurrencySearch = CollectedData.Vials.Lines.Find(x => x.Name == item.BaseName);
                        if (vialCurrencySearch != null)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * vialCurrencySearch.ChaosValue ?? 0;
                            item.PriceData.ChangeInLast7Days = vialCurrencySearch.Sparkline.TotalChange ?? 0;
                            item.PriceData.DetailsId = vialCurrencySearch.DetailsId;
                        }

                        break;
                    case ItemTypes.Incubator:
                        var incubatorSearch = CollectedData.Incubators.Lines.Find(x => x.Name == item.BaseName);
                        if (incubatorSearch != null)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * incubatorSearch.ChaosValue ?? 0;
                            item.PriceData.ChangeInLast7Days = incubatorSearch.Sparkline.TotalChange ?? 0;
                            item.PriceData.DetailsId = incubatorSearch.DetailsId;
                        }

                        break;
                    case ItemTypes.Scarab:
                        var scarabSearch = CollectedData.Scarabs.LinesByName.GetValueOrDefault(item.BaseName);
                        if (scarabSearch != default)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * scarabSearch.ChaosEquivalent;
                            item.PriceData.ChangeInLast7Days = scarabSearch.Line.sparkline.totalChange ?? 0;
                            item.PriceData.DetailsId = scarabSearch.Item.detailsId;
                        }

                        break;
                    case ItemTypes.UniqueAccessory:
                        var uniqueAccessorySearch = CollectedData.UniqueAccessories.Lines.FindAll(x =>
                            x.Name == item.UniqueName || item.UniqueNameCandidates.Contains(x.Name));
                        if (uniqueAccessorySearch.Count == 1)
                        {
                            item.PriceData.MinChaosValue = uniqueAccessorySearch[0].ChaosValue ?? 0;
                            item.PriceData.ChangeInLast7Days = uniqueAccessorySearch[0].Sparkline.TotalChange ?? 0;
                            item.PriceData.DetailsId = uniqueAccessorySearch[0].DetailsId;
                        }
                        else if (uniqueAccessorySearch.Count > 1)
                        {
                            item.PriceData.MinChaosValue = uniqueAccessorySearch.Min(x => x.ChaosValue) ?? 0;
                            item.PriceData.MaxChaosValue = uniqueAccessorySearch.Max(x => x.ChaosValue) ?? 0;
                            item.PriceData.ChangeInLast7Days = 0;
                            item.PriceData.DetailsId = uniqueAccessorySearch[0].DetailsId;
                        }
                        else
                        {
                            item.PriceData.MinChaosValue = 0;
                            item.PriceData.ChangeInLast7Days = 0;
                        }

                        break;
                    case ItemTypes.UniqueArmour:
                    {
                        var allLinksLines = CollectedData.UniqueArmours.Lines.Where(x =>
                            x.Name == item.UniqueName || item.UniqueNameCandidates.Contains(x.Name));
                        var uniqueArmourSearchLinks = item.LargestLink switch
                        {
                            < 5 => allLinksLines.Where(x => x.Links != 5 && x.Links != 6).ToList(),
                            5 => allLinksLines.Where(x => x.Links == 5).ToList(),
                            6 => allLinksLines.Where(x => x.Links == 6).ToList(),
                            _ => new List<UniqueArmours.Line>()
                        };

                        if (uniqueArmourSearchLinks.Count == 1)
                        {
                            item.PriceData.MinChaosValue = uniqueArmourSearchLinks[0].ChaosValue ?? 0;
                            item.PriceData.ChangeInLast7Days = uniqueArmourSearchLinks[0].Sparkline.TotalChange ?? 0;
                            item.PriceData.DetailsId = uniqueArmourSearchLinks[0].DetailsId;
                        }
                        else if (uniqueArmourSearchLinks.Count > 1)
                        {
                            item.PriceData.MinChaosValue = uniqueArmourSearchLinks.Min(x => x.ChaosValue) ?? 0;
                            item.PriceData.MaxChaosValue = uniqueArmourSearchLinks.Max(x => x.ChaosValue) ?? 0;
                            item.PriceData.ChangeInLast7Days = 0;
                            item.PriceData.DetailsId = uniqueArmourSearchLinks[0].DetailsId;
                        }
                        else
                        {
                            item.PriceData.MinChaosValue = 0;
                            item.PriceData.ChangeInLast7Days = 0;
                        }

                        break;
                    }
                    case ItemTypes.UniqueFlask:
                        var uniqueFlaskSearch = CollectedData.UniqueFlasks.Lines.FindAll(x =>
                            x.Name == item.UniqueName || item.UniqueNameCandidates.Contains(x.Name));
                        if (uniqueFlaskSearch.Count == 1)
                        {
                            item.PriceData.MinChaosValue = uniqueFlaskSearch[0].ChaosValue ?? 0;
                            item.PriceData.ChangeInLast7Days = uniqueFlaskSearch[0].Sparkline.TotalChange ?? 0;
                            item.PriceData.DetailsId = uniqueFlaskSearch[0].DetailsId;
                        }
                        else if (uniqueFlaskSearch.Count > 1)
                        {
                            item.PriceData.MinChaosValue = uniqueFlaskSearch.Min(x => x.ChaosValue) ?? 0;
                            item.PriceData.MaxChaosValue = uniqueFlaskSearch.Max(x => x.ChaosValue) ?? 0;
                            item.PriceData.ChangeInLast7Days = 0;
                            item.PriceData.DetailsId = uniqueFlaskSearch[0].DetailsId;
                        }
                        else
                        {
                            item.PriceData.MinChaosValue = 0;
                            item.PriceData.ChangeInLast7Days = 0;
                        }

                        break;
                    case ItemTypes.UniqueJewel:
                        var uniqueJewelSearch = CollectedData.UniqueJewels.Lines.FindAll(x =>
                            x.Name == item.UniqueName || item.UniqueNameCandidates.Contains(x.Name));
                        if (uniqueJewelSearch.Count == 1)
                        {
                            item.PriceData.MinChaosValue = uniqueJewelSearch[0].ChaosValue ?? 0;
                            item.PriceData.ChangeInLast7Days = uniqueJewelSearch[0].Sparkline.TotalChange ?? 0;
                            item.PriceData.DetailsId = uniqueJewelSearch[0].DetailsId;
                        }
                        else if (uniqueJewelSearch.Count > 1)
                        {
                            item.PriceData.MinChaosValue = uniqueJewelSearch.Min(x => x.ChaosValue) ?? 0;
                            item.PriceData.MaxChaosValue = uniqueJewelSearch.Max(x => x.ChaosValue) ?? 0;
                            item.PriceData.ChangeInLast7Days = 0;
                            item.PriceData.DetailsId = uniqueJewelSearch[0].DetailsId;
                        }
                        else
                        {
                            item.PriceData.MinChaosValue = 0;
                            item.PriceData.ChangeInLast7Days = 0;
                        }

                        break;
                    case ItemTypes.UniqueMap:
                        var uniqueMapSearch = CollectedData.UniqueMaps.Lines.FindAll(x =>
                            (x.Name == item.UniqueName || item.UniqueNameCandidates.Contains(x.Name)) &&
                            item.MapInfo.MapTier == x.MapTier);
                        if (uniqueMapSearch.Count == 1)
                        {
                            item.PriceData.MinChaosValue = uniqueMapSearch[0].ChaosValue ?? 0;
                            item.PriceData.ChangeInLast7Days = uniqueMapSearch[0].Sparkline.TotalChange ?? 0;
                            item.PriceData.DetailsId = uniqueMapSearch[0].DetailsId;
                        }
                        else if (uniqueMapSearch.Count > 1)
                        {
                            item.PriceData.MinChaosValue = uniqueMapSearch.Min(x => x.ChaosValue) ?? 0;
                            item.PriceData.MaxChaosValue = uniqueMapSearch.Max(x => x.ChaosValue) ?? 0;
                            item.PriceData.ChangeInLast7Days = 0;
                            item.PriceData.DetailsId = uniqueMapSearch[0].DetailsId;
                        }
                        else
                        {
                            item.PriceData.MinChaosValue = 0;
                            item.PriceData.ChangeInLast7Days = 0;
                        }

                        break;
                    case ItemTypes.Resonator:
                        var resonatorSearch = CollectedData.Resonators.LinesByName.GetValueOrDefault(item.BaseName);
                        if (resonatorSearch != default)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * resonatorSearch.ChaosEquivalent;
                            item.PriceData.ChangeInLast7Days = resonatorSearch.Line.sparkline.totalChange ?? 0;
                            item.PriceData.DetailsId = resonatorSearch.Item.detailsId;
                        }

                        break;
                    case ItemTypes.Fossil:
                        var fossilSearch = CollectedData.Fossils.LinesByName.GetValueOrDefault(item.BaseName);
                        if (fossilSearch != default)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * fossilSearch.ChaosEquivalent;
                            item.PriceData.ChangeInLast7Days = fossilSearch.Line.sparkline.totalChange ?? 0;
                            item.PriceData.DetailsId = fossilSearch.Item.detailsId;
                        }

                        break;
                    case ItemTypes.UniqueWeapon:
                    {
                        var allLinksLines = CollectedData.UniqueWeapons.Lines.Where(x =>
                            x.Name == item.UniqueName || item.UniqueNameCandidates.Contains(x.Name));
                        var uniqueArmourSearchLinks = item.LargestLink switch
                        {
                            < 5 => allLinksLines.Where(x => x.Links != 5 && x.Links != 6).ToList(),
                            5 => allLinksLines.Where(x => x.Links == 5).ToList(),
                            6 => allLinksLines.Where(x => x.Links == 6).ToList(),
                            _ => new List<UniqueWeapons.Line>()
                        };
                        if (uniqueArmourSearchLinks.Count == 1)
                        {
                            item.PriceData.MinChaosValue = uniqueArmourSearchLinks[0].ChaosValue ?? 0;
                            item.PriceData.ChangeInLast7Days = uniqueArmourSearchLinks[0].Sparkline.TotalChange ?? 0;
                            item.PriceData.DetailsId = uniqueArmourSearchLinks[0].DetailsId;
                        }
                        else if (uniqueArmourSearchLinks.Count > 1)
                        {
                            item.PriceData.MinChaosValue = uniqueArmourSearchLinks.Min(x => x.ChaosValue) ?? 0;
                            item.PriceData.MaxChaosValue = uniqueArmourSearchLinks.Max(x => x.ChaosValue) ?? 0;
                            item.PriceData.ChangeInLast7Days = 0;
                            item.PriceData.DetailsId = uniqueArmourSearchLinks[0].DetailsId;
                        }
                        else
                        {
                            item.PriceData.MinChaosValue = 0;
                            item.PriceData.ChangeInLast7Days = 0;
                        }

                        break;
                    }
                    case ItemTypes.Map:
                        switch (item.MapInfo.MapType)
                        {
                            case MapTypes.Blighted:

                                var blightedBaseName = $"Blighted {item.BaseName}";
                                var normalBlightedMapSearch = Settings.DataSourceSettings.CheckMapVariant.Value
                                    ? CollectedData.BlightedMaps.Lines.Find(x =>
                                        x.BaseType == blightedBaseName && item.MapInfo.MapTier == x.MapTier &&
                                        x.Variant == Settings.DataSourceSettings.League.Value)
                                    : CollectedData.BlightedMaps.Lines.Find(x =>
                                        x.BaseType == blightedBaseName && item.MapInfo.MapTier == x.MapTier);

                                if (normalBlightedMapSearch != null)
                                {
                                    item.PriceData.MinChaosValue = normalBlightedMapSearch.ChaosValue ?? 0;
                                    item.PriceData.ChangeInLast7Days = normalBlightedMapSearch.Sparkline.TotalChange ?? 0;
                                    item.PriceData.DetailsId = normalBlightedMapSearch.DetailsId;
                                }

                                break;
                            case MapTypes.BlightRavaged:

                                var blightRavagedBaseName = $"Blight-ravaged {item.BaseName}";
                                var blightRavagedMapSearch = Settings.DataSourceSettings.CheckMapVariant.Value
                                    ? CollectedData.BlightRavagedMaps.Lines.Find(x =>
                                        x.BaseType == blightRavagedBaseName && item.MapInfo.MapTier == x.MapTier &&
                                        x.Variant == Settings.DataSourceSettings.League.Value)
                                    : CollectedData.BlightRavagedMaps.Lines.Find(x =>
                                        x.BaseType == blightRavagedBaseName && item.MapInfo.MapTier == x.MapTier);

                                if (blightRavagedMapSearch != null)
                                {
                                    item.PriceData.MinChaosValue = blightRavagedMapSearch.ChaosValue ?? 0;
                                    item.PriceData.ChangeInLast7Days = blightRavagedMapSearch.Sparkline.TotalChange ?? 0;
                                    item.PriceData.DetailsId = blightRavagedMapSearch.DetailsId;
                                }

                                break;
                            case MapTypes.None:

                                var normalMapSearch = Settings.DataSourceSettings.CheckMapVariant.Value
                                                          ? CollectedData.WhiteMaps.Lines.Find(x =>
                                                              x.BaseType == item.BaseName && item.MapInfo.MapTier == x.MapTier && x.Variant == Settings.DataSourceSettings.League.Value)
                                                          : CollectedData.WhiteMaps.Lines.Find(x =>
                                                              x.BaseType == item.BaseName && item.MapInfo.MapTier == x.MapTier);

                                if (normalMapSearch != null)
                                {
                                    item.PriceData.MinChaosValue = normalMapSearch.ChaosValue ?? 0;
                                    item.PriceData.ChangeInLast7Days = normalMapSearch.Sparkline.TotalChange ?? 0;
                                    item.PriceData.DetailsId = normalMapSearch.DetailsId;
                                }

                                break;
                        }

                        break;
                    case ItemTypes.Beast:
                        var beastSearch = CollectedData.Beasts.lines.Find(x => x.name == item.CapturedMonsterName);
                        if (beastSearch != null)
                        {
                            item.PriceData.MinChaosValue = beastSearch.chaosValue ?? 0;
                            item.PriceData.ChangeInLast7Days = beastSearch.sparkline.totalChange ?? 0;
                            item.PriceData.DetailsId = beastSearch.detailsId;
                        }

                        break;
                    case ItemTypes.KalguuranRune:
                        var runeSearch = CollectedData.KalguuranRunes.LinesByName.GetValueOrDefault(item.BaseName);
                        if (runeSearch != default)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * runeSearch.ChaosEquivalent;
                            item.PriceData.ChangeInLast7Days = runeSearch.Line.sparkline.totalChange ?? 0;
                            item.PriceData.DetailsId = runeSearch.Item.detailsId;
                        }

                        break;
                    case ItemTypes.AllflameEmber:
                        var allflameSearch = CollectedData.AllflameEmbers.LinesByName.GetValueOrDefault(item.BaseName);
                        if (allflameSearch != default)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * allflameSearch.ChaosEquivalent;
                            item.PriceData.ChangeInLast7Days = allflameSearch.Line.sparkline.totalChange ?? 0;
                            item.PriceData.DetailsId = allflameSearch.Item.detailsId;
                        }

                        break;
                    case ItemTypes.InscribedUltimatum:
                        if (item.Entity.TryGetComponent<UltimatumTrial>(out var ultimatumTrial))
                        {
                            switch (ultimatumTrial.Reward.RewardType)
                            {
                                case UltimatumItemisedRewardType.Mirror:
                                {
                                    var line = CollectedData.Currency.LinesByName.GetValueOrDefault("Mirror of Kalandra") switch { (null, null, 0) => (null, null, 100000), var o => o };
                                    item.PriceData.MinChaosValue = line.ChaosEquivalent;
                                    item.PriceData.ChangeInLast7Days = line.Line?.sparkline.totalChange ?? 0;
                                    item.PriceData.DetailsId = "ultimatum-inscribed-mirror";
                                    break;
                                }
                                case UltimatumItemisedRewardType.Currency:
                                {
                                    var sacItem = new CustomItem(ultimatumTrial.Reward.SacrificeItem) { CurrencyInfo = { StackSize = ultimatumTrial.Reward.SacrificeAmount } };
                                    GetValue(sacItem);
                                    item.PriceData.MinChaosValue = sacItem.PriceData.MinChaosValue;
                                    item.PriceData.ChangeInLast7Days = sacItem.PriceData.ChangeInLast7Days;
                                    item.PriceData.DetailsId = $"ultimatum-inscribed-currency-{sacItem.PriceData.DetailsId}";
                                    break;
                                }
                                case UltimatumItemisedRewardType.DivinationCard:
                                {
                                    var sacItem = new CustomItem(ultimatumTrial.SacrificedItemType)
                                    {
                                        CurrencyInfo =
                                        {
                                            StackSize = ((ultimatumTrial.SacrificedItemType.CurrencyInfo?.MaxStackSize ?? 1) + 1) / 2
                                        }
                                    };
                                    GetValue(sacItem);
                                    item.PriceData.MinChaosValue = sacItem.PriceData.MinChaosValue;
                                    item.PriceData.ChangeInLast7Days = sacItem.PriceData.ChangeInLast7Days;
                                    item.PriceData.DetailsId = $"ultimatum-inscribed-div-{sacItem.PriceData.DetailsId}";
                                    break;
                                }
                                case UltimatumItemisedRewardType.Unique:
                                {
                                    var sacUniqueType = FindUniqueType(ultimatumTrial.SacrificedItemWord.Text);
                                    var gainUniqueType = FindUniqueType(ultimatumTrial.RewardItemWord.Text);
                                    var sacItem = new CustomItem(ultimatumTrial.SacrificedItemWord.Text, sacUniqueType ?? ItemTypes.None) { MapInfo = { MapTier = 16 } };
                                    GetValue(sacItem);
                                    var gainItem = new CustomItem(ultimatumTrial.RewardItemWord.Text, gainUniqueType ?? ItemTypes.None) { MapInfo = { MapTier = 16 } };
                                    GetValue(gainItem);
                                    item.PriceData.MinChaosValue = gainItem.PriceData.MinChaosValue - sacItem.PriceData.MinChaosValue;
                                    item.PriceData.DetailsId = $"ultimatum-inscribed-unique-{sacItem.PriceData.DetailsId}-{gainItem.PriceData.DetailsId}";
                                    break;
                                }
                            }
                        }
                        break;
                }
            }
        }
        catch (Exception)
        {
            if (Settings.DebugSettings.EnableDebugLogging) { LogMessage($"{GetCurrentMethod()}.GetValue()", 5, Color.Red); }
        }
        finally
        {
            if (item.PriceData.MaxChaosValue == 0)
            {
                item.PriceData.MaxChaosValue = item.PriceData.MinChaosValue;
            }
        }
    }

    private void GetValueHaggle(CustomItem item)
    {
        try
        {
            switch (item.ItemType) // easier to get data for each item type and handle logic based on that
            {
                case ItemTypes.UniqueArmour:
                    var uniqueArmourSearch = CollectedData.UniqueArmours.Lines.FindAll(x => x.BaseType == item.BaseName && x.IsChanceable() && (x.Links < 5 || x.Links == null));
                    if (uniqueArmourSearch.Count > 0)
                    {
                        foreach (var result in uniqueArmourSearch)
                        {
                            item.PriceData.ItemBasePrices.Add((double)result.ChaosValue);
                        }
                    }
                    break;
                case ItemTypes.UniqueWeapon:
                    var uniqueWeaponSearch = CollectedData.UniqueWeapons.Lines.FindAll(x => x.BaseType == item.BaseName && x.IsChanceable() && (x.Links < 5 || x.Links == null));
                    if (uniqueWeaponSearch.Count > 0)
                    {
                        foreach (var result in uniqueWeaponSearch)
                        {
                            item.PriceData.ItemBasePrices.Add((double)result.ChaosValue);
                        }
                    }
                    break;
                case ItemTypes.UniqueAccessory:
                    var uniqueAccessorySearch = CollectedData.UniqueAccessories.Lines.FindAll(x => x.BaseType == item.BaseName && x.IsChanceable());
                    if (uniqueAccessorySearch.Count > 0)
                    {
                        foreach (var result in uniqueAccessorySearch)
                        {
                            item.PriceData.ItemBasePrices.Add((double)result.ChaosValue);
                        }
                    }
                    break;
                case ItemTypes.UniqueJewel:
                    var uniqueJewelSearch = CollectedData.UniqueJewels.Lines.FindAll(x => x.DetailsId.Contains(item.BaseName.ToLower().Replace(" ", "-")) && x.IsChanceable());
                    if (uniqueJewelSearch.Count > 0)
                    {
                        foreach (var result in uniqueJewelSearch)
                        {
                            item.PriceData.ItemBasePrices.Add((double)result.ChaosValue);
                        }
                    }
                    break;
            }
        }
        catch (Exception e)
        {
            if (Settings.DebugSettings.EnableDebugLogging)
            {
                LogMessage($"{GetCurrentMethod()}.GetValueHaggle() Error that i dont understand, Item: {item.BaseName}", 5, Color.Red);
                LogMessage($"{GetCurrentMethod()}.GetValueHaggle() {e.Message}", 5, Color.Red);
            }
        }
    }

    private bool ShouldUpdateValues()
    {
        if (StashUpdateTimer.ElapsedMilliseconds > Settings.DataSourceSettings.ItemUpdatePeriodMs)
        {
            StashUpdateTimer.Restart();
            if (Settings.DebugSettings.EnableDebugLogging) { LogMessage($"{GetCurrentMethod()} ValueUpdateTimer.Restart()", 5, Color.DarkGray); }
        }
        else
        {
            return false;
        }
        // TODO: Get inventory items and not just stash tab items, this will be done at a later date
        try
        {
            if (!Settings.StashValueSettings.Show)
            {
                if (Settings.DebugSettings.EnableDebugLogging) { LogMessage($"{GetCurrentMethod()}.ShouldUpdateValues() Stash is not visible", 5, Color.DarkGray); }
                return false;
            }
        }
        catch (Exception)
        {
            if (Settings.DebugSettings.EnableDebugLogging) LogMessage($"{GetCurrentMethod()}.ShouldUpdateValues()", 5, Color.DarkGray);
            return false;
        }

        if (Settings.DebugSettings.EnableDebugLogging) LogMessage($"{GetCurrentMethod()}.ShouldUpdateValues() == True", 5, Color.LimeGreen);
        return true;
    }

    private bool ShouldUpdateValuesInventory()
    {
        if (InventoryUpdateTimer.ElapsedMilliseconds > Settings.DataSourceSettings.ItemUpdatePeriodMs)
        {
            InventoryUpdateTimer.Restart();
            if (Settings.DebugSettings.EnableDebugLogging) { LogMessage($"{GetCurrentMethod()} ValueUpdateTimer.Restart()", 5, Color.DarkGray); }
        }
        else
        {
            return false;
        }
        // TODO: Get inventory items and not just stash tab items, this will be done at a later date
        try
        {
            if (!Settings.InventoryValueSettings.Show.Value || !GameController.Game.IngameState.IngameUi.InventoryPanel.IsVisible)
            {
                if (Settings.DebugSettings.EnableDebugLogging) { LogMessage($"{GetCurrentMethod()}.ShouldUpdateValuesInventory() Inventory is not visible", 5, Color.DarkGray); }
                return false;
            }

            // Dont continue if the stash page isnt even open
            if (GameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory].VisibleInventoryItems == null)
            {
                if (Settings.DebugSettings.EnableDebugLogging) LogMessage($"{GetCurrentMethod()}.ShouldUpdateValuesInventory() Items == null", 5, Color.DarkGray);
                return false;
            }
        }
        catch (Exception)
        {
            if (Settings.DebugSettings.EnableDebugLogging) LogMessage($"{GetCurrentMethod()}.ShouldUpdateValuesInventory()", 5, Color.DarkGray);
            return false;
        }

        if (Settings.DebugSettings.EnableDebugLogging) LogMessage($"{GetCurrentMethod()}.ShouldUpdateValuesInventory() == True", 5, Color.LimeGreen);
        return true;
    }
}