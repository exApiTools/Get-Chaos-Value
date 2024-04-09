using Ninja_Price.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using Color = SharpDX.Color;
using RectangleF = SharpDX.RectangleF;
using ImGuiNET;
using static Ninja_Price.Enums.HaggleTypes.HaggleType;
using ExileCore.PoEMemory.Elements.Necropolis;
using System.Drawing;
using System.Xml.Linq;

namespace Ninja_Price.Main;

public partial class Main
{
    public Stopwatch StashUpdateTimer = Stopwatch.StartNew();
    public Stopwatch InventoryUpdateTimer = Stopwatch.StartNew();
    public double StashTabValue { get; set; }
    public double InventoryTabValue { get; set; }
    public double? DivineDalue { get; set; }
    public List<NormalInventoryItem> ItemList { get; set; } = new List<NormalInventoryItem>();
    public List<CustomItem> FormattedItemList { get; set; } = new List<CustomItem>();

    public List<NormalInventoryItem> InventoryItemList { get; set; } = new List<NormalInventoryItem>();
    public List<CustomItem> FormattedInventoryItemList { get; set; } = new List<CustomItem>();

    public List<CustomItem> ItemsToDrawList { get; set; } = new List<CustomItem>();
    public List<CustomItem> InventoryItemsToDrawList { get; set; } = new List<CustomItem>();
    public StashElement StashPanel { get; set; }
    public StashElement GuildStashPanel { get; set; }
    public InventoryElement InventoryPanel { get; set; }
    public Element HagglePanel { get; set; }

    public CustomItem HoveredItem { get; set; }

    private readonly CachedValue<List<ItemOnGround>> _slowGroundItems;
    private readonly CachedValue<List<ItemOnGround>> _groundItems;

    public Main()
    {
        _slowGroundItems = new TimeCache<List<ItemOnGround>>(GetItemsOnGroundSlow, 500);
        _groundItems = new FrameCache<List<ItemOnGround>>(CacheUtils.RememberLastValue(GetItemsOnGround, new List<ItemOnGround>()));
    }

    private List<ItemOnGround> GetItemsOnGround(List<ItemOnGround> previousValue)
    {
        var prevDict = previousValue
            .Where(x => x.Type == GroundItemProcessingType.WorldItem)
            .DistinctBy(x => (x.Item.Element?.Address, x.Item.Entity?.Address))
            .ToDictionary(x => (x.Item.Element?.Address, x.Item.Entity?.Address));
        var labelsOnGround = GameController.IngameState.IngameUi.ItemsOnGroundLabelElement.VisibleGroundItemLabels;
        var result = new List<ItemOnGround>();
        foreach (var description in labelsOnGround)
        {
            if (description.Entity.TryGetComponent<WorldItem>(out var worldItem) &&
                worldItem.ItemEntity is { IsValid: true } groundItemEntity)
            {
                var customItem = prevDict.GetValueOrDefault((description.Label?.Address, groundItemEntity.Address))?.Item;
                if (customItem == null)
                {
                    customItem = new CustomItem(groundItemEntity, description.Label);
                    GetValue(customItem);
                }

                result.Add(new ItemOnGround(customItem, GroundItemProcessingType.WorldItem, description.ClientRect));
            }
        }
        result.AddRange(_slowGroundItems.Value);
        return result;
    }

    private List<ItemOnGround> GetItemsOnGroundSlow()
    {
        var labelsOnGround = GameController.IngameState.IngameUi.ItemsOnGroundLabelsVisible;
        var result = new List<ItemOnGround>();
        foreach (var labelOnGround in labelsOnGround)
        {
            var item = labelOnGround.ItemOnGround;
            if (item.TryGetComponent<HeistRewardDisplay>(out var heistReward) &&
                     heistReward.RewardItem is { IsValid: true } heistItemEntity)
            {
                result.Add(new ItemOnGround(new CustomItem(heistItemEntity, labelOnGround.Label), GroundItemProcessingType.HeistReward, null));
            }

            if (Settings.GroundItemSettings.PriceCoffins && item.Path == "Metadata/Terrain/Leagues/Necropolis/Objects/NecropolisCorpseMarker")
            {
                var customItem = new CustomItem(labelOnGround.ItemOnGround, labelOnGround.Label.GetChildFromIndices(0, 0, 0));
                var typedLabel = labelOnGround.Label.AsObject<NecropolisCollectableCorpse>();
                var corpses = typedLabel.CorpsesByEntityId;
                var surrogateComponent = corpses.GetValueOrDefault(labelOnGround.ItemOnGround.Id);
                customItem.NecropolisMod = surrogateComponent.CraftingMod;
                customItem.ItemLevel = surrogateComponent.Level;
                customItem.ItemType = ItemTypes.Coffin;
                result.Add(new ItemOnGround(customItem, GroundItemProcessingType.CollectableCorpse, null));
            }
        }

        result.ForEach(x => GetValue(x.Item));
        return result;
    }

