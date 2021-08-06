using Ninja_Price.Enums;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared.Enums;
using Ninja_Price.API.PoeNinja.Classes;
using Color = SharpDX.Color;
using RectangleF = SharpDX.RectangleF;
using Vector4 = System.Numerics.Vector4;

namespace Ninja_Price.Main
{
    public partial class Main
    {
        public List<NormalInventoryItem> GetInventoryItems()
        {
            var inventory = GameController.Game.IngameState.IngameUi.InventoryPanel;
            return !inventory.IsVisible ? null : inventory[InventoryIndex.PlayerInventory].VisibleInventoryItems.ToList();
        }

        public Vector4 ToImVector4(Vector4 vector)
        {
            return new Vector4(vector.X, vector.Y, vector.Z, vector.W);
        }

        /// <summary>
        ///     Used for converting SharpDX.Color into string #AARRGGBB.
        /// </summary>
        public static string HexConverter(Color c)
        {
            var rtn = string.Empty;
            try
            {
                rtn = "#" + c.A.ToString("X2") + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
                return rtn;
            }
            catch
            {
                // ignored
            }

            return rtn;
        }

        public void DownloadChaosIcon()
        {
            // check if image exists, if it doesn't, download it.
            var fileName = $"{DirectoryFullName}//images//Chaos_Orb_inventory_icon.png";
            if (File.Exists(fileName)) return;
            Directory.CreateDirectory($"{DirectoryFullName}//images//");
            using (var client = new WebClient())
            {
                client.DownloadFile(new Uri("https://gamepedia.cursecdn.com/pathofexile_gamepedia/9/9c/Chaos_Orb_inventory_icon.png"), fileName);
            }
        }

        public List<CustomItem> FormatItems(List<NormalInventoryItem> itemList)
        {
            return itemList.ToList().Select(inventoryItem => new CustomItem(inventoryItem)).ToList();
        }

        public string GetShardPartent(string shardBaseName)
        {
            var name = "";
            var orbsAndTheirRespectiveShards = new Dictionary<string, string>
            {
                {"Transmutation Shard", "Orb of Transmutation"},
                {"Alteration Shard", "Orb of Alteration"},
                {"Annulment Shard", "Orb of Annulment"},
                {"Exalted Shard", "Exalted Orb"},
                {"Mirror Shard", "Mirror of Kalandra"},
                {"Regal Shard", "Regal Orb"},
                {"Alchemy Shard", "Orb of Alchemy"},
                {"Chaos Shard", "Chaos Orb"},
                {"Ancient Shard", "Ancient Orb"},
                {"Engineer's Shard", "Engineer's Orb"},
                {"Harbinger's Shard", "Harbinger's Orb"},
                {"Horizon Shard", "Orb of Horizons"},
                {"Binding Shard", "Orb of Binding"},
                {"Scroll Fragment", "Scroll of Wisdom"},
                {"Ritual Splinter", "Ritual Vessel"},
                {"Crescent Splinter", "The Maven's Writ" }
            };
            try
            {
                name = orbsAndTheirRespectiveShards[shardBaseName];
            }
            catch
            {
                //LogMessage($"Couldn't find key with value: {shardBaseName}.", 1);
            }

            return name;
        }

        public void GetHoveredItem()
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
                            Hovereditem = new CustomItem(inventoryItemIcon);
                            if (Hovereditem.ItemType != ItemTypes.None)
                                GetValue(Hovereditem);
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

