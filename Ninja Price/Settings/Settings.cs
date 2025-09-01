using System;
using System.Windows.Forms;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using ImGuiNET;
using Newtonsoft.Json;
using SharpDX;

namespace Ninja_Price.Settings;

public class Settings : ISettings
{
    public DataSourceSettings DataSourceSettings { get; set; } = new();
    public DebugSettings DebugSettings { get; set; } = new();
    public StashValueSettings StashValueSettings { get; set; } = new();
    public InventoryValueSettings InventoryValueSettings { get; set; } = new();
    public GroundItemSettings GroundItemSettings { get; set; } = new();
    public UniqueIdentificationSettings UniqueIdentificationSettings { get; set; } = new();
    public TradeWindowSettings TradeWindowSettings { get; set; } = new();
    public HoveredItemSettings HoveredItemSettings { get; set; } = new();
    public PriceOverlaySettings PriceOverlaySettings { get; set; } = new();
    public LeagueSpecificSettings LeagueSpecificSettings { get; set; } = new();
    public VisualPriceSettings VisualPriceSettings { get; set; } = new();
    public SoundNotificationSettings SoundNotificationSettings { get; set; } = new();
    public ToggleNode Enable { get; set; } = new(true);
}

[Submenu(CollapsedByDefault = true)]
public class DebugSettings
{
    public ToggleNode EnableDebugLogging { get; set; } = new(false);
    public HotkeyNode InspectHoverHotkey { get; set; } = new(Keys.None);

    [JsonIgnore]
    public ButtonNode ResetInspectedItem { get; set; } = new();
}

[Submenu(CollapsedByDefault = true)]
public class DataSourceSettings
{
    [IgnoreMenu]
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
}

[Submenu(CollapsedByDefault = true)]
public class LeagueSpecificSettings
{
    public ToggleNode ShowMercenaryInventoryPrices { get; set; } = new(true);
    public ToggleNode ShowRitualWindowPrices { get; set; } = new(true);
    public ToggleNode ShowVillageRewardWindowPrices { get; set; } = new(true);
    public ToggleNode ShowPurchaseWindowPrices { get; set; } = new(true);
    public ToggleNode ShowSanctumRewardPrices { get; set; } = new(true);
    public ToggleNode ShowVillageUniqueDisenchantValueWindow { get; set; } = new(false);

    public ToggleNode ShowExpeditionVendorOverlay { get; set; } = new(false);
    public ToggleNode ShowUltimatumOverlay { get; set; } = new(true);

    [Menu(null, "Display chaos equivalent price for items with artifact costs")]
    [JsonProperty("ShowArtifactChaosPricesv2")]
    public ToggleNode ShowArtifactChaosPrices { get; set; } = new(false);
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
    public ToggleNode OnlyPriceItemsAboveThreshold { get; set; } = new(false);
    public RangeNode<int> ValueThreshold { get; set; } = new(50, 0, 1000);
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
    public ContentNode<TextNode> ExcludedUniques { get; set; } = new ContentNode<TextNode> { EnableControls = true, UseFlatItems = true, ItemFactory = () => new TextNode("") };
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
    public ToggleNode IgnoreChatPanel { get; set; } = new(false);
}

[Submenu(CollapsedByDefault = true)]
public class PriceOverlaySettings
{
    public ToggleNode Show { get; set; } = new(true);

    [JsonProperty("DoNotDrawWhileAnItemIsHovered2")]
    public ToggleNode DoNotDrawWhileAnItemIsHovered { get; set; } = new(false);

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

[Submenu(CollapsedByDefault = true)]
public class SoundNotificationSettings
{
    [JsonIgnore]
    public CustomNode Information { get; set; } = new CustomNode(() =>
    {
        ImGui.Text($"By default, plays {Main.Main.DefaultWav} in the plugin's config directory.\nTo customize sounds per unique, create UniqueName.wav in the same directory");
    });

    [JsonIgnore]
    public ButtonNode OpenConfigDirectory { get; set; } = new ButtonNode();

    public ToggleNode Enabled { get; set; } = new ToggleNode(true);

    [JsonIgnore]
    public ButtonNode ReloadSoundList { get; set; } = new ButtonNode();

    [JsonIgnore]
    [Menu(null, "For debugging your alerts")]
    public ButtonNode ResetEntityNotificationFlags { get; set; } = new ButtonNode();

    public RangeNode<float> Volume { get; set; } = new(1, 0, 2);
    public RangeNode<int> ValueThreshold { get; set; } = new(50, 0, 100000);
    public ToggleNode PlayCustomSoundsIfBelowThreshold { get; set; } = new ToggleNode(true);
}