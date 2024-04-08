using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ExileCore;
using ExileCore.PoEMemory.FilesInMemory;
using ExileCore.PoEMemory.MemoryObjects;
using Newtonsoft.Json;
using Ninja_Price.API.PoeNinja.Classes;

namespace Ninja_Price.Main;

public partial class Main : BaseSettingsPlugin<Settings.Settings>
{
    private string NinjaDirectory;
    private CollectiveApiData CollectedData;
    private const string PoeLeagueApiList = "http://api.pathofexile.com/leagues?type=main&compact=1";
    private const string CustomUniqueArtMappingPath = "uniqueArtMapping.json";
    private const string DefaultUniqueArtMappingPath = "uniqueArtMapping.default.json";
    private int _updating;
    public Dictionary<string, List<string>> UniqueArtMapping = new Dictionary<string, List<string>>();
    private readonly ConcurrentDictionary<NecropolisCraftingMod, string> _necropolisModText = [];

    [GeneratedRegex("<[^>]*>{{(?<data>[^}]*)}}")]
    private static partial Regex StripTagsRegex();

    public override bool Initialise()
    {
        Name = "Ninja Price";
        NinjaDirectory = Path.Join(DirectoryFullName, "NinjaData");
        Directory.CreateDirectory(NinjaDirectory);

        UpdateLeagueList();
        StartDataReload(Settings.LeagueList.Value, false);

        Settings.ReloadPrices.OnPressed += () => StartDataReload(Settings.LeagueList.Value, true);
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
        Settings.SyncCurrentLeague.OnValueChanged += (_, _) => SyncCurrentLeague();
        CustomItem.InitCustomItem(this);

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
        if (Settings.SyncCurrentLeague)
        {
            var playerLeague = PlayerLeague;
            if (playerLeague != null)
            {
                if (!Settings.LeagueList.Values.Contains(playerLeague))
                {
                    Settings.LeagueList.Values.Add(playerLeague);
                }

                if (Settings.LeagueList.Value != playerLeague)
                {
                    Settings.LeagueList.Value = playerLeague;
                    StartDataReload(Settings.LeagueList.Value, false);
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
                if (Settings.Debug)
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
        var leagueList = new HashSet<string>();
        var playerLeague = PlayerLeague;
        if (playerLeague != null)
        {
            leagueList.Add(playerLeague);
        }

        try
        {
            var leagueListFromUrl = Utils.DownloadFromUrl(PoeLeagueApiList).Result;
            var leagueData = JsonConvert.DeserializeObject<List<Leagues>>(leagueListFromUrl);
            leagueList.UnionWith(leagueData.Where(league => !league.Id.Contains("SSF")).Select(league => league.Id));
        }
        catch (Exception ex)
        {
            LogError($"Failed to download the league list: {ex}");
        }

        leagueList.Add("Standard");
        leagueList.Add("Hardcore");

        if (!leagueList.Contains(Settings.LeagueList.Value))
        {
            Settings.LeagueList.Value = leagueList.MaxBy(x => x == playerLeague);
        }

        Settings.LeagueList.SetListValues(leagueList.ToList());
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
}