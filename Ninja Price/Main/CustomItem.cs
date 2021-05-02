using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared.Enums;
using Ninja_Price.Enums;

namespace Ninja_Price.Main
{

    public class CustomItem
    {
        public static Main Core;
        public string BaseName;
        public string ClassName;
        public int Height;
        public bool IsElder;
        public bool IsIdentified;
        public bool IsRgb;
        public bool IsShaper;
        public bool IsWeapon;
        public bool IsHovered;
        public NormalInventoryItem Item;
        public int ItemLevel;
        public int LargestLink;
        public string Path;
        public string ProphecyName;
        public int Quality;
        public ItemRarity Rarity;
        public int Sockets;
        public string UniqueName;
        public int Width;
        public ItemTypes ItemType;
        public MapData MapInfo { get; set; } =  new MapData();
        public CurrencyData CurrencyInfo { get; set; } =  new CurrencyData();
        public Main.ReleventPriceData PriceData { get; set; } = new Main.ReleventPriceData();

        public static void InitCustomItem(Main _core)
        {
            Core = _core;
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
            public int StackSize;
            public int MaxStackSize = 0;
        }

        public CustomItem()
        {
        }

        public CustomItem(NormalInventoryItem item)
        {
            try
            {
                if (item != null && item.Address != 0)
                    Item = item;
                Path = item.Item.Path;
                var baseItemType = Core.GameController.Files.BaseItemTypes.Translate(item.Item.Path);
                ClassName = baseItemType.ClassName;
                BaseName = baseItemType.BaseName;
                Width = baseItemType.Width;
                Height = baseItemType.Height;
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
                if (item.Item.HasComponent<Quality>())
                {
                    var quality = item.Item.GetComponent<Quality>();
                    Quality = quality.ItemQuality;
                }

                if (item.Item.HasComponent<Base>())
                {
                    var @base = item.Item.GetComponent<Base>();
                    IsElder = @base.isElder;
                    IsShaper = @base.isShaper;

                    if (@base.Name == "Prophecy")
                    {
                        var prophParse = item.Item.GetComponent<Prophecy>();
                        ProphecyName = prophParse.DatProphecy.Name.ToLower();
                        ProphecyName = ProphecyName.Replace(" ", "");
                        ProphecyName = ProphecyName.Replace(",", "");
                        ProphecyName = ProphecyName.Replace("'", "");
                    }
                }

                if (item.Item.HasComponent<Mods>())
                {
                    var mods = item.Item.GetComponent<Mods>();
                    Rarity = mods.ItemRarity;
                    IsIdentified = mods.Identified;
                    ItemLevel = mods.ItemLevel;
                    UniqueName = mods.UniqueName;
                }

                if (item.Item.HasComponent<Sockets>())
                {
                    try
                    {
                        var sockets = item.Item.GetComponent<Sockets>();
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

                MapInfo.MapTier = item.Item.HasComponent<Map>() ? item.Item.GetComponent<Map>().Tier : 0;
                MapInfo.IsMap = MapInfo.MapTier > 0;

                if (Rarity != ItemRarity.Unique && MapInfo.IsMap)
                {
                    MapInfo.MapType = MapTypes.None;

                    foreach (var itemList in item.Item.GetComponent<Mods>().ItemMods)
                    {
                        if (itemList.RawName.Contains("MapShaped"))
                        {
                            MapInfo.MapType = MapTypes.None;
                            break;
                        }
                        else if (itemList.RawName.Contains("MapElder"))
                        {
                            MapInfo.MapType = MapTypes.None;
                            break;
                        }
                        else if (itemList.RawName.Contains("InfectedMap"))
                        {
                            MapInfo.MapType = MapTypes.Blighted;
                            break;
                        }
                    }
                }

                if (item.Item.HasComponent<Stack>())
                {
                    CurrencyInfo.StackSize = item.Item.GetComponent<Stack>().Size;
                    CurrencyInfo.MaxStackSize = item.Item.GetComponent<Stack>().Info.MaxStackSize;
                    if (BaseName.EndsWith(" Shard") || BaseName.EndsWith(" Fragment") ||
                        BaseName.EndsWith("Ritual Splinter"))
                        CurrencyInfo.IsShard = true;
                }


                IsHovered = Core.GameController.Game.IngameState.UIHover.AsObject<NormalInventoryItem>().Address ==
                            item.Address;

                // sort items into types to use correct json data later from poe.ninja
                // This might need tweaking since if this catches anything other than currency.
                if (ClassName == "StackableCurrency" && !BaseName.StartsWith("Simulacrum") &&
                    !BaseName.EndsWith("Delirium Orb") && !BaseName.Contains("Essence") && !BaseName.EndsWith(" Oil") &&
                    !BaseName.Contains("Remnant of") && !BaseName.Contains("Timeless ") && BaseName != "Prophecy" &&
                    ClassName != "MapFragment" && !BaseName.EndsWith(" Fossil") && !BaseName.StartsWith("Splinter of ") && ClassName != "Incubator" &&
                    !BaseName.EndsWith(
                        " Catalyst") /*&& !BaseName.Contains("Shard") && BaseName != "Chaos Orb" && !BaseName.Contains("Wisdom")*/
                )
                {
                    ItemType = ItemTypes.Currency;
                }
                else if (BaseName.EndsWith(" Catalyst"))
                {
                    ItemType = ItemTypes.Catalyst;
                }
                else if (BaseName.EndsWith(" Oil"))
                {
                    ItemType = ItemTypes.Oil;
                }
                else if (Path.Contains("Metadata/Items/DivinationCards"))
                {
                    ItemType = ItemTypes.DivinationCard;
                }
                else if (BaseName.Contains("Essence") || BaseName.Contains("Remnant of"))
                {
                    ItemType = ItemTypes.Essence;
                }
                else if (((ClassName == "MapFragment" || BaseName.Contains("Timeless ") ||
                          BaseName.StartsWith("Simulacrum")) && !BaseName.EndsWith(" Scarab")) || ClassName == "StackableCurrency" && BaseName.StartsWith("Splinter of "))
                {
                    ItemType = ItemTypes.Fragment;
                }
                else if (ClassName == "MapFragment" && BaseName.EndsWith(" Scarab"))
                {
                    ItemType = ItemTypes.Scarab;
                }
                else if (BaseName == "Prophecy")
                {
                    ItemType = ItemTypes.Prophecy;
                }
                else if (MapInfo.IsMap && Rarity != ItemRarity.Unique)
                {
                    ItemType = ItemTypes.NormalMap;
                }
                else if (BaseName.EndsWith(" Fossil"))
                {
                    ItemType = ItemTypes.Fossil;
                }
                else if (ClassName == "DelveStackableSocketableCurrency")
                {
                    ItemType = ItemTypes.Resonator;
                }
                else if (ClassName == "Incubator")
                {
                    ItemType = ItemTypes.Incubator;
                }
                else if (BaseName.EndsWith("Delirium Orb"))
                {
                    ItemType = ItemTypes.DeliriumOrbs;
                }
                else if (ClassName == "HarvestSeed")
                {
                    ItemType = ItemTypes.HarvestSeeds;
                }
                else
                    switch (Rarity) // Unique information
                    {
                        case ItemRarity.Unique when IsIdentified && IsIdentified && ClassName == "Amulet" ||
                                                    ClassName == "Ring" || ClassName == "Belt":
                            ItemType = ItemTypes.UniqueAccessory;
                            break;
                        case ItemRarity.Unique
                            when IsIdentified && item.Item.HasComponent<Armour>() || ClassName == "Quiver":
                            ItemType = ItemTypes.UniqueArmour;
                            break;
                        case ItemRarity.Unique when IsIdentified && item.Item.HasComponent<Flask>():
                            ItemType = ItemTypes.UniqueFlask;
                            break;
                        case ItemRarity.Unique when IsIdentified && ClassName.Equals("Jewel"):
                            ItemType = ItemTypes.UniqueJewel;
                            break;
                        case ItemRarity.Unique when MapInfo.IsMap:
                            ItemType = ItemTypes.UniqueMap;
                            break;
                        case ItemRarity.Unique when IsIdentified && item.Item.HasComponent<Weapon>():
                            ItemType = ItemTypes.UniqueWeapon;
                            break;
                    }
            }
            catch (Exception exception)
            {
                if (Core.Settings.Debug)
                    Core.LogError("Ninja Pricer.CustomItem Error:" + Environment.NewLine + exception.ToString());
            }

        }
    }
}