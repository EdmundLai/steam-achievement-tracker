using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteamAchievementTracker.Models
{
    public class GamesInCommon
    {
        public string SteamName1 { get; set; }
        public string SteamName2 { get; set; }
        public List<CommonGame> CommonGames { get; set; }
    }
}
