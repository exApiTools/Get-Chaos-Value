using System;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using Newtonsoft.Json;
using SharpDX;

namespace Ninja_Price.Settings;

public class Settings : ISettings
{
    public DateTime LastUpDateTime { get; set; } = DateTime.Now;

    [Menu("Value Loop Timer")]
    public RangeNode<int> ValueLoopTimerMS { get; set; } = new(250, 1, 2000);

    [Menu("League", 1)]
    public ListNode LeagueList { get; set; } = new();

    public ToggleNode SyncCurrentLeague { get; set; } = new(true);

    [Menu("Map Variant Check", "Toggle Map Variant Checking", 1)]
    public ToggleNode MapVariant { get; set; } = new(true);

    [JsonIgnore]
    public ButtonNode ReloadPrices { get; set; } = new();

    [Menu("Auto Reload Toggle", 3)]
    public ToggleNode AutoReload { get; set; } = new();

    [Menu("Reload timer (Minutes)", 31, 3)]
    public RangeNode<int> ReloadTimer { get; set; } = new(15, 1, 60);

    [Menu("Plugin Wide Text Color", 355, 3)]
    public ColorNode UniTextColor { get; set; } = Color.White;

    [Menu("Debug", "Display debug strings", 6)]
    public ToggleNode Debug { get; set; } = new(false);

    [Menu(null, "Set to 0 to disable")]
    public RangeNode<float> MaximalValueForFractionalDisplay { get; set; } = new(0.2f, 0, 1);

    [Menu(null, "ChaosEquivalent is not used by poe.ninja itself and can have weird values. If you like it better, try it")]
    public ToggleNode UseChaosEquivalentDataForCurrency { get; set; } = new(false);

    [JsonProperty("visibleStashValue2")]
    public StashValueSettings VisibleStashValue { get; set; } = new();

    public ToggleNode DisplayExpeditionVendorOverlay { get; set; } = new(false);

    public InventoryValueSettings InventoryValueSettings { get; set; } = new();

    [Menu("Artifact Chaos Prices", "Display chaos equivalent price for items with artifact costs", 7)]
    public ToggleNode ArtifactChaosPrices { get; set; } = new(true);

    public GroundItemSettings GroundItemSettings { get; set; } = new();
    public UniqueIdentificationSettings UniqueIdentificationSettings { get; set; } = new();
    public TradeWindowSettings TradeWindowSettings { get; set; } = new();
    public HoveredItemSettings HoveredItemSettings { get; set; } = new();

    public ToggleNode Enable { get; set; } = new(true);
}

[Submenu]
public class InventoryValueSettings
{
    [Menu(null, "Calculate value (in chaos) for the current visible Inventory tab.")]
    public ToggleNode Show { get; set; } = new(true);

    [Menu(null, "Horizontal position of where the value in chaos should be drawn.")]
    public RangeNode<int> PositionX { get; set; } = new(100, 0, 5000);

    [Menu(null, "Horizontal position of where the value in chaos should be drawn.")]
    public RangeNode<int> PositionY { get; set; } = new(800, 0, 5000);
}

[Submenu]
public class TradeWindowSettings
{
    public ToggleNode Show { get; set; } = new(true);
    public RangeNode<int> OffsetX { get; set; } = new(0, -2000, 2000);
    public RangeNode<int> OffsetY { get; set; } = new(0, -2000, 2000);
}

[Submenu]
public class HoveredItemSettings
{
    public ToggleNode Show { get; set; } = new(true);
    public RangeNode<int> ValuableColorThreshold { get; set; } = new(50, 0, 100000);
    public ColorNode ValuableColor { get; set; } = new(Color.Violet);
}

[Submenu]
public class GroundItemSettings
{
    public ToggleNode PriceHeistRewards { get; set; } = new(true);
    public ToggleNode PriceCoffins { get;set; } = new(true);
    public ToggleNode PriceItemsOnGround { get; set; } = new(true);

    [ConditionalDisplay(nameof(PriceItemsOnGround))]
    public ToggleNode OnlyPriceUniquesOnGround { get; set; } = new(true);

    [ConditionalDisplay(nameof(PriceItemsOnGround))]
    public RangeNode<float> GroundPriceTextScale { get; set; } = new(2, 0, 10);

    [ConditionalDisplay(nameof(PriceItemsOnGround))]
    public ColorNode GroundPriceTextColor { get; set; } = new(Color.White);

    [ConditionalDisplay(nameof(PriceItemsOnGround))]
    public ColorNode GroundPriceBackgroundColor { get; set; } = new(Color.Black);

    [ConditionalDisplay(nameof(PriceItemsOnGround))]
    public ToggleNode UseRawElementPositionWhileMoving { get; set; } = new(true);

    [ConditionalDisplay(nameof(PriceItemsOnGround))]
    public ToggleNode AlwaysUseRawElementPosition { get; set; } = new(false);

    public ToggleNode DisplayRealUniqueNameOnGround { get; set; } = new(true);

    public ToggleNode OnlyDisplayRealUniqueNameForValuableUniques { get; set; } = new(false);
    public ToggleNode DisplayWarningTextForUnknownUniques { get; set; } = new(true);

    public RangeNode<float> UniqueLabelSize { get; set; } = new(0.8f, 0.1f, 1);

    public ColorNode UniqueItemNameTextColor { get; set; } = new(Color.Black);

    public ColorNode UniqueItemNameBackgroundColor { get; set; } = new(new Color(175, 96, 37));

    public RangeNode<int> ValuableUniqueOnGroundValueThreshold { get; set; } = new(50, 0, 100000);

    public ColorNode ValuableUniqueItemNameTextColor { get; set; } = new(new Color(175, 96, 37));

    public ColorNode ValuableUniqueItemNameBackgroundColor { get; set; } = new(Color.White);
}

[Submenu]
public class UniqueIdentificationSettings
{
    [JsonIgnore]
    public ButtonNode RebuildUniqueItemArtMappingBackup { get; set; } = new();

    [Menu(null, "Use if you want to ignore what's in game memory and rely only on your custom/builtin file")]
    public ToggleNode IgnoreGameUniqueArtMapping { get; set; } = new(false);
}

[Submenu]
public class StashValueSettings
{
    [Menu(null, "Calculate value (in chaos) for the current visible stash tab.")]
    public ToggleNode Show { get; set; } = new(true);

    [Menu("X", "Horizontal position of where the value should be drawn.")]
    public RangeNode<int> PositionX { get; set; } = new(100, 0, 5000);

    [Menu("Y", "Horizontal position of where the value should be drawn.")]
    public RangeNode<int> PositionY { get; set; } = new(100, 0, 5000);

    public RangeNode<int> SignificantDigits { get; set; } = new(2, 0, 2);
    public RangeNode<int> TopValuedItemCount { get; set; } = new(3, 0, 10);
    public ToggleNode EnableBackground { get; set; } = new(true);
    public CurrencyTabSettings CurrencyTabSettings { get; set; } = new();
}

[Submenu]
public class CurrencyTabSettings
{
    public ToggleNode ShowItemOverlay { get; set; } = new(true);
    public ToggleNode DoNotDrawWhileAnItemIsHovered { get; set; } = new(true);
    public RangeNode<int> FontSize { get; set; } = new(14, 5, 50);
    public RangeNode<int> SignificantDigits { get; set; } = new(2, 0, 2);
    public RangeNode<int> BoxHeight { get; set; } = new(15, 0, 100);
    public ColorNode FontColor { get; set; } = new Color(216, 216, 216, 255);
    public ColorNode BackgroundColor { get; set; } = new Color(0, 0, 0, 255);
}