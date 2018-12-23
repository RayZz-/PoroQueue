﻿using System.Drawing;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PoroQueue
{
    public static class Icon
    {
        public enum GameMode
        {
            ARAM = 0,
            NexusBlitz = 1,
            URF = 2
        };

        public class EffectIcon
        {
            public int ID;
            public GameMode[] GameModes;
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
            var CDragonRequestFormat = "https://cdn.communitydragon.org/{0}/profile-icon/{1}";
            var RequestURL = string.Format(CDragonRequestFormat, LeagueOfLegends.LatestVersion, Icon.ToString());
            var Request = WebRequest.Create(RequestURL);

            await Task.Run(() =>
            {
                using (var Response = Request.GetResponse())
                {
                    using (var ResponseStream = Response.GetResponseStream())
                    {
                        Picture.Image = Image.FromStream(ResponseStream);
                    }
                }
            });
        }

        public static async Task<int[]> GetOwnedIconIDs()
        {
            return (await MissionDataRequest.Get()).playerInventory.icons.ToArray();
        }

        public static async Task<EffectIcon[]> GetAllowedIcons()
        {
            return await JSONRequest.Get<EffectIcon[]>("https://querijn.codes/poro_queue/icons.json");
        }
    }
}