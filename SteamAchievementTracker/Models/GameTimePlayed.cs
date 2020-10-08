using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteamAchievementTracker.Models
{
    public class GameTimePlayed
    {
        public int appid { get; set; }
        public int playtime_forever { get; set; }
        public int playtime_windows_forever { get; set; }
        public int playtime_mac_forever { get; set; }
        public int playtime_linux_forever { get; set; }
    }
}
