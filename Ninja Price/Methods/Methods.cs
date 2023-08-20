using Ninja_Price.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared.Enums;
using Color = SharpDX.Color;
using Ninja_Price.API.PoeNinja;

namespace Ninja_Price.Main;

public partial class Main
{
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

    private double DivinePrice => CollectedData.Currency.Lines.Find(x => x.CurrencyTypeName == "Divine Orb")?.ChaosEquivalent ?? throw new Exception("Divine price is missing");

    private List<NormalInventoryItem> GetInventoryItems()
    {
        var inventory = GameController.Game.IngameState.IngameUi.InventoryPanel;
        return !inventory.IsVisible ? null : inventory[InventoryIndex.PlayerInventory].VisibleInventoryItems.ToList();
    }

    private static List<CustomItem> FormatItems(IEnumerable<NormalInventoryItem> itemList)
    {
        return itemList.ToList().Where(x => x?.Item?.IsValid == true).Select(inventoryItem => new CustomItem(inventoryItem)).ToList();
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
            if (uiHover.AsObject<HoverItemIcon>().ToolTipType != ToolTipType.ItemInChat)
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
                        if (HoveredItem.ItemType != ItemTypes.None)
                            GetValue(HoveredItem);
                    }
                }
            }
        }
        catch
        {
            // ignored
            //LogError("Error in GetHoveredItem()", 10);
        }
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
                        var shardCurrencySearch = CollectedData.Currency.FindLine(pricedItem, Settings.UseChaosEquivalentDataForCurrency);
                        if (shardCurrencySearch != null)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * shardCurrencySearch.ChaosEquivalent / pricedStack;
                            item.PriceData.ChangeInLast7Days = shardCurrencySearch.PriceChange;
                            item.PriceData.DetailsId = shardCurrencySearch.DetailsId;
                        }

                        break;
                    }
                    case ItemTypes.Catalyst:
                        var catalystSearch = CollectedData.Currency.FindLine(item.BaseName, Settings.UseChaosEquivalentDataForCurrency);
                        if (catalystSearch != null)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * catalystSearch.ChaosEquivalent;
                            item.PriceData.ChangeInLast7Days = catalystSearch.PriceChange;
                            item.PriceData.DetailsId = catalystSearch.DetailsId;
                        }

                        break;
                    case ItemTypes.DivinationCard:
                        var divinationSearch = CollectedData.DivinationCards.Lines.Find(x => x.Name == item.BaseName);
                        if (divinationSearch != null)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * divinationSearch.ChaosValue ?? 0;
                            item.PriceData.ChangeInLast7Days = divinationSearch.Sparkline.TotalChange ?? 0;
                            item.PriceData.DetailsId = divinationSearch.DetailsId;
                        }

                        break;
                    case ItemTypes.Essence:
                        var essenceSearch = CollectedData.Essences.Lines.Find(x => x.Name == item.BaseName);
                        if (essenceSearch != null)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * essenceSearch.ChaosValue ?? 0;
                            item.PriceData.ChangeInLast7Days = essenceSearch.Sparkline.TotalChange ?? 0;
                            item.PriceData.DetailsId = essenceSearch.DetailsId;
                        }

                        break;
                    case ItemTypes.Oil:
                        var oilSearch = CollectedData.Oils.Lines.Find(x => x.Name == item.BaseName);
                        if (oilSearch != null)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * oilSearch.ChaosValue ?? 0;
                            item.PriceData.ChangeInLast7Days = oilSearch.Sparkline.TotalChange ?? 0;
                            item.PriceData.DetailsId = oilSearch.DetailsId;
                        }
                        break;
                    case ItemTypes.Tattoo:
                        var tattooSearch = CollectedData.Tattoos.Lines.Find(x => x.Name == item.BaseName);
                        if (tattooSearch != null)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * tattooSearch.ChaosValue ?? 0;
                            item.PriceData.ChangeInLast7Days = tattooSearch.Sparkline.TotalChange ?? 0;
                            item.PriceData.DetailsId = tattooSearch.DetailsId;
                        }
                        break;
                    case ItemTypes.Omen:
                        var omenSearch = CollectedData.Omens.Lines.Find(x => x.Name == item.BaseName);
                        if (omenSearch != null)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * omenSearch.ChaosValue ?? 0;
                            item.PriceData.ChangeInLast7Days = omenSearch.Sparkline.TotalChange ?? 0;
                            item.PriceData.DetailsId = omenSearch.DetailsId;
                        }
                        break;
                    case ItemTypes.Artifact:
                        var artifactSearch = CollectedData.Artifacts.Lines.Find(x => x.Name == item.BaseName);
                        if (artifactSearch != null)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * artifactSearch.ChaosValue ?? 0;
                            item.PriceData.ChangeInLast7Days = artifactSearch.Sparkline.TotalChange ?? 0;
                            item.PriceData.DetailsId = artifactSearch.DetailsId;
                        }

                        break;
                    case ItemTypes.Fragment:
                    {
                        var (pricedStack, pricedItem) = item.CurrencyInfo.IsShard && TryGetShardParent(item.BaseName, out var shardParent)
                            ? (item.CurrencyInfo.MaxStackSize > 0 ? item.CurrencyInfo.MaxStackSize : 20, shardParent)
                            : (1, item.BaseName);
                        var fragmentSearch = CollectedData.Fragments.Lines.Find(x => x.CurrencyTypeName == pricedItem);
                        if (fragmentSearch != null)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * (fragmentSearch.ChaosEquivalent ?? 0) / pricedStack;
                            item.PriceData.ChangeInLast7Days = fragmentSearch.ReceiveSparkLine.TotalChange ?? 0;
                            item.PriceData.DetailsId = fragmentSearch.DetailsId;
                        }

                        break;
                    }
                    case ItemTypes.SkillGem:
                        var displayText = item.QualityType == SkillGemQualityTypeE.Superior ? item.BaseName : $"{item.QualityType} {item.BaseName}";
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
                    case ItemTypes.MavenInvitation:
                        var InvitationSearch = CollectedData.Invitations.Lines.Find(x => x.Name == item.BaseName);
                        if (InvitationSearch != null)
                        {
                            item.PriceData.MinChaosValue = InvitationSearch.ChaosValue ?? 0;
                            item.PriceData.ChangeInLast7Days = InvitationSearch.Sparkline.TotalChange ?? 0;
                            item.PriceData.DetailsId = InvitationSearch.DetailsId;
                        }

                        break;
                    case ItemTypes.DeliriumOrbs:
                        var deliriumOrbsSearch = CollectedData.DeliriumOrb.Lines.Find(x => x.Name == item.BaseName);
                        if (deliriumOrbsSearch != null)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * deliriumOrbsSearch.ChaosValue ?? 0;
                            item.PriceData.ChangeInLast7Days = deliriumOrbsSearch.Sparkline.TotalChange ?? 0;
                            item.PriceData.DetailsId = deliriumOrbsSearch.DetailsId;
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
                        var scarabSearch = CollectedData.Scarabs.Lines.Find(x => x.Name == item.BaseName);
                        if (scarabSearch != null)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * scarabSearch.ChaosValue ?? 0;
                            item.PriceData.ChangeInLast7Days = scarabSearch.Sparkline.TotalChange ?? 0;
                            item.PriceData.DetailsId = scarabSearch.DetailsId;
                        }

                        break;
                    case ItemTypes.UniqueAccessory:
                        var uniqueAccessorySearch = CollectedData.UniqueAccessories.Lines.FindAll(x =>
                            (x.Name == item.UniqueName || item.UniqueNameCandidates.Contains(x.Name)) &&
                            !x.DetailsId.Contains("-relic"));
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
                            (x.Name == item.UniqueName || item.UniqueNameCandidates.Contains(x.Name)) &&
                            !x.DetailsId.Contains("-relic"));
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
                            (x.Name == item.UniqueName || item.UniqueNameCandidates.Contains(x.Name)) &&
                            !x.DetailsId.Contains("-relic"));
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
                            (x.Name == item.UniqueName || item.UniqueNameCandidates.Contains(x.Name)) &&
                            !x.DetailsId.Contains("-relic"));
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
                            (x.BaseType == item.UniqueName || item.UniqueNameCandidates.Contains(x.BaseType)) &&
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
                        var resonatorSearch = CollectedData.Resonators.Lines.Find(x => x.Name == item.BaseName);
                        if (resonatorSearch != null)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * resonatorSearch.ChaosValue;
                            item.PriceData.ChangeInLast7Days = resonatorSearch.Sparkline.TotalChange;
                            item.PriceData.DetailsId = resonatorSearch.DetailsId;
                        }

                        break;
                    case ItemTypes.Fossil:
                        var fossilSearch = CollectedData.Fossils.Lines.Find(x => x.Name == item.BaseName);
                        if (fossilSearch != null)
                        {
                            item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * fossilSearch.ChaosValue ?? 0;
                            item.PriceData.ChangeInLast7Days = fossilSearch.Sparkline.TotalChange ?? 0;
                            item.PriceData.DetailsId = fossilSearch.DetailsId;
                        }

                        break;
                    case ItemTypes.UniqueWeapon:
                    {
                        var allLinksLines = CollectedData.UniqueWeapons.Lines.Where(x =>
                            (x.Name == item.UniqueName || item.UniqueNameCandidates.Contains(x.Name)) &&
                            !x.DetailsId.Contains("-relic"));
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
                                var normalBlightedMapSearch = Settings.MapVariant.Value
                                                                  ? CollectedData.WhiteMaps.Lines.Find(x =>
                                                                      x.BaseType == blightedBaseName && item.MapInfo.MapTier == x.MapTier &&
                                                                      x.Variant == Settings.LeagueList.Value)
                                                                  : CollectedData.WhiteMaps.Lines.Find(x =>
                                                                      x.BaseType == blightedBaseName && item.MapInfo.MapTier == x.MapTier);

                                if (normalBlightedMapSearch != null)
                                {
                                    item.PriceData.MinChaosValue = normalBlightedMapSearch.ChaosValue ?? 0;
                                    item.PriceData.ChangeInLast7Days = normalBlightedMapSearch.Sparkline.TotalChange ?? 0;
                                    item.PriceData.DetailsId = normalBlightedMapSearch.DetailsId;
                                }

                                break;
                            case MapTypes.None:

                                var normalMapSearch = Settings.MapVariant.Value
                                                          ? CollectedData.WhiteMaps.Lines.Find(x =>
                                                              x.BaseType == item.BaseName && item.MapInfo.MapTier == x.MapTier && x.Variant == Settings.LeagueList.Value)
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
                }
            }
        }
        catch (Exception)
        {
            if (Settings.Debug) { LogMessage($"{GetCurrentMethod()}.GetValue()", 5, Color.Red); }
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
            if (Settings.Debug)
            {
                LogMessage($"{GetCurrentMethod()}.GetValueHaggle() Error that i dont understand, Item: {item.BaseName}", 5, Color.Red);
                LogMessage($"{GetCurrentMethod()}.GetValueHaggle() {e.Message}", 5, Color.Red);
            }
        }
    }

    private bool ShouldUpdateValues()
    {
        if (StashUpdateTimer.ElapsedMilliseconds > Settings.ValueLoopTimerMS)
        {
            StashUpdateTimer.Restart();
            if (Settings.Debug) { LogMessage($"{GetCurrentMethod()} ValueUpdateTimer.Restart()", 5, Color.DarkGray); }
        }
        else
        {
            return false;
        }
        // TODO: Get inventory items and not just stash tab items, this will be done at a later date
        try
        {
            if (!Settings.VisibleStashValue.Show.Value || !GameController.Game.IngameState.IngameUi.StashElement.IsVisible)
            {
                if (Settings.Debug) { LogMessage($"{GetCurrentMethod()}.ShouldUpdateValues() Stash is not visible", 5, Color.DarkGray); }
                return false;
            }

            // Dont continue if the stash page isnt even open
            if (GameController.Game.IngameState.IngameUi.StashElement.VisibleStash?.VisibleInventoryItems == null)
            {
                if (Settings.Debug) LogMessage($"{GetCurrentMethod()}.ShouldUpdateValues() Items == null", 5, Color.DarkGray);
                return false;
            }
        }
        catch (Exception)
        {
            if (Settings.Debug) LogMessage($"{GetCurrentMethod()}.ShouldUpdateValues()", 5, Color.DarkGray);
            return false;
        }

        if (Settings.Debug) LogMessage($"{GetCurrentMethod()}.ShouldUpdateValues() == True", 5, Color.LimeGreen);
        return true;
    }

    private bool ShouldUpdateValuesInventory()
    {
        if (InventoryUpdateTimer.ElapsedMilliseconds > Settings.ValueLoopTimerMS)
        {
            InventoryUpdateTimer.Restart();
            if (Settings.Debug) { LogMessage($"{GetCurrentMethod()} ValueUpdateTimer.Restart()", 5, Color.DarkGray); }
        }
        else
        {
            return false;
        }
        // TODO: Get inventory items and not just stash tab items, this will be done at a later date
        try
        {
            if (!Settings.VisibleInventoryValue.Value || !GameController.Game.IngameState.IngameUi.InventoryPanel.IsVisible)
            {
                if (Settings.Debug) { LogMessage($"{GetCurrentMethod()}.ShouldUpdateValuesInventory() Inventory is not visible", 5, Color.DarkGray); }
                return false;
            }

            // Dont continue if the stash page isnt even open
            if (GameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory].VisibleInventoryItems == null)
            {
                if (Settings.Debug) LogMessage($"{GetCurrentMethod()}.ShouldUpdateValuesInventory() Items == null", 5, Color.DarkGray);
                return false;
            }
        }
        catch (Exception)
        {
            if (Settings.Debug) LogMessage($"{GetCurrentMethod()}.ShouldUpdateValuesInventory()", 5, Color.DarkGray);
            return false;
        }

        if (Settings.Debug) LogMessage($"{GetCurrentMethod()}.ShouldUpdateValuesInventory() == True", 5, Color.LimeGreen);
        return true;
    }
        
    private CustomItem GetHelmetEnchantValue(string EnchantName)
    {
        if (string.IsNullOrWhiteSpace(EnchantName))
            return null;

        var enchantSearch = CollectedData.HelmetEnchants.lines.Find(x => x.name.ToLower().Contains(EnchantName.ToLower()));
        return enchantSearch == null
                   ? null
                   : new CustomItem
                   {
                       PriceData = new RelevantPriceData
                       {
                           MinChaosValue = enchantSearch.chaosValue, 
                           ItemType = ItemTypes.None, 
                           ChangeInLast7Days = enchantSearch.sparkline.totalChange
                       },
                       BaseName = enchantSearch.name
                   };
    }
}