using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteamAchievementTracker.Models
{
    public class GameInfo
    {
        public string GameName { get; set; }
        public int AppId { get; set; }
        public UserAchievementStats AchievementStats { get; set; }
        // playtime in hours
        public int GamePlaytime { get; set; }
    }
}
