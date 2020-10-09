using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SteamAchievementTracker.Models;
using SteamAchievementTracker.Services;

namespace SteamAchievementTracker.Controllers
{
    public class SteamController : Controller
    {

        private static readonly HttpClient HttpClient;

        private readonly IApiKeyService _apiKeyService;
        
        private string ApiKey 
        {
            get
            {
                return _apiKeyService.GetApiKey();
            }
        }

        private static List<AppInfo> GameList { get; set; }

        static SteamController()
        {
            HttpClient = new HttpClient();
            
        }

        public SteamController(IApiKeyService apiKeyService)
        {
            _apiKeyService = apiKeyService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public void DebugLog(string msg)
        {
            System.Diagnostics.Debug.WriteLine(msg);
        }

        public async Task<JObject> GetProfileInfo(string steamId)
        {
            // Call asynchronous network methods in a try/catch block to handle exceptions.
            try
            {
                //string steamId = "76561198055381702";
                HttpResponseMessage response = await HttpClient.GetAsync($"http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={ApiKey}&steamids={steamId}");
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                // Above three lines can be replaced with new helper method below
                // string responseBody = await client.GetStringAsync(uri);

                dynamic playerDetails = JsonConvert.DeserializeObject(responseBody);

                //DebugLog($"Type of playerDetails: {playerDetails.GetType()}");

                var playersArray = playerDetails["response"]["players"];

                if (playersArray.Count != 0)
                {
                    return playersArray[0];

                }

                return null;


            }
            catch (HttpRequestException e)
            {
                DebugLog("\nException Caught!");
                DebugLog("From GetProfileInfo");
                DebugLog($"Message :{e.Message}");
                //var error = JsonConvert.DeserializeObject(e.Message);
                return null;
            }
        }

        public async Task<JObject> GetGameInfo(int appid)
        {
            try
            {
                string apiURL = $"http://api.steampowered.com/ISteamUserStats/GetSchemaForGame/v2/?key={ApiKey}&appid={appid}";
                HttpResponseMessage response = await HttpClient.GetAsync(apiURL);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                // Above three lines can be replaced with new helper method below
                // string responseBody = await client.GetStringAsync(uri);

                // gamesOwnedDetails is of JObject type
                dynamic gamesOwnedDetails = JsonConvert.DeserializeObject(responseBody);

                return gamesOwnedDetails;


                //return Content("Player with that id does not exist.");

            }
            catch (HttpRequestException e)
            {
                DebugLog("\nException Caught!");
                DebugLog("From GetGameInfo");
                DebugLog($"Message :{e.Message}");
                //var error = JsonConvert.DeserializeObject(e.Message);
                return null;
            }
        }

        // don't use this, has rate limit of 200 requests every 5 minutes
        public async Task<JObject> GetGameStorePageInfo(int appid)
        {
            try
            {
                string apiURL = $"https://store.steampowered.com/api/appdetails?appids={appid}";
                HttpResponseMessage response = await HttpClient.GetAsync(apiURL);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                // Above three lines can be replaced with new helper method below
                // string responseBody = await client.GetStringAsync(uri);

                // gamesOwnedDetails is of JObject type
                dynamic gameStorePage = JsonConvert.DeserializeObject(responseBody);

                return gameStorePage;

                //return Content("Player with that id does not exist.");

            }
            catch (HttpRequestException e)
            {
                DebugLog("\nException Caught!");
                DebugLog("From GetGameStorePageInfo");
                DebugLog($"Message :{e.Message}");
                //var error = JsonConvert.DeserializeObject(e.Message);
                return null;
            }
        }

        public async Task<JObject> GetAppList()
        {
            try
            {
                string apiURL = $"http://api.steampowered.com/ISteamApps/GetAppList/v0002/";
                HttpResponseMessage response = await HttpClient.GetAsync(apiURL);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                // Above three lines can be replaced with new helper method below
                // string responseBody = await client.GetStringAsync(uri);

                // gamesOwnedDetails is of JObject type
                dynamic appList = JsonConvert.DeserializeObject(responseBody);

                return appList;

                //return Content("Player with that id does not exist.");

            }
            catch (HttpRequestException e)
            {
                DebugLog("\nException Caught!");
                DebugLog("From GetAppList");
                DebugLog($"Message :{e.Message}");
                //var error = JsonConvert.DeserializeObject(e.Message);
                return null;
            }

        }

        public void LoadJson()
        {
            //DebugLog(Environment.CurrentDirectory.ToString());
            using (StreamReader r = new StreamReader(Path.Combine(Environment.CurrentDirectory, "steamapplist.json")))
            {
                string json = r.ReadToEnd();
                dynamic jsonData = JsonConvert.DeserializeObject(json);
                //if(jsonData == null)
                //{
                //    DebugLog("WE NULL BOIS");
                //} else
                //{
                //    DebugLog("WE NOT NULL BOIS");
                //    DebugLog($"{jsonData["applist"]["apps"].GetType()}");
                //}
                DebugLog($"{jsonData["applist"]["apps"].Count}");
                GameList = jsonData["applist"]["apps"].ToObject<List<AppInfo>>();
            }
        }

        // this is a bad idea. the load is too large
        // memory is going to 1 GB
        // very bad very bad
        // DONT USE THIS IT WILL BREAK
        //public async Task<SteamController> InitializeAsync()
        //{
        //    dynamic appList = await GetAppList();

        //    if (appList != null)
        //    {
        //        DebugLog($"{appList["applist"]["apps"].Count}");
        //        GameList = appList["applist"]["apps"].ToObject<List<AppInfo>>();
        //    }

        //    return this;
        //}

        public string GetGameName(int appid)
        {
            if (GameList == null)
            {
                LoadJson();
            }

            AppInfo foundApp = GameList.Find(appInfo => appInfo.appid == appid);

            if(foundApp == null || foundApp.name == null)
            {
                return "";
            }

            return foundApp.name;
        }

        public async Task<GameInfo> ConvertGameTimePlayedToGameInfo(GameTimePlayed gameTimePlayed, string steamId)
        {
            string gameName = GetGameName(gameTimePlayed.appid);
            int playtime = (int) Math.Round(gameTimePlayed.playtime_forever / (double) 60);
            UserAchievementStats achievementStats = await CalculateAchievements(gameTimePlayed.appid, steamId);
            GameInfo gameInfo = new GameInfo {
                GameName = gameName,
                AppId = gameTimePlayed.appid,
                GamePlaytime = playtime,
                AchievementStats = achievementStats
            };

            return gameInfo;

        }

        public async Task<string> GetSteamIdFromVanityUrl(string steamId)
        {
            try
            {
                string apiURL = $"http://api.steampowered.com/ISteamUser/ResolveVanityURL/v0001/?key={ApiKey}&vanityurl={steamId}";
                HttpResponseMessage response = await HttpClient.GetAsync(apiURL);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                // Above three lines can be replaced with new helper method below
                // string responseBody = await client.GetStringAsync(uri);

                // gamesOwnedDetails is of JObject type
                dynamic responseObj = JsonConvert.DeserializeObject(responseBody);

                if(responseObj["response"]["success"] == 1)
                {
                    return responseObj["response"]["steamid"];
                }

                return null;

                //return Content("Player with that id does not exist.");

            }
            catch (HttpRequestException e)
            {
                DebugLog("\nException Caught!");
                DebugLog("From GetSteamIdFromVanityUrl");
                DebugLog($"Message :{e.Message}");
                //var error = JsonConvert.DeserializeObject(e.Message);
                return null;
            }
        }

        public async Task<string> ProcessSteamId(string steamId)
        {
            string processedSteamId = steamId;

            int testInt = 0;

            bool steamIdIsNumber = int.TryParse(steamId, out testInt);

            if (!steamIdIsNumber)
            {
                processedSteamId = await GetSteamIdFromVanityUrl(steamId);

                // processedSteamId should not be null unless steamId is numeric or the steamId is garbage
                if(processedSteamId == null)
                {
                    processedSteamId = steamId;
                }
            }

            return processedSteamId;
        }

        public async Task<IActionResult> GetOwnedGames(string steamId)
        {
            // Call asynchronous network methods in a try/catch block to handle exceptions.
            try
            {
                //string steamId = "76561198055381702";

                DebugLog($"original steamId: {steamId}");

                string processedSteamId = await ProcessSteamId(steamId);

                DebugLog($"processed Steam Id: {processedSteamId}");

                dynamic profileInfo = await GetProfileInfo(processedSteamId);
                if(profileInfo == null)
                {
                    return Content("No steam user with that steam ID exists.");
                }

                if(profileInfo["communityvisibilitystate"] != 3)
                {
                    return Content("User's profile is set to private.");
                }

                string apiURL = $"http://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key={ApiKey}&steamid={processedSteamId}&format=json";
                HttpResponseMessage response = await HttpClient.GetAsync(apiURL);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                // Above three lines can be replaced with new helper method below
                // string responseBody = await client.GetStringAsync(uri);

                // gamesOwnedDetails is of JObject type
                dynamic gamesOwnedDetails = JsonConvert.DeserializeObject(responseBody);

                // gamesArray of JArray type
                var gamesArray = gamesOwnedDetails["response"]["games"];

                //DebugLog($"Type of games Array: {gamesArray.GetType()}");

                List<GameTimePlayed> gamesList = gamesArray.ToObject<List<GameTimePlayed>>();
                List<GameInfo> gameInfos = new List<GameInfo>();

                List<Task<GameInfo>> tasks = new List<Task<GameInfo>>();


                foreach (GameTimePlayed game in gamesList)
                {
                    var task = ConvertGameTimePlayedToGameInfo(game, steamId);
                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);

                foreach(var task in tasks)
                {
                    var gameInfo = task.Result;
                    gameInfos.Add(gameInfo);
                }

                var ordGameInfos = gameInfos.OrderByDescending(gameInfo => gameInfo.GamePlaytime).ToList();

                //DebugLog($"Type of gamesList: {gamesList.GetType()}");

                //var gamesListOrdByPlaytime = gamesList.OrderByDescending(game => game.playtime_forever).ToList();

                //ViewData["GamesPlayed"] = gamesListOrdByPlaytime;

               var allGamesPlayed = new GamesPlayed
               {
                   GameInfos = ordGameInfos
               };

                //dynamic achObj = await GetAchievementsForGame(99900);

                //DebugLog($"Game name: {achObj["playerstats"]["gameName"]}");

                return View(allGamesPlayed);
                

                //return Content("Player with that id does not exist.");

            }
            catch (HttpRequestException e)
            {
                DebugLog("\nException Caught!");
                DebugLog("From GetOwnedGames");
                DebugLog($"Message :{e.Message}");
                //var error = JsonConvert.DeserializeObject(e.Message);
                return Content(e.Message);
            }
        }

        public async Task<JObject> GetAchievementsForGame(int appid, string steamId)
        {
            try
            {
                //string steamID = "76561198055381702";

                string apiURL = $"http://api.steampowered.com/ISteamUserStats/GetUserStatsForGame/v0002/?appid={appid}&key={ApiKey}&steamid={steamId}";
                HttpResponseMessage response = await HttpClient.GetAsync(apiURL);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                // Above three lines can be replaced with new helper method below
                // string responseBody = await client.GetStringAsync(uri);

                // gamesOwnedDetails is of JObject type
                dynamic achievementList = JsonConvert.DeserializeObject(responseBody);

                return achievementList;

                //return Content("Player with that id does not exist.");

            }
            catch (HttpRequestException e)
            {
                DebugLog("\nException Caught!");
                DebugLog("From GetAchievementsForGame");
                DebugLog($"Message :{e.Message}");
                //var error = JsonConvert.DeserializeObject(e.Message);
                return null;
            }
        }

        
        public async Task<UserAchievementStats> CalculateAchievements(int appid, string steamId)
        {
            DebugLog($"App id: {appid}");
            dynamic achievementsObj = await GetAchievementsForGame(appid, steamId);
            dynamic gameInfoObj = await GetGameInfo(appid);

            //DebugLog($"{gameInfoObj.ToString()}");
            //DebugLog($"{achievementsObj.ToString()}");

            // error checking for no achievement information available (ex. appid = 10, game = Counter-Strike)
            if(gameInfoObj["game"] == null || achievementsObj == null || achievementsObj["playerstats"]["achievements"] == null)
            {
                UserAchievementStats achievementStats = new UserAchievementStats
                {
                    NumUserAchievements = 0,
                    NumGameAchievements = 0,
                    AchievementPercentage = 0
                };

                return achievementStats;
            } else
            {
                int numUserAchievements = (int)achievementsObj["playerstats"]["achievements"].Count;
                DebugLog($"num user achivements: {numUserAchievements}");
                //DebugLog($"type of numUserAchievements: {achievementsObj["playerstats"]["achievements"].GetType()}");
                //DebugLog($"user achievements: {achievementsObj["playerstats"]["achievements"].ToString()}");

                int numGameAchievements = (int) gameInfoObj["game"]["availableGameStats"]["achievements"].Count;

                DebugLog($"num game achivements: {numGameAchievements}");

                //DebugLog($"type of numGameAchievements: {gameInfoObj["game"]["availableGameStats"]["achievements"].GetType()}");
                //DebugLog($"game achievements: {gameInfoObj["game"]["availableGameStats"]["achievements"].ToString()}");

                int achievementPercentage = (int) Math.Round((numUserAchievements / (double) numGameAchievements) * 100);

                UserAchievementStats achievementStats = new UserAchievementStats
                {
                    NumUserAchievements = numUserAchievements,
                    NumGameAchievements = numGameAchievements,
                    AchievementPercentage = achievementPercentage
                };

                return achievementStats;
            }
        }
    }
}