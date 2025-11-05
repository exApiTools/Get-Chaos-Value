using Newtonsoft.Json;
using Ninja_Price.API.PoeNinja;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ninja_Price.Main;

public partial class Main
{
    private const string CurrencyUrl = "https://poe.ninja/poe1/api/economy/exchange/current/overview?league={0}&type=Currency";
    private const string FragmentsUrl = "https://poe.ninja/poe1/api/economy/exchange/current/overview?league={0}&type=Fragment";
    private const string DivinationCardsUrl = "https://poe.ninja/poe1/api/economy/exchange/current/overview?league={0}&type=DivinationCard";
    private const string EssencesUrl = "https://poe.ninja/poe1/api/economy/exchange/current/overview?league={0}&type=Essence";
    private const string ResonatorsUrl = "https://poe.ninja/poe1/api/economy/exchange/current/overview?league={0}&type=Resonator";
    private const string FossilsUrl = "https://poe.ninja/poe1/api/economy/exchange/current/overview?league={0}&type=Fossil";
    private const string ScarabsUrl = "https://poe.ninja/poe1/api/economy/exchange/current/overview?league={0}&type=Scarab";
    private const string OilUrl = "https://poe.ninja/poe1/api/economy/exchange/current/overview?league={0}&type=Oil";
    private const string DeliriumOrbUrl = "https://poe.ninja/poe1/api/economy/exchange/current/overview?league={0}&type=DeliriumOrb";
    private const string ArtifactsUrl = "https://poe.ninja/poe1/api/economy/exchange/current/overview?league={0}&type=Artifact";
    private const string TattooUrl = "https://poe.ninja/poe1/api/economy/exchange/current/overview?league={0}&type=Tattoo";
    private const string OmenUrl = "https://poe.ninja/poe1/api/economy/exchange/current/overview?league={0}&type=Omen";
    private const string KalguuranRunesUrl = "https://poe.ninja/poe1/api/economy/exchange/current/overview?league={0}&type=Runegraft";
    private const string AllflameEmbersUrl = "https://poe.ninja/poe1/api/economy/exchange/current/overview?league={0}&type=AllflameEmber";

    private const string InvitationUrl = "https://poe.ninja/api/data/ItemOverview?league={0}&type=Invitation&language=en";
    private const string UniqueAccessoriesUrl = "https://poe.ninja/api/data/itemoverview?league={0}&type=UniqueAccessory&language=en";
    private const string UniqueArmoursUrl = "https://poe.ninja/api/data/itemoverview?league={0}&type=UniqueArmour&language=en";
    private const string UniqueFlasksUrl = "https://poe.ninja/api/data/itemoverview?league={0}&type=UniqueFlask&language=en";
    private const string UniqueJewelsUrl = "https://poe.ninja/api/data/itemoverview?league={0}&type=UniqueJewel&language=en";
    private const string UniqueMapsUrl = "https://poe.ninja/api/data/itemoverview?league={0}&type=UniqueMap&language=en";
    private const string UniqueWeaponsUrl = "https://poe.ninja/api/data/itemoverview?league={0}&type=UniqueWeapon&language=en";
    private const string WhiteMapsUrl = "https://poe.ninja/api/data/itemoverview?league={0}&type=Map&language=en";
    private const string BlightedMapsUrl = "https://poe.ninja/api/data/itemoverview?league={0}&type=BlightedMap&language=en";
    private const string BlightRavagedMapsUrl = "https://poe.ninja/api/data/itemoverview?league={0}&type=BlightRavagedMap&language=en";
    private const string IncubatorsUrl = "https://poe.ninja/api/data/itemoverview?league={0}&type=Incubator&language=en";
    private const string SkillGemsUrl = "https://poe.ninja/api/data/ItemOverview?league={0}&type=SkillGem&language=en";
    private const string ClusterJewelsUrl = "https://poe.ninja/api/data/itemoverview?league={0}&type=ClusterJewel&language=en";
    private const string BeastUrl = "https://poe.ninja/api/data/itemoverview?league={0}&type=Beast&language=en";
    private const string VialUrl = "https://poe.ninja/api/data/itemoverview?league={0}&type=Vial&language=en";

    private class LeagueMetadata
    {
        public DateTime LastLoadTime { get; set; }
    }

