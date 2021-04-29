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
                {"Ritual Splinter", "Ritual Vessel"}
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

        /// <summary>
        ///     Draws a plugin image to screen.
        /// </summary>
        /// <param name="rec"></param>
        /// <param name="fileName">The full path including file, fx. C:\\image\\Carl.png</param>
        /// <returns></returns>
        private bool DrawImage(string fileName, RectangleF rec)
        {
            try
            {
                //Graphics.DrawPluginImage(fileName, rec);
            }
            catch
            {
                return false;
            }

            return true;
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
                switch (item.ItemType) // easier to get data for each item type and handle logic based on that
                {
                    // TODO: Complete
                    case ItemTypes.Currency:
                        if (item.BaseName.StartsWith("Chaos "))
                        {
                            switch (item.CurrencyInfo.IsShard)
                            {
                                case false:
                                    item.PriceData.ChaosValue = item.CurrencyInfo.StackSize;
                                    break;
                                case true:
                                    item.PriceData.ChaosValue = item.CurrencyInfo.StackSize / 20.0;
                                    break;
                            }
                            break;
                        }
                        switch (item.CurrencyInfo.IsShard)
                        {
                            case false:
                                var normalCurrencySearch = CollectedData.Currency.Lines.Find(x => x.CurrencyTypeName == item.BaseName);
                                if (normalCurrencySearch != null)
                                {
                                    item.PriceData.ChaosValue = item.CurrencyInfo.StackSize * (double)normalCurrencySearch.ChaosEquivalent;
                                    item.PriceData.ChangeInLast7Days = (double)normalCurrencySearch.ReceiveSparkLine.TotalChange;
                                }

                                break;
                            case true:
                                var shardParent = GetShardPartent(item.BaseName);
                                var shardCurrencySearch = CollectedData.Currency.Lines.Find(x => x.CurrencyTypeName == shardParent);
                                if (shardCurrencySearch != null)
                                {
                                    item.PriceData.ChaosValue = item.CurrencyInfo.StackSize * (double)shardCurrencySearch.ChaosEquivalent / (item.CurrencyInfo.MaxStackSize > 0 ? item.CurrencyInfo.MaxStackSize : 20);
                                    item.PriceData.ChangeInLast7Days = (double)shardCurrencySearch.ReceiveSparkLine.TotalChange;
                                }

                                break;
                        }
                        break;
                    case ItemTypes.Catalyst:
                        var catalystSearch = CollectedData.Currency.Lines.Find(x => x.CurrencyTypeName == item.BaseName);
                        if (catalystSearch != null)
                        {
                            item.PriceData.ChaosValue = item.CurrencyInfo.StackSize * (double)catalystSearch.ChaosEquivalent;
                            item.PriceData.ChangeInLast7Days = (double)catalystSearch.ReceiveSparkLine.TotalChange;
                        }

                        break;
                    case ItemTypes.DivinationCard:
                        var divinationSearch = CollectedData.DivinationCards.Lines.Find(x => x.Name == item.BaseName);
                        if (divinationSearch != null)
                        {
                            item.PriceData.ChaosValue = item.CurrencyInfo.StackSize * (double)divinationSearch.ChaosValue;
                            item.PriceData.ChangeInLast7Days = (double)divinationSearch.Sparkline.TotalChange;
                        }

                        break;
                    case ItemTypes.Essence:
                        var essenceSearch = CollectedData.Essences.Lines.Find(x => x.Name == item.BaseName);
                        if (essenceSearch != null)
                        {
                            item.PriceData.ChaosValue = item.CurrencyInfo.StackSize * (double)essenceSearch.ChaosValue;
                            item.PriceData.ChangeInLast7Days = (double)essenceSearch.Sparkline.TotalChange;
                        }
                        break;
                    case ItemTypes.Oil:
                        var oilSearch = CollectedData.Oils.Lines.Find(x => x.Name == item.BaseName);
                        if (oilSearch != null)
                        {
                            item.PriceData.ChaosValue = item.CurrencyInfo.StackSize * (double)oilSearch.ChaosValue;
                            item.PriceData.ChangeInLast7Days = (double)oilSearch.Sparkline.TotalChange;
                        }
                        break;
                    case ItemTypes.Fragment:
                        var fragmentSearch = CollectedData.Fragments.Lines.Find(x => x.CurrencyTypeName == item.BaseName);
                        if (fragmentSearch != null)
                        {
                            item.PriceData.ChaosValue = item.CurrencyInfo.StackSize * (double)fragmentSearch.ChaosEquivalent;
                            item.PriceData.ChangeInLast7Days = (double)fragmentSearch.ReceiveSparkLine.TotalChange;
                        }

                        break;
                    case ItemTypes.DeliriumOrbs:
                        var deliriumOrbsSearch = CollectedData.DeliriumOrb.Lines.Find(x => x.Name == item.BaseName);
                        if (deliriumOrbsSearch != null)
                        {
                            item.PriceData.ChaosValue = item.CurrencyInfo.StackSize * (double)deliriumOrbsSearch.ChaosValue;
                            item.PriceData.ChangeInLast7Days = (double)deliriumOrbsSearch.Sparkline.TotalChange;
                        }

                        break;
                    case ItemTypes.Incubator:
                        var incubatorSearch = CollectedData.Incubators.Lines.Find(x => x.Name == item.BaseName);
                        if (incubatorSearch != null)
                        {
                            item.PriceData.ChaosValue = (double)incubatorSearch.ChaosValue;
                            item.PriceData.ChangeInLast7Days = (double)incubatorSearch.Sparkline.TotalChange;
                        }

                        break;
                    case ItemTypes.Scarab:
                        var scarabSearch = CollectedData.Scarabs.Lines.Find(x => x.Name == item.BaseName);
                        if (scarabSearch != null)
                        {
                            item.PriceData.ChaosValue = item.CurrencyInfo.StackSize * (double)scarabSearch.ChaosValue;
                            item.PriceData.ChangeInLast7Days = (double)scarabSearch.Sparkline.TotalChange;
                        }

                        break;
                    case ItemTypes.Prophecy:
                        var prophecySearch = CollectedData.Prophecies.Lines.Find(x => x.Name == item.Item.Item.GetComponent<Prophecy>().DatProphecy.Name);
                        if (prophecySearch != null)
                        {
                            item.PriceData.ChaosValue = (double)prophecySearch.ChaosValue;
                            item.PriceData.ChangeInLast7Days = (double)prophecySearch.Sparkline.TotalChange;
                        }

                        break;
                    // TODO: add a quick function to turn known names into the correct name for poe.ninja - See old plugin code
                    case ItemTypes.UniqueAccessory:
                        var uniqueAccessorieSearch = CollectedData.UniqueAccessories.Lines.Find(x => x.Name == item.UniqueName);
                        if (uniqueAccessorieSearch != null)
                        {
                            item.PriceData.ChaosValue = (double)uniqueAccessorieSearch.ChaosValue;
                            item.PriceData.ChangeInLast7Days = (double)uniqueAccessorieSearch.Sparkline.TotalChange;
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
                                var uniqueArmourSearchLinks04 = CollectedData.UniqueArmours.Lines.Find(x => x.Name == item.UniqueName && x.Links <= 4 && x.Links >= 0);
                                if (uniqueArmourSearchLinks04 != null)
                                {
                                    item.PriceData.ChaosValue = (double)uniqueArmourSearchLinks04.ChaosValue;
                                    item.PriceData.ChangeInLast7Days = (double)uniqueArmourSearchLinks04.Sparkline.TotalChange;
                                }

                                break;
                            case 5:
                                var uniqueArmourSearch5 = CollectedData.UniqueArmours.Lines.Find(x => x.Name == item.UniqueName && x.Links == 5);
                                if (uniqueArmourSearch5 != null)
                                {
                                    item.PriceData.ChaosValue = (double)uniqueArmourSearch5.ChaosValue;
                                    item.PriceData.ChangeInLast7Days = (double)uniqueArmourSearch5.Sparkline.TotalChange;
                                }

                                break;
                            case 6:
                                var uniqueArmourSearch6 = CollectedData.UniqueArmours.Lines.Find(x => x.Name == item.UniqueName && x.Links == 6);
                                if (uniqueArmourSearch6 != null)
                                {
                                    item.PriceData.ChaosValue = (double)uniqueArmourSearch6.ChaosValue;
                                    item.PriceData.ChangeInLast7Days = (double)uniqueArmourSearch6.Sparkline.TotalChange;
                                }

                                break;
                        }
                        break;
                    case ItemTypes.UniqueFlask:
                        var uniqueFlaskSearch = CollectedData.UniqueFlasks.Lines.Find(x => x.Name == item.UniqueName);
                        if (uniqueFlaskSearch != null)
                        {
                            item.PriceData.ChaosValue = (double)uniqueFlaskSearch.ChaosValue;
                            item.PriceData.ChangeInLast7Days = (double)uniqueFlaskSearch.Sparkline.TotalChange;
                        }

                        break;
                    case ItemTypes.UniqueJewel:
                        var uniqueJewelSearch = CollectedData.UniqueJewels.Lines.Find(x => x.Name == item.UniqueName);
                        if (uniqueJewelSearch != null)
                        {
                            item.PriceData.ChaosValue = (double)uniqueJewelSearch.ChaosValue;
                            item.PriceData.ChangeInLast7Days = (double)uniqueJewelSearch.Sparkline.TotalChange;
                        }

                        break;
                    case ItemTypes.UniqueMap:
                        var uniqueMapSearch = CollectedData.UniqueMaps.Lines.Find(x => x.BaseType == item.BaseName && item.MapInfo.MapTier == x.MapTier);
                        if (uniqueMapSearch != null)
                        {
                            item.PriceData.ChaosValue = (double)uniqueMapSearch.ChaosValue;
                            item.PriceData.ChangeInLast7Days = (double)uniqueMapSearch.Sparkline.TotalChange;
                        }

                        break;
                    case ItemTypes.Resonator:
                        var resonatorSearch = CollectedData.Resonators.Lines.Find(x => x.Name == item.BaseName);
                        if (resonatorSearch != null)
                        {
                            item.PriceData.ChaosValue = item.CurrencyInfo.StackSize * (double)resonatorSearch.ChaosValue;
                            item.PriceData.ChangeInLast7Days = (double)resonatorSearch.Sparkline.TotalChange;
                        }

                        break;
                    case ItemTypes.HarvestSeeds:
                        var seedLevel = 76; // TODO Diffrentiate between ilvl 1+ and 76+ seeds.
                        var seedSearch = CollectedData.Seeds.Lines.Find(x => x.Name == item.BaseName && x.LevelRequired == seedLevel);
                        if (seedSearch != null)
                        {
                            item.PriceData.ChaosValue = item.CurrencyInfo.StackSize * (double)seedSearch.ChaosValue;
                            item.PriceData.ChangeInLast7Days = (double)seedSearch.Sparkline.TotalChange;
                        }
                        break;
                    case ItemTypes.Fossil:
                        var fossilSearch = CollectedData.Fossils.Lines.Find(x => x.Name == item.BaseName);
                        if (fossilSearch != null)
                        {
                            item.PriceData.ChaosValue = item.CurrencyInfo.StackSize * (double)fossilSearch.ChaosValue;
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
                                var uniqueWeaponSearch04 = CollectedData.UniqueWeapons.Lines.Find(x => x.Name == item.UniqueName && x.Links <= 4 && x.Links >= 0);
                                if (uniqueWeaponSearch04 != null)
                                {
                                    item.PriceData.ChaosValue = (double)uniqueWeaponSearch04.ChaosValue;
                                    item.PriceData.ChangeInLast7Days = (double)uniqueWeaponSearch04.Sparkline.TotalChange;
                                }

                                break;
                            case 5:
                                var uniqueWeaponSearch5 = CollectedData.UniqueWeapons.Lines.Find(x => x.Name == item.UniqueName && x.Links == 5);
                                if (uniqueWeaponSearch5 != null)
                                {
                                    item.PriceData.ChaosValue = (double)uniqueWeaponSearch5.ChaosValue;
                                    item.PriceData.ChangeInLast7Days = (double)uniqueWeaponSearch5.Sparkline.TotalChange;
                                }

                                break;
                            case 6:
                                var uniqueWeaponSearch6 = CollectedData.UniqueWeapons.Lines.Find(x => x.Name == item.UniqueName && x.Links == 6);
                                if (uniqueWeaponSearch6 != null)
                                {
                                    item.PriceData.ChaosValue = (double)uniqueWeaponSearch6.ChaosValue;
                                    item.PriceData.ChangeInLast7Days = (double)uniqueWeaponSearch6.Sparkline.TotalChange;
                                }

                                break;
                        }
                        break;
                    case ItemTypes.NormalMap:
                        // TODO: Deal with old maps, this is literally the last thing i will do as it has next to no gain for having this information
                        switch (item.MapInfo.MapType)
                        {
                            case MapTypes.Blighted:
                                var normalBlightedMapSearch = CollectedData.WhiteMaps.Lines.Find(x => x.BaseType == $"Blighted {item.BaseName}" && item.MapInfo.MapTier == x.MapTier && x.Variant == Settings.LeagueList.Value);
                                if (normalBlightedMapSearch != null)
                                {
                                    item.PriceData.ChaosValue = (double)normalBlightedMapSearch.ChaosValue;
                                    item.PriceData.ChangeInLast7Days = (double)normalBlightedMapSearch.Sparkline.TotalChange;
                                }

                                break;
                        // TODO: Poe.Ninja can not recognized Shaped and Elder maps, so i think no price - is better than count price equal no influence map.
                                /*
                            case MapTypes.Shaped:
                                var normalSharpedMapSearch = CollectedData.WhiteMaps.Lines.Find(x => x.BaseType == $"Shaped {item.BaseName}" && item.MapInfo.MapTier == x.MapTier && x.Variant == "Harvest");
                                if (normalSharpedMapSearch != null)
                                {
                                    item.PriceData.ChaosValue = (double)normalSharpedMapSearch.ChaosValue;
                                    item.PriceData.ChangeInLast7Days = (double)normalSharpedMapSearch.Sparkline.TotalChange;
                                }

                                break;
                                */
                            case  MapTypes.None:
                                var normalMapSearch = CollectedData.WhiteMaps.Lines.Find(x => x.BaseType == item.BaseName && item.MapInfo.MapTier == x.MapTier && x.Variant == Settings.LeagueList.Value);
                                if (normalMapSearch != null)
                                {
                                    item.PriceData.ChaosValue = (double)normalMapSearch.ChaosValue;
                                    item.PriceData.ChangeInLast7Days = (double)normalMapSearch.Sparkline.TotalChange;
                                }

                                break;
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                if (Settings.Debug) { LogMessage($"{GetCurrentMethod()}.GetValue() Error that i dont understand", 5, Color.Red); }
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
                    if (Settings.Debug) { LogMessage($"{GetCurrentMethod()}.ShouldUpdateValues() Stash is not visable", 5, Color.DarkGray); }
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
                if (Settings.Debug) LogMessage($"{GetCurrentMethod()}.ShouldUpdateValues() Error that i need to fucking fix", 5, Color.DarkGray);
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
                    if (Settings.Debug) { LogMessage($"{GetCurrentMethod()}.ShouldUpdateValues() Inventory is not visable", 5, Color.DarkGray); }
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
                if (Settings.Debug) LogMessage($"{GetCurrentMethod()}.ShouldUpdateValues() Error that i need to fucking fix", 5, Color.DarkGray);
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