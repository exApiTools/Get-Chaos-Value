using ExileCore;
using ExileCore.PoEMemory.FilesInMemory;
using ExileCore.PoEMemory.MemoryObjects;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using Ninja_Price.API.PoeNinja;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Ninja_Price.Main;

public partial class Main : BaseSettingsPlugin<Settings.Settings>
{
    private string NinjaDirectory;
    private CollectiveApiData CollectedData;
    private const string CustomUniqueArtMappingPath = "uniqueArtMapping.json";
    private const string DefaultUniqueArtMappingPath = "uniqueArtMapping.default.json";
    private const string ItemDataCSVPath = "tainted_mythic_orb_outcomes.csv";
    private int _updating;
    public Dictionary<string, List<string>> UniqueArtMapping = new Dictionary<string, List<string>>();
    public List<ItemData> csvItemData;
    private readonly ConcurrentDictionary<NecropolisCraftingMod, string> _necropolisModText = [];

    [GeneratedRegex("<[^>]*>{{(?<data>[^}]*)}}")]
    private static partial Regex StripTagsRegex();

    public override bool Initialise()
    {
        Name = "Ninja Price";
        NinjaDirectory = Path.Join(DirectoryFullName, "NinjaData");
        Directory.CreateDirectory(NinjaDirectory);

        UpdateLeagueList();
        StartDataReload(Settings.DataSourceSettings.League.Value, false);

        Settings.DataSourceSettings.ReloadPrices.OnPressed += () => StartDataReload(Settings.DataSourceSettings.League.Value, true);
        Settings.UniqueIdentificationSettings.RebuildUniqueItemArtMappingBackup.OnPressed += () =>
        {
            var mapping = GetGameFileUniqueArtMapping();
            if (mapping != null)
            {
                File.WriteAllText(Path.Join(DirectoryFullName, CustomUniqueArtMappingPath), JsonConvert.SerializeObject(mapping, Formatting.Indented));
            }
        };
        Settings.UniqueIdentificationSettings.IgnoreGameUniqueArtMapping.OnValueChanged += (_, _) =>
        {
            UniqueArtMapping = GetUniqueArtMapping();
        };
        Settings.DataSourceSettings.SyncCurrentLeague.OnValueChanged += (_, _) => SyncCurrentLeague();
        CustomItem.InitCustomItem(this);

        csvItemData = ReadItemDataCsvFile();
        //   foreach (var item in items)
        //{
        //   LogMessage($"Base Item: {item.BaseItem}, Chance: {item.Chance}, Average Orbs: {item.AverageOrbs}, Destruction Chance: {item.DestructionChance}, Base Item Disambiguation: {item.BaseItemDisambiguation}, Tier: {item.Tier}, Name: {item.Name}, Disambiguation: {item.Disambiguation}, Min ILvl: {item.MinILvl}, Weight: {item.Weight}, PoeWikiLink: {item.PoeWikiLink}",5);
        //}

        GameController.PluginBridge.SaveMethod("NinjaPrice.GetValue", (Entity e) =>
        {
            var customItem = new CustomItem(e, null);
            GetValue(customItem);
            return customItem.PriceData.MinChaosValue;
        });
        return true;
    }

    public override void AreaChange(AreaInstance area)
    {
        UniqueArtMapping = GetUniqueArtMapping();
        _necropolisModText.Clear();
        SyncCurrentLeague();
    }

    private void SyncCurrentLeague()
    {
        if (Settings.DataSourceSettings.SyncCurrentLeague)
        {
            var playerLeague = PlayerLeague;
            if (playerLeague != null)
            {
                if (!Settings.DataSourceSettings.League.Values.Contains(playerLeague))
                {
                    Settings.DataSourceSettings.League.Values.Add(playerLeague);
                }

                if (Settings.DataSourceSettings.League.Value != playerLeague)
                {
                    Settings.DataSourceSettings.League.Value = playerLeague;
                    StartDataReload(Settings.DataSourceSettings.League.Value, false);
                }
            }
        }
    }

    private Dictionary<string, List<string>> GetUniqueArtMapping()
    {
        Dictionary<string, List<string>> mapping = null;
        if (!Settings.UniqueIdentificationSettings.IgnoreGameUniqueArtMapping &&
            GameController.Files.UniqueItemDescriptions.EntriesList.Count != 0 &&
            GameController.Files.ItemVisualIdentities.EntriesList.Count != 0)
        {
            mapping = GetGameFileUniqueArtMapping();
        }

        var customFilePath = Path.Join(DirectoryFullName, CustomUniqueArtMappingPath);
        if (File.Exists(customFilePath))
        {
            try
            {
                mapping ??= JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(File.ReadAllText(customFilePath));
            }
            catch (Exception ex)
            {
                LogError($"Unable to load custom art mapping: {ex}");
            }
        }

        mapping ??= GetEmbeddedUniqueArtMapping();
        mapping ??= [];
        return mapping.ToDictionary(x => x.Key, x => x.Value.Select(str => str.Replace('’', '\'')).ToList());
    }

