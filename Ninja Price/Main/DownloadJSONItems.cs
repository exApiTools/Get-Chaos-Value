using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Ninja_Price.API.PoeNinja;

namespace Ninja_Price.Main;

public partial class Main
{
    private const string CurrencyUrl = "https://poe.ninja/api/data/currencyoverview?league={0}&type=Currency&language=en";
    private const string FragmentsUrl = "https://poe.ninja/api/data/currencyoverview?league={0}&type=Fragment&language=en";
    private const string DivinationCardsUrl = "https://poe.ninja/api/data/itemoverview?league={0}&type=DivinationCard&language=en";
    private const string EssencesUrl = "https://poe.ninja/api/data/itemoverview?league={0}&type=Essence&language=en";
    private const string UniqueAccessoriesUrl = "https://poe.ninja/api/data/itemoverview?league={0}&type=UniqueAccessory&language=en";
    private const string UniqueArmoursUrl = "https://poe.ninja/api/data/itemoverview?league={0}&type=UniqueArmour&language=en";
    private const string UniqueFlasksUrl = "https://poe.ninja/api/data/itemoverview?league={0}&type=UniqueFlask&language=en";
    private const string UniqueJewelsUrl = "https://poe.ninja/api/data/itemoverview?league={0}&type=UniqueJewel&language=en";
    private const string UniqueMapsUrl = "https://poe.ninja/api/data/itemoverview?league={0}&type=UniqueMap&language=en";
    private const string UniqueWeaponsUrl = "https://poe.ninja/api/data/itemoverview?league={0}&type=UniqueWeapon&language=en";
    private const string WhiteMapsUrl = "https://poe.ninja/api/data/itemoverview?league={0}&type=Map&language=en";
    private const string ResonatorsUrl = "https://poe.ninja/api/data/itemoverview?league={0}&type=Resonator&language=en";
    private const string FossilsUrl = "https://poe.ninja/api/data/itemoverview?league={0}&type=Fossil&language=en";
    private const string ScarabsUrl = "https://poe.ninja/api/data/itemoverview?league={0}&type=Scarab&language=en";
    private const string IncubatorsUrl = "https://poe.ninja/api/data/itemoverview?league={0}&type=Incubator&language=en";
    private const string OilUrl = "https://poe.ninja/api/data/itemoverview?league={0}&type=Oil&language=en";
    private const string DeliriumOrbUrl = "https://poe.ninja/api/data/itemoverview?league={0}&type=DeliriumOrb&language=en";
    private const string VialUrl = "https://poe.ninja/api/data/itemoverview?league={0}&type=Vial&language=en";
    private const string InvitationUrl = "https://poe.ninja/api/data/ItemOverview?league={0}&type=Invitation&language=en";
    private const string ArtifactsUrl = "https://poe.ninja/api/data/ItemOverview?league={0}&type=Artifact&language=en";
    private const string SkillGemsUrl = "https://poe.ninja/api/data/ItemOverview?league={0}&type=SkillGem&language=en";
    private const string ClusterJewelsUrl = "https://poe.ninja/api/data/itemoverview?league={0}&type=ClusterJewel&language=en";
    private const string TattooUrl = "https://poe.ninja/api/data/itemoverview?league={0}&type=Tattoo&language=en";
    private const string OmenUrl = "https://poe.ninja/api/data/itemoverview?league={0}&type=Omen&language=en";
    private const string CoffinUrl = "https://poe.ninja/api/data/itemoverview?league={0}&type=Coffin&language=en";
    private const string AllflameUrl = "https://poe.ninja/api/data/itemoverview?league={0}&type=AllflameEmber&language=en";

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
                if (!tryWebFirst && Settings.AutoReload)
                {
                    tryWebFirst = await IsLocalCacheStale(metadataPath);
                }