    private void StartDataReload(string league, bool forceRefresh)
    {
        LogMessage($"Getting data for {league}", 5);

        if (Interlocked.CompareExchange(ref _updating, 1, 0) != 0)
        {
            LogMessage("Update is already in progress");
            return;
        }

        Task.Run(async () =>
        {
            try
            {
                LogMessage("Gathering Data from Poe.Ninja.", 5);

                var newData = new CollectiveApiData();
                var tryWebFirst = forceRefresh;
                var metadataPath = Path.Join(NinjaDirectory, league, "meta.json");
                if (!tryWebFirst && Settings.DataSourceSettings.AutoReload)
                {
                    tryWebFirst = await IsLocalCacheStale(metadataPath);
                }

                await LoadData<CurrencyOverviewData.RootObject>("Currency2.json", CurrencyUrl, league, tryWebFirst, t => newData.Currency = t);
                await LoadData<CurrencyOverviewData.RootObject>("DivinationCards2.json", DivinationCardsUrl, league, tryWebFirst, t => newData.DivinationCards = t);
                await LoadData<CurrencyOverviewData.RootObject>("Essences2.json", EssencesUrl, league, tryWebFirst, t => newData.Essences = t);
                await LoadData<CurrencyOverviewData.RootObject>("Fragments2.json", FragmentsUrl, league, tryWebFirst, t => newData.Fragments = t);
                await LoadData<CurrencyOverviewData.RootObject>("Resonators2.json", ResonatorsUrl, league, tryWebFirst, t => newData.Resonators = t);
                await LoadData<CurrencyOverviewData.RootObject>("Fossils2.json", FossilsUrl, league, tryWebFirst, t => newData.Fossils = t);
                await LoadData<CurrencyOverviewData.RootObject>("Oils2.json", OilUrl, league, tryWebFirst, t => newData.Oils = t);
                await LoadData<CurrencyOverviewData.RootObject>("Scarabs2.json", ScarabsUrl, league, tryWebFirst, t => newData.Scarabs = t);
                await LoadData<CurrencyOverviewData.RootObject>("DeliriumOrbs2.json", DeliriumOrbUrl, league, tryWebFirst, t => newData.DeliriumOrb = t);
                await LoadData<CurrencyOverviewData.RootObject>("Artifacts2.json", ArtifactsUrl, league, tryWebFirst, t => newData.Artifacts = t);
                await LoadData<CurrencyOverviewData.RootObject>("Tattoos2.json", TattooUrl, league, tryWebFirst, t => newData.Tattoos = t);
                await LoadData<CurrencyOverviewData.RootObject>("Omens2.json", OmenUrl, league, tryWebFirst, t => newData.Omens = t);
                await LoadData<CurrencyOverviewData.RootObject>("KalguuranRunes2.json", KalguuranRunesUrl, league, tryWebFirst, t => newData.KalguuranRunes = t);
                await LoadData<CurrencyOverviewData.RootObject>("AllflameEmbers2.json", AllflameEmbersUrl, league, tryWebFirst, t => newData.AllflameEmbers = t);

                await LoadData<Invitations.RootObject>("Invitations.json", InvitationUrl, league, tryWebFirst, t => newData.Invitations = t);
                await LoadData<Vials.RootObject>("Vials.json", VialUrl, league, tryWebFirst, t => newData.Vials = t);
                await LoadData<Incubators.RootObject>("Incubators.json", IncubatorsUrl, league, tryWebFirst, t => newData.Incubators = t);
                await LoadData<SkillGems.RootObject>("SkillGems.json", SkillGemsUrl, league, tryWebFirst, t => newData.SkillGems = t);
                await LoadData<ClusterJewelNinjaData>("ClusterJewels.json", ClusterJewelsUrl, league, tryWebFirst, t => newData.ClusterJewels = t);
                await LoadData<Beasts.RootObject>("Beasts.json", BeastUrl, league, tryWebFirst, t => newData.Beasts = t);
                await LoadData<UniqueAccessories.RootObject>("UniqueAccessories.json", UniqueAccessoriesUrl, league, tryWebFirst, t => newData.UniqueAccessories = t);
                await LoadData<UniqueArmours.RootObject>("UniqueArmours.json", UniqueArmoursUrl, league, tryWebFirst, t => newData.UniqueArmours = t);
                await LoadData<UniqueFlasks.RootObject>("UniqueFlasks.json", UniqueFlasksUrl, league, tryWebFirst, t => newData.UniqueFlasks = t);
                await LoadData<UniqueJewels.RootObject>("UniqueJewels.json", UniqueJewelsUrl, league, tryWebFirst, t => newData.UniqueJewels = t);
                await LoadData<UniqueMaps.RootObject>("UniqueMaps.json", UniqueMapsUrl, league, tryWebFirst, t => newData.UniqueMaps = t);
                await LoadData<UniqueWeapons.RootObject>("UniqueWeapons.json", UniqueWeaponsUrl, league, tryWebFirst, t => newData.UniqueWeapons = t);
                await LoadData<WhiteMaps.RootObject>("WhiteMaps.json", WhiteMapsUrl, league, tryWebFirst, t => newData.WhiteMaps = t);
                await LoadData<BlightedMaps.RootObject>("BlightedMaps.json", BlightedMapsUrl, league, tryWebFirst, t => newData.BlightedMaps = t);
                await LoadData<BlightRavagedMaps.RootObject>("BlightRavagedMaps.json", BlightRavagedMapsUrl, league, tryWebFirst, t => newData.BlightRavagedMaps = t);

                new FileInfo(metadataPath).Directory?.Create();
                await File.WriteAllTextAsync(metadataPath, JsonConvert.SerializeObject(new LeagueMetadata { LastLoadTime = DateTime.UtcNow }));

                LogMessage("Finished Gathering Data from Poe.Ninja.", 5);
                RemoveUnlikelyItems(newData);
                CollectedData = newData;
                LogMessage("Updated CollectedData.", 5);
            }
            finally
            {
                Interlocked.Exchange(ref _updating, 0);
            }
        });
    }