    // TODO: Get hovered items && items from inventory - Getting hovered item  will become useful later on

    public override void Render()
    {
        #region Reset All Data

        StashTabValue = 0;
        InventoryTabValue = 0;
        HoveredItem = null;

        GuildStashPanel = GameController.Game.IngameState.IngameUi.GuildStashElement;
        StashPanel = GameController.Game.IngameState.IngameUi.StashElement;
        InventoryPanel = GameController.Game.IngameState.IngameUi.InventoryPanel;
        HagglePanel = GameController.Game.IngameState.IngameUi.HaggleWindow;

        #endregion

        if (CollectedData == null)
        {
            //nothing loaded yet, don't waste time
            return;
        }

        try // Im lazy and just want to surpress all errors produced
        {
            // only update if the time between last update is more than AutoReloadTimer interval
            if (Settings.AutoReload && Settings.LastUpDateTime.AddMinutes(Settings.ReloadTimer.Value) < DateTime.Now)
            {
                StartDataReload(Settings.LeagueList.Value, true);
                Settings.LastUpDateTime = DateTime.Now;
            }

            if (Settings.Debug) LogMessage($"{GetCurrentMethod()}.Loop() is Alive", 5, Color.LawnGreen);

            if (Settings.Debug)
                LogMessage($"{GetCurrentMethod()}: Selected League: {Settings.LeagueList.Value}", 5, Color.White);

            var tabType = StashPanel.VisibleStash?.InvType ?? GuildStashPanel.VisibleStash?.InvType;

            // Everything is updated, lets check if we should draw
            if (StashPanel.IsVisible || GuildStashPanel.IsVisible && tabType != null)
            {
                if (ShouldUpdateValues())
                {
                    // Format stash items
                    switch (tabType)
                    {
                        case InventoryType.BlightStash:
                            if (StashPanel.IsVisible)
                            {
                                ItemList = StashPanel.VisibleStash.VisibleInventoryItems.ToList();
                                ItemList.RemoveAt(0);
                                ItemList.RemoveAt(ItemList.Count - 1);
                            }
                            else if (GuildStashPanel.IsVisible)
                            {
                                ItemList = GuildStashPanel.VisibleStash.VisibleInventoryItems.ToList();
                                ItemList.RemoveAt(0);
                                ItemList.RemoveAt(ItemList.Count - 1);
                            }
                            break;
                        case InventoryType.MetamorphStash:
                            if (StashPanel.IsVisible)
                            {
                                ItemList = StashPanel.VisibleStash.VisibleInventoryItems.ToList();
                                ItemList.RemoveAt(0);
                            }
                            else if (GuildStashPanel.IsVisible)
                            {
                                ItemList = GuildStashPanel.VisibleStash.VisibleInventoryItems.ToList();
                            }
                            break;
                        default:
                            if (StashPanel.IsVisible)
                            {
                                ItemList = StashPanel.VisibleStash.VisibleInventoryItems.ToList();
                            }
                            else if (GuildStashPanel.IsVisible)
                            {
                                ItemList = GuildStashPanel.VisibleStash.VisibleInventoryItems.ToList();
                            }
                            break;
                    }
                    if (ItemList.Count == 0)
                    {
                        ItemList = (List<NormalInventoryItem>)GameController.Game.IngameState.IngameUi.RitualWindow.Items;
                    }
                    FormattedItemList = FormatItems(ItemList);

                    if (Settings.Debug)
                        LogMessage($"{GetCurrentMethod()}.Render() Looping if (ShouldUpdateValues())", 5,
                            Color.LawnGreen);

                    FormattedItemList.ForEach(GetValue);
                }

                // Gather all information needed before rendering as we only want to iterate through the list once

                ItemsToDrawList = new List<CustomItem>();
                foreach (var item in FormattedItemList)
                {
                    if (item == null || item.Element.Address == 0) continue; // Item is fucked, skip
                    if (!item.Element.IsVisible && item.ItemType != ItemTypes.None)
                        continue; // Disregard non visible items as that usually means they aren't part of what we want to look at

                    StashTabValue += item.PriceData.MinChaosValue;
                    ItemsToDrawList.Add(item);
                }
            }
            if (InventoryPanel.IsVisible)
            {
                if (ShouldUpdateValuesInventory())
                {
                    // Format Inventory Items
                    InventoryItemList = GetInventoryItems();
                    FormattedInventoryItemList = FormatItems(InventoryItemList);

                    if (Settings.Debug)
                        LogMessage($"{GetCurrentMethod()}.Render() Looping if (ShouldUpdateValuesInventory())", 5,
                            Color.LawnGreen);

                    FormattedInventoryItemList.ForEach(GetValue);
                }

                // Gather all information needed before rendering as we only want to iterate through the list once
                InventoryItemsToDrawList = new List<CustomItem>();
                foreach (var item in FormattedInventoryItemList)
                {
                    if (item == null || item.Element.Address == 0) continue; // Item is fucked, skip
                    if (!item.Element.IsVisible && item.ItemType != ItemTypes.None)
                        continue; // Disregard non visible items as that usually means they aren't part of what we want to look at

                    InventoryTabValue += item.PriceData.MinChaosValue;
                    InventoryItemsToDrawList.Add(item);
                }
            }

            GetHoveredItem(); // Get information for the hovered item
            DrawGraphics();
        }
        catch (Exception e)
        {
            // ignored
            if (Settings.Debug)
            {
                LogMessage("Error in: Main Render Loop, restart PoEHUD.", 5, Color.Red);
                LogMessage(e.ToString(), 5, Color.Orange);
            }
        }
    }

