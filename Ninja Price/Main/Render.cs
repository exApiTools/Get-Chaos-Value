using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using ImGuiNET;
using Ninja_Price.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text.RegularExpressions;
using ExileCore.PoEMemory.FilesInMemory;
using static Ninja_Price.Enums.HaggleTypes.HaggleType;
using Color = SharpDX.Color;
using RectangleF = SharpDX.RectangleF;

namespace Ninja_Price.Main;

public partial class Main
{
    public Stopwatch StashUpdateTimer = Stopwatch.StartNew();
    public Stopwatch InventoryUpdateTimer = Stopwatch.StartNew();
    public double StashTabValue { get; set; }
    public double InventoryTabValue { get; set; }
    public List<NormalInventoryItem> ItemList { get; set; } = new List<NormalInventoryItem>();
    public List<CustomItem> FormattedItemList { get; set; } = new List<CustomItem>();

    public List<NormalInventoryItem> InventoryItemList { get; set; } = new List<NormalInventoryItem>();
    public List<CustomItem> FormattedInventoryItemList { get; set; } = new List<CustomItem>();

    public List<CustomItem> ItemsToDrawList { get; set; } = new List<CustomItem>();
    public List<CustomItem> InventoryItemsToDrawList { get; set; } = new List<CustomItem>();
    public StashElement StashPanel { get; set; }
    public InventoryElement InventoryPanel { get; set; }
    public Element HagglePanel { get; set; }

    private CustomItem HoveredItem;
    private RectangleF? HoveredItemTooltipRect;

    private readonly CachedValue<List<ItemOnGround>> _slowGroundItems;
    private readonly CachedValue<List<ItemOnGround>> _groundItems;
    private readonly Dictionary<uint, bool> _soundPlayedTracker = new Dictionary<uint, bool>();

    public Main()
    {
        _slowGroundItems = new TimeCache<List<ItemOnGround>>(GetItemsOnGroundSlow, 500);
        _groundItems = new FrameCache<List<ItemOnGround>>(CacheUtils.RememberLastValue(GetItemsOnGround, new List<ItemOnGround>()));
        _disenchantCache = new TimeCache<List<VillageUniqueDisenchantValue>>(() => GameController.Files.VillageUniqueDisenchantValues.EntriesList, 1000);
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
        foreach (var id in _soundPlayedTracker.Keys.Except(result.Select(x => x.Item.EntityId)).ToList())
        {
            _soundPlayedTracker.Remove(id);
        }
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
        }

        GetValue(result.Select(x => x.Item));

