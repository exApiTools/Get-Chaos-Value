using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.FilesInMemory;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using Ninja_Price.Enums;

namespace Ninja_Price.Main;

public record ClusterJewelData(string Name, int PassiveCount);

public class CustomItem
{
    //because inlining is weird
    private static readonly GameStat ClusterJewelPassiveCountStat = Enum.Parse<GameStat>(nameof(GameStat.LocalJewelExpansionPassiveNodeCount));

    public static Main Core;
    public string BaseName;
    public string UniqueName;
    public readonly string ClassName;
    public readonly bool IsElder;
    public readonly bool IsIdentified;
    public readonly bool IsCorrupted;
    public readonly bool IsRgb;
    public readonly bool IsShaper;
    public readonly bool IsWeapon;
    public readonly bool IsHovered;
    public readonly Element Element;
    public readonly Entity Entity;
    public int ItemLevel;
    public readonly int LargestLink = 0;
    public readonly string Path;
    public readonly int Quality;
    public readonly int GemLevel;
    public string GemName;
    public readonly ItemRarity Rarity;
    public readonly int Sockets;
    public readonly List<string> UniqueNameCandidates;
    public ItemTypes ItemType;
    public readonly ClusterJewelData ClusterJewelData;
    public readonly List<string> EnchantedStats;
    public NecropolisCraftingMod NecropolisMod;
    public MapData MapInfo { get; set; } =  new MapData();
    public CurrencyData CurrencyInfo { get; set; } =  new CurrencyData();
    public Main.RelevantPriceData PriceData { get; set; } = new Main.RelevantPriceData();

    public static void InitCustomItem(Main core)
    {
        Core = core;
    }

    public class MapData
    {
        public bool IsMap;
        public MapTypes MapType;
        public int MapTier;
    }
    public class CurrencyData
    {
        public bool IsShard;
        public int StackSize = 1;
        public int MaxStackSize = 0;
    }

    public CustomItem()
    {
    }

    public CustomItem(NormalInventoryItem item) : this(item.Item, item)
    {
    }