    public void DrawGraphics()
    {
        ProcessExpeditionWindow();
        ProcessItemsOnGround();
        ProcessTradeWindow();
        ProcessDivineFontRewards();
        ProcessHoveredItem();
        VisibleInventoryValue();

        if (StashPanel.IsVisible || GuildStashPanel.IsVisible)
        {
            VisibleStashValue();

            var tabType = StashPanel.VisibleStash?.InvType ?? GuildStashPanel.VisibleStash?.InvType;
            foreach (var customItem in ItemsToDrawList)
            {
                if (customItem.ItemType == ItemTypes.None) continue;

                if (Settings.VisibleStashValue.CurrencyTabSettings.ShowItemOverlay &&
                    (!Settings.VisibleStashValue.CurrencyTabSettings.DoNotDrawWhileAnItemIsHovered || HoveredItem == null))
                {
                    switch (tabType)
                    {
                        case InventoryType.CurrencyStash:
                        case InventoryType.FragmentStash:
                        case InventoryType.DelveStash:
                        case InventoryType.DeliriumStash:
                        case InventoryType.MetamorphStash:
                        case InventoryType.BlightStash:
                            PriceBoxOverItem(customItem, null, Settings.VisibleStashValue.CurrencyTabSettings.FontColor);
                            break;
                    }
                }
            }
        }
    }

