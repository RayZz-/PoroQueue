﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PoroQueue
{
    public static class Icon
    {
        private static string CacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PoroQueue", "Cache");
        private static EffectIcon[] AllowedIcons = null;

        private static int DefaultID = -1;
        public static int Default
        {
            get { return DefaultID; }
            private set { DefaultID = value; Debug.WriteLine("Default icon = " + value); }
        }

        static Icon()
        {
            if (!Directory.Exists(CacheDirectory))
                Directory.CreateDirectory(CacheDirectory);

            LeagueOfLegends.IconChanged += (s,e) => Default = LeagueOfLegends.CurrentSummoner.profileIconId;
            LeagueOfLegends.LoggedIn += (s,e) => Default = LeagueOfLegends.CurrentSummoner.profileIconId;
            LeagueOfLegends.Stopped += (s,e) => Default = -1;
        }

        public static void Init()
        {
        }

        public class EffectIcon
        {
            public int ID;
            public LeagueOfLegends.GameMode[] GameModes;
        };

        public static async void LoadCurrentIntoPictureBox(PictureBox Picture)
        {
            if (LeagueOfLegends.IsActive && LeagueOfLegends.IsLoggedIn)
            {
                Summoner CurrentSummoner = await Summoner.GetCurrent();
                LoadIntoPictureBox(CurrentSummoner.profileIconId, Picture);
            }
        }

        public static async void LoadIntoPictureBox(int Icon, PictureBox Picture)
        {
            var CacheLocation = Path.Combine(CacheDirectory, Icon + ".png");
            if (File.Exists(CacheLocation))
            {
                Picture.Image = Image.FromFile(CacheLocation);
                return;
            }

            var CDragonRequestFormat = "https://cdn.communitydragon.org/{0}/profile-icon/{1}";
            var RequestURL = string.Format(CDragonRequestFormat, LeagueOfLegends.LatestVersion, Icon.ToString());
            var Request = WebRequest.Create(RequestURL);

            await Task.Run(() =>
            {
                using (var Response = Request.GetResponse())
                {
                    using (var ResponseStream = Response.GetResponseStream())
                    {
                        var Result = Image.FromStream(ResponseStream);
                        Result.Save(CacheLocation);

                        Picture.Image = Result;
                    }
                }
            });
        }

        public static async Task<int[]> GetOwnedIconIDs()
        {
            return (await MissionDataRequest.Get()).playerInventory.icons.ToArray();
        }

        public static async Task<EffectIcon[]> GetEffectIcons()
        {
            if (AllowedIcons == null)
                AllowedIcons = await JSONRequest.Get<EffectIcon[]>("https://querijn.codes/poro_queue/icons.json");

            return AllowedIcons;
        }

        internal static void SetToPoro(LeagueOfLegends.GameMode Mode, out int IconID)
        {
            string ID = Config.Current.GetEntryIDForCurrentSummonerSync();

            int[] IconSet;
            int Index;

            switch (Mode)
            {
                default:
                    IconID = -1;
                    return;

                case LeagueOfLegends.GameMode.ARAM:
                    IconSet = Config.Current.Entries[ID].EnabledForARAM.ToArray();
                    Index = Config.Current.Entries[ID].ARAMIterator;
                    break;

                case LeagueOfLegends.GameMode.NexusBlitz:
                    IconSet = Config.Current.Entries[ID].EnabledForBlitz.ToArray();
                    Index = Config.Current.Entries[ID].BlitzIterator;
                    break;

                case LeagueOfLegends.GameMode.URF:
                    IconSet = Config.Current.Entries[ID].EnabledForURF.ToArray();
                    Index = Config.Current.Entries[ID].URFIterator;
                    break;
            }
            
            if (Index >= IconSet.Length)
                Index = 0;

            if (IconSet.Length == 0 || IconSet[Index] == LeagueOfLegends.CurrentSummoner.profileIconId)
            {
                IconID = -1;
                return;
            }
            
            IconID = IconSet[Index];
            Debug.WriteLine("Setting Poro Icon " + IconID);
            Set(IconID);

            switch (Mode)
            {
                default:
                    return;

                case LeagueOfLegends.GameMode.ARAM:
                    Config.Current.Entries[ID].ARAMIterator++;
                    break;

                case LeagueOfLegends.GameMode.NexusBlitz:
                    Config.Current.Entries[ID].BlitzIterator++;
                    break;

                case LeagueOfLegends.GameMode.URF:
                    Config.Current.Entries[ID].URFIterator++;
                    break;
            }
        }

        internal static async void Set(int Icon)
        {
            await Request.Put(LeagueOfLegends.APIDomain + "/lol-summoner/v1/current-summoner/icon", "{\"profileIconId\": " + Icon + "}");
        }

        internal static void ResetToDefault()
        {
            if (Default == -1)
            {
                Debug.WriteLine("Got a request to reset to default icon, but we haven't set one.");
                return;
            }

            if (Default == LeagueOfLegends.CurrentSummoner.profileIconId)
                return;

            Debug.WriteLine("Resetting Icon to default ID " + Default);
            Set(Default);
        }
    }
}
