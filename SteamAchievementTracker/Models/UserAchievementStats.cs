using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteamAchievementTracker.Models
{
    public class UserAchievementStats
    {
        public int NumUserAchievements { get; set; }
        public int NumGameAchievements { get; set; }
        public double AchievementPercentage { get; set; }
    }
}