    private void ProcessHoveredItem()
    {
        if (!Settings.HoveredItemSettings.Show) return;
        if (HoveredItem == null || HoveredItem.ItemType == ItemTypes.None) return;
        var textSections = new List<string> { "" };
        void AddSection() => textSections.Add("");
        void AddText(string text) => textSections[^1] += text;

        var changeText = $"Change in last 7 Days: {HoveredItem.PriceData.ChangeInLast7Days}%%";
        var changeTextLength = changeText.Length - 1;
        var sectionBreak = $"\n{new string('-', changeTextLength)}\n";
        if (HoveredItem.PriceData.ChangeInLast7Days != 0)
        {
            AddText(changeText);
        }

        var priceInChaos = HoveredItem.PriceData.MinChaosValue;
        var priceInDivines = priceInChaos / DivinePrice;
        var priceInDivinesText = priceInDivines.FormatNumber(2);
        var minPriceText = priceInChaos.FormatNumber(2, Settings.MaximalValueForFractionalDisplay);
        AddSection();
        switch (HoveredItem.ItemType)
        {
            case ItemTypes.Currency:
            case ItemTypes.Essence:
            case ItemTypes.Fragment:
            case ItemTypes.Scarab:
            case ItemTypes.Resonator:
            case ItemTypes.Fossil:
            case ItemTypes.Oil:
            case ItemTypes.Artifact:
            case ItemTypes.Catalyst:
            case ItemTypes.DeliriumOrbs:
            case ItemTypes.Vials:
            case ItemTypes.DivinationCard:
            case ItemTypes.Incubator:
            case ItemTypes.Tattoo:
            case ItemTypes.Omen:
                if (priceInDivines >= 0.1)
                {
                    var priceInDivinessPerOne = priceInDivines / HoveredItem.CurrencyInfo.StackSize;
                    AddText(priceInDivinessPerOne >= 0.1
                        ? $"\nDivine: {priceInDivinesText}d ({priceInDivinessPerOne.FormatNumber(2)}d per one)"
                        : $"\nDivine: {priceInDivinesText}d");
                }
                AddText($"\nChaos: {minPriceText}c ({(priceInChaos / HoveredItem.CurrencyInfo.StackSize).FormatNumber(2, Settings.MaximalValueForFractionalDisplay)}c per one)");
                break;
            case ItemTypes.UniqueAccessory:
            case ItemTypes.UniqueArmour:
            case ItemTypes.UniqueFlask:
            case ItemTypes.UniqueJewel:
            case ItemTypes.UniqueMap:
            case ItemTypes.UniqueWeapon:
                if (HoveredItem.UniqueNameCandidates.Any())
                {
                    AddText(HoveredItem.UniqueNameCandidates.Count == 1
                        ? $"\nIdentified as: {HoveredItem.UniqueNameCandidates.First()}"
                        : $"\nIdentified as one of:\n{string.Join('\n', HoveredItem.UniqueNameCandidates)}");
                }

                AddSection();
                if (priceInDivines >= 0.1)
                {
                    var maxDivinePriceText = (HoveredItem.PriceData.MaxChaosValue / DivinePrice).FormatNumber(2);
                    AddText(priceInDivinesText != maxDivinePriceText
                        ? $"\nDivine: {priceInDivinesText}d - {maxDivinePriceText}d"
                        : $"\nDivine: {priceInDivinesText}d");
                }

                var maxPriceText = HoveredItem.PriceData.MaxChaosValue.FormatNumber(2, Settings.MaximalValueForFractionalDisplay);
                AddText(minPriceText != maxPriceText
                    ? $"\nChaos: {minPriceText}c - {maxPriceText}c"
                    : $"\nChaos: {minPriceText}c");

                break;
            case ItemTypes.Map:
            case ItemTypes.Invitation:
            case ItemTypes.SkillGem:
            case ItemTypes.ClusterJewel:
            case ItemTypes.Coffin:
            case ItemTypes.Allflame:
                if (priceInDivines >= 0.1)
                {
                    AddText($"\nDivine: {priceInDivinesText}d");
                }

                AddText($"\nChaos: {minPriceText}c");
                break;
        }

        if (Settings.Debug)
        {
            AddSection();
            AddText($"\nUniqueName: {HoveredItem.UniqueName}"
                    + $"\nBaseName: {HoveredItem.BaseName}"
                    + $"\nItemType: {HoveredItem.ItemType}"
                    + $"\nMapType: {HoveredItem.MapInfo.MapType}"
                    + $"\nDetailsId: {HoveredItem.PriceData.DetailsId}");
        }

        if (Settings.ArtifactChaosPrices)
        {
            if (TryGetArtifactPrice(HoveredItem, out var amount, out var artifactName))
            {
                AddSection();
                AddText($"\nArtifact price: ({(priceInChaos / amount * 100).FormatNumber(2)}c per 100 {artifactName})");
            }
        }

        var tooltipText = string.Join(sectionBreak, textSections.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
        if (!string.IsNullOrWhiteSpace(tooltipText))
        {
            ImGui.BeginTooltip();
            var valuable = priceInChaos >= Settings.HoveredItemSettings.ValuableColorThreshold.Value;
            if (valuable)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Settings.HoveredItemSettings.ValuableColor.Value.ToImgui());
            }

            ImGui.TextUnformatted(tooltipText);
            if (valuable)
            {
                ImGui.PopStyleColor();
            }

            ImGui.EndTooltip();
        }
    }

    private void VisibleStashValue()
    {
        try
        {
            if (!Settings.VisibleStashValue.Show || (!StashPanel.IsVisible && !GuildStashPanel.IsVisible)) return;
            {
                var pos = new Vector2(Settings.VisibleStashValue.PositionX.Value, Settings.VisibleStashValue.PositionY.Value);
                var chaosValue = StashTabValue;
                var topValueItems = ItemsToDrawList
                    .Where(x => x.PriceData.MinChaosValue != 0)
                    .GroupBy(x => (x.PriceData.DetailsId, x.BaseName, x.UniqueName, x.ItemType))
                    .Select(group => new CustomItem
                    {
                        PriceData = { MinChaosValue = group.Sum(i => i.PriceData.MinChaosValue) },
                        CurrencyInfo = { StackSize = group.Sum(i => i.CurrencyInfo.StackSize) },
                        BaseName = group.Key.BaseName,
                        UniqueName = group.Key.UniqueName,
                    })
                    .OrderByDescending(x => x.PriceData.MinChaosValue)
                    .Take(Settings.VisibleStashValue.TopValuedItemCount.Value)
                    .ToList();

                DrawWorthWidget(chaosValue, pos, Settings.VisibleStashValue.SignificantDigits.Value, Settings.UniTextColor, Settings.VisibleStashValue.EnableBackground,
                    topValueItems);
            }
        }
        catch (Exception e)
        {
            // ignored
            if (Settings.Debug)
            {
                LogMessage("Error in: VisibleStashValue, restart PoEHUD.", 5, Color.Red);
                LogMessage(e.ToString(), 5, Color.Orange);
            }
        }
    }

    private void DrawWorthWidget(double chaosValue, Vector2 pos, int significantDigits, Color textColor, bool drawBackground, List<CustomItem> topValueItems)
    {
        var text = $"Chaos: {chaosValue.FormatNumber(significantDigits)}" +
                   (DivineDalue != null
                       ? $"\nDivine: {(chaosValue / DivineDalue.Value).FormatNumber(significantDigits)}"
                       : "");
        if (topValueItems.Count > 0)
        {
            var maxChaosValueLength = topValueItems.Max(x => x.PriceData.MinChaosValue.FormatNumber(2, forceDecimals: true).Length);
            var topValuedTexts = string.Join("\n",
                topValueItems.Select(x => $"{x.PriceData.MinChaosValue.FormatNumber(2, forceDecimals: true).PadLeft(maxChaosValueLength)}: {x}" +
                                          (x.CurrencyInfo.StackSize > 0 ? $" ({x.CurrencyInfo.StackSize})" : null)));
            text += $"\nTop value:\n{topValuedTexts}";
        }

        var box = Graphics.DrawText(text, pos, textColor);
        if (drawBackground)
        {
            Graphics.DrawBox(pos, pos + new Vector2(box.X, box.Y), Color.Black);
        }
    }

    private void VisibleInventoryValue()
    {
        try
        {
            var inventory = GameController.Game.IngameState.IngameUi.InventoryPanel;
            if (!Settings.InventoryValueSettings.Show.Value || !inventory.IsVisible) return;
            {
                var pos = new Vector2(Settings.InventoryValueSettings.PositionX.Value, Settings.InventoryValueSettings.PositionY.Value);
                DrawWorthWidget(InventoryTabValue, pos, Settings.VisibleStashValue.SignificantDigits.Value, Settings.UniTextColor, false, new List<CustomItem>());
            }
        }
        catch (Exception e)
        {
            // ignored
            if (Settings.Debug)
            {

                LogMessage("Error in: VisibleInventoryValue, restart PoEHUD.", 5, Color.Red);
                LogMessage(e.ToString(), 5, Color.Orange);
            }
        }
    }

    private void PriceBoxOverItem(CustomItem item, RectangleF? containerBox, Color textColor)
    {
        var box = item.Element.GetClientRect();
        var drawBox = new RectangleF(box.X, box.Y - 2, box.Width, -Settings.VisibleStashValue.CurrencyTabSettings.BoxHeight);

        (containerBox ?? default).Contains(ref drawBox, out var contains);
        if (containerBox == null || contains)
        {
            Graphics.DrawBox(drawBox, Settings.VisibleStashValue.CurrencyTabSettings.BackgroundColor);
            var textPosition = new Vector2(drawBox.Center.X, drawBox.Center.Y - Settings.VisibleStashValue.CurrencyTabSettings.FontSize.Value / 2);
            Graphics.DrawText(item.PriceData.MinChaosValue.FormatNumber(Settings.VisibleStashValue.CurrencyTabSettings.SignificantDigits.Value), textPosition,
                textColor, FontAlign.Center);
        }
    }

    private void PriceBoxOverItemHaggle(CustomItem item)
    {
        var box = item.Element.GetClientRect();
        var drawBox = new RectangleF(box.X, box.Y + 2, box.Width, +Settings.VisibleStashValue.CurrencyTabSettings.BoxHeight);
        var position = new Vector2(drawBox.Center.X, drawBox.Center.Y - Settings.VisibleStashValue.CurrencyTabSettings.FontSize.Value / 2);

        if (item.PriceData.ItemBasePrices.Count == 0)
            return;

        Graphics.DrawBox(drawBox, Settings.VisibleStashValue.CurrencyTabSettings.BackgroundColor);
        Graphics.DrawText(item.PriceData.ItemBasePrices.Max().FormatNumber(Settings.VisibleStashValue.CurrencyTabSettings.SignificantDigits.Value), position, Settings.VisibleStashValue.CurrencyTabSettings.FontColor, FontAlign.Center);
        if (Settings.Debug)
            Graphics.DrawText(string.Join(",", item.PriceData.ItemBasePrices), position, Settings.VisibleStashValue.CurrencyTabSettings.FontColor, FontAlign.Center);
    }

    private void ProcessExpeditionWindow()
    {
        if (!Settings.DisplayExpeditionVendorOverlay || !HagglePanel.IsVisible) return;

        // Return Haggle Window Type
        var haggleText = HagglePanel.GetChildFromIndices(6, 2, 0)?.Text;

        var haggleType = haggleText switch
        {
            "Exchange" => Exchange,
            "Gamble" => Gamble,
            "Deal" => Deal,
            "Haggle" => Haggle,
            _ => None
        };

        var inventory = HagglePanel.GetChildFromIndices(8, 1, 0, 0);
        var itemList = inventory?.GetChildrenAs<NormalInventoryItem>().Skip(1).ToList() ?? new List<NormalInventoryItem>();
        if (haggleType == Gamble)
        {
            if (Settings.Debug)
            {
                foreach (var (item, index) in itemList.Select((item, index) => (item, index)))
                {
                    LogMessage(
                        $"Haggle Item[{index}]: {GameController.Files.BaseItemTypes.Translate(item.Item.Path).BaseName}");
                }
            }

            var formattedItemList = FormatItems(itemList);

            foreach (var customItem in formattedItemList)
            {
                GetValueHaggle(customItem);
                try
                {
                    PriceBoxOverItemHaggle(customItem);
                }
                catch (Exception e)
                {
                    // ignored
                    if (Settings.Debug)
                    {
                        LogMessage("Error in: ExpeditionGamble, restart PoEHUD.", 5, Color.Red);
                        LogMessage(e.ToString(), 5, Color.Orange);
                    }
                }
            }
        }

        if (haggleType == Haggle)
        {
            var formattedItemList = FormatItems(itemList);
            formattedItemList.ForEach(GetValue);
            var tooltipRect = HoveredItem?.Element.AsObject<HoverItemIcon>()?.Tooltip?.GetClientRect() ?? new RectangleF(0, 0, 0, 0);
            foreach (var customItem in formattedItemList)
            {
                var box = customItem.Element.GetClientRectCache;
                if (tooltipRect.Intersects(box))
                {
                    continue;
                }

                if (customItem.PriceData.MinChaosValue > 0)
                {
                    Graphics.DrawText(customItem.PriceData.MinChaosValue.FormatNumber(2), box.TopRight, Settings.VisibleStashValue.CurrencyTabSettings.FontColor,
                        FontAlign.Right);
                }

                if (Settings.ArtifactChaosPrices && TryGetArtifactPrice(customItem, out var amount, out var artifactName))
                {
                    var text = $"[{artifactName[..3]}]\n" +
                               (customItem.PriceData.MinChaosValue > 0
                                   ? (customItem.PriceData.MinChaosValue / amount * 100).FormatNumber(2)
                                   : "");
                    var textSize = Graphics.MeasureText(text);
                    var leftTop = box.BottomLeft.ToVector2Num() - new Vector2(0, textSize.Y);
                    Graphics.DrawBox(leftTop, leftTop + textSize, Color.Black);
                    Graphics.DrawText(text, leftTop, Settings.VisibleStashValue.CurrencyTabSettings.FontColor);
                }
            }
        }
    }

    private void ProcessTradeWindow()
    {
        if (!Settings.TradeWindowSettings.Show) return;

        var (yourItems, theirItems, element) =
            (GameController.IngameState.IngameUi.TradeWindow,
             GameController.IngameState.IngameUi.SellWindow,
             GameController.IngameState.IngameUi.SellWindowHideout)
                switch
            {
                ({ IsVisible: true } trade, _, _) => (trade.YourOffer, trade.OtherOffer, trade.SellDialog),
                (_, { IsVisible: true } sell, _) => (sell.YourOfferItems, sell.OtherOfferItems, sell.SellDialog),
                (_, _, { IsVisible: true } sellHideout) => (sellHideout.YourOfferItems, sellHideout.OtherOfferItems, sellHideout.SellDialog),
                (_, _, _) => (null, null, null),
            };
        if (yourItems == null || theirItems == null || element == null || yourItems.Count + theirItems.Count == 0)
        {
            return;
        }

        var yourFormattedItems = FormatItems(yourItems);
        var theirFormatterItems = FormatItems(theirItems);
        yourFormattedItems.ForEach(GetValue);
        theirFormatterItems.ForEach(GetValue);
        var yourTradeWindowValue = yourFormattedItems.Sum(x => x.PriceData.MinChaosValue);
        var theirTradeWindowValue = theirFormatterItems.Sum(x => x.PriceData.MinChaosValue);
        var textPosition = new Vector2(element.GetClientRectCache.Right, element.GetClientRectCache.Center.Y - ImGui.GetTextLineHeight() * 3)
                         + new Vector2(Settings.TradeWindowSettings.OffsetX, Settings.TradeWindowSettings.OffsetY);
        DrawWorthWidget(theirTradeWindowValue, textPosition, 2, Settings.UniTextColor, true, new List<CustomItem>());
        textPosition.Y += ImGui.GetTextLineHeight() * 2;
        var diff = theirTradeWindowValue - yourTradeWindowValue;
        DrawWorthWidget(diff, textPosition, 2, diff switch { > 0 => Color.Green, 0 => Settings.UniTextColor, < 0 => Color.Red, double.NaN => Color.Purple }, true, new List<CustomItem>());
        textPosition.Y += ImGui.GetTextLineHeight() * 2;
        DrawWorthWidget(yourTradeWindowValue, textPosition, 2, Settings.UniTextColor, true, new List<CustomItem>());
    }

    private void ProcessDivineFontRewards()
    {
        if ((!Settings.VisibleStashValue.CurrencyTabSettings.DoNotDrawWhileAnItemIsHovered || HoveredItem == null) &&
            GameController.IngameState.IngameUi.LabyrinthDivineFontPanel is { IsVisible: true, GemElements: { Count: > 0 } options })
        {
            foreach (var x in options.Where(x => x.IsVisible))
            {
                try
                {
                    var customItem = new CustomItem(x[0].Entity, x);
                    GetValue(customItem);
                    PriceBoxOverItem(customItem, null, Color.White);
                }
                catch (Exception ex)
                {
                    LogError(ex.ToString());
                }
            }
        }
    }

    private void ProcessItemsOnGround()
    {
        if (!Settings.GroundItemSettings.PriceItemsOnGround && !Settings.GroundItemSettings.DisplayRealUniqueNameOnGround && !Settings.GroundItemSettings.PriceHeistRewards) return;
        //this window allows us to change the size of the text we draw to the background list
        //yeah, it's weird
        ImGui.Begin("lmao",
            ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav);
        var drawList = ImGui.GetBackgroundDrawList();
        var tooltipRect = HoveredItem?.Element.AsObject<HoverItemIcon>()?.Tooltip?.GetClientRect() ?? new RectangleF(0, 0, 0, 0);
        var leftPanelRect = GameController.IngameState.IngameUi.OpenLeftPanel.Address != 0
                                ? GameController.IngameState.IngameUi.OpenLeftPanel.GetClientRectCache
                                : RectangleF.Empty;
        var rightPanelRect = GameController.IngameState.IngameUi.OpenRightPanel.Address != 0
                                 ? GameController.IngameState.IngameUi.OpenRightPanel.GetClientRectCache
                                 : RectangleF.Empty;
        foreach (var (item, processingType, clientRect) in _groundItems.Value)
        {
            var box = clientRect ?? item.Element.GetClientRect();
            switch (processingType)
            {
                case GroundItemProcessingType.WorldItem:
                case GroundItemProcessingType.CollectableCorpse when Settings.GroundItemSettings.PriceCoffins:
                    {
                        if (!tooltipRect.Intersects(box) && !leftPanelRect.Intersects(box) && !rightPanelRect.Intersects(box))
                        {
                            var isValuable = item.PriceData.MaxChaosValue >= Settings.GroundItemSettings.ValuableValueThreshold;

                            if (Settings.GroundItemSettings.PriceItemsOnGround &&
                                (!Settings.GroundItemSettings.OnlyPriceUniquesOnGround || item.Rarity == ItemRarity.Unique))
                            {
                                if (item.PriceData.MinChaosValue > 0)
                                {
                                    var s = item.PriceData.MinChaosValue.FormatNumber(2);
                                    if (item.PriceData.MaxChaosValue > item.PriceData.MinChaosValue)
                                    {
                                        s += $"-{item.PriceData.MaxChaosValue.FormatNumber(2)}";
                                    }

                                    using (Graphics.SetTextScale(Settings.GroundItemSettings.GroundPriceTextScale))
                                    {
                                        var textSize = Graphics.MeasureText(s);
                                        var textPos = new Vector2(box.Right - textSize.X, box.Top);
                                        Graphics.DrawBox(textPos, new Vector2(box.Right, box.Top + textSize.Y), Settings.GroundItemSettings.GroundPriceBackgroundColor);
                                        Graphics.DrawText(s, textPos, isValuable ? Settings.GroundItemSettings.ValuablePriceColor : Settings.GroundItemSettings.GroundPriceTextColor);
                                    }
                                }
                            }

                            if (Settings.GroundItemSettings.DisplayRealUniqueNameOnGround && !item.IsIdentified && item.Rarity == ItemRarity.Unique)
                            {
                                float GetRatio(string text)
                                {
                                    var textSize = Graphics.MeasureText(text);
                                    return Math.Min(box.Width * Settings.GroundItemSettings.UniqueLabelSize / textSize.X, (box.Height - 2) / textSize.Y);
                                }

                                void DrawOnItemLabel(float scale, string text, Color backgroundColor, Color textColor)
                                {
                                    ImGui.SetWindowFontScale(scale);
                                    var newTextSize = ImGui.CalcTextSize(text);
                                    var textPosition = box.Center.ToVector2Num() - newTextSize / 2;
                                    var rectPosition = new Vector2(textPosition.X, box.Top + 1);
                                    drawList.AddRectFilled(rectPosition, rectPosition + new Vector2(newTextSize.X, box.Height - 2), backgroundColor.ToImgui());
                                    drawList.AddText(textPosition, textColor.ToImgui(), text);
                                    ImGui.SetWindowFontScale(1);
                                }

                                if (item.UniqueNameCandidates.Any())
                                {
                                    if (Settings.GroundItemSettings.OnlyDisplayRealUniqueNameForValuableUniques && !isValuable)
                                    {
                                        continue;
                                    }

                                    var textColor = isValuable ? Settings.GroundItemSettings.ValuableUniqueItemNameTextColor : Settings.GroundItemSettings.UniqueItemNameTextColor;
                                    var backgroundColor = isValuable
                                        ? Settings.GroundItemSettings.ValuableUniqueItemNameBackgroundColor
                                        : Settings.GroundItemSettings.UniqueItemNameBackgroundColor;
                                    var (text, ratio) = Enumerable.Range(1, item.UniqueNameCandidates.Count).Select(perOneLine =>
                                            string.Join('\n', MoreLinq.Extensions.BatchExtension.Batch(item.UniqueNameCandidates, perOneLine)
                                                .Select(onLine => string.Join(" / ", onLine))))
                                        .Select(text => (text, ratio: GetRatio(text)))
                                        .MaxBy(x => x.ratio);

                                    DrawOnItemLabel(ratio, text, backgroundColor, textColor);
                                }
                                else if (Settings.GroundItemSettings.DisplayWarningTextForUnknownUniques)
                                {
                                    const string text = "???";
                                    var ratio = GetRatio(text);
                                    DrawOnItemLabel(ratio, text, Color.Blue, Color.Red);
                                }
                            }
                        }
                        break;
                    }
                case GroundItemProcessingType.HeistReward:
                    {
                        if (Settings.GroundItemSettings.PriceHeistRewards && !leftPanelRect.Contains(box.TopRight) && !rightPanelRect.Contains(box.TopRight))
                        {
                            if (item.PriceData.MinChaosValue > 0)
                            {
                                var s = item.PriceData.MinChaosValue.FormatNumber(2);
                                if (item.PriceData.MaxChaosValue > item.PriceData.MinChaosValue)
                                {
                                    s += $"-{item.PriceData.MaxChaosValue.FormatNumber(2)}";
                                }

                                using (Graphics.SetTextScale(Settings.GroundItemSettings.GroundPriceTextScale))
                                {
                                    var textSize = Graphics.MeasureText(s);
                                    var textPos = new Vector2(box.Right - textSize.X, box.Top);
                                    Graphics.DrawBox(textPos, textPos + textSize, Settings.GroundItemSettings.GroundPriceBackgroundColor);
                                    Graphics.DrawText(s, textPos, Settings.GroundItemSettings.GroundPriceTextColor);
                                }
                            }
                        }

                        break;
                    }
            }

        }

        ImGui.End();
    }

    private bool TryGetArtifactPrice(CustomItem item, out double amount, out string artifactName)
    {
        amount = 0;
        artifactName = null;
        if (item?.Element == null)
            return false;

        Element GetElementByString(Element element, string str)
        {
            if (element == null || string.IsNullOrWhiteSpace(str))
                return null;

            if (element.Text?.Trim() == str)
                return element;

            return element.Children.Select(c => GetElementByString(c, str)).FirstOrDefault(e => e != null);
        }

        var costElement = GetElementByString(item.Element?.AsObject<HoverItemIcon>()?.Tooltip, "Cost:");
        if (costElement?.Parent == null ||
            costElement.Parent.ChildCount < 2 ||
            costElement.Parent.GetChildAtIndex(1).ChildCount < 3)
            return false;
        var amountText = costElement.Parent.GetChildFromIndices(1, 0)?.Text;
        if (amountText == null)
            return false;
        artifactName = costElement.Parent.GetChildFromIndices(1, 2)?.Text;
        if (artifactName == null)
            return false;
        if (costElement.Text.Equals("Cost:")) // Tujen haggling
        {
            if (!int.TryParse(amountText.TrimEnd('x').Replace(".", null), NumberStyles.Integer, CultureInfo.InvariantCulture, out var amountInt))
            {
                return false;
            }

            amount = amountInt;
            return true;
        }

        if (costElement.Text.Equals("Cost Per Unit:")) // Artifact stacks (Dannig)
        {
            if (!double.TryParse(amountText, NumberStyles.Float, CultureInfo.InvariantCulture, out var costPerUnit))
            {
                return false;
            }

            amount = item.CurrencyInfo.StackSize * costPerUnit;
            return true;
        }

        return false;
    }
}

internal record ItemOnGround(CustomItem Item, GroundItemProcessingType Type, RectangleF? ClientRect);