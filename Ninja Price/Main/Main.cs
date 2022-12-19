using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ExileCore;
using Newtonsoft.Json;
using Ninja_Price.API.PoeNinja;
using Ninja_Price.API.PoeNinja.Classes;

namespace Ninja_Price.Main
{
    public partial class Main : BaseSettingsPlugin<Settings.Settings>
    {
        public const int NotFound = -1;
        public string NinjaDirectory;
        public DateTime BuildDate;
        public CollectiveApiData CollectedData = new CollectiveApiData();
        public bool DownloadDone;
        public bool InitJsonDone;
        public string PluginVersion;
        public string PoeLeagueApiList = "http://api.pathofexile.com/leagues?type=main&compact=1";
        public bool UpdatingFromJson { get; set; } = false;
        public bool UpdatingFromAPI { get; set; } = false;

        //https://stackoverflow.com/questions/826777/how-to-have-an-auto-incrementing-version-number-visual-studio
        public Version Version = Assembly.GetExecutingAssembly().GetName().Version;

        public static Main Controller { get; set; }


        public string CurrentLeague { get; set; }

        public override bool Initialise()
        {
            Name = "Ninja Price";
            Controller = this;
            BuildDate = new DateTime(2000, 1, 1).AddDays(Version.Build).AddSeconds(Version.Revision * 2);
            PluginVersion = $"{Version}";
            NinjaDirectory = DirectoryFullName + "\\NinjaData\\";
            var file = new FileInfo(NinjaDirectory);
            file.Directory?.Create();

            GatherLeagueNames();

            if (Settings.FirstTime)
            {
                LoadJsonData();
                UpdatePoeNinjaData();
                Settings.FirstTime = true;
            }
            else
            {
                UpdatePoeNinjaData();
            }

            CurrentLeague = Settings.LeagueList.Value;

            // Enable Events
            Settings.ReloadButton.OnPressed += LoadJsonData;

            CustomItem.InitCustomItem(this);

            return true;
        }

        public void LoadJsonData()
        {
            LogMessage($"Getting data for {CurrentLeague}", 5);
            GetJsonData(CurrentLeague);
            UpdatePoeNinjaData();
        }

        private void GatherLeagueNames()
        {
            var leagueListFromUrl = Api.DownloadFromUrl(PoeLeagueApiList);
            var leagueData = JsonConvert.DeserializeObject<List<Leagues>>(leagueListFromUrl);
            Api.Json.SaveSettingFile($"{NinjaDirectory}Leagues.json", leagueData);
            var leagueList = (from league in leagueData where !league.Id.Contains("SSF") select league.Id).ToList();

            // set wanted league
            CurrentLeague = CurrentLeague == null ? leagueList[0] : Settings.LeagueList.Value;
            // display default league in setting
            if (Settings.LeagueList.Value == null)
                Settings.LeagueList.Value = CurrentLeague;

            Settings.LeagueList.SetListValues(leagueList);
        }
    }
}