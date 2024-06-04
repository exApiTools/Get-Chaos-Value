using System;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using Newtonsoft.Json;
using SharpDX;

namespace Ninja_Price.Settings;

public class Settings : ISettings
{
    public DataSourceSettings DataSourceSettings { get; set; } = new();
    public ToggleNode EnableDebugLogging { get; set; } = new(false);
    public StashValueSettings StashValueSettings { get; set; } = new();
    public InventoryValueSettings InventoryValueSettings { get; set; } = new();
    public GroundItemSettings GroundItemSettings { get; set; } = new();
    public UniqueIdentificationSettings UniqueIdentificationSettings { get; set; } = new();
    public TradeWindowSettings TradeWindowSettings { get; set; } = new();
    public HoveredItemSettings HoveredItemSettings { get; set; } = new();
    public PriceOverlaySettings PriceOverlaySettings { get; set; } = new();
    public LeagueSpecificSettings LeagueSpecificSettings { get; set; } = new();
    public LeagueHighlight LeagueHighlight { get; set; } = new();
    public VisualPriceSettings VisualPriceSettings { get; set; } = new();
    public ToggleNode Enable { get; set; } = new(true);
}

[Submenu(CollapsedByDefault = true)]
public class DataSourceSettings
{
    public DateTime LastUpdateTime { get; set; } = DateTime.Now;

    public RangeNode<int> ItemUpdatePeriodMs { get; set; } = new(250, 1, 2000);

    public ListNode League { get; set; } = new();

    public ToggleNode SyncCurrentLeague { get; set; } = new(true);

    [JsonIgnore]
    public ButtonNode ReloadPrices { get; set; } = new();

    public ToggleNode AutoReload { get; set; } = new();

    [Menu(null, "Minutes")]
    public RangeNode<int> ReloadPeriod { get; set; } = new(15, 1, 60);

    public ToggleNode CheckMapVariant { get; set; } = new(false);

    [Menu(null, "ChaosEquivalent is not used by poe.ninja itself and can have weird values. If you like it better, try it")]
    public ToggleNode UseChaosEquivalentDataForCurrency { get; set; } = new(false);
}

[Submenu(CollapsedByDefault = true)]
public class LeagueSpecificSettings
{
    public ToggleNode ShowCoffinPrices { get; set; } = new(true);
    public ToggleNode ShowRitualWindowPrices { get; set; } = new(true);
    public ToggleNode ShowSanctumRewardPrices { get; set; } = new(true);

    public ToggleNode ShowExpeditionVendorOverlay { get; set; } = new(false);

    [Menu("Artifact Chaos Prices", "Display chaos equivalent price for items with artifact costs", 7)]
    public ToggleNode ShowArtifactChaosPrices { get; set; } = new(true);
    public ToggleNode ShowUniquesGamble { get; set; } = new(true);
}
[Submenu(CollapsedByDefault = true)]
public class LeagueHighlight
{
    public RangeNode<float> ExceptionalBlackScytheThreshold { get; set; } = new(5f, 0, 25);

    public RangeNode<float> GrandBlackScytheThreshold { get; set; } = new(1.80f, 0, 5);

    public RangeNode<float> GreaterBlackScytheThreshold { get; set; } = new(1.40f, 0, 5);

    public RangeNode<float> LesserBlackScytheThreshold { get; set; } = new(1.25f, 0, 5);

    public RangeNode<int> GwennenChaosCutoff { get; set; } = new(0, 15, 200);
    public RangeNode<float> RitualThrehsold { get; set; } = new(1.0f, 0, 5);
    public ColorNode HighlightColor { get; set; } = new Color(0, 150, 0, 255);
}
[Submenu(CollapsedByDefault = true)]
public class InventoryValueSettings
{
    [Menu(null, "Calculate value for the inventory")]
    public ToggleNode Show { get; set; } = new(true);

    [Menu(null, "Horizontal position of where the value should be drawn")]
    public RangeNode<int> PositionX { get; set; } = new(100, 0, 5000);