    public CustomItem(Entity itemEntity, Element element)
    {
        try
        {
            if (element != null && element.Address != 0)
                Element = element;

            Path = itemEntity.Path;
            Entity = itemEntity;
            var baseItemType = Core.GameController.Files.BaseItemTypes.Translate(itemEntity.Path);
            ClassName = baseItemType?.ClassName ?? string.Empty;
            BaseName = baseItemType?.BaseName ?? string.Empty;
            var weaponClass = new List<string>
            {
                "One Hand Mace",
                "Two Hand Mace",
                "One Hand Axe",
                "Two Hand Axe",
                "One Hand Sword",
                "Two Hand Sword",
                "Thrusting One Hand Sword",
                "Bow",
                "Claw",
                "Dagger",
                "Sceptre",
                "Staff",
                "Wand"
            };
            if (itemEntity.TryGetComponent<Quality>(out var quality))
            {
                Quality = quality.ItemQuality;
            }

            if (itemEntity.TryGetComponent<SkillGem>(out var skillGem))
            {
                GemLevel = skillGem.Level;
                GemName = skillGem.GemEffect?.Name;
            }

            if (itemEntity.TryGetComponent<Base>(out var @base))
            {
                IsElder = @base.isElder;
                IsShaper = @base.isShaper;
                IsCorrupted = @base.isCorrupted;
                ItemLevel = @base.CurrencyItemLevel;
            }

            if (itemEntity.TryGetComponent<NecropolisCorpse>(out var corpse))
            {
                ItemLevel = corpse.Level;
                NecropolisMod = corpse.CraftingMod;
            }

            if (itemEntity.TryGetComponent<Mods>(out var mods))
            {
                Rarity = mods.ItemRarity;
                IsIdentified = mods.Identified;
                ItemLevel = mods.ItemLevel;
                EnchantedStats = mods.EnchantedStats;
                UniqueName = mods.UniqueName?.Replace('\x2019', '\x27');
                if (!IsIdentified && Rarity == ItemRarity.Unique)
                {
                    var artPath = itemEntity.GetComponent<RenderItem>()?.ResourcePath;
                    if (artPath != null)
                    {
                        UniqueNameCandidates = (Core.UniqueArtMapping.GetValueOrDefault(artPath) ?? Enumerable.Empty<string>())
                            .Where(x => !x.StartsWith("Replica ") || x.StartsWith("Replica Dragonfang's Flight"))
                            .ToList();
                    }
                }
            }

            UniqueNameCandidates ??= [];

            if (itemEntity.TryGetComponent<Sockets>(out var sockets))
            {
                try
                {
                    IsRgb = sockets.IsRGB;
                    Sockets = sockets.NumberOfSockets;
                    LargestLink = sockets.LargestLinkSize;
                }
                catch
                {
                }
            }

            if (weaponClass.Any(ClassName.Equals))
                IsWeapon = true;

            MapInfo.MapTier = itemEntity.TryGetComponent<Map>(out var map) ? map.Tier : 0;
            MapInfo.IsMap = MapInfo.MapTier > 0;

            if (Rarity != ItemRarity.Unique && MapInfo.IsMap)
            {
                MapInfo.MapType = MapTypes.None;

                foreach (var itemList in itemEntity.GetComponent<Mods>().ItemMods)
                {
                    if (itemList.RawName.Contains("Shaped "))
                    {
                        MapInfo.MapType = MapTypes.Shaped;
                        break;
                    }
                    else if (itemList.RawName.Contains("Elder "))
                    {
                        MapInfo.MapType = MapTypes.Elder;
                        break;
                    }
                    else if (itemList.RawName.Contains("Blighted "))
                    {
                        MapInfo.MapType = MapTypes.Blighted;
                        break;
                    }
                }
            }

            if (itemEntity.TryGetComponent<Stack>(out var stack))
            {
                CurrencyInfo.StackSize = stack.Size;
                CurrencyInfo.MaxStackSize = stack.Info.MaxStackSize;
                if (BaseName.EndsWith(" Shard") || 
                    BaseName.EndsWith(" Fragment") ||
                    BaseName.EndsWith(" Splinter") ||
                    BaseName.StartsWith("Splinter of"))
                    CurrencyInfo.IsShard = true;
            }


            IsHovered = Core.GameController.Game.IngameState.UIHover.AsObject<NormalInventoryItem>().Address == Element?.Address;

            // sort items into types to use correct json data later from poe.ninja
            // This might need tweaking since if this catches anything other than currency.
            if (BaseName == "Filled Coffin")
            {
                ItemType = ItemTypes.Coffin;
            }
            else if (ClassName == "NecropolisPack")
            {
                ItemType = ItemTypes.Allflame;
            }
            else if (ClassName == "MiscMapItem" && Path.StartsWith("Metadata/Items/MapFragments/Primordial/", StringComparison.Ordinal) &&
                     Path.EndsWith("Key", StringComparison.Ordinal))
            {
                ItemType = ItemTypes.Invitation;
            }
            else if (ClassName == "MapFragment" && Path.StartsWith("Metadata/Items/Scarabs/"))
            {
                ItemType = ItemTypes.Scarab;
            }
            else if (ClassName == "StackableCurrency" && 
                !BaseName.StartsWith("Crescent Splinter") &&
                !BaseName.StartsWith("Simulacrum") &&
                !BaseName.EndsWith("Delirium Orb") &&
                !BaseName.Contains("Essence") &&
                !BaseName.EndsWith(" Oil") &&
                !BaseName.Contains("Tattoo ") &&
                !BaseName.StartsWith("Omen ") &&
                !BaseName.EndsWith("Artifact") &&
                !BaseName.Contains("Astragali") &&
                !BaseName.Contains("Burial Medallion") &&
                !BaseName.Contains("Scrap Metal") &&
                !BaseName.Contains("Exotic Coinage") &&
                !BaseName.Contains("Remnant of") &&
                !BaseName.Contains("Timeless ") &&
                BaseName != "Prophecy" &&
                BaseName != "Charged Compass" &&
                ClassName != "MapFragment" &&
                !BaseName.EndsWith(" Fossil") &&
                !BaseName.StartsWith("Splinter of ") &&
                ClassName != "Incubator" &&
                !BaseName.EndsWith(" Catalyst") &&
                BaseName != "Valdo's Puzzle Box")
            {
                ItemType = ItemTypes.Currency;
            }
            else if (BaseName.EndsWith(" Catalyst"))
            {
                ItemType = ItemTypes.Catalyst;
            }
            else if (BaseName.Contains("Astragali") || BaseName.Contains("Burial Medallion") || BaseName.Contains("Scrap Metal") || BaseName.Contains("Exotic Coinage"))     
            {
                ItemType = ItemTypes.Artifact;
            }
            else if (BaseName.EndsWith(" Oil"))
            {
                ItemType = ItemTypes.Oil;
            }
            else if (BaseName.Contains("Tattoo "))
            {
                ItemType = ItemTypes.Tattoo;
            }
            else if (BaseName.StartsWith("Omen "))
            {
                ItemType = ItemTypes.Omen;
            }
            else if (Path.Contains("Metadata/Items/DivinationCards"))
            {
                ItemType = ItemTypes.DivinationCard;
            }
            else if (BaseName.Contains("Essence") || BaseName.Contains("Remnant of"))
            {
                ItemType = ItemTypes.Essence;
            }
            else if (ClassName == "MapFragment" || BaseName.Contains("Timeless ") || BaseName.StartsWith("Simulacrum") ||
                     ClassName == "StackableCurrency" && BaseName.StartsWith("Splinter of ") ||
                     BaseName.StartsWith("Crescent Splinter") ||
                     ClassName == "VaultKey" ||
                     BaseName == "Valdo's Puzzle Box")
            {
                ItemType = ItemTypes.Fragment;
            }
            else if (MapInfo.IsMap && Rarity != ItemRarity.Unique)
            {
                ItemType = ItemTypes.Map;
            }
            else if (BaseName.EndsWith(" Fossil"))
            {
                ItemType = ItemTypes.Fossil;
            }
            else if (ClassName == "DelveStackableSocketableCurrency")
            {
                ItemType = ItemTypes.Resonator;
            }
            else if (ClassName is "Incubator" or "IncubatorStackable")
            {
                ItemType = ItemTypes.Incubator;
            }
            else if (BaseName.EndsWith("Delirium Orb"))
            {
                ItemType = ItemTypes.DeliriumOrbs;
            }
            else if (BaseName.StartsWith("Vial "))
            {
                ItemType = ItemTypes.Vials;
            }
            else if (ClassName is "Support Skill Gem" or "Active Skill Gem")
            {
                ItemType = ItemTypes.SkillGem;
            }
            else if (Rarity != ItemRarity.Unique && BaseName is
                         "Large Cluster Jewel" or
                         "Medium Cluster Jewel" or
                         "Small Cluster Jewel")
            {
                ItemType = ItemTypes.ClusterJewel;
                var passiveCount = itemEntity.GetComponent<LocalStats>()?.StatDictionary.GetValueOrDefault(ClusterJewelPassiveCountStat) ?? 0;
                const string namePrefix = "Added Small Passive Skills grant: ";
                var name = itemEntity.GetComponent<Mods>()?.EnchantedStats.FirstOrDefault(x => x.StartsWith(namePrefix))?.Replace(namePrefix, null)?.Replace("\n", ", ");
                ClusterJewelData = new ClusterJewelData(name, passiveCount);
            }
            else
            {
                switch (Rarity) // Unique information
                {
                    case ItemRarity.Unique or ItemRarity.Normal when ClassName is "Amulet" or "Ring" or "Belt":
                        ItemType = ItemTypes.UniqueAccessory;
                        break;
                    case ItemRarity.Unique or ItemRarity.Normal when itemEntity.HasComponent<Armour>() || ClassName == "Quiver":
                        ItemType = ItemTypes.UniqueArmour;
                        break;
                    case ItemRarity.Unique when itemEntity.HasComponent<Flask>():
                        ItemType = ItemTypes.UniqueFlask;
                        break;
                    case ItemRarity.Unique or ItemRarity.Normal when ClassName == "Jewel":
                        ItemType = ItemTypes.UniqueJewel;
                        break;
                    case ItemRarity.Unique when MapInfo.IsMap:
                        ItemType = ItemTypes.UniqueMap;
                        break;
                    case ItemRarity.Unique or ItemRarity.Normal when itemEntity.HasComponent<Weapon>():
                        ItemType = ItemTypes.UniqueWeapon;
                        break;
                }
            }
        }
        catch (Exception exception)
        {
            if (Core.Settings.Debug)
                Core.LogError($"Ninja Pricer.CustomItem Error:\n{exception}");
        }

    }

    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(UniqueName) ? BaseName : UniqueName;
    }
}