    private Dictionary<string, List<string>> GetEmbeddedUniqueArtMapping()
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(DefaultUniqueArtMappingPath);
            if (stream == null)
            {
                if (Settings.EnableDebugLogging)
                {
                    LogMessage($"Embedded stream {DefaultUniqueArtMappingPath} is missing");
                }

                return null;
            }

            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();
            return JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(content);
        }
        catch (Exception ex)
        {
            LogError($"Unable to load embedded art mapping: {ex}");
            return null;
        }
    }

    private Dictionary<string, List<string>> GetGameFileUniqueArtMapping()
    {
        if (GameController.Files.UniqueItemDescriptions.EntriesList.Count == 0)
        {
            GameController.Files.LoadFiles();
        }

        return GameController.Files.ItemVisualIdentities.EntriesList.Where(x => x.ArtPath != null)
            .GroupJoin(GameController.Files.UniqueItemDescriptions.EntriesList.Where(x => x.ItemVisualIdentity != null),
                x => x,
                x => x.ItemVisualIdentity, (ivi, descriptions) => (ivi.ArtPath, descriptions: descriptions.ToList()))
            .GroupBy(x => x.ArtPath, x => x.descriptions)
            .Select(x => (x.Key, Names: x
                .SelectMany(items => items)
                .Select(item => item.UniqueName?.Text)
                .Where(name => name != null)
                .Distinct()
                .ToList()))
            .Where(x => x.Names.Any())
            .ToDictionary(x => x.Key, x => x.Names);
    }

    private void UpdateLeagueList()
    {
        var leagueList = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        var playerLeague = PlayerLeague;
        if (playerLeague != null)
        {
            leagueList.Add(playerLeague);
        }

        try
        {
            var leagueListFromUrl = Utils.DownloadFromUrl("https://poe.ninja/api/data/getindexstate").Result;
            var leagueData = JsonConvert.DeserializeObject<NinjaLeagueListRootObject>(leagueListFromUrl);
            leagueList.UnionWith(leagueData.economyLeagues.Where(league => league.indexed).Select(league => league.name));
        }
        catch (Exception ex)
        {
            LogError($"Failed to download the league list: {ex}");
        }

        leagueList.Add("Standard");
        leagueList.Add("Hardcore");

        if (!leagueList.Contains(Settings.DataSourceSettings.League.Value))
        {
            Settings.DataSourceSettings.League.Value = leagueList.MaxBy(x => x == playerLeague);
        }

        Settings.DataSourceSettings.League.SetListValues(leagueList.ToList());
    }

    private string PlayerLeague
    {
        get
        {
            var playerLeague = GameController.IngameState.ServerData.League;
            if (string.IsNullOrWhiteSpace(playerLeague))
            {
                playerLeague = null;
            }
            else
            {
                if (playerLeague.StartsWith("SSF "))
                {
                    playerLeague = playerLeague["SSF ".Length..];
                }
            }

            return playerLeague;
        }
    }
    private double ParsePercentage(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        // Remove '%' character and convert to double
        if (double.TryParse(value.Replace("%", ""), out double result))
            return result / 100;
        else
            return 0;
    }
    static int ParseInt(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        int parsedValue;
        if (int.TryParse(value, out parsedValue))
            return parsedValue;
        else
            return 0;
    }

    static double ParseDouble(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        double parsedValue;
        if (double.TryParse(value, out parsedValue))
            return parsedValue;
        else
            return 0;
    }

    private List<ItemData> ReadItemDataCsvFile()
    {
        List<ItemData> items = new List<ItemData>();

        var filePath = Path.Join(DirectoryFullName, ItemDataCSVPath);



        if (!File.Exists(filePath))
        {
            LogError("Item data CSV File not found at " + filePath);
            return items;
        }

        using (TextFieldParser parser = new TextFieldParser(filePath))
        {
            parser.TextFieldType = FieldType.Delimited;
            parser.SetDelimiters(",");

            parser.ReadLine();

            while (!parser.EndOfData)
            {
                string[] fields = parser.ReadFields();
                if (fields != null)
                {
                    ItemData item = new ItemData
                    {
                        BaseItem = fields[0],
                        Chance = ParsePercentage(fields[1]),
                        AverageOrbs = ParseInt(fields[2]),
                        DestructionChance = ParsePercentage(fields[3]),
                        BaseItemDisambiguation = fields[4],
                        Tier = fields[5],
                        Name = fields[6],
                        Disambiguation = fields[7],
                        MinILvl = ParseInt(fields[8]),
                        Weight = ParseDouble(fields[9]),
                        PoeWikiLink = fields[10]
                    };

                    items.Add(item);
                }
            }
        }

        return items;
    }
}