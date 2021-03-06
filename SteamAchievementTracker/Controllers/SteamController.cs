﻿using System;
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

        public async Task<string> GetSteamName(string steamId)
        {
            var processedSteamId = await ProcessSteamId(steamId);
            var profileInfo = await GetProfileInfo(processedSteamId);

            if(profileInfo != null)
            {
                return (string) profileInfo["personaname"];
            }

            return "";
        }

        public async Task<JObject> GetProfileInfo(string steamId)
        {
            // Call asynchronous network methods in a try/catch block to handle exceptions.
            try
            {
                string apiURL = $"http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={ApiKey}&steamids={steamId}";
                string responseBody = await HttpClient.GetStringAsync(apiURL);

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
                string responseBody = await HttpClient.GetStringAsync(apiURL);

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
                string responseBody = await HttpClient.GetStringAsync(apiURL);

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
                string responseBody = await HttpClient.GetStringAsync(apiURL);

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
                DebugLog($"{jsonData["applist"]["apps"].Count}");
                GameList = jsonData["applist"]["apps"].ToObject<List<AppInfo>>();
            }
        }

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
                string responseBody = await HttpClient.GetStringAsync(apiURL);

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

        // Retrieving games for the provided steam ID or customurl
        public async Task<IActionResult> GetOwnedGames(string steamId)
        {
            // Call asynchronous network methods in a try/catch block to handle exceptions.
            try
            {

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

                var allGamesPlayed = await GetGamesPlayedByUser(processedSteamId, true);
                if(allGamesPlayed == null)
                {
                    return Content("Some error occurred in retrieving games for that steam ID.");
                }

                return View(allGamesPlayed);
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

        // Want to be able to display games that each person has in common with other person
        // Should be able to also see playtimes of each person ???? (maybe)
        public async Task<IActionResult> GetGamesInCommon(string steamId1, string steamId2)
        {
            try
            {
                var gamesPlayedUser1 = await GetGamesPlayedByUser(steamId1, false);
                var gamesPlayedUser2 = await GetGamesPlayedByUser(steamId2, false);

                if (gamesPlayedUser1 == null || gamesPlayedUser2 == null)
                {
                    if(gamesPlayedUser1 == null && gamesPlayedUser2 == null)
                    {
                        return Content($"{steamId1} and {steamId2} both had issues retrieving data associated with their accounts.");
                    }
                    if(gamesPlayedUser1 == null)
                    {
                        return Content($"{steamId1} either has a private profile, steam user with that id does not exist, or some other error occurred.");
                    }
                    if (gamesPlayedUser2 == null)
                    {
                        return Content($"{steamId2} either has a private profile, steam user with that id does not exist, or some other error occurred.");
                    }

                }

                var gameInfos1 = gamesPlayedUser1.GameInfos;
                var gameInfos2 = gamesPlayedUser2.GameInfos;

                var gamesInCommonList = new List<CommonGame>();

                // key is game name, int is hours of playtime
                var gameDict = new Dictionary<string, int>();

                foreach (GameInfo gameInfo in gameInfos1)
                {
                    gameDict.Add(gameInfo.GameName, gameInfo.GamePlaytime);
                }

                foreach (GameInfo gameInfo in gameInfos2)
                {
                    if (gameDict.ContainsKey(gameInfo.GameName))
                    {
                        CommonGame commonGame = new CommonGame
                        {
                            GameName = gameInfo.GameName,
                            PlaytimeUser1 = gameDict[gameInfo.GameName],
                            PlaytimeUser2 = gameInfo.GamePlaytime
                        };
                        gamesInCommonList.Add(commonGame);
                    }
                }

                var gamesInCommon = new GamesInCommon
                {
                    SteamName1 = await GetSteamName(steamId1),
                    SteamName2 = await GetSteamName(steamId2),
                    CommonGames = gamesInCommonList
                };

                return View(gamesInCommon);
            }
            catch
            {
                DebugLog("\nException Caught!");
                DebugLog("From GetGamesInCommon");
                return Content("Error while calculating games that users share.");
            }
            
        }


        public async Task<GamesPlayed> GetGamesPlayedByUser(string steamId, bool isProcessed)
        {
            try
            {
                string processedSteamId;

                if(!isProcessed)
                {
                    DebugLog($"original steamId: {steamId}");

                    processedSteamId = await ProcessSteamId(steamId);

                    DebugLog($"processed Steam Id: {processedSteamId}");

                    dynamic profileInfo = await GetProfileInfo(processedSteamId);
                    if (profileInfo == null)
                    {
                        return null;
                    }

                    if (profileInfo["communityvisibilitystate"] != 3)
                    {
                        return null;
                    }
                } else
                {
                    processedSteamId = steamId;
                }

                string apiURL = $"http://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key={ApiKey}&steamid={processedSteamId}&format=json";
                string responseBody = await HttpClient.GetStringAsync(apiURL);

                // gamesOwnedDetails is of JObject type
                dynamic gamesOwnedDetails = JsonConvert.DeserializeObject(responseBody);

                // gamesArray of JArray type
                var gamesArray = gamesOwnedDetails["response"]["games"];

                //DebugLog($"Type of games Array: {gamesArray.GetType()}");

                List<GameTimePlayed> gamesList = gamesArray.ToObject<List<GameTimePlayed>>();

                List<GameInfo> gameInfos = await ConvertGameTimePlayedListToGameInfoList(gamesList, steamId);


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

                return allGamesPlayed;


                //return Content("Player with that id does not exist.");

            }
            catch (HttpRequestException e)
            {
                DebugLog("\nException Caught!");
                DebugLog("From GetGamesPlayedByUser");
                DebugLog($"Message :{e.Message}");
                //var error = JsonConvert.DeserializeObject(e.Message);
                // returm empty list
                return null;
            }
        }

        public async Task<List<GameInfo>> ConvertGameTimePlayedListToGameInfoList(List<GameTimePlayed> gamesList, string steamId)
        {
            List<GameInfo> gameInfos = new List<GameInfo>();

            List<Task<GameInfo>> tasks = new List<Task<GameInfo>>();


            foreach (GameTimePlayed game in gamesList)
            {
                var task = ConvertGameTimePlayedToGameInfo(game, steamId);
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            foreach (var task in tasks)
            {
                var gameInfo = task.Result;
                gameInfos.Add(gameInfo);
            }

            return gameInfos;
        }

        public async Task<JObject> GetAchievementsForGame(int appid, string steamId)
        {
            try
            {

                string apiURL = $"http://api.steampowered.com/ISteamUserStats/GetUserStatsForGame/v0002/?appid={appid}&key={ApiKey}&steamid={steamId}";
                string responseBody = await HttpClient.GetStringAsync(apiURL);

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