    [Menu(null, "Vertical position of where the value should be drawn")]
    public RangeNode<int> PositionY { get; set; } = new(800, 0, 5000);
}

[Submenu(CollapsedByDefault = true)]
public class TradeWindowSettings
{
    public ToggleNode Show { get; set; } = new(true);
    public RangeNode<int> OffsetX { get; set; } = new(0, -2000, 2000);
    public RangeNode<int> OffsetY { get; set; } = new(0, -2000, 2000);
}

[Submenu(CollapsedByDefault = true)]
public class HoveredItemSettings
{
    public ToggleNode Show { get; set; } = new(true);
}

[Submenu(CollapsedByDefault = true)]
public class GroundItemSettings
{
    public ToggleNode PriceHeistRewards { get; set; } = new(true);
    public ToggleNode PriceItemsOnGround { get; set; } = new(true);
    public ToggleNode OnlyPriceUniquesOnGround { get; set; } = new(false);
    public RangeNode<float> GroundPriceTextScale { get; set; } = new(2, 0, 10);
    public ColorNode GroundPriceBackgroundColor { get; set; } = new(Color.Black);
}

[Submenu(CollapsedByDefault = true)]
public class UniqueIdentificationSettings
{
    [JsonIgnore]
    public ButtonNode RebuildUniqueItemArtMappingBackup { get; set; } = new();

    [Menu(null, "Use if you want to ignore what's in game memory and rely only on your custom/builtin file")]
    public ToggleNode IgnoreGameUniqueArtMapping { get; set; } = new(false);

    public ToggleNode ShowRealUniqueNameOnGround { get; set; } = new(true);
    public ToggleNode OnlyShowRealUniqueNameForValuableUniques { get; set; } = new(false);
    public ToggleNode ShowWarningTextForUnknownUniques { get; set; } = new(true);
    public RangeNode<float> UniqueLabelSize { get; set; } = new(0.8f, 0.1f, 1);
    public ColorNode UniqueItemNameTextColor { get; set; } = new(Color.Black);
    public ColorNode UniqueItemNameBackgroundColor { get; set; } = new(new Color(175, 96, 37));
    public ColorNode ValuableUniqueItemNameTextColor { get; set; } = new(new Color(175, 96, 37));
    public ColorNode ValuableUniqueItemNameBackgroundColor { get; set; } = new(Color.White);
}

[Submenu(CollapsedByDefault = true)]
public class StashValueSettings
{
    [Menu(null, "Calculate value for the current visible stash tab")]
    public ToggleNode Show { get; set; } = new(true);

    [Menu(null, "Horizontal position of where the value should be drawn")]
    public RangeNode<int> PositionX { get; set; } = new(100, 0, 5000);

    [Menu(null, "Vertical position of where the value should be drawn")]
    public RangeNode<int> PositionY { get; set; } = new(100, 0, 5000);

    public RangeNode<int> TopValuedItemCount { get; set; } = new(3, 0, 10);
    public ToggleNode EnableBackground { get; set; } = new(true);
}

[Submenu(CollapsedByDefault = true)]
public class PriceOverlaySettings
{
    public ToggleNode Show { get; set; } = new(true);
    public ToggleNode DoNotDrawWhileAnItemIsHovered { get; set; } = new(true);
    public RangeNode<int> BoxHeight { get; set; } = new(15, 0, 100);
}

[Submenu(CollapsedByDefault = true)]
public class VisualPriceSettings
{
    public RangeNode<int> SignificantDigits { get; set; } = new(2, 0, 2);
    public ColorNode FontColor { get; set; } = new Color(216, 216, 216, 255);
    public ColorNode BackgroundColor { get; set; } = new Color(0, 0, 0, 255);
    public RangeNode<int> ValuableColorThreshold { get; set; } = new(50, 0, 100000);
    public ColorNode ValuableColor { get; set; } = new(Color.Violet);

    [Menu(null, "Set to 0 to disable")]
    public RangeNode<float> MaximalValueForFractionalDisplay { get; set; } = new(0.2f, 0, 1);
}