                await LoadData<Currency.RootObject>("Currency.json", CurrencyUrl, league, tryWebFirst, t => newData.Currency = t);
                await LoadData<DivinationCards.RootObject>("DivinationCards.json", DivinationCardsUrl, league, tryWebFirst, t => newData.DivinationCards = t);
                await LoadData<Essences.RootObject>("Essences.json", EssencesUrl, league, tryWebFirst, t => newData.Essences = t);
                await LoadData<Fragments.RootObject>("Fragments.json", FragmentsUrl, league, tryWebFirst, t => newData.Fragments = t);
                await LoadData<UniqueAccessories.RootObject>("UniqueAccessories.json", UniqueAccessoriesUrl, league, tryWebFirst, t => newData.UniqueAccessories = t);
                await LoadData<UniqueArmours.RootObject>("UniqueArmours.json", UniqueArmoursUrl, league, tryWebFirst, t => newData.UniqueArmours = t);
                await LoadData<UniqueFlasks.RootObject>("UniqueFlasks.json", UniqueFlasksUrl, league, tryWebFirst, t => newData.UniqueFlasks = t);
                await LoadData<UniqueJewels.RootObject>("UniqueJewels.json", UniqueJewelsUrl, league, tryWebFirst, t => newData.UniqueJewels = t);
                await LoadData<UniqueMaps.RootObject>("UniqueMaps.json", UniqueMapsUrl, league, tryWebFirst, t => newData.UniqueMaps = t);
                await LoadData<UniqueWeapons.RootObject>("UniqueWeapons.json", UniqueWeaponsUrl, league, tryWebFirst, t => newData.UniqueWeapons = t);
                await LoadData<WhiteMaps.RootObject>("WhiteMaps.json", WhiteMapsUrl, league, tryWebFirst, t => newData.WhiteMaps = t);
                await LoadData<Resonators.RootObject>("Resonators.json", ResonatorsUrl, league, tryWebFirst, t => newData.Resonators = t);
                await LoadData<Fossils.RootObject>("Fossils.json", FossilsUrl, league, tryWebFirst, t => newData.Fossils = t);
                await LoadData<Oils.RootObject>("Oils.json", OilUrl, league, tryWebFirst, t => newData.Oils = t);
                await LoadData<Incubators.RootObject>("Incubators.json", IncubatorsUrl, league, tryWebFirst, t => newData.Incubators = t);
                await LoadData<Scarab.RootObject>("Scarabs.json", ScarabsUrl, league, tryWebFirst, t => newData.Scarabs = t);
                await LoadData<DeliriumOrb.RootObject>("DeliriumOrbs.json", DeliriumOrbUrl, league, tryWebFirst, t => newData.DeliriumOrb = t);
                await LoadData<Vials.RootObject>("Vials.json", VialUrl, league, tryWebFirst, t => newData.Vials = t);
                await LoadData<Invitations.RootObject>("Invitations.json", InvitationUrl, league, tryWebFirst, t => newData.Invitations = t);
                await LoadData<Artifacts.RootObject>("Artifacts.json", ArtifactsUrl, league, tryWebFirst, t => newData.Artifacts = t);
                await LoadData<SkillGems.RootObject>("SkillGems.json", SkillGemsUrl, league, tryWebFirst, t => newData.SkillGems = t);
                await LoadData<ClusterJewelNinjaData>("ClusterJewels.json", ClusterJewelsUrl, league, tryWebFirst, t => newData.ClusterJewels = t);
                await LoadData<Tattoos.RootObject>("Tattoos.json", TattooUrl, league, tryWebFirst, t => newData.Tattoos = t);
                await LoadData<Omens.RootObject>("Omens.json", OmenUrl, league, tryWebFirst, t => newData.Omens = t);
                await LoadData<Coffins.RootObject>("Coffins.json", CoffinUrl, league, tryWebFirst, t => newData.Coffins = t);
                await LoadData<Allflames.RootObject>("Allflames.json", AllflameUrl, league, tryWebFirst, t => newData.Allflames = t);

                new FileInfo(metadataPath).Directory?.Create();
                await File.WriteAllTextAsync(metadataPath, JsonConvert.SerializeObject(new LeagueMetadata { LastLoadTime = DateTime.UtcNow }));

                LogMessage("Finished Gathering Data from Poe.Ninja.", 5);
                CollectedData = newData;
                DivineDalue = CollectedData.Currency.Lines.Find(x => x.CurrencyTypeName == "Divine Orb")?.ChaosEquivalent;
                LogMessage("Updated CollectedData.", 5);
            }
            finally
            {
                Interlocked.Exchange(ref _updating, 0);
            }
        });
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
            return DateTime.UtcNow - metadata.LastLoadTime > TimeSpan.FromMinutes(Settings.ReloadTimer);
        }
        catch (Exception ex)
        {
            if (Settings.Debug)
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
                if (Settings.Debug)
                {
                    LogError($"{fileName} backup data load failed: {backupEx}");
                }
            }
        }
        else if (Settings.Debug)
        {
            LogError($"No backup for {fileName}");
        }

        return false;
    }

    private async Task<bool> LoadDataFromWeb<T>(string fileName, string url, string league, Action<T> dataAction, string backupFile)
    {
        try
        {
            if (Settings.Debug)
            {
                LogMessage($"Downloading {fileName}");
            }

            var data = JsonConvert.DeserializeObject<T>(await Utils.DownloadFromUrl(string.Format(url, league)));
            if (Settings.Debug)
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
                if (Settings.Debug)
                {
                    LogError($"{fileName} save failed: {ex}");
                }
            }

            dataAction(data);
            return true;
        }
        catch (Exception ex)
        {
            if (Settings.Debug)
            {
                LogError($"{fileName} fresh data download failed: {ex}");
            }

            return false;
        }
    }
}