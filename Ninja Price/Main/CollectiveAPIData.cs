using Ninja_Price.API.PoeNinja;

namespace Ninja_Price.Main;

public class CollectiveApiData
{
    public CurrencyOverviewData.RootObject Currency { get; set; } = new();
    public CurrencyOverviewData.RootObject DivinationCards { get; set; } = new();
    public CurrencyOverviewData.RootObject Essences { get; set; } = new();
    public CurrencyOverviewData.RootObject Fragments { get; set; } = new();
    public CurrencyOverviewData.RootObject Resonators { get; set; } = new();
    public CurrencyOverviewData.RootObject Fossils { get; set; } = new();
    public CurrencyOverviewData.RootObject Scarabs { get; set; } = new();
    public CurrencyOverviewData.RootObject Oils { get; set; } = new();
    public CurrencyOverviewData.RootObject DeliriumOrb { get; set; } = new();
    public CurrencyOverviewData.RootObject Artifacts { get; set; } = new();
    public CurrencyOverviewData.RootObject Tattoos { get; set; } = new();
    public CurrencyOverviewData.RootObject Omens { get; set; } = new();
    public CurrencyOverviewData.RootObject KalguuranRunes { get; set; } = new();
    public CurrencyOverviewData.RootObject AllflameEmbers { get; set; } = new();

    public Invitations.RootObject Invitations { get; set; } = new();
    public Vials.RootObject Vials { get; set; } = new();
    public Incubators.RootObject Incubators { get; set; } = new();
    public UniqueAccessories.RootObject UniqueAccessories { get; set; } = new();
    public UniqueArmours.RootObject UniqueArmours { get; set; } = new();
    public UniqueFlasks.RootObject UniqueFlasks { get; set; } = new();
    public UniqueJewels.RootObject UniqueJewels { get; set; } = new();
    public UniqueMaps.RootObject UniqueMaps { get; set; } = new();
    public UniqueWeapons.RootObject UniqueWeapons { get; set; } = new();
    public WhiteMaps.RootObject WhiteMaps { get; set; } = new();
    public BlightedMaps.RootObject BlightedMaps { get; set; } = new();
    public BlightRavagedMaps.RootObject BlightRavagedMaps { get; set; } = new();
    public SkillGems.RootObject SkillGems { get; set; } = new();
    public Beasts.RootObject Beasts { get; set; } = new();
    public ClusterJewelNinjaData ClusterJewels { get; set; } = new();
}