        return result;
    }

    // TODO: Get hovered items && items from inventory - Getting hovered item  will become useful later on

    public override void Render()
    {
        #region Reset All Data

        StashTabValue = 0;
        InventoryTabValue = 0;
        HoveredItem = null;
        if (_inspectedItem != null)
        {
            GameController.InspectObject(_inspectedItem, "Ninja pricer hovered item");
        }

        StashPanel = (GameController.Game.IngameState.IngameUi.StashElement, GameController.Game.IngameState.IngameUi.GuildStashElement) switch
        {
            ({ IsVisible: false }, { IsVisible: true, IsValid: true } gs) => gs,
            var (s, _) => s
        };
        InventoryPanel = GameController.Game.IngameState.IngameUi.InventoryPanel;
        HagglePanel = GameController.Game.IngameState.IngameUi.HaggleWindow;

        #endregion

        if (CollectedData == null)
        {
            //nothing loaded yet, don't waste time
            return;
        }

        // only update if the time between last update is more than AutoReloadTimer interval
        if (Settings.DataSourceSettings.AutoReload && Settings.DataSourceSettings.LastUpdateTime.AddMinutes(Settings.DataSourceSettings.ReloadPeriod.Value) < DateTime.Now)
        {
            StartDataReload(Settings.DataSourceSettings.League.Value, true);
            Settings.DataSourceSettings.LastUpdateTime = DateTime.Now;
        }

        if (Settings.DebugSettings.EnableDebugLogging) LogMessage($"{GetCurrentMethod()}.Loop() is Alive", 5, Color.LawnGreen);

        if (Settings.DebugSettings.EnableDebugLogging)
            LogMessage($"{GetCurrentMethod()}: Selected League: {Settings.DataSourceSettings.League.Value}", 5, Color.White);

        var tabType = StashPanel.VisibleStash?.InvType;

        // Everything is updated, lets check if we should draw
        if (ShouldUpdateValues())
        {
            // Format stash items
            ItemList = StashPanel.IsVisible && tabType != null ? StashPanel.VisibleStash?.VisibleInventoryItems?.ToList() ?? [] : [];
            if (ItemList.Count == 0)
            {
                if (Settings.LeagueSpecificSettings.ShowMercenaryInventoryPrices &&
                    GameController.Game.IngameState.IngameUi.MercenaryEncounterWindow is { IsVisible: true, } mercenaryEncounterWindow)
                {
                    ItemList = mercenaryEncounterWindow.Inventories.SelectMany(x => x.VisibleInventoryItems).ToList();
                }
                if (Settings.LeagueSpecificSettings.ShowRitualWindowPrices &&
                    GameController.Game.IngameState.IngameUi.RitualWindow is { IsVisible: true, Items: { Count: > 0 } ritualItems })
                {
                    ItemList = ritualItems;
                }
                else if (Settings.LeagueSpecificSettings.ShowVillageRewardWindowPrices &&
                         GameController.Game.IngameState.IngameUi.VillageRewardWindow is { IsVisible: true, Items: { Count: > 0 } villageItems })
                {
                    ItemList = villageItems;
                }
                else if (Settings.LeagueSpecificSettings.ShowPurchaseWindowPrices &&
                         GameController.Game.IngameState.IngameUi.PurchaseWindow?.TabContainer?.VisibleStash is { IsVisible: true, VisibleInventoryItems: { Count: > 0 } purchaseWindowItems })
                {
                    ItemList = purchaseWindowItems.ToList();
                }
                else if (Settings.LeagueSpecificSettings.ShowPurchaseWindowPrices &&
                         GameController.Game.IngameState.IngameUi.PurchaseWindowHideout?.TabContainer?.VisibleStash is { IsVisible: true, VisibleInventoryItems: { Count: > 0 } hideoutPurchaseWindowItems })
                {
                    ItemList = hideoutPurchaseWindowItems.ToList();
                }
            }

            FormattedItemList = FormatItems(ItemList);

            if (Settings.DebugSettings.EnableDebugLogging)
                LogMessage($"{GetCurrentMethod()}.Render() Looping if (ShouldUpdateValues())", 5,
                    Color.LawnGreen);

            GetValue(FormattedItemList);
        }

        // Gather all information needed before rendering as we only want to iterate through the list once
        ItemsToDrawList = [];
        foreach (var item in FormattedItemList)
        {
            if (item == null || item.Element.Address == 0) continue; // Item is fucked, skip
            if (!item.Element.IsVisible && item.ItemType != ItemTypes.None)
                continue; // Disregard non visible items as that usually means they aren't part of what we want to look at

            StashTabValue += item.PriceData.MinChaosValue;
            ItemsToDrawList.Add(item);
        }
        if (InventoryPanel.IsVisible)
        {
            if (ShouldUpdateValuesInventory())
            {
                // Format Inventory Items
                InventoryItemList = GetInventoryItems();
                FormattedInventoryItemList = FormatItems(InventoryItemList);

                if (Settings.DebugSettings.EnableDebugLogging)
                    LogMessage($"{GetCurrentMethod()}.Render() Looping if (ShouldUpdateValuesInventory())", 5,
                        Color.LawnGreen);

                GetValue(FormattedInventoryItemList);
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

    public void DrawGraphics()
    {
        ProcessExpeditionWindow();
        ProcessItemsOnGround();
        ProcessTradeWindow();
        ProcessDivineFontRewards();
        DrawVillageUniqueWindow();
        ShowSanctumOfferPrices();
        ProcessUltimatumPanel();
        ProcessHoveredItem();
        VisibleInventoryValue();

        if (StashPanel.IsVisible)
        {
            VisibleStashValue();

            var tabType = StashPanel.VisibleStash?.InvType;
            if (Settings.PriceOverlaySettings.Show &&
                (!Settings.PriceOverlaySettings.DoNotDrawWhileAnItemIsHovered || HoveredItem == null))
            {
                foreach (var customItem in ItemsToDrawList)
                {
                    if (customItem.ItemType == ItemTypes.None) continue;

                    switch (tabType)
                    {
                        case InventoryType.CurrencyStash:
                        case InventoryType.FragmentStash:
                        case InventoryType.DelveStash:
                        case InventoryType.DeliriumStash:
                        case InventoryType.UltimatumStash:
                        case InventoryType.BlightStash:
                            PriceBoxOverItem(customItem, null, Settings.VisualPriceSettings.FontColor);
                            break;
                    }
                }
            }
        }
        else if (Settings.LeagueSpecificSettings.ShowRitualWindowPrices && GameController.IngameState.IngameUi.RitualWindow.IsVisible ||
                 Settings.LeagueSpecificSettings.ShowVillageRewardWindowPrices && GameController.IngameState.IngameUi.VillageRewardWindow.IsVisible ||
                 Settings.LeagueSpecificSettings.ShowMercenaryInventoryPrices && GameController.IngameState.IngameUi.MercenaryEncounterWindow.IsVisible ||
                 Settings.LeagueSpecificSettings.ShowPurchaseWindowPrices && (GameController.IngameState.IngameUi.PurchaseWindow.IsVisible ||
                                                                              GameController.IngameState.IngameUi.PurchaseWindowHideout.IsVisible))
        {
            if (Settings.PriceOverlaySettings.Show &&
                (!Settings.PriceOverlaySettings.DoNotDrawWhileAnItemIsHovered || HoveredItem == null))
            {
                foreach (var customItem in ItemsToDrawList)
                {
                    if (customItem.ItemType == ItemTypes.None) continue;
                    DrawItemPriceInline(customItem);
                }
            }
        }
    }

    private void DrawItemPriceInline(CustomItem customItem)
    {
        var text = customItem.PriceData.MinChaosValue.FormatNumber(Settings.VisualPriceSettings.SignificantDigits.Value);
        var textSize = Graphics.MeasureText(text);
        var topRight = customItem.Element.GetClientRectCache.TopRight.ToVector2Num();
        if (HoveredItemTooltipRect?.Intersects(new RectangleF(topRight.X - textSize.X, topRight.Y, textSize.X, textSize.Y)) ?? false)
        {
            return;
        }

        Graphics.DrawTextWithBackground(text,
            topRight,
            customItem.PriceData.MinChaosValue >= Settings.VisualPriceSettings.ValuableColorThreshold
                ? Settings.VisualPriceSettings.ValuableColor
                : Settings.VisualPriceSettings.FontColor, FontAlign.Right, Color.Black);
    }

    private void ProcessHoveredItem()
    {
        if (!Settings.HoveredItemSettings.Show) return;
        if (HoveredItem == null || HoveredItem.ItemType == ItemTypes.None) return;
        var textSections = new List<string> { "" };
        void AddSection() => textSections.Add("");
        void AddText(string text) => textSections[^1] += text;

        var changeText = $"Change in last 7 Days: {HoveredItem.PriceData.ChangeInLast7Days:+#;-#;0}%";
        var changeTextLength = changeText.Length - 1;
        var sectionBreak = $"\n{new string('-', changeTextLength)}\n";
        if (Math.Abs(HoveredItem.PriceData.ChangeInLast7Days) > 0.5)
        {
            AddText(changeText);
        }

        var priceInChaos = HoveredItem.PriceData.MinChaosValue;
        var priceInDivines = priceInChaos / DivinePrice;
        var priceInDivinesText = priceInDivines.FormatNumber(2);
        var minPriceText = priceInChaos.FormatNumber(2, Settings.VisualPriceSettings.MaximalValueForFractionalDisplay);
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
            case ItemTypes.KalguuranRune:
            case ItemTypes.AllflameEmber:
                if (priceInDivines >= 0.1)
                {
                    var priceInDivinessPerOne = priceInDivines / HoveredItem.CurrencyInfo.StackSize;
                    AddText(priceInDivinessPerOne >= 0.1
                        ? $"\nDivine: {priceInDivinesText}d ({priceInDivinessPerOne.FormatNumber(2)}d per one)"
                        : $"\nDivine: {priceInDivinesText}d");
                }
                AddText($"\nChaos: {minPriceText}c ({(priceInChaos / HoveredItem.CurrencyInfo.StackSize).FormatNumber(2, Settings.VisualPriceSettings.MaximalValueForFractionalDisplay)}c per one)");
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
                        ? $"\nIdentified as: {HoveredItem.UniqueNameCandidates.First()} (disenchants for {_disenchantCache.Value.FirstOrDefault(y => y.UniqueName?.Text == HoveredItem.UniqueNameCandidates.First())?.Value * 2000})"
                        : $"\nIdentified as one of:\n{string.Join('\n', HoveredItem.UniqueNameCandidates.Select(x => $"{x} (disenchants for {_disenchantCache.Value.FirstOrDefault(y => y.UniqueName?.Text == x)?.Value * 2000})"))}");
                }

                AddSection();
                if (priceInDivines >= 0.1)
                {
                    var maxDivinePriceText = (HoveredItem.PriceData.MaxChaosValue / DivinePrice).FormatNumber(2);
                    AddText(priceInDivinesText != maxDivinePriceText 
                        ? $"\nDivine: {priceInDivinesText}d - {maxDivinePriceText}d" 
                        : $"\nDivine: {priceInDivinesText}d");
                }

                var maxPriceText = HoveredItem.PriceData.MaxChaosValue.FormatNumber(2, Settings.VisualPriceSettings.MaximalValueForFractionalDisplay);
                AddText(minPriceText != maxPriceText 
                    ? $"\nChaos: {minPriceText}c - {maxPriceText}c" 
                    : $"\nChaos: {minPriceText}c");

                if (!string.IsNullOrEmpty(HoveredItem.UniqueName))
                {
                    AddText($"\nDisenchants for {_disenchantCache.Value.FirstOrDefault(x => x.UniqueName?.Text == HoveredItem.UniqueName)?.Value * 2000}");
                }

                break;
            case ItemTypes.Map:
            case ItemTypes.Invitation:
            case ItemTypes.SkillGem:
            case ItemTypes.ClusterJewel:
            case ItemTypes.InscribedUltimatum:
            case ItemTypes.Memory:
            case ItemTypes.Beast:
                if (priceInDivines >= 0.1)
                {
                    AddText($"\nDivine: {priceInDivinesText}d");
                }

                AddText($"\nChaos: {minPriceText}c");
                break;
        }

        if (Settings.DebugSettings.EnableDebugLogging)
        {
            AddSection();
            AddText($"\nUniqueName: {HoveredItem.UniqueName}"
                    + $"\nBaseName: {HoveredItem.BaseName}"
                    + $"\nItemType: {HoveredItem.ItemType}"
                    + $"\nMapType: {HoveredItem.MapInfo.MapType}"
                    + $"\nDetailsId: {HoveredItem.PriceData.DetailsId}");
        } 
                
        if (Settings.LeagueSpecificSettings.ShowArtifactChaosPrices)
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
            var valuable = priceInChaos >= Settings.VisualPriceSettings.ValuableColorThreshold.Value;
            if (valuable)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Settings.VisualPriceSettings.ValuableColor.Value.ToImgui());
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
            if (!Settings.StashValueSettings.Show ||
                !StashPanel.IsVisible ||
                !Settings.StashValueSettings.IgnoreChatPanel && GameController.Game.IngameState.IngameUi.ChatTitlePanel.IsVisible) return;
            {
                var pos = new Vector2(Settings.StashValueSettings.PositionX.Value, Settings.StashValueSettings.PositionY.Value);
                var chaosValue = StashTabValue;
                var topValueItems = GetTopValueItems(ItemsToDrawList)
                    .Take(Settings.StashValueSettings.TopValuedItemCount.Value)
                    .ToList();

                DrawWorthWidget(chaosValue, pos, Settings.VisualPriceSettings.SignificantDigits.Value, Settings.VisualPriceSettings.FontColor, Settings.StashValueSettings.EnableBackground,
                    topValueItems);
            }
        }
        catch (Exception e)
        {
            // ignored
            if (Settings.DebugSettings.EnableDebugLogging)
            {
                LogMessage("Error in: VisibleStashValue, restart PoEHUD.", 5, Color.Red);
                LogMessage(e.ToString(), 5, Color.Orange);
            }
        }
    }

    private static IEnumerable<CustomItem> GetTopValueItems(List<CustomItem> items)
    {
        return items
            .Where(x => x.PriceData.MinChaosValue != 0)
            .GroupBy(x => (x.PriceData.DetailsId, x.BaseName, 
                UniqueName: x.ItemType == ItemTypes.Beast ? null : x.UniqueName, 
                x.ItemType, x.CapturedMonsterName))
            .Select(group => new CustomItem
            {
                PriceData = { MinChaosValue = group.Sum(i => i.PriceData.MinChaosValue) },
                CurrencyInfo = { StackSize = group.Sum(i => i.CurrencyInfo.StackSize) },
                BaseName = group.Key.ItemType switch
                {
                    ItemTypes.Beast => group.Key.CapturedMonsterName,
                    _ => string.IsNullOrWhiteSpace(group.Key.UniqueName) ? group.Key.BaseName : group.Key.UniqueName,
                },
            })
            .OrderByDescending(x => x.PriceData.MinChaosValue);
    }

    private void DrawWorthWidget(double chaosValue, Vector2 pos, int significantDigits, Color textColor, bool drawBackground, List<CustomItem> topValueItems) => DrawWorthWidget("", false, chaosValue, pos, significantDigits, textColor, drawBackground, topValueItems);
    private void DrawWorthWidget(string initialString, bool indent, double chaosValue, Vector2 pos, int significantDigits, Color textColor, bool drawBackground, List<CustomItem> topValueItems)
    {
        var text = $"{initialString}{(indent ? "\t" : "")}Chaos: {chaosValue.FormatNumber(significantDigits)}\n" +
                   $"{(indent ? "\t" : "")}Divine: {(chaosValue / DivinePrice).FormatNumber(significantDigits)}";
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
                DrawWorthWidget(InventoryTabValue, pos, Settings.VisualPriceSettings.SignificantDigits.Value, Settings.VisualPriceSettings.FontColor, false, []);
            }
        }
        catch (Exception e)
        {
            // ignored
            if (Settings.DebugSettings.EnableDebugLogging)
            {

                LogMessage("Error in: VisibleInventoryValue, restart PoEHUD.", 5, Color.Red);
                LogMessage(e.ToString(), 5, Color.Orange);
            }
        }
    }

    private void PriceBoxOverItem(CustomItem item, RectangleF? containerBox, Color textColor)
    {
        var box = item.Element.GetClientRect();
        var drawBox = new RectangleF(box.X, box.Y - 2, box.Width, -Settings.PriceOverlaySettings.BoxHeight);

        (containerBox ?? default).Contains(ref drawBox, out var contains);
        if ((containerBox == null || contains) && 
            !drawBox.Intersects(HoveredItem?.Element?.Tooltip?.GetClientRectCache ?? default))
        {
            Graphics.DrawBox(drawBox, Settings.VisualPriceSettings.BackgroundColor);
            var textPosition = new Vector2(drawBox.Center.X, drawBox.Center.Y - ImGui.GetTextLineHeight() / 2);
            Graphics.DrawText(item.PriceData.MinChaosValue.FormatNumber(Settings.VisualPriceSettings.SignificantDigits.Value), textPosition,
                textColor, FontAlign.Center);
        }
    }

    private void PriceBoxOverItemHaggle(CustomItem item)
    {
        var box = item.Element.GetClientRect();
        var drawBox = new RectangleF(box.X, box.Y + 2, box.Width, +Settings.PriceOverlaySettings.BoxHeight);
        var position = new Vector2(drawBox.Center.X, drawBox.Center.Y - ImGui.GetTextLineHeight() / 2);

        if (item.PriceData.ItemBasePrices.Count == 0)
            return;

        Graphics.DrawBox(drawBox, Settings.VisualPriceSettings.BackgroundColor);
        Graphics.DrawText(item.PriceData.ItemBasePrices.Max().FormatNumber(Settings.VisualPriceSettings.SignificantDigits.Value), position, Settings.VisualPriceSettings.FontColor, FontAlign.Center);
        if (Settings.DebugSettings.EnableDebugLogging)
            Graphics.DrawText(string.Join(",", item.PriceData.ItemBasePrices), position, Settings.VisualPriceSettings.FontColor, FontAlign.Center);
    }

    private void ProcessExpeditionWindow()
    {
        if (!Settings.LeagueSpecificSettings.ShowExpeditionVendorOverlay || !HagglePanel.IsVisible) return;

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
        var itemList = inventory?.GetChildrenAs<NormalInventoryItem>().Skip(1).ToList() ?? [];
        if (haggleType == Gamble)
        {
            if (Settings.DebugSettings.EnableDebugLogging)
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
                    if (Settings.DebugSettings.EnableDebugLogging)
                    {
                        LogMessage("Error in: ExpeditionGamble, restart PoEHUD.", 5, Color.Red);
                        LogMessage(e.ToString(), 5, Color.Orange);
                    }
                }
            }
        }

        if (haggleType == Haggle)
        {
            var formattedItemList = GetValue(FormatItems(itemList));
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
                    Graphics.DrawText(customItem.PriceData.MinChaosValue.FormatNumber(2), box.TopRight.ToVector2Num(), Settings.VisualPriceSettings.FontColor, FontAlign.Right);
                }

                if (Settings.LeagueSpecificSettings.ShowArtifactChaosPrices && TryGetArtifactPrice(customItem, out var amount, out var artifactName))
                {
                    var text = $"[{artifactName[..3]}]\n" +
                               (customItem.PriceData.MinChaosValue > 0
                                   ? (customItem.PriceData.MinChaosValue / amount * 100).FormatNumber(2)
                                   : "");
                    var textSize = Graphics.MeasureText(text);
                    var leftTop = box.BottomLeft.ToVector2Num() - new Vector2(0, textSize.Y);
                    Graphics.DrawTextWithBackground(text, leftTop, Settings.VisualPriceSettings.FontColor, Color.Black);
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

        var yourFormattedItems = GetValue(FormatItems(yourItems));
        var theirFormatterItems = GetValue(FormatItems(theirItems));
        var yourTradeWindowValue = yourFormattedItems.Sum(x => x.PriceData.MinChaosValue);
        var theirTradeWindowValue = theirFormatterItems.Sum(x => x.PriceData.MinChaosValue);
        var textPosition = new Vector2(element.GetClientRectCache.Right, element.GetClientRectCache.Center.Y - ImGui.GetTextLineHeight() * 3) 
                         + new Vector2(Settings.TradeWindowSettings.OffsetX, Settings.TradeWindowSettings.OffsetY);
        DrawWorthWidget("Theirs\n", true, theirTradeWindowValue, textPosition, 2, Settings.VisualPriceSettings.FontColor, true, []);
        textPosition.Y += ImGui.GetTextLineHeight() * 3;
        var diff = theirTradeWindowValue - yourTradeWindowValue;
        DrawWorthWidget("Profit/Loss\n", true, diff, textPosition, 2, diff switch { > 0 => Color.Green, 0 => Settings.VisualPriceSettings.FontColor, < 0 => Color.Red, double.NaN => Color.Purple }, true, []);
        textPosition.Y += ImGui.GetTextLineHeight() * 3;
        DrawWorthWidget("Yours\n", true, yourTradeWindowValue, textPosition, 2, Settings.VisualPriceSettings.FontColor, true, []);
    }

    private void ProcessDivineFontRewards()
    {
        if ((!Settings.PriceOverlaySettings.DoNotDrawWhileAnItemIsHovered || HoveredItem == null) &&
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

    private string _disenchantFilter = "";
    private float _minDisenchantValue = 0;
    private float _maxDisenchantCost = 1000;
    private readonly CachedValue<List<VillageUniqueDisenchantValue>> _disenchantCache;

    private void DrawVillageUniqueWindow()
    {
        if (Settings.LeagueSpecificSettings.ShowVillageUniqueDisenchantValueWindow)
        {
            var show = true;
            if (ImGui.Begin("Unique disenchant values", ref show))
            {
                ImGui.SliderFloat("Minimum disenchant value", ref _minDisenchantValue, 0, 1000000);
                ImGui.SliderFloat("Maximum disenchant cost", ref _maxDisenchantCost, 0, 100000);
                ImGui.InputTextWithHint("##filter", "Filter", ref _disenchantFilter, 200);
                if (ImGui.BeginTable("Unique disenchant values", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.Sortable | ImGuiTableFlags.SizingFixedFit))
                {
                    ImGui.TableSetupColumn("Name (click to search on trade)");
                    ImGui.TableSetupColumn("Price");
                    ImGui.TableSetupColumn("Dust");
                    ImGui.TableSetupColumn("Dust/chaos", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.PreferSortDescending);
                    ImGui.TableHeadersRow();
                    var uniquePrices = CollectedData.UniqueArmours.Lines.GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.Min(y => y.ChaosValue ?? 0))
                        .Concat(CollectedData.UniqueWeapons.Lines.GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.Min(y => y.ChaosValue ?? 0)))
                        .Concat(CollectedData.UniqueAccessories.Lines.GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.Min(y => y.ChaosValue ?? 0)))
                        .ToDictionary(x => x.Key.Replace('\x2019', '\x27'), x => x.Value);
                    var excludedUniques = CollectedData.UniqueFlasks.Lines.Select(x => x.Name)
                        .Concat(CollectedData.UniqueMaps.Lines.Select(x => x.Name))
                        .Concat(CollectedData.UniqueJewels.Lines.Select(x => x.Name))
                        .Select(x => x.Replace('\x2019', '\x27'))
                        .ToHashSet();

                    var unfilteredItems = _disenchantCache.Value.ExceptBy(excludedUniques, x => x.UniqueName?.Text.Replace('\x2019', '\x27') ?? "")
                        .Select(x => (
                            Name: x.UniqueName?.Text.Replace('\x2019', '\x27') ?? "",
                            Cost: uniquePrices.GetValueOrDefault(x.UniqueName?.Text ?? ""),
                            Value: x.Value * 2000,
                            ValuePerChaos: uniquePrices.TryGetValue(x.UniqueName?.Text.Replace('\x2019', '\x27') ?? "", out var price) ? x.Value * 2000 / Math.Max(1, price) : 0))
                        .ToList();
                    var items = unfilteredItems.Any()
                        ? unfilteredItems
                            .Where(x => string.IsNullOrEmpty(_disenchantFilter) || x.Name.Contains(_disenchantFilter, StringComparison.InvariantCultureIgnoreCase))
                            .Where(x => x.Value >= _minDisenchantValue)
                            .Where(x => x.Cost <= _maxDisenchantCost)
                            .ToList()
                        : [("No items in game's memory...\nYou may need to approach Rog and give him\na unique one for data to show up", 0, -1, 0)];
                    var sortSpecs = ImGui.TableGetSortSpecs();
                    if (sortSpecs.SpecsCount > 0)
                    {
                        object SortSelector((string Name, double Cost, float Value, double ValuePerChaos) x) =>
                            sortSpecs.Specs.ColumnIndex switch
                            {
                                0 => x.Name,
                                1 => x.Cost,
                                2 => x.Value,
                                3 => x.ValuePerChaos,
                                _ => 0,
                            };

                        if (sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending)
                        {
                            items = items.OrderBy(SortSelector).ToList();
                        }
                        else if (sortSpecs.Specs.SortDirection == ImGuiSortDirection.Descending)
                        {
                            items = items.OrderByDescending(SortSelector).ToList();
                        }
                    }

                    foreach (var item in items)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        if (item.Value == -1)
                        {
                            ImGui.TextUnformatted(item.Name);
                        }
                        else if (ImGui.Selectable(item.Name))
                        {
                            var query = $$$"""{"query":{"status":{"option":"online"},"term":"{{{item.Name}}}","stats":[{"type":"and","filters":[]}]},"sort":{"price":"asc"}}""";
                            Process.Start(new ProcessStartInfo(
                                $"https://www.pathofexile.com/trade/search/{Settings.DataSourceSettings.League.Value}?q={WebUtility.UrlEncode(query)}")
                            {
                                UseShellExecute = true
                            });
                        }

                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"{item.Cost:0.##}");
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"{item.Value:F0}");
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"{item.ValuePerChaos:F0}");
                    }

                    ImGui.EndTable();
                }

                ImGui.End();
            }

            Settings.LeagueSpecificSettings.ShowVillageUniqueDisenchantValueWindow.Value = show;
        }
    }

    private static readonly Regex SanctumOfferParse = new(
        @"(Receive)\s((?'currencysize'(\d+))x(?'currencyname'.*))\s(right now|at the end of the next Floor|at the end of the Floor|on completing the Sanctum)$",
        RegexOptions.Compiled);

    private void ShowSanctumOfferPrices()
    {
        if (!Settings.LeagueSpecificSettings.ShowSanctumRewardPrices ||
            GameController.IngameState.IngameUi.SanctumRewardWindow is not
            {
                IsVisible: true,
                RewardElements: { Count: > 0 } rewardElements
            })
            return;

        foreach (var offer in rewardElements)
        {
            var offerText = offer.Children[1].Text;
            if (offerText == null) continue;
            var match = SanctumOfferParse.Match(offerText);
            if (!match.Success)
            {
                continue;
            }

            var currencyName = match.Groups["currencyname"].Value.Trim().Replace("Orbs", "Orb").TrimEnd('s');
            var stackSizeText = match.Groups["currencysize"].ValueSpan.Trim();
            if (!int.TryParse(stackSizeText, out var stackSize))
                continue;
            var data = new CustomItem
            {
                CurrencyInfo = new CustomItem.CurrencyData
                {
                    IsShard = false,
                    StackSize = stackSize
                },
                BaseName = currencyName,
                ItemType = ItemTypes.Currency,
                Element = offer.GetChildFromIndices(0, 0),
            };
            GetValue(data);
            PriceBoxOverItem(data, null, data.PriceData.MinChaosValue >= Settings.VisualPriceSettings.ValuableColorThreshold
                ? Settings.VisualPriceSettings.ValuableColor
                : Settings.VisualPriceSettings.FontColor);
        }
    }

    private void ProcessItemsOnGround()
    {
        if (!Settings.GroundItemSettings.PriceItemsOnGround && !Settings.UniqueIdentificationSettings.ShowRealUniqueNameOnGround && !Settings.GroundItemSettings.PriceHeistRewards) return;
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
                {
                    if (Settings.SoundNotificationSettings.Enabled && 
                        !_soundPlayedTracker.ContainsKey(item.EntityId))
                    {
                        var matchingCustomFile =
                            item.UniqueNameCandidates.Any() ||
                            !string.IsNullOrEmpty(item.UniqueName)
                                ? item.UniqueNameCandidates
                                    .DefaultIfEmpty(item.UniqueName)
                                    .Select(x => _soundFiles.GetValueOrDefault(x))
                                    .FirstOrDefault(x => x != null)
                                : null;
                        if (item.PriceData.MaxChaosValue >= Settings.SoundNotificationSettings.ValueThreshold ||
                            Settings.SoundNotificationSettings.PlayCustomSoundsIfBelowThreshold && matchingCustomFile != null)
                        {
                            if (_soundPlayedTracker.TryAdd(item.EntityId, true))
                            {
                                var defaultFile = Path.Join(ConfigDirectory, "default.wav");
                                if (matchingCustomFile != null && !File.Exists(matchingCustomFile))
                                {
                                    LogError($"Unable to find {matchingCustomFile}. It was probably deleted. Reload the sound list to update your preferences");
                                    matchingCustomFile = null;
                                }

                                var fileToPlay = matchingCustomFile ?? defaultFile;

                                if (File.Exists(fileToPlay))
                                {
                                    GameController.SoundController.PlaySound(fileToPlay, Settings.SoundNotificationSettings.Volume);
                                }
                                else if (fileToPlay == defaultFile)
                                {
                                    LogError(
                                        $"Unable to find the default sound file ({defaultFile}) to play. Disable the sound notification feature, reload the sound list to let the plugin create it, or create it yourself");
                                }
                            }
                        }
                    }

                    if (!tooltipRect.Intersects(box) && !leftPanelRect.Intersects(box) && !rightPanelRect.Intersects(box))
                    {
                        var isValuable = item.PriceData.MaxChaosValue >= Settings.VisualPriceSettings.ValuableColorThreshold;

                        if (Settings.GroundItemSettings.PriceItemsOnGround &&
                            (Settings.GroundItemSettings.OnlyPriceItemsAboveThreshold
                                ? item.PriceData.MinChaosValue >= Settings.GroundItemSettings.ValueThreshold
                                : item.PriceData.MinChaosValue > 0) &&
                            (!Settings.GroundItemSettings.OnlyPriceUniquesOnGround || item.Rarity == ItemRarity.Unique))
                        {
                            var s = item.PriceData.MinChaosValue.FormatNumber(2);
                            if (item.PriceData.MaxChaosValue > item.PriceData.MinChaosValue)
                                s += $"-{item.PriceData.MaxChaosValue.FormatNumber(2)}";

                            using (Graphics.SetTextScale(Settings.GroundItemSettings.GroundPriceTextScale))
                            {
                                var textSize = Graphics.MeasureText(s);
                                var textPos = new Vector2(box.Right - textSize.X, box.Top);
                                Graphics.DrawBox(textPos, new Vector2(box.Right, box.Top + textSize.Y), Settings.GroundItemSettings.GroundPriceBackgroundColor);
                                Graphics.DrawText(s, textPos, isValuable ? Settings.VisualPriceSettings.ValuableColor : Settings.VisualPriceSettings.FontColor);
                            }
                        }

                        if (Settings.UniqueIdentificationSettings.ShowRealUniqueNameOnGround && !item.IsIdentified && item.Rarity == ItemRarity.Unique)
                        {
                            float GetRatio(string text)
                            {
                                var textSize = Graphics.MeasureText(text);
                                return Math.Min(box.Width * Settings.UniqueIdentificationSettings.UniqueLabelSize / textSize.X, (box.Height - 2) / textSize.Y);
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
                                if (Settings.UniqueIdentificationSettings.OnlyShowRealUniqueNameForValuableUniques && !isValuable)
                                {
                                    continue;
                                }

                                var textColor = isValuable ? Settings.UniqueIdentificationSettings.ValuableUniqueItemNameTextColor : Settings.UniqueIdentificationSettings.UniqueItemNameTextColor;
                                var backgroundColor = isValuable
                                    ? Settings.UniqueIdentificationSettings.ValuableUniqueItemNameBackgroundColor
                                    : Settings.UniqueIdentificationSettings.UniqueItemNameBackgroundColor;
                                var (text, ratio) = Enumerable.Range(1, item.UniqueNameCandidates.Count).Select(perOneLine =>
                                        string.Join('\n', MoreLinq.Extensions.BatchExtension.Batch(item.UniqueNameCandidates, perOneLine)
                                            .Select(onLine => string.Join(" / ", onLine))))
                                    .Select(text => (text, ratio: GetRatio(text)))
                                    .MaxBy(x => x.ratio);

                                DrawOnItemLabel(ratio, text, backgroundColor, textColor);
                            }
                            else if (Settings.UniqueIdentificationSettings.ShowWarningTextForUnknownUniques)
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
                                Graphics.DrawText(s, textPos, Settings.VisualPriceSettings.FontColor);
                            }
                        }
                    }

                    break;
                }
            }
                
        }
        
        ImGui.End();
    }

    private void ProcessUltimatumPanel()
    {
        var ultimatumPanel = GameController.IngameState.IngameUi.UltimatumPanel;
        if (!Settings.LeagueSpecificSettings.ShowUltimatumOverlay || ultimatumPanel is not
            {
                IsVisible: true
            })
            return;

        var earnedRewardsInventory = ultimatumPanel.EarnedRewardsInventory;
        var earnedItems = earnedRewardsInventory?.VisibleInventoryItems ?? [];
        var earnedItemsFormatted = GetValue(FormatItems(earnedItems));

        if (earnedRewardsInventory is { IsVisible: true })
        {
            foreach (var customItem in earnedItemsFormatted)
            {
                if (customItem.PriceData.MinChaosValue > 0)
                    DrawItemPriceInline(customItem);
            }
        }
        else
        {
            var itemList = ultimatumPanel.NextRewardInventory?.VisibleInventoryItems ?? [];

            var formattedItemList = GetValue(FormatItems(itemList));

            foreach (var customItem in formattedItemList)
            {
                if (customItem.PriceData.MinChaosValue > 0)
                    DrawItemPriceInline(customItem);
            }

            itemList = ultimatumPanel.LastRewardInventory?.VisibleInventoryItems ?? [];

            formattedItemList = GetValue(FormatItems(itemList));

            foreach (var customItem in formattedItemList)
            {
                if (customItem.PriceData.MinChaosValue > 0)
                    DrawItemPriceInline(customItem);
            }

            var initialString = "(cached)\n";
            if (GameController.IngameState.ServerData.NPCInventories.FirstOrDefault(x => x.Inventory.InventType == InventoryTypeE.UltimatumEarnedRewards)?.Inventory is { } inventory)
            {
                var items = inventory.Items.Select(x => new CustomItem(x, null)).ToList();
                GetValue(items);
                earnedItemsFormatted = items;
                initialString = "";
            }

            DrawWorthWidget(initialString, false,
                earnedItemsFormatted.Sum(x => x.PriceData.MinChaosValue),
                ultimatumPanel.OpenEarnedRewardsInventoryButton.GetClientRectCache.TopRight.ToVector2Num(),
                2, Color.White, true,
                GetTopValueItems(earnedItemsFormatted).Take(10).ToList());
        }
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