        public void GetValue(CustomItem item)
        {
            try
            {

                item.PriceData.ExaltedPrice = (double)CollectedData.Currency.Lines.Find(x => x.CurrencyTypeName == "Exalted Orb").ChaosEquivalent;
                if(item.BaseName.Contains("Infused Engineer's Orb") || item.BaseName.Contains("Rogue's Marker") || item.BaseName.Contains("Perandus Coin"))
                {
                    item.PriceData.MinChaosValue = 0;
                }
                else
                {
                    switch (item.ItemType) // easier to get data for each item type and handle logic based on that
                    {
                        // TODO: Complete
                        case ItemTypes.Currency:
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
                            if (item.BaseName.Contains("Ritual Splinter")) // Ritual
                            {
                                var shardParent = GetShardPartent(item.BaseName);
                                var shardCurrencySearch = CollectedData.Currency.Lines.Find(x => x.CurrencyTypeName == shardParent);
                                if (shardCurrencySearch != null)
                                {
                                    item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * (double)shardCurrencySearch.ChaosEquivalent / 100;
                                    item.PriceData.ChangeInLast7Days = (double)shardCurrencySearch.ReceiveSparkLine.TotalChange;
                                }
                                break;
                            }
                            switch (item.CurrencyInfo.IsShard)
                            {
                                case false:
                                    var normalCurrencySearch = CollectedData.Currency.Lines.Find(x => x.CurrencyTypeName == item.BaseName);
                                    if (normalCurrencySearch != null)
                                    {
                                        item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * (double)normalCurrencySearch.ChaosEquivalent;
                                        item.PriceData.ChangeInLast7Days = (double)normalCurrencySearch.ReceiveSparkLine.TotalChange;
                                    }

                                    break;
                                case true:
                                    var shardParent = GetShardPartent(item.BaseName);
                                    var shardCurrencySearch = CollectedData.Currency.Lines.Find(x => x.CurrencyTypeName == shardParent);
                                    if (shardCurrencySearch != null)
                                    {
                                        item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * (double)shardCurrencySearch.ChaosEquivalent / (item.CurrencyInfo.MaxStackSize > 0 ? item.CurrencyInfo.MaxStackSize : 20);
                                        item.PriceData.ChangeInLast7Days = (double)shardCurrencySearch.ReceiveSparkLine.TotalChange;
                                    }

                                    break;
                            }
                            break;
                        case ItemTypes.Catalyst:
                            var catalystSearch = CollectedData.Currency.Lines.Find(x => x.CurrencyTypeName == item.BaseName);
                            if (catalystSearch != null)
                            {
                                item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * (double)catalystSearch.ChaosEquivalent;
                                item.PriceData.ChangeInLast7Days = (double)catalystSearch.ReceiveSparkLine.TotalChange;
                            }

                            break;
                        case ItemTypes.DivinationCard:
                            var divinationSearch = CollectedData.DivinationCards.Lines.Find(x => x.Name == item.BaseName);
                            if (divinationSearch != null)
                            {
                                item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * (double)divinationSearch.ChaosValue;
                                item.PriceData.ChangeInLast7Days = (double)divinationSearch.Sparkline.TotalChange;
                            }

                            break;
                        case ItemTypes.Essence:
                            var essenceSearch = CollectedData.Essences.Lines.Find(x => x.Name == item.BaseName);
                            if (essenceSearch != null)
                            {
                                item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * (double)essenceSearch.ChaosValue;
                                item.PriceData.ChangeInLast7Days = (double)essenceSearch.Sparkline.TotalChange;
                            }
                            break;
                        case ItemTypes.Oil:
                            var oilSearch = CollectedData.Oils.Lines.Find(x => x.Name == item.BaseName);
                            if (oilSearch != null)
                            {
                                item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * (double)oilSearch.ChaosValue;
                                item.PriceData.ChangeInLast7Days = (double)oilSearch.Sparkline.TotalChange;
                            }
                            break;
                        case ItemTypes.Fragment:
                            var fragmentSearch = CollectedData.Fragments.Lines.Find(x => x.CurrencyTypeName == item.BaseName);
                            if (fragmentSearch != null)
                            {
                                item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * (double)fragmentSearch.ChaosEquivalent;
                                item.PriceData.ChangeInLast7Days = (double)fragmentSearch.ReceiveSparkLine.TotalChange;
                            }

                            break;
                        case ItemTypes.MavenInvitation:
                            var InvitationSearch = CollectedData.Invitations.Lines.Find(x => x.Name == item.BaseName);
                            if (InvitationSearch != null)
                            {
                                item.PriceData.MinChaosValue = (double)InvitationSearch.ChaosValue;
                                item.PriceData.ChangeInLast7Days = (double)InvitationSearch.Sparkline.TotalChange;
                            }

                            break;
                        case ItemTypes.DeliriumOrbs:
                            var deliriumOrbsSearch = CollectedData.DeliriumOrb.Lines.Find(x => x.Name == item.BaseName);
                            if (deliriumOrbsSearch != null)
                            {
                                item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * (double)deliriumOrbsSearch.ChaosValue;
                                item.PriceData.ChangeInLast7Days = (double)deliriumOrbsSearch.Sparkline.TotalChange;
                            }

                            break;
                        case ItemTypes.Vials:
                            var vialCurrencySearch = CollectedData.Vials.Lines.Find(x => x.Name == item.BaseName);
                            if (vialCurrencySearch != null)
                            {
                                item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * (double)vialCurrencySearch.ChaosValue;
                                item.PriceData.ChangeInLast7Days = (double)vialCurrencySearch.Sparkline.TotalChange;
                            }
                            break;
                        case ItemTypes.Incubator:
                            var incubatorSearch = CollectedData.Incubators.Lines.Find(x => x.Name == item.BaseName);
                            if (incubatorSearch != null)
                            {
                                item.PriceData.MinChaosValue = (double)incubatorSearch.ChaosValue;
                                item.PriceData.ChangeInLast7Days = (double)incubatorSearch.Sparkline.TotalChange;
                            }

                            break;
                        case ItemTypes.Scarab:
                            var scarabSearch = CollectedData.Scarabs.Lines.Find(x => x.Name == item.BaseName);
                            if (scarabSearch != null)
                            {
                                item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * (double)scarabSearch.ChaosValue;
                                item.PriceData.ChangeInLast7Days = (double)scarabSearch.Sparkline.TotalChange;
                            }

                            break;
                        case ItemTypes.Prophecy:
                            var prophecySearch = CollectedData.Prophecies.Lines.Find(x => x.Name == item.Item.Item.GetComponent<Prophecy>().DatProphecy.Name);
                            if (prophecySearch != null)
                            {
                                item.PriceData.MinChaosValue = (double)prophecySearch.ChaosValue;
                                item.PriceData.ChangeInLast7Days = (double)prophecySearch.Sparkline.TotalChange;
                            }

                            break;
                        case ItemTypes.UniqueAccessory:
                            var uniqueAccessorieSearch = CollectedData.UniqueAccessories.Lines.FindAll(x => x.Name == item.UniqueName && !x.DetailsId.Contains("-relic"));
                            if (uniqueAccessorieSearch.Count() == 1)
                            {
                                item.PriceData.MinChaosValue = (double)uniqueAccessorieSearch[0].ChaosValue;
                                item.PriceData.ChangeInLast7Days = (double)uniqueAccessorieSearch[0].Sparkline.TotalChange;
                            }
                            else if (uniqueAccessorieSearch.Count() > 1)
                            {
                                item.PriceData.MinChaosValue = (double)uniqueAccessorieSearch.OrderBy(x => x.ChaosValue).First().ChaosValue;
                                item.PriceData.MaxChaosValue = (double)uniqueAccessorieSearch.OrderBy(x => x.ChaosValue).Last().ChaosValue;
                                item.PriceData.ChangeInLast7Days = 0;
                            }
                            else
                            {
                                item.PriceData.MinChaosValue = 0;
                                item.PriceData.ChangeInLast7Days = 0;
                            }
                            break;
                        case ItemTypes.UniqueArmour:
                            switch (item.LargestLink)
                            {
                                case 0:
                                case 1:
                                case 2:
                                case 3:
                                case 4:
                                    var uniqueArmourSearchLinks04 = CollectedData.UniqueArmours.Lines.FindAll(x => x.Name == item.UniqueName && x.Links != 5 && x.Links != 6 && !x.DetailsId.Contains("-relic"));
                                    if (uniqueArmourSearchLinks04.Count() == 1)
                                    {
                                        item.PriceData.MinChaosValue = (double)uniqueArmourSearchLinks04[0].ChaosValue;
                                        item.PriceData.ChangeInLast7Days = (double)uniqueArmourSearchLinks04[0].Sparkline.TotalChange;
                                    }
                                    else if (uniqueArmourSearchLinks04.Count() > 1)
                                    {
                                        item.PriceData.MinChaosValue = (double)uniqueArmourSearchLinks04.OrderBy(x => x.ChaosValue).First().ChaosValue;
                                        item.PriceData.MaxChaosValue = (double)uniqueArmourSearchLinks04.OrderBy(x => x.ChaosValue).Last().ChaosValue;
                                        item.PriceData.ChangeInLast7Days = 0;
                                    }
                                    else
                                    {
                                        item.PriceData.MinChaosValue = 0;
                                        item.PriceData.ChangeInLast7Days = 0;
                                    }

                                    break;
                                case 5:
                                    var uniqueArmourSearchLinks05 = CollectedData.UniqueArmours.Lines.FindAll(x => x.Name == item.UniqueName && x.Links == 5 && !x.DetailsId.Contains("-relic"));
                                    if (uniqueArmourSearchLinks05.Count() == 1)
                                    {
                                        item.PriceData.MinChaosValue = (double)uniqueArmourSearchLinks05[0].ChaosValue;
                                        item.PriceData.ChangeInLast7Days = (double)uniqueArmourSearchLinks05[0].Sparkline.TotalChange;
                                    }
                                    else if (uniqueArmourSearchLinks05.Count() > 1)
                                    {
                                        item.PriceData.MinChaosValue = (double)uniqueArmourSearchLinks05.OrderBy(x => x.ChaosValue).First().ChaosValue;
                                        item.PriceData.MaxChaosValue = (double)uniqueArmourSearchLinks05.OrderBy(x => x.ChaosValue).Last().ChaosValue;
                                        item.PriceData.ChangeInLast7Days = 0;
                                    }
                                    else
                                    {
                                        item.PriceData.MinChaosValue = 0;
                                        item.PriceData.ChangeInLast7Days = 0;
                                    }

                                    break;
                                case 6:
                                    var uniqueArmourSearchLinks06 = CollectedData.UniqueArmours.Lines.FindAll(x => x.Name == item.UniqueName && x.Links == 6 && !x.DetailsId.Contains("-relic"));
                                    if (uniqueArmourSearchLinks06.Count() == 1)
                                    {
                                        item.PriceData.MinChaosValue = (double)uniqueArmourSearchLinks06[0].ChaosValue;
                                        item.PriceData.ChangeInLast7Days = (double)uniqueArmourSearchLinks06[0].Sparkline.TotalChange;
                                    }
                                    else if (uniqueArmourSearchLinks06.Count() > 1)
                                    {
                                        item.PriceData.MinChaosValue = (double)uniqueArmourSearchLinks06.OrderBy(x => x.ChaosValue).First().ChaosValue;
                                        item.PriceData.MaxChaosValue = (double)uniqueArmourSearchLinks06.OrderBy(x => x.ChaosValue).Last().ChaosValue;
                                        item.PriceData.ChangeInLast7Days = 0;
                                    }
                                    else
                                    {
                                        item.PriceData.MinChaosValue = 0;
                                        item.PriceData.ChangeInLast7Days = 0;
                                    }

                                    break;
                            }

                            break;
                        case ItemTypes.UniqueFlask:
                            var uniqueFlaskSearch = CollectedData.UniqueFlasks.Lines.FindAll(x => x.Name == item.UniqueName && !x.DetailsId.Contains("-relic"));
                            if (uniqueFlaskSearch.Count() == 1)
                            {
                                item.PriceData.MinChaosValue = (double)uniqueFlaskSearch[0].ChaosValue;
                                item.PriceData.ChangeInLast7Days = (double)uniqueFlaskSearch[0].Sparkline.TotalChange;
                            }
                            else if (uniqueFlaskSearch.Count() > 1)
                            {
                                item.PriceData.MinChaosValue = (double)uniqueFlaskSearch.OrderBy(x => x.ChaosValue).First().ChaosValue;
                                item.PriceData.MaxChaosValue = (double)uniqueFlaskSearch.OrderBy(x => x.ChaosValue).Last().ChaosValue;
                                item.PriceData.ChangeInLast7Days = 0;
                            }
                            else
                            {
                                item.PriceData.MinChaosValue = 0;
                                item.PriceData.ChangeInLast7Days = 0;
                            }

                            break;
                        case ItemTypes.UniqueJewel:
                            var uniqueJewelSearch = CollectedData.UniqueJewels.Lines.FindAll(x => x.Name == item.UniqueName && !x.DetailsId.Contains("-relic"));
                            if (uniqueJewelSearch.Count() == 1)
                            {
                                item.PriceData.MinChaosValue = (double)uniqueJewelSearch[0].ChaosValue;
                                item.PriceData.ChangeInLast7Days = (double)uniqueJewelSearch[0].Sparkline.TotalChange;
                            }
                            else if (uniqueJewelSearch.Count() > 1)
                            {
                                item.PriceData.MinChaosValue = (double)uniqueJewelSearch.OrderBy(x => x.ChaosValue).First().ChaosValue;
                                item.PriceData.MaxChaosValue = (double)uniqueJewelSearch.OrderBy(x => x.ChaosValue).Last().ChaosValue;
                                item.PriceData.ChangeInLast7Days = 0;
                            }
                            else
                            {
                                item.PriceData.MinChaosValue = 0;
                                item.PriceData.ChangeInLast7Days = 0;
                            }

                            break;
                        case ItemTypes.UniqueMap:
                            var uniqueMapSearch = CollectedData.UniqueMaps.Lines.FindAll(x => x.BaseType == item.UniqueName && item.MapInfo.MapTier == x.MapTier);
                            if (uniqueMapSearch.Count() == 1)
                            {
                                item.PriceData.MinChaosValue = (double)uniqueMapSearch[0].ChaosValue;
                                item.PriceData.ChangeInLast7Days = (double)uniqueMapSearch[0].Sparkline.TotalChange;
                            }
                            else if (uniqueMapSearch.Count() > 1)
                            {
                                item.PriceData.MinChaosValue = (double)uniqueMapSearch.OrderBy(x => x.ChaosValue).First().ChaosValue;
                                item.PriceData.MaxChaosValue = (double)uniqueMapSearch.OrderBy(x => x.ChaosValue).Last().ChaosValue;
                                item.PriceData.ChangeInLast7Days = 0;
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
                                item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * (double)resonatorSearch.ChaosValue;
                                item.PriceData.ChangeInLast7Days = (double)resonatorSearch.Sparkline.TotalChange;
                            }

                            break;
                        case ItemTypes.Fossil:
                            var fossilSearch = CollectedData.Fossils.Lines.Find(x => x.Name == item.BaseName);
                            if (fossilSearch != null)
                            {
                                item.PriceData.MinChaosValue = item.CurrencyInfo.StackSize * (double)fossilSearch.ChaosValue;
                                item.PriceData.ChangeInLast7Days = (double)fossilSearch.Sparkline.TotalChange;
                            }

                            break;
                        case ItemTypes.UniqueWeapon:
                            switch (item.LargestLink)
                            {
                                case 0:
                                case 1:
                                case 2:
                                case 3:
                                case 4:
                                    var uniqueWeaponSearch04 = CollectedData.UniqueWeapons.Lines.FindAll(x => x.Name == item.UniqueName && x.Links != 5 && x.Links != 6 && !x.DetailsId.Contains("-relic"));
                                    if (uniqueWeaponSearch04.Count() == 1)
                                    {
                                        item.PriceData.MinChaosValue = (double)uniqueWeaponSearch04[0].ChaosValue;
                                        item.PriceData.ChangeInLast7Days = (double)uniqueWeaponSearch04[0].Sparkline.TotalChange;
                                    }
                                    else if (uniqueWeaponSearch04.Count() > 1)
                                    {
                                        item.PriceData.MinChaosValue = (double)uniqueWeaponSearch04.OrderBy(x => x.ChaosValue).First().ChaosValue;
                                        item.PriceData.MaxChaosValue = (double)uniqueWeaponSearch04.OrderBy(x => x.ChaosValue).Last().ChaosValue;
                                        item.PriceData.ChangeInLast7Days = 0;
                                    }
                                    else
                                    {
                                        item.PriceData.MinChaosValue = 0;
                                        item.PriceData.ChangeInLast7Days = 0;
                                    }

                                    break;
                                case 5:
                                    var uniqueWeaponSearch5 = CollectedData.UniqueWeapons.Lines.FindAll(x => x.Name == item.UniqueName && x.Links == 5 && !x.DetailsId.Contains("-relic"));
                                    if (uniqueWeaponSearch5.Count() == 1)
                                    {
                                        item.PriceData.MinChaosValue = (double)uniqueWeaponSearch5[0].ChaosValue;
                                        item.PriceData.ChangeInLast7Days = (double)uniqueWeaponSearch5[0].Sparkline.TotalChange;
                                    }
                                    else if (uniqueWeaponSearch5.Count() > 1)
                                    {
                                        item.PriceData.MinChaosValue = (double)uniqueWeaponSearch5.OrderBy(x => x.ChaosValue).First().ChaosValue;
                                        item.PriceData.MaxChaosValue = (double)uniqueWeaponSearch5.OrderBy(x => x.ChaosValue).Last().ChaosValue;
                                        item.PriceData.ChangeInLast7Days = 0;
                                    }
                                    else
                                    {
                                        item.PriceData.MinChaosValue = 0;
                                        item.PriceData.ChangeInLast7Days = 0;
                                    }

                                    break;
                                case 6:
                                    var uniqueWeaponSearch6 = CollectedData.UniqueWeapons.Lines.FindAll(x => x.Name == item.UniqueName && x.Links == 6 && !x.DetailsId.Contains("-relic"));
                                    if (uniqueWeaponSearch6.Count() == 1)
                                    {
                                        item.PriceData.MinChaosValue = (double)uniqueWeaponSearch6[0].ChaosValue;
                                        item.PriceData.ChangeInLast7Days = (double)uniqueWeaponSearch6[0].Sparkline.TotalChange;
                                    }
                                    else if (uniqueWeaponSearch6.Count() > 1)
                                    {
                                        item.PriceData.MinChaosValue = (double)uniqueWeaponSearch6.OrderBy(x => x.ChaosValue).First().ChaosValue;
                                        item.PriceData.MaxChaosValue = (double)uniqueWeaponSearch6.OrderBy(x => x.ChaosValue).Last().ChaosValue;
                                        item.PriceData.ChangeInLast7Days = 0;
                                    }
                                    else
                                    {
                                        item.PriceData.MinChaosValue = 0;
                                        item.PriceData.ChangeInLast7Days = 0;
                                    }

                                    break;
                            }
                            break;
                        case ItemTypes.Map:
                            switch (item.MapInfo.MapType)
                            {
                                case MapTypes.Blighted:
                                    WhiteMaps.Line normalBlightedMapSearch;

                                    if (Settings.MapVariant.Value)
                                        normalBlightedMapSearch = CollectedData.WhiteMaps.Lines.Find(x => x.BaseType == $"Blighted {item.BaseName}" && item.MapInfo.MapTier == x.MapTier && x.Variant == Settings.LeagueList.Value);
                                    else
                                        normalBlightedMapSearch = CollectedData.WhiteMaps.Lines.Find(x => x.BaseType == $"Blighted {item.BaseName}" && item.MapInfo.MapTier == x.MapTier);

                                        if (normalBlightedMapSearch != null)
                                        {
                                            item.PriceData.MinChaosValue = (double)normalBlightedMapSearch.ChaosValue;
                                            item.PriceData.ChangeInLast7Days = (double)normalBlightedMapSearch.Sparkline.TotalChange;
                                        }

                                    break;
                                case MapTypes.None:
                                    WhiteMaps.Line normalMapSearch;

                                    if (Settings.MapVariant.Value)
                                        normalMapSearch = CollectedData.WhiteMaps.Lines.Find(x => x.BaseType == item.BaseName && item.MapInfo.MapTier == x.MapTier && x.Variant == Settings.LeagueList.Value);
                                    else
                                        normalMapSearch = CollectedData.WhiteMaps.Lines.Find(x => x.BaseType == item.BaseName && item.MapInfo.MapTier == x.MapTier);

                                    if (normalMapSearch != null)
                                    {
                                        item.PriceData.MinChaosValue = (double)normalMapSearch.ChaosValue;
                                        item.PriceData.ChangeInLast7Days = (double)normalMapSearch.Sparkline.TotalChange;
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
                if(item.PriceData.MaxChaosValue == 0)
                {
                    item.PriceData.MaxChaosValue = item.PriceData.MinChaosValue;
                }
            }
        }

        public bool ShouldUpdateValues()
        {
            if (ValueUpdateTimer.ElapsedMilliseconds > Settings.ValueLoopTimerMS)
            {
                ValueUpdateTimer.Restart();
                if (Settings.Debug) { LogMessage($"{GetCurrentMethod()} ValueUpdateTimer.Restart()", 5, Color.DarkGray); }
            }
            else
            {
                return false;
            }
            // TODO: Get inventory items and not just stash tab items, this will be done at a later date
            try
            {
                if (!Settings.VisibleStashValue.Value || !GameController.Game.IngameState.IngameUi.StashElement.IsVisible)
                {
                    if (Settings.Debug) { LogMessage($"{GetCurrentMethod()}.ShouldUpdateValues() Stash is not visible", 5, Color.DarkGray); }
                    return false;
                }

                // Dont continue if the stash page isnt even open
                if (GameController.Game.IngameState.IngameUi.StashElement.VisibleStash.VisibleInventoryItems == null)
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

        public bool ShouldUpdateValuesInventory()
        {
            if (ValueUpdateTimer.ElapsedMilliseconds > Settings.ValueLoopTimerMS)
            {
                ValueUpdateTimer.Restart();
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
                    if (Settings.Debug) { LogMessage($"{GetCurrentMethod()}.ShouldUpdateValues() Inventory is not visible", 5, Color.DarkGray); }
                    return false;
                }

                // Dont continue if the stash page isnt even open
                if (GameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory].VisibleInventoryItems == null)
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


        private double? GetProphecyValues(string ProphName)
        {
            var item = CollectedData.Prophecies.Lines.Find(x => x.Name == ProphName);
            if (item == null) return NotFound;
            var value = item.ChaosValue;
            return value;
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
                    PriceData = new ReleventPriceData
                    {
                        MinChaosValue = enchantSearch.chaosValue, ExaltedPrice = enchantSearch.exaltedValue,
                        ItemType = ItemTypes.None, ChangeInLast7Days = enchantSearch.sparkline.totalChange
                    },
                    BaseName = enchantSearch.name
                };
        }

        private Vector4 ToImVector4(SharpDX.Vector4 vector)
        {
            return new Vector4(vector.X, vector.Y, vector.Z, vector.W);
        }

        /*
         * format is as follows
         * To change color of the string surround hex codes with {} Example: "Uncolored {#AARRGGBB}Colored"
         * having a blank {} will make it go back to default imgui text color, Example: "Uncolored {#AARRGGBB}Colored {}Back to orig color"
         */
        //public void Coloredtext(string TextIn)
        //{
        //    try
        //    {
        //        var accumulatedText = "";
        //        var startColor = ImGui.GetStyle().GetColor(ColorTarget.Text);
        //        var hexCode = "";
        //        var sameLine = false;
        //        var nextColor = startColor;
        //        for (var i = 0; i < TextIn.Length; i++)
        //        {
        //            if (TextIn[i] == '{')
        //            {
        //                var foundBracketStart = TextIn.Substring(i + 1);
        //                for (var j = 0; j < foundBracketStart.Length; j++)
        //                {
        //                    i++;
        //                    if (foundBracketStart[j] == '}')
        //                        break;
        //                    hexCode += foundBracketStart[j];
        //                }

        //                if (sameLine)
        //                    ImGui.SameLine(0f, 0f);
        //                ImGui.Text(accumulatedText);
        //                if (TextIn[i - 1] == '{')
        //                    nextColor = startColor;
        //                accumulatedText = "";
        //                sameLine = true;
        //                if (hexCode != "")
        //                {
        //                    var tempColor = ColorTranslator.FromHtml(hexCode);
        //                    var tempColor2 = new Color(tempColor.R, tempColor.G, tempColor.B, tempColor.A).ToVector4();
        //                    nextColor = new Vector4(tempColor2.X, tempColor2.Y, tempColor2.Z, tempColor2.W);
        //                }

        //                i++;
        //                hexCode = "";
        //            }

        //            accumulatedText += TextIn[i];
        //        }

        //        if (sameLine)
        //            ImGui.SameLine(0f, 0f);
        //        ImGui.Text(accumulatedText);
        //    }
        //    catch (Exception)
        //    {
        //        // This spams all the time even tho nothing seems broken so it can fuck riiiiiiiight off
        //        //LogError("ColorText: Incorrect hex format \n" + e, 15);
        //    }
        //}
    }
}
