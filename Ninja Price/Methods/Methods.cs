using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared.Enums;
using Ninja_Price.API.PoeNinja;
using Ninja_Price.Enums;
using System;
using System.Collections;
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

    private static void SetPriceChangeData(RelevantPriceData priceData, double? totalChange, IEnumerable sparklineData)
    {
        priceData.ChangeInLast7Days = totalChange ?? 0;
        if (sparklineData == null)
        {
            priceData.ChangeSparkline7Days = [];
            return;
        }

        var points = (from object value in sparklineData select ConvertSparklinePoint(value)).ToList();

        priceData.ChangeSparkline7Days = points;
    }

    private static float? ConvertSparklinePoint(object value) => value == null ? null : Convert.ToSingle(value);

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
            item.PriceData.ChangeSparkline7Days = [];
            if(item.BaseName.Contains("Rogue's Marker"))
            {
                item.PriceData.MinChaosValue = 0;
            }
            else if (!Settings.ValuationDisablingSettings.IsValuationDisabled(item.ItemType))
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
                            SetPriceChangeData(item.PriceData, currencySearch.Line.sparkline.totalChange, currencySearch.Line.sparkline.data);
                            item.PriceData.DetailsId = currencySearch.Item.detailsId;
                        }

                        break;
                    }
                    case ItemTypes.DjinnCoin:
                        var djinnSearch = CollectedData.DjinnCoins.LinesByName.GetValueOrDefault(item.BaseName);
                        if (djinnSearch != default)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * djinnSearch.ChaosEquivalent;
                            SetPriceChangeData(item.PriceData, djinnSearch.Line.sparkline.totalChange, djinnSearch.Line.sparkline.data);
                            item.PriceData.DetailsId = djinnSearch.Item.detailsId;
                        }
                        break;

                    case ItemTypes.Astrolabe:
                        var astrolabeSearch = CollectedData.Astrolabe.LinesByName.GetValueOrDefault(item.BaseName);
                        if (astrolabeSearch != default)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * astrolabeSearch.ChaosEquivalent;
                            SetPriceChangeData(item.PriceData, astrolabeSearch.Line.sparkline.totalChange, astrolabeSearch.Line.sparkline.data);
                            item.PriceData.DetailsId = astrolabeSearch.Item.detailsId;
                        }

                        break;
                    case ItemTypes.Catalyst:
                        var catalystSearch = CollectedData.Currency.LinesByName.GetValueOrDefault(item.BaseName);
                        if (catalystSearch != default)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * catalystSearch.ChaosEquivalent;
                            SetPriceChangeData(item.PriceData, catalystSearch.Line.sparkline.totalChange, catalystSearch.Line.sparkline.data);
                            item.PriceData.DetailsId = catalystSearch.Item.detailsId;
                        }

                        break;
                    case ItemTypes.DivinationCard:
                        var divinationSearch = CollectedData.DivinationCards.LinesByName.GetValueOrDefault(item.BaseName);
                        if (divinationSearch != default)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * divinationSearch.ChaosEquivalent;
                            SetPriceChangeData(item.PriceData, divinationSearch.Line.sparkline.totalChange, divinationSearch.Line.sparkline.data);
                            item.PriceData.DetailsId = divinationSearch.Item.detailsId;
                        }

                        break;
                    case ItemTypes.Essence:
                        var essenceSearch = CollectedData.Essences.LinesByName.GetValueOrDefault(item.BaseName);
                        if (essenceSearch != default)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * essenceSearch.ChaosEquivalent;
                            SetPriceChangeData(item.PriceData, essenceSearch.Line.sparkline.totalChange, essenceSearch.Line.sparkline.data);
                            item.PriceData.DetailsId = essenceSearch.Item.detailsId;
                        }

                        break;
                    case ItemTypes.Oil:
                        var oilSearch = CollectedData.Oils.LinesByName.GetValueOrDefault(item.BaseName);
                        if (oilSearch != default)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * oilSearch.ChaosEquivalent;
                            SetPriceChangeData(item.PriceData, oilSearch.Line.sparkline.totalChange, oilSearch.Line.sparkline.data);
                            item.PriceData.DetailsId = oilSearch.Item.detailsId;
                        }
                        break;
                    case ItemTypes.Tattoo:
                        var tattooSearch = CollectedData.Tattoos.LinesByName.GetValueOrDefault(item.BaseName);
                        if (tattooSearch != default)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * tattooSearch.ChaosEquivalent;
                            SetPriceChangeData(item.PriceData, tattooSearch.Line.sparkline.totalChange, tattooSearch.Line.sparkline.data);
                            item.PriceData.DetailsId = tattooSearch.Item.detailsId;
                        }
                        break;
                    case ItemTypes.Omen:
                        var omenSearch = CollectedData.Omens.LinesByName.GetValueOrDefault(item.BaseName);
                        if (omenSearch != default)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * omenSearch.ChaosEquivalent;
                            SetPriceChangeData(item.PriceData, omenSearch.Line.sparkline.totalChange, omenSearch.Line.sparkline.data);
                            item.PriceData.DetailsId = omenSearch.Item.detailsId;
                        }
                        break;
                    case ItemTypes.Artifact:
                        var artifactSearch = CollectedData.Artifacts.LinesByName.GetValueOrDefault(item.BaseName);
                        if (artifactSearch != default)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * artifactSearch.ChaosEquivalent;
                            SetPriceChangeData(item.PriceData, artifactSearch.Line.sparkline.totalChange, artifactSearch.Line.sparkline.data);
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
                            SetPriceChangeData(item.PriceData, fragmentSearch.Line.sparkline.totalChange, fragmentSearch.Line.sparkline.data);
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
                            if (minValueRecord.Sparkline.Data?.Any() == true)
                                SetPriceChangeData(item.PriceData, minValueRecord.Sparkline.TotalChange, minValueRecord.Sparkline.Data);
                            else
                                SetPriceChangeData(item.PriceData, minValueRecord.LowConfidenceSparkline.TotalChange, minValueRecord.LowConfidenceSparkline.Data);
                            item.PriceData.DetailsId = minValueRecord.DetailsId;
                        }

                        break;
                    case ItemTypes.BaseType:
                    {
                        var baseTypeName = item.BaseName;
                        var influenceFlags = new (string Name, bool IsPresent)[]
                        {
                            ("Shaper", item.IsShaper),
                            ("Elder", item.IsElder),
                            ("Crusader", item.IsCrusader),
                            ("Redeemer", item.IsRedeemer),
                            ("Warlord", item.IsWarlord),
                            ("Hunter", item.IsHunter)
                        };

                        var activeInfluences = influenceFlags.Where(x => x.IsPresent).Select(x => x.Name).ToList();
                        var canonicalVariant = string.Join("/", activeInfluences);
                        var hasInfluence = activeInfluences.Count > 0;
                        var effectiveItemLevel = item.ItemLevel >= 86 ? 86 : item.ItemLevel;
                        var matchingLines = (CollectedData.BaseType.Lines ?? Enumerable.Empty<BaseTypes.Line>()).Where(line =>
                            (line.BaseType ?? line.Name) == baseTypeName && line.LevelRequired == effectiveItemLevel && VariantMatches(line.Variant)).ToList();

                        if (matchingLines.Count == 0) break;

                        var cheapestMatch = matchingLines.MinBy(line => line.ChaosValue)!;
                        var hasPrimarySparklineData = cheapestMatch.Sparkline.Data.Any();
                        item.PriceData.MinChaosValue = cheapestMatch.ChaosValue;
                        if (hasPrimarySparklineData)
                            SetPriceChangeData(item.PriceData, cheapestMatch.Sparkline.TotalChange, cheapestMatch.Sparkline.Data);
                        else
                            SetPriceChangeData(item.PriceData, cheapestMatch.LowConfidenceSparkline.TotalChange, cheapestMatch.LowConfidenceSparkline.Data);
                        item.PriceData.DetailsId = cheapestMatch.DetailsId;
                        break;

                        bool VariantMatches(string? variant)
                        {
                            if (!hasInfluence) return string.IsNullOrWhiteSpace(variant);
                            return !string.IsNullOrWhiteSpace(variant) && string.Equals(variant.Trim(), canonicalVariant, StringComparison.OrdinalIgnoreCase);
                        }
                    }
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
                            if (bestFit.Sparkline.Data?.Any() == true)
                                SetPriceChangeData(item.PriceData, bestFit.Sparkline.TotalChange, bestFit.Sparkline.Data);
                            else
                                SetPriceChangeData(item.PriceData, bestFit.LowConfidenceSparkline.TotalChange, bestFit.LowConfidenceSparkline.Data);
                            item.PriceData.DetailsId = bestFit.DetailsId;
                        }

                        break;
                    case ItemTypes.Wombgift:
                        var wombgiftSearch = CollectedData.Wombgifts.Lines.Find(x => x.Name == item.BaseName && x.LevelRequired == item.WombgiftLevel);
                        if (wombgiftSearch != null)
                        {
                            item.PriceData.MinChaosValue = wombgiftSearch.ChaosValue ?? 0;
                            SetPriceChangeData(item.PriceData, wombgiftSearch.Sparkline.TotalChange, wombgiftSearch.Sparkline.Data);
                            item.PriceData.DetailsId = wombgiftSearch.DetailsId;
                        }
                        break;
                    case ItemTypes.Invitation:
                        var invitationSearch = CollectedData.Invitations.Lines.Find(x => x.Name == item.BaseName);
                        if (invitationSearch != null)
                        {
                            item.PriceData.MinChaosValue = invitationSearch.ChaosValue ?? 0;
                            SetPriceChangeData(item.PriceData, invitationSearch.Sparkline.TotalChange, invitationSearch.Sparkline.Data);
                            item.PriceData.DetailsId = invitationSearch.DetailsId;
                        }

                        break;
                    case ItemTypes.DeliriumOrbs:
                        var deliriumOrbsSearch = CollectedData.DeliriumOrb.LinesByName.GetValueOrDefault(item.BaseName);
                        if (deliriumOrbsSearch != default)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * deliriumOrbsSearch.ChaosEquivalent;
                            SetPriceChangeData(item.PriceData, deliriumOrbsSearch.Line.sparkline.totalChange, deliriumOrbsSearch.Line.sparkline.data);
                            item.PriceData.DetailsId = deliriumOrbsSearch.Item.detailsId;
                        }

                        break;
                    case ItemTypes.Vials:
                        var vialCurrencySearch = CollectedData.Vials.Lines.Find(x => x.Name == item.BaseName);
                        if (vialCurrencySearch != null)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * vialCurrencySearch.ChaosValue ?? 0;
                            SetPriceChangeData(item.PriceData, vialCurrencySearch.Sparkline.TotalChange, vialCurrencySearch.Sparkline.Data);
                            item.PriceData.DetailsId = vialCurrencySearch.DetailsId;
                        }

                        break;
                    case ItemTypes.Incubator:
                        var incubatorSearch = CollectedData.Incubators.Lines.Find(x => x.Name == item.BaseName);
                        if (incubatorSearch != null)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * incubatorSearch.ChaosValue ?? 0;
                            SetPriceChangeData(item.PriceData, incubatorSearch.Sparkline.TotalChange, incubatorSearch.Sparkline.Data);
                            item.PriceData.DetailsId = incubatorSearch.DetailsId;
                        }

                        break;
                    case ItemTypes.Scarab:
                        var scarabSearch = CollectedData.Scarabs.LinesByName.GetValueOrDefault(item.BaseName);
                        if (scarabSearch != default)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * scarabSearch.ChaosEquivalent;
                            SetPriceChangeData(item.PriceData, scarabSearch.Line.sparkline.totalChange, scarabSearch.Line.sparkline.data);
                            item.PriceData.DetailsId = scarabSearch.Item.detailsId;
                        }

                        break;
                    case ItemTypes.UniqueAccessory:
                    {
                        var uniqueName = item.UniqueName;
                        if (item.FoulbornMods.Any() && !string.IsNullOrEmpty(uniqueName))
                        {
                            uniqueName = $"Foulborn {uniqueName}";
                        }

                        var uniqueAccessorySearch = CollectedData.UniqueAccessories.Lines.FindAll(x =>
                            x.Name == uniqueName || item.UniqueNameCandidates.Contains(x.Name));
                        if (uniqueAccessorySearch.Where(x => item.FoulbornMods.SetEquals(x.MutatedModifiers.Select(m => m.Text))).ToList() is { Count: > 0 } refined)
                        {
                            uniqueAccessorySearch = refined;
                        }

                        if (uniqueAccessorySearch.Count == 1)
                        {
                            item.PriceData.MinChaosValue = uniqueAccessorySearch[0].ChaosValue ?? 0;
                            SetPriceChangeData(item.PriceData, uniqueAccessorySearch[0].Sparkline.TotalChange, uniqueAccessorySearch[0].Sparkline.Data);
                            item.PriceData.DetailsId = uniqueAccessorySearch[0].DetailsId;
                        }
                        else if (uniqueAccessorySearch.Count > 1)
                        {
                            item.PriceData.MinChaosValue = uniqueAccessorySearch.Min(x => x.ChaosValue) ?? 0;
                            item.PriceData.MaxChaosValue = uniqueAccessorySearch.Max(x => x.ChaosValue) ?? 0;
                            SetPriceChangeData(item.PriceData, 0, (IEnumerable<double?>)null);
                            item.PriceData.DetailsId = uniqueAccessorySearch[0].DetailsId;
                        }
                        else
                        {
                            item.PriceData.MinChaosValue = 0;
                            SetPriceChangeData(item.PriceData, 0, (IEnumerable<double?>)null);
                        }

                        break;
                    }
                    case ItemTypes.UniqueArmour:
                    {
                        var uniqueName = item.UniqueName;
                        if (item.FoulbornMods.Any() && !string.IsNullOrEmpty(uniqueName))
                        {
                            uniqueName = $"Foulborn {uniqueName}";
                        }

                        var allLinksLines = CollectedData.UniqueArmours.Lines.Where(x =>
                            x.Name == uniqueName || item.UniqueNameCandidates.Contains(x.Name));

                        var uniqueArmourSearchLinks = item.LargestLink switch
                        {
                            < 5 => allLinksLines.Where(x => x.Links != 5 && x.Links != 6).ToList(),
                            5 => allLinksLines.Where(x => x.Links == 5).ToList(),
                            6 => allLinksLines.Where(x => x.Links == 6).ToList(),
                            _ => new List<UniqueArmours.Line>()
                        };
                        if (uniqueArmourSearchLinks.Where(x => item.FoulbornMods.SetEquals(x.MutatedModifiers.Select(m => m.Text))).ToList() is { Count: > 0 } refined)
                        {
                            uniqueArmourSearchLinks = refined;
                        }

                        if (uniqueArmourSearchLinks.Count == 1)
                        {
                            item.PriceData.MinChaosValue = uniqueArmourSearchLinks[0].ChaosValue ?? 0;
                            SetPriceChangeData(item.PriceData, uniqueArmourSearchLinks[0].Sparkline.TotalChange, uniqueArmourSearchLinks[0].Sparkline.Data);
                            item.PriceData.DetailsId = uniqueArmourSearchLinks[0].DetailsId;
                        }
                        else if (uniqueArmourSearchLinks.Count > 1)
                        {
                            item.PriceData.MinChaosValue = uniqueArmourSearchLinks.Min(x => x.ChaosValue) ?? 0;
                            item.PriceData.MaxChaosValue = uniqueArmourSearchLinks.Max(x => x.ChaosValue) ?? 0;
                            SetPriceChangeData(item.PriceData, 0, (IEnumerable<double?>)null);
                            item.PriceData.DetailsId = uniqueArmourSearchLinks[0].DetailsId;
                        }
                        else
                        {
                            item.PriceData.MinChaosValue = 0;
                            SetPriceChangeData(item.PriceData, 0, (IEnumerable<double?>)null);
                        }

                        break;
                    }
                    case ItemTypes.UniqueFlask:
                    {
                        var uniqueName = item.UniqueName;
                        if (item.FoulbornMods.Any() && !string.IsNullOrEmpty(uniqueName))
                        {
                            uniqueName = $"Foulborn {uniqueName}";
                        }

                        var uniqueFlaskSearch = CollectedData.UniqueFlasks.Lines.FindAll(x =>
                            x.Name == uniqueName || item.UniqueNameCandidates.Contains(x.Name));
                        if (uniqueFlaskSearch.Where(x => item.FoulbornMods.SetEquals(x.MutatedModifiers.Select(m => m.Text))).ToList() is { Count: > 0 } refined)
                        {
                            uniqueFlaskSearch = refined;
                        }

                        if (uniqueFlaskSearch.Count == 1)
                        {
                            item.PriceData.MinChaosValue = uniqueFlaskSearch[0].ChaosValue ?? 0;
                            SetPriceChangeData(item.PriceData, uniqueFlaskSearch[0].Sparkline.TotalChange, uniqueFlaskSearch[0].Sparkline.Data);
                            item.PriceData.DetailsId = uniqueFlaskSearch[0].DetailsId;
                        }
                        else if (uniqueFlaskSearch.Count > 1)
                        {
                            item.PriceData.MinChaosValue = uniqueFlaskSearch.Min(x => x.ChaosValue) ?? 0;
                            item.PriceData.MaxChaosValue = uniqueFlaskSearch.Max(x => x.ChaosValue) ?? 0;
                            SetPriceChangeData(item.PriceData, 0, (IEnumerable<double?>)null);
                            item.PriceData.DetailsId = uniqueFlaskSearch[0].DetailsId;
                        }
                        else
                        {
                            item.PriceData.MinChaosValue = 0;
                            SetPriceChangeData(item.PriceData, 0, (IEnumerable<double?>)null);
                        }

                        break;
                    }
                    case ItemTypes.UniqueJewel:
                    {
                        var uniqueName = item.UniqueName;
                        if (item.FoulbornMods.Any() && !string.IsNullOrEmpty(uniqueName))
                        {
                            uniqueName = $"Foulborn {uniqueName}";
                        }
                        
                        var uniqueJewelSearch = CollectedData.UniqueJewels.Lines.FindAll(x =>
                            x.Name == uniqueName || item.UniqueNameCandidates.Contains(x.Name));
                        if (uniqueJewelSearch.Where(x => item.FoulbornMods.SetEquals(x.MutatedModifiers.Select(m => m.Text))).ToList() is { Count: > 0 } refined)
                        {
                            uniqueJewelSearch = refined;
                        }

                        if (uniqueJewelSearch.Count == 1)
                        {
                            item.PriceData.MinChaosValue = uniqueJewelSearch[0].ChaosValue ?? 0;
                            SetPriceChangeData(item.PriceData, uniqueJewelSearch[0].Sparkline.TotalChange, uniqueJewelSearch[0].Sparkline.Data);
                            item.PriceData.DetailsId = uniqueJewelSearch[0].DetailsId;
                        }
                        else if (uniqueJewelSearch.Count > 1)
                        {
                            item.PriceData.MinChaosValue = uniqueJewelSearch.Min(x => x.ChaosValue) ?? 0;
                            item.PriceData.MaxChaosValue = uniqueJewelSearch.Max(x => x.ChaosValue) ?? 0;
                            SetPriceChangeData(item.PriceData, 0, (IEnumerable<double?>)null);
                            item.PriceData.DetailsId = uniqueJewelSearch[0].DetailsId;
                        }
                        else
                        {
                            item.PriceData.MinChaosValue = 0;
                            SetPriceChangeData(item.PriceData, 0, (IEnumerable<double?>)null);
                        }

                        break;
                    }
                    case ItemTypes.UniqueMap:
                        var firstCandidate = item.UniqueNameCandidates?.FirstOrDefault();

                        var uniqueMapSearch = CollectedData.UniqueMaps.Lines.FindAll(x => x.BaseType == item.BaseName && (x.Name == item.UniqueName || x.Name == firstCandidate));
                        if (uniqueMapSearch.Count == 1)
                        {
                            item.PriceData.MinChaosValue = uniqueMapSearch[0].ChaosValue ?? 0;
                            SetPriceChangeData(item.PriceData, uniqueMapSearch[0].Sparkline.TotalChange, uniqueMapSearch[0].Sparkline.Data);
                            item.PriceData.DetailsId = uniqueMapSearch[0].DetailsId;
                        }
                        else if (uniqueMapSearch.Count > 1)
                        {
                            item.PriceData.MinChaosValue = uniqueMapSearch.Min(x => x.ChaosValue) ?? 0;
                            item.PriceData.MaxChaosValue = uniqueMapSearch.Max(x => x.ChaosValue) ?? 0;
                            SetPriceChangeData(item.PriceData, 0, (IEnumerable<double?>)null);
                            item.PriceData.DetailsId = uniqueMapSearch[0].DetailsId;
                        }
                        else
                        {
                            item.PriceData.MinChaosValue = 0;
                            SetPriceChangeData(item.PriceData, 0, (IEnumerable<double?>)null);
                        }

                        break;
                    case ItemTypes.Resonator:
                        var resonatorSearch = CollectedData.Resonators.LinesByName.GetValueOrDefault(item.BaseName);
                        if (resonatorSearch != default)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * resonatorSearch.ChaosEquivalent;
                            SetPriceChangeData(item.PriceData, resonatorSearch.Line.sparkline.totalChange, resonatorSearch.Line.sparkline.data);
                            item.PriceData.DetailsId = resonatorSearch.Item.detailsId;
                        }

                        break;
                    case ItemTypes.Fossil:
                        var fossilSearch = CollectedData.Fossils.LinesByName.GetValueOrDefault(item.BaseName);
                        if (fossilSearch != default)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * fossilSearch.ChaosEquivalent;
                            SetPriceChangeData(item.PriceData, fossilSearch.Line.sparkline.totalChange, fossilSearch.Line.sparkline.data);
                            item.PriceData.DetailsId = fossilSearch.Item.detailsId;
                        }

                        break;
                    case ItemTypes.UniqueWeapon:
                    {
                        var uniqueName = item.UniqueName;
                        if (item.FoulbornMods.Any() && !string.IsNullOrEmpty(uniqueName))
                        {
                            uniqueName = $"Foulborn {uniqueName}";
                        }

                        var allLinksLines = CollectedData.UniqueWeapons.Lines.Where(x =>
                            x.Name == uniqueName || item.UniqueNameCandidates.Contains(x.Name));
                        var uniqueArmourSearchLinks = item.LargestLink switch
                        {
                            < 5 => allLinksLines.Where(x => x.Links != 5 && x.Links != 6).ToList(),
                            5 => allLinksLines.Where(x => x.Links == 5).ToList(),
                            6 => allLinksLines.Where(x => x.Links == 6).ToList(),
                            _ => new List<UniqueWeapons.Line>()
                        };

                        if (uniqueArmourSearchLinks.Where(x => item.FoulbornMods.SetEquals(x.MutatedModifiers.Select(m => m.Text))).ToList() is { Count: > 0 } refined)
                        {
                            uniqueArmourSearchLinks = refined;
                        }

                        if (uniqueArmourSearchLinks.Count == 1)
                        {
                            item.PriceData.MinChaosValue = uniqueArmourSearchLinks[0].ChaosValue ?? 0;
                            SetPriceChangeData(item.PriceData, uniqueArmourSearchLinks[0].Sparkline.TotalChange, uniqueArmourSearchLinks[0].Sparkline.Data);
                            item.PriceData.DetailsId = uniqueArmourSearchLinks[0].DetailsId;
                        }
                        else if (uniqueArmourSearchLinks.Count > 1)
                        {
                            item.PriceData.MinChaosValue = uniqueArmourSearchLinks.Min(x => x.ChaosValue) ?? 0;
                            item.PriceData.MaxChaosValue = uniqueArmourSearchLinks.Max(x => x.ChaosValue) ?? 0;
                            SetPriceChangeData(item.PriceData, 0, (IEnumerable<double?>)null);
                            item.PriceData.DetailsId = uniqueArmourSearchLinks[0].DetailsId;
                        }
                        else
                        {
                            item.PriceData.MinChaosValue = 0;
                            SetPriceChangeData(item.PriceData, 0, (IEnumerable<double?>)null);
                        }

                        break;
                        }
                    case ItemTypes.Map:
                        switch (item.MapInfo.MapType)
                        {
                            case MapTypes.Blighted:

                                var blightedBaseName = $"Blighted {item.BaseName}";
                                var normalBlightedMapSearch = CollectedData.BlightedMaps.Lines.Find(x => x.BaseType == blightedBaseName);

                                if (normalBlightedMapSearch != null)
                                {
                                    item.PriceData.MinChaosValue = normalBlightedMapSearch.ChaosValue ?? 0;
                                    SetPriceChangeData(item.PriceData, normalBlightedMapSearch.Sparkline.TotalChange, normalBlightedMapSearch.Sparkline.Data);
                                    item.PriceData.DetailsId = normalBlightedMapSearch.DetailsId;
                                }

                                break;
                            case MapTypes.BlightRavaged:

                                var blightRavagedBaseName = $"Blight-ravaged {item.BaseName}";
                                var blightRavagedMapSearch = CollectedData.BlightRavagedMaps.Lines.Find(x => x.BaseType == blightRavagedBaseName);

                                if (blightRavagedMapSearch != null)
                                {
                                    item.PriceData.MinChaosValue = blightRavagedMapSearch.ChaosValue ?? 0;
                                    SetPriceChangeData(item.PriceData, blightRavagedMapSearch.Sparkline.TotalChange, blightRavagedMapSearch.Sparkline.Data);
                                    item.PriceData.DetailsId = blightRavagedMapSearch.DetailsId;
                                }

                                break;
                            case MapTypes.Valdo:
                                var valdoMapSearch = CollectedData.ValdoMaps.Lines.Find(x => x.Name == item.UniqueName);

                                if (valdoMapSearch != null)
                                {
                                    item.PriceData.MinChaosValue = valdoMapSearch.ChaosValue ?? 0;
                                    SetPriceChangeData(item.PriceData, valdoMapSearch.Sparkline.TotalChange, valdoMapSearch.Sparkline.Data);
                                    item.PriceData.DetailsId = valdoMapSearch.DetailsId;
                                }

                                break;
                            case MapTypes.None:

                                var normalMapBaseName = item.BaseName;

                                #region Occupied Types

                                var prefix = item.MapInfo.Occupier switch
                                {
                                    MapOccupier.AlHezmin => "Al-Hezmin",
                                    MapOccupier.Baran => "Baran",
                                    MapOccupier.Drox => "Drox",
                                    MapOccupier.Veritania => "Veritania",
                                    MapOccupier.Constrictor => "The Constrictor",
                                    MapOccupier.Enslaver => "The Enslaver",
                                    MapOccupier.Eradicator => "The Eradicator",
                                    MapOccupier.Purifier => "The Purifier",
                                    _ => null
                                };

                                if (prefix != null)
                                    normalMapBaseName = $"{prefix} {normalMapBaseName}";

                                #endregion

                                var normalMapSearch = CollectedData.WhiteMaps.Lines.Find(x =>
                                    x.Name == normalMapBaseName);

                                if (normalMapSearch != null)
                                {
                                    item.PriceData.MinChaosValue = normalMapSearch.ChaosValue ?? 0;
                                    SetPriceChangeData(item.PriceData, normalMapSearch.Sparkline.TotalChange, normalMapSearch.Sparkline.Data);
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
                            SetPriceChangeData(item.PriceData, beastSearch.sparkline.totalChange, beastSearch.sparkline.data);
                            item.PriceData.DetailsId = beastSearch.detailsId;
                        }

                        break;
                    case ItemTypes.KalguuranRune:
                        var runeSearch = CollectedData.KalguuranRunes.LinesByName.GetValueOrDefault(item.BaseName);
                        if (runeSearch != default)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * runeSearch.ChaosEquivalent;
                            SetPriceChangeData(item.PriceData, runeSearch.Line.sparkline.totalChange, runeSearch.Line.sparkline.data);
                            item.PriceData.DetailsId = runeSearch.Item.detailsId;
                        }

                        break;
                    case ItemTypes.AllflameEmber:
                        var allflameSearch = CollectedData.AllflameEmbers.LinesByName.GetValueOrDefault(item.BaseName);
                        if (allflameSearch != default)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * allflameSearch.ChaosEquivalent;
                            SetPriceChangeData(item.PriceData, allflameSearch.Line.sparkline.totalChange, allflameSearch.Line.sparkline.data);
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
                                    SetPriceChangeData(item.PriceData, line.Line?.sparkline.totalChange, line.Line?.sparkline.data);
                                    item.PriceData.DetailsId = "ultimatum-inscribed-mirror";
                                    break;
                                }
                                case UltimatumItemisedRewardType.Currency:
                                {
                                    var sacItem = new CustomItem(ultimatumTrial.Reward.SacrificeItem) { CurrencyInfo = { StackSize = ultimatumTrial.Reward.SacrificeAmount } };
                                    GetValue(sacItem);
                                    item.PriceData.MinChaosValue = sacItem.PriceData.MinChaosValue;
                                    SetPriceChangeData(item.PriceData, sacItem.PriceData.ChangeInLast7Days, sacItem.PriceData.ChangeSparkline7Days);
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
                                    SetPriceChangeData(item.PriceData, sacItem.PriceData.ChangeInLast7Days, sacItem.PriceData.ChangeSparkline7Days);
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