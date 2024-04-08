using Ninja_Price.API.PoeNinja;

namespace Ninja_Price.Main;

public class CollectiveApiData
{
    public Currency.RootObject Currency { get; set; } = new();
    public DivinationCards.RootObject DivinationCards { get; set; } = new();
    public Essences.RootObject Essences { get; set; } = new();
    public Fragments.RootObject Fragments { get; set; } = new();
    public UniqueAccessories.RootObject UniqueAccessories { get; set; } = new();
    public UniqueArmours.RootObject UniqueArmours { get; set; } = new();
    public UniqueFlasks.RootObject UniqueFlasks { get; set; } = new();
    public UniqueJewels.RootObject UniqueJewels { get; set; } = new();
    public UniqueMaps.RootObject UniqueMaps { get; set; } = new();
    public UniqueWeapons.RootObject UniqueWeapons { get; set; } = new();
    public WhiteMaps.RootObject WhiteMaps { get; set; } = new();
    public Resonators.RootObject Resonators { get; set; } = new();
    public Fossils.RootObject Fossils { get; set; } = new();
    public Scarab.RootObject Scarabs { get; set; } = new();
    public Oils.RootObject Oils { get; set; } = new();
    public Incubators.RootObject Incubators { get; set; } = new();
    public DeliriumOrb.RootObject DeliriumOrb { get; set; } = new();
    public Vials.RootObject Vials { get; set; } = new();
    public Invitations.RootObject Invitations { get; set; } = new();
    public Artifacts.RootObject Artifacts { get; set; } = new();
    public SkillGems.RootObject SkillGems { get; set; } = new();
    public ClusterJewelNinjaData ClusterJewels { get; set; } = new();
    public Tattoos.RootObject Tattoos { get; set; } = new();
    public Omens.RootObject Omens { get; set; } = new();
    public Coffins.RootObject Coffins { get; set; } = new();
}