    private void RemoveUnlikelyItems(CollectiveApiData data)
    {
        data.UniqueAccessories.Lines.RemoveAll(x => x.DetailsId.Contains("-relic"));
        data.UniqueArmours.Lines.RemoveAll(x => x.DetailsId.Contains("-relic"));
        data.UniqueFlasks.Lines.RemoveAll(x => x.DetailsId.Contains("-relic"));
        data.UniqueJewels.Lines.RemoveAll(x => x.DetailsId.Contains("-relic"));
        data.UniqueWeapons.Lines.RemoveAll(x => x.DetailsId.Contains("-relic"));
    }

    private async Task<bool> IsLocalCacheStale(string metadataPath)
    {
        if (!File.Exists(metadataPath))
        {
            return true;
        }

        try
        {
            var metadata = JsonConvert.DeserializeObject<LeagueMetadata>(await File.ReadAllTextAsync(metadataPath));
            return DateTime.UtcNow - metadata.LastLoadTime > TimeSpan.FromMinutes(Settings.DataSourceSettings.ReloadPeriod);
        }
        catch (Exception ex)
        {
            if (Settings.DebugSettings.EnableDebugLogging)
            {
                LogError($"Metadata loading failed: {ex}");
            }

            return true;
        }
    }

    private async Task LoadData<T>(string fileName, string url, string league, bool tryWebFirst, Action<T> dataAction)
    {
        var backupFile = Path.Join(NinjaDirectory, league, fileName);
        if (tryWebFirst)
        {
            if (await LoadDataFromWeb(fileName, url, league, dataAction, backupFile))
            {
                return;
            }
        }

        if (await LoadDataFromBackup(fileName, dataAction, backupFile))
        {
            return;
        }

        if (!tryWebFirst)
        {
            await LoadDataFromWeb(fileName, url, league, dataAction, backupFile);
        }
    }

    private async Task<bool> LoadDataFromBackup<T>(string fileName, Action<T> dataAction, string backupFile)
    {
        if (File.Exists(backupFile))
        {
            try
            {
                var data = JsonConvert.DeserializeObject<T>(await File.ReadAllTextAsync(backupFile));
                dataAction(data);
                return true;
            }
            catch (Exception backupEx)
            {
                if (Settings.DebugSettings.EnableDebugLogging)
                {
                    LogError($"{fileName} backup data load failed: {backupEx}");
                }
            }
        }
        else if (Settings.DebugSettings.EnableDebugLogging)
        {
            LogError($"No backup for {fileName}");
        }

        return false;
    }

    private async Task<bool> LoadDataFromWeb<T>(string fileName, string url, string league, Action<T> dataAction, string backupFile)
    {
        try
        {
            if (Settings.DebugSettings.EnableDebugLogging)
            {
                LogMessage($"Downloading {fileName}");
            }

            var data = JsonConvert.DeserializeObject<T>(await Utils.DownloadFromUrl(string.Format(url, league)));
            if (Settings.DebugSettings.EnableDebugLogging)
            {
                LogMessage($"{fileName} downloaded");
            }

            try
            {
                new FileInfo(backupFile).Directory.Create();
                await File.WriteAllTextAsync(backupFile, JsonConvert.SerializeObject(data, Formatting.Indented));
            }
            catch (Exception ex)
            {
                var errorPath = backupFile + ".error";
                new FileInfo(errorPath).Directory.Create();
                await File.WriteAllTextAsync(errorPath, ex.ToString());
                if (Settings.DebugSettings.EnableDebugLogging)
                {
                    LogError($"{fileName} save failed: {ex}");
                }
            }

            dataAction(data);
            return true;
        }
        catch (Exception ex)
        {
            if (Settings.DebugSettings.EnableDebugLogging)
            {
                LogError($"{fileName} fresh data download failed: {ex}");
            }

            return false;
        }
    }
}