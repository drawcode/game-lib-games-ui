using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 
 GAME COMMUNITY
 
 Simple
  - Uses facebook login
  - Uses platform profiles, statistics, achievements for 
    local persistence in streaming assets/documents cache
  - Get statistics lists
  - Get Facebook scores for app
  - Get Facebook scores for user and friends
  
  Advanced
  - See advanced for custom online leaderboards and more stats, also achievements.
  
  This sample was setup for any app that needs stats and one stat score aggregated 
  and sent to facebook scores (facebook limitation of only one stat per app)
  
  bool GameCommunity.isLoggedIn
  or
  bool GameCommunity.networkLoggedIn
   -check logged in state (will automatically login after initial)
   
  string GameCommunity.networkUserId
   - userId from current network
   
  string GameCommunity.networkUserName
   - username from current network
   
  string GameCommunity.networkFullName
   - first and last name from current network
  
  string GameCommunity.networkAuth
  - auth code from current network
  
  string GameCommunity.networkAppAuth
  - app auth code from current network
  
  string GameCommunity.networkType
  - type from current network 'facebook', 'twitter', 'custom' etc.
  
  ## Facebook needed calls
     
  GameCommunity.Login() 
   - put on facebook button, currently setup for facebook default, 
     other networks are Login[network]()
     
  GameCommunity.SetHighScore(int score)
   - Facebook specific scores api set for single supported facebook score
     uses 'high-score' statistic in statistics list 
     (StreamingAssets/[app]/[app folder name ("vehicle-shoot")]/version/shared/data/statistics-data.json)
   - Auto calls GameCommunity.SendStatistics(), other stats this needs to be called 
     explicitely to send to other networks
     
  int GameCommunity.GetHighScore() 
   - Get local high score and last sent to facebook scores api as int
   - Uses 'high-score' statistic in statistics list 
     (StreamingAssets/[app]/[app folder name ("vehicle-shoot")]/version/shared/data/statistics-data.json)
     
  int GameCommunity.GetHighScore() 
   - Get high score formatted as .ToString("N0") or 1000 becomes 1,000
 
  GameCommunity.SendStatistics()
   - Send statistics not sent to all networks
      
  List<GameCommunityStatisticItem> GameCommunity.GetStatistics()
   - Get GameCommunityStatisticItem list for display
   
  List<GameCommunityAchievementItem> GameCommunity.GetAchievements()
   - Get GameCommunityAchievementItem list for display
      
  List<GameCommunityLeaderboardItem> GameCommunity.GetLeaderboards()
   - Get GameCommunityLeaderboardItem list for display
      
  List<GameCommunityLeaderboardItem> GameCommunity.GetLeaderboardFriends()
   - Get GameCommunityLeaderboardItem list for display   
   
  ## Extended methods for local/online stats/achievements/leaderboards
   
  GameCommunity.SetStatisticValue(string code, float value)
   - Set another stat by code and float 
   - statistic in statistics list 
     (StreamingAssets/[app]/[app folder name ("vehicle-shoot")]/version/shared/data/statistics-data.json)
     
  double GameCommunity.GetStatisticValue(string statisticCode)
   - Get another stat by statisticCode or 'code' in data
   - statistic in statistics list 
     (StreamingAssets/[app]/[app folder name ("vehicle-shoot")]/version/shared/data/statistics-data.json)
     
  bool GameCommunity.SetAchievement(string achievementCode, bool completed)
   - Set achievement by achievementCode or 'code' in data
   - achievement in achievements list 
     (StreamingAssets/[app]/[app folder name ("vehicle-shoot")]/version/shared/data/achievements-data.json)
     
  bool GameCommunity.GetAchievement(string achievementCode)
   - Get achievement by achievementCode or 'code' in data 
   - achievement in achievements list 
     (StreamingAssets/[app]/[app folder name ("vehicle-shoot")]/version/shared/data/achievements-data.json)
     
  bool GameCommunity.GetAchievement(string achievementCode)
   - Get achievement by achievementCode or 'code' in data 
   - achievement in achievements list 
     (StreamingAssets/[app]/[app folder name ("vehicle-shoot")]/version/shared/data/achievements-data.json)

*/

public enum GameCommunityNetworkLoginState {
    none,
    loggingIn
}

public class GameCommunity {
    
    private static volatile GameCommunity instance;
    private static System.Object syncRoot = new System.Object();
    private GameCommunitySyncData syncData = new GameCommunitySyncData();
    public Dictionary<string,GameCommunityNetworkLoginState> networkProcessStates;

    public static GameCommunity Instance {
        get {
            if (instance == null) { 
                lock (syncRoot) {
                    if (instance == null) 
                        instance = new GameCommunity();
                }
            }   
            return instance;
        }
    }

    public GameCommunity() {
        Init();
    }

    public void Init() {
        networkProcessStates = new Dictionary<string, GameCommunityNetworkLoginState>();
    }

    public bool IsNetworkLoggingIn(string networkType) {

        GameCommunityNetworkLoginState keyState = 
            networkProcessStates.Get(networkType);

        //if (keyState != null) {
        if (keyState == GameCommunityNetworkLoginState.loggingIn) {
            return true;
        }
        //}

        return false;
    }

    public GameCommunityNetworkLoginState GetNetworkLoginState(string networkType) {
        
        GameCommunityNetworkLoginState keyState = networkProcessStates.Get(networkType);
        
        return keyState;
    }

    public void SetNetworkLoginState(string networkType, GameCommunityNetworkLoginState loginState) {

        networkProcessStates.Set(networkType, loginState);
    }

    // LOGIN
    
    public static bool IsLoggedIn(string networkType) {
        if (Instance != null) {
            return Instance.isLoggedIn(networkType);
        }
        return false;
    }
    
    public bool isLoggedIn(string networkType) {

        string networkUserName = GameProfiles.Current.GetNetworkValueUsername(networkType);

        if (networkType == SocialNetworkTypes.facebook) {
            bool isLoggedInFacebook = SocialNetworks.IsLoggedInFacebook();
            
            Debug.Log(">>>>>>> isLoggedIn:networkType: " + networkType + "\r\n\r\n\r\n");     
            Debug.Log(">>>>>>> isLoggedIn:networkUserName: " + networkUserName + "\r\n\r\n\r\n");
            Debug.Log(">>>>>>> isLoggedIn:isLoggedInFacebook: " + isLoggedInFacebook + "\r\n\r\n\r\n");
            
            if (!string.IsNullOrEmpty(networkType)) {
                if (!string.IsNullOrEmpty(networkUserName)) {
                    if (isLoggedInFacebook) {
                        return true;
                    }
                }
            }
        }
        else if (networkType == SocialNetworkTypes.twitter) {
            bool isLoggedInTwitter = SocialNetworks.IsLoggedInTwitter();
            
            Debug.Log(">>>>>>> isLoggedIn:networkType: " + networkType + "\r\n\r\n\r\n");     
            Debug.Log(">>>>>>> isLoggedIn:networkUserName: " + networkUserName + "\r\n\r\n\r\n");
            Debug.Log(">>>>>>> isLoggedIn:isLoggedInTwitter: " + isLoggedInTwitter + "\r\n\r\n\r\n");
                        
            if (!string.IsNullOrEmpty(networkType)) {
                if (!string.IsNullOrEmpty(networkUserName)) {
                    if (isLoggedInTwitter) {
                        return true;
                    }
                }
            }
        }

        return false;
    }
        
    public static void Login(string networkType) {
        if (Instance != null) {
            Instance.login(networkType);
        }
    }
    
    public void login(string networkType) {
        
        Debug.Log("login: isLoggingIn:" + isLoggedIn(networkType).ToString());
        if (networkType == SocialNetworkTypes.facebook) {
            Debug.Log("login: IsLoggedInFacebook:" + SocialNetworks.IsLoggedInFacebook().ToString());
        }
        else if (networkType == SocialNetworkTypes.twitter) {
            Debug.Log("login: IsLoggedInTwitter:" + SocialNetworks.IsLoggedInTwitter().ToString());
        }
        
        if (!IsLoggingIn(networkType)) {

            SetNetworkLoginState(networkType, GameCommunityNetworkLoginState.loggingIn);
        
            GameCommunity.TrackGameView("Logging In", "logging-in");
            GameCommunity.TrackGameEvent("logging-in", "login", 1);
            
            if (networkType == SocialNetworkTypes.facebook) {
                loginFacebook();
            }
            else if (networkType == SocialNetworkTypes.twitter) {
                loginTwitter();                
            }
            
        }
    }
    
    public static bool IsLoggingIn(string networkType) {
        if (Instance != null) {
            return Instance.IsNetworkLoggingIn(networkType);
        }

        return false;
    }

    public static void ResetLoggingIn(string networkType) {
        if (Instance != null) {
            Instance.resetLoggingIn(networkType);
        }
    }
    
    public void resetLoggingIn(string networkType) {
        SetNetworkLoginState(networkType, GameCommunityNetworkLoginState.none);
    }
    
    private void loginFacebook() {
        LogUtil.Log("Logging in with Facebook");
        SocialNetworks.ShowLoginFacebook();
    }

    private void loginTwitter() {
        LogUtil.Log("Logging in with Twitter");
        SocialNetworks.ShowLoginTwitter();
    }
    
    public static bool ShouldSetupGame() {

        string networkType = SocialNetworkTypes.facebook;

        bool loggedIn = GameCommunity.IsLoggedIn(networkType); 
        
        if (!Context.Current.hasNetworkAccess) {
            return true;    
        }
        
        LogUtil.Log(">>>>>>> ShowLoginOrSetupGame:loggedIn" + loggedIn.ToString() + "\r\n\r\n\r\n");
        
        if (!loggedIn) {
            GameCommunity.ShowGameCommunityLogin();
        }
        
        GameCommunity.ProcessTrackers();
        
        return loggedIn;
    }

    // attributes
    
    
    // profile attributes - user info, gender, dob etc. Global across all games.
    
    // game profile attributes - game specific profile attributes like tracking or stat info, specific to this game.
    
    // profile game data attributes - local profile DataAttribute sync
    
    // set stats
    
    public static void SetStatisticValue(string statisticCode, int statisticValue) {
        if (Instance != null) {
            Instance.setStatisticValue(statisticCode, (float)statisticValue);
        }
    }
    
    public static void SetStatisticValue(string statisticCode, double statisticValue) {
        if (Instance != null) {
            Instance.setStatisticValue(statisticCode, (float)statisticValue);
        }
    }
    
    public static void SetStatisticValue(string statisticCode, float statisticValue) {
        if (Instance != null) {
            Instance.setStatisticValue(statisticCode, statisticValue);
        }
    }
    
    public void setStatisticValue(string statisticCode, float statisticValue) {
        
        GameCommunity.TrackGameEvent("game-stats", statisticCode, (int)statisticValue);
        
        // queue for sending since it was updated
        GameCommunityTrackingController.SetSyncStatistic(statisticCode, statisticValue);        
        // save local
        GamePlayerProgress.Instance.SetStatisticValue(statisticCode, statisticValue);
    }
    
    // get stats
    
    public static int GetStatisticValueInt(string statisticCode) {
        if (Instance != null) {
            return Instance.getStatisticValueInt(statisticCode);
        }
        return 0;
    }
    
    public int getStatisticValueInt(string statisticCode) {
        return (int)GameProfileStatistics.Current.GetStatisticValue(statisticCode);
    }
    
    public static double GetStatisticValueDouble(string statisticCode) {
        if (Instance != null) {
            return Instance.getStatisticValueDouble(statisticCode);
        }
        return 0;
    }
    
    public double getStatisticValueDouble(string statisticCode) {
        if (Instance != null) {
            return GameProfileStatistics.Current.GetStatisticValue(statisticCode);
        }
        return 0;
    }
    
    public static float GetStatisticValueFloat(string statisticCode) {
        return Instance.getStatisticValueFloat(statisticCode);
    }
    
    public float getStatisticValueFloat(string statisticCode) {
        return (float)GameProfileStatistics.Current.GetStatisticValue(statisticCode);
    }
    
    public static string GetStatisticValueFormatted(string statisticCode) {
        if (Instance != null) {
            return Instance.getStatisticValueFormatted(statisticCode);
        }
        return "";
    }
    
    public string getStatisticValueFormatted(string statisticCode) {
        GameStatistic statistic = GameStatistics.Instance.GetById(statisticCode);
        if (statistic == null) {
            statistic = new GameStatistic();
            statistic.code = statisticCode;
            statistic.type = "int";
        }
        double statValue = GameProfileStatistics.Current.GetStatisticValue(statisticCode);
        return GameStatistics.Instance.GetStatisticDisplayValue(statistic, statValue);
    }
    
    // achievements
    
    public static void SetAchievementValue(string achievementCode, bool completed) {
        if (Instance != null) {
            Instance.setAchievementValue(achievementCode, completed ? 1.0f : 0.0f);
        }
    }
    
    public static void SetAchievementValue(string achievementCode, float achievementValue) {
        if (Instance != null) {
            Instance.setAchievementValue(achievementCode, (float)achievementValue);
        }
    }
    
    public void setAchievementValue(string achievementCode, float achievementValue) {
        GamePlayerProgress.Instance.SetAchievement(achievementCode, true);//achievementValue);
    }
    
    // simple helpers
    
    public static void SetHighScore(int highScore) {
        if (Instance != null) {
            Instance.getHighScore(highScore);
        }
    }
    
    public void getHighScore(int highScore) {
        
        int currentScore = GetHighScore();
        
        if (highScore > currentScore) {
            
            GameCommunityTrackingController.SetSyncStatistic(
                AppConfigs.socialStatisticForFacebook,
                (float)currentScore);
        }
        
        if (highScore > 0) {
            SetStatisticValue(
                AppConfigs.socialStatisticForFacebook,
                highScore);
        }
        
        GameState.SaveProfile();
    }
    
    // --------------------------------------------------------------

    // PROGRESS

    // points   
    
    public static void SetPoints(int points) {
        if (Instance != null) {
            Instance.getPoints(points);
        }
    }
    
    public void getPoints(int points) {
        
        int currentScore = GetPoints();
        
        if (points > currentScore) {
            
            GameCommunityTrackingController.SetSyncStatistic(
                AppConfigs.socialStatisticForFacebook,
                (float)currentScore);
        }
        
        if (currentScore > 0) {
            SetStatisticValue(
                AppConfigs.socialStatisticForFacebook,
                currentScore);
        }
        
        GameState.SaveProfile();
    }
    
    // rank
    
    public static int GetRank() {
        if (Instance != null) { 
            return Instance.getRank();
        }
        return 0;
    }
    
    public int getRank() {
        return GetStatisticValueInt(GameCommunityStatisticCodes.currentRank);
    }
    
    public static int GetRankStat(string statCode) {
        if (Instance != null) { 
            return Instance.getRankStat(statCode);
        }
        return 0;
    }
    
    public int getRankStat(string statCode) {
        return GetStatisticValueInt(GameCommunityStatisticCodes.currentRank + "-" + statCode);
    }
    
    // rank total
    
    public static int GetRankTotal() {
        if (Instance != null) { 
            return Instance.getRankTotal();
        }
        return 0;
    }
    
    public int getRankTotal() {
        return GetStatisticValueInt(GameCommunityStatisticCodes.currentRankTotal);
    }
    
    public static int GetRankTotalStat(string statCode) {
        if (Instance != null) { 
            return Instance.getRankTotalStat(statCode);
        }
        return 0;
    }
    
    public int getRankTotalStat(string statCode) {
        return GetStatisticValueInt(GameCommunityStatisticCodes.currentRankTotal + "-" + statCode);
    }
    
    // set rank
    
    public static void SetRank(double statValue) {
        if (Instance != null) { 
            Instance.setRank(statValue);
        }
    }
    
    public void setRank(double statValue) {

        // Update user rank.
        GameProfileStatistics.Current.SetStatisticValue(
            GameCommunityStatisticCodes.topRank, statValue);
        GameProfileStatistics.Current.SetStatisticValue(
            GameCommunityStatisticCodes.currentRank, statValue);
        
        SetRankStat(GameCommunityStatisticCodes.topRank + "-" + GameCommunityStatisticCodes.highScore, statValue);
        SetRankStat(GameCommunityStatisticCodes.currentRank + "-" + GameCommunityStatisticCodes.highScore, statValue);
    }
    
    public static void SetRankStat(string statCode, double statValue) {
        if (Instance != null) { 
            Instance.setRankStat(statCode, statValue);
        }       
    }
        
    public void setRankStat(string statCode, double statValue) {
        if (statCode == GameCommunityStatisticCodes.highScore) {
            SetRank(statValue);
        }
        else {      
            GameProfileStatistics.Current.SetStatisticValue(
                GameCommunityStatisticCodes.topRank + "-" + statCode, statValue);
            GameProfileStatistics.Current.SetStatisticValue(
                GameCommunityStatisticCodes.currentRank + "-" + statCode, statValue);   
        }
    }
    
    // set rank total
    
    public static void SetRankTotal(double statValue) {
        if (Instance != null) { 
            Instance.setRankTotal(statValue);
        }
    }
    
    public void setRankTotal(double statValue) {

        // Update user rank.
        GameProfileStatistics.Current.SetStatisticValue(
            GameCommunityStatisticCodes.currentRankTotal, statValue);
        
        SetRankStat(GameCommunityStatisticCodes.currentRankTotal + "-" + GameCommunityStatisticCodes.highScore, statValue);
    }
    
    public static void SetRankTotalStat(string statCode, double statValue) {
        if (Instance != null) { 
            Instance.setRankTotalStat(statCode, statValue);
        }       
    }
        
    public void setRankTotalStat(string statCode, double statValue) {
        if (statCode == GameCommunityStatisticCodes.highScore) {
            setRankTotal(statValue);
        }
        else {
            GameProfileStatistics.Current.SetStatisticValue(
                GameCommunityStatisticCodes.currentRankTotal + "-" + statCode, statValue);  
        }
    }
    
    // get high score
    
    public static int GetHighScore() {
        if (Instance != null) {
            return Instance.getHighScore();
        }
        return 0;
    }

    public int getHighScore() {
        return GetStatisticValueInt(
            AppConfigs.socialStatisticForFacebook);
    }
    
    public static string GetHighScoreFormatted() {
        if (Instance != null) {
            return Instance.getHighScoreFormatted();
        }
        return "";
    }
    
    public string getHighScoreFormatted() {
        return GetStatisticValueFormatted(
            AppConfigs.socialStatisticForFacebook);
    }
    
    
    // get high score
    
    public static int GetPoints() {
        if (Instance != null) {
            return Instance.getPoints();
        }
        return 0;
    }

    public int getPoints() {
        return GetStatisticValueInt(
            AppConfigs.socialStatisticForFacebook);
    }
    
    public static string GetPointsFormatted() {
        if (Instance != null) {
            return Instance.getPointsFormatted();
        }
        return "";
    }
    
    public string getPointsFormatted() {
        return GetStatisticValueFormatted(
            AppConfigs.socialStatisticForFacebook);
    }
    
    // stats
    
    public static List<GameCommunityStatisticItem> GetStatistics() {
        if (Instance != null) {
            return Instance.getStatistics();
        }
        return null;
    }
    
    public List<GameCommunityStatisticItem> getStatistics() {
        return new List<GameCommunityStatisticItem>();
    }
    
    // achievements
   
    public static List<GameCommunityAchievementItem> GetAchievements() {
        if (Instance != null) {
            return Instance.getAchievements();  
        }
        return null; 
    }
    
    public List<GameCommunityAchievementItem> getAchievements() {
        return new List<GameCommunityAchievementItem>();
    }
    
    // leaderboards / requests
    
    public enum GameLeaderboardType {
        ALL,
        DAILY,
        WEEKLY,
        MONTHLY
    }   
    
    public static void RequestLeaderboardsDefaultCode(int page, int pageSize, GameLeaderboardType leaderboardType) {
        RequestLeaderboards(AppConfigs.socialStatisticForFacebook,
            page, pageSize, leaderboardType);
    }
      
    public static void RequestLeaderboards(string code, int page, int pageSize, GameLeaderboardType leaderboardType) {
        
        string rangeType = "all";
        
        if (leaderboardType == GameLeaderboardType.DAILY) {
            rangeType = "daily";
        }
        else if (leaderboardType == GameLeaderboardType.WEEKLY) {
            rangeType = "weekly";
        }
        else if (leaderboardType == GameLeaderboardType.MONTHLY) {
            rangeType = "monthly";
        }       
        
        RequestLeaderboards(
            code, page, pageSize, rangeType);
        
    }
    
    public static void RequestLeaderboardUser(string code, int page, int pageSize, GameLeaderboardType leaderboardType, string username) {
        
        string rangeType = "all";
        
        if (leaderboardType == GameLeaderboardType.DAILY) {
            rangeType = "daily";
        }
        else if (leaderboardType == GameLeaderboardType.WEEKLY) {
            rangeType = "weekly";
        }
        else if (leaderboardType == GameLeaderboardType.MONTHLY) {
            rangeType = "monthly";
        }       
        
        RequestLeaderboardUser(
            code, page, pageSize, rangeType, username);
        
    }
    
    public static void RequestLeaderboards(string code, int page, int pageSize, string rangeType) {
                
        GameCommunityService.GetLeaderboardFull(
            code, page, pageSize, rangeType);
        
        // If just facebook scores are on
        //GameNetworks.GetScoresFacebook();
    }
    
    public static void RequestLeaderboardUser(string code, int page, int pageSize, string rangeType, string username) {
                
        GameCommunityService.GetLeaderboardUser(
            code, page, pageSize, rangeType, username);
    }
    
    public static void RequestLeaderboardFriends() {
        GameNetworks.GetScoresFacebookFriends();
    }
    
    // --------------------------------------------------------------

    // PROGRESS

    // statistics
    
    
    /*
    public static List<GameCommunityStatisticItem> GetProfileStatisticItems() {
        return GameCommunity.GetProfileStatisticData().statistics;
    }
    
    
    public static GameCommunityStatisticData GetProfileStatisticData() {
        GameCommunityStatisticData statisticData = new GameCommunityStatisticData();
        
        foreach(GameStatistic statistic in GameStatistics.Instance.GetAll()) {
            GameCommunityStatisticItem item = new GameCommunityStatisticItem();
            
            item.code = statistic.code;
            item.displayName = statistic.display_name;
            item.description = statistic.description;
            
            double statValue = GameProfileStatistics.Current.GetStatisticValue(statistic.code);
            string displayValue = GameStatistics.Instance.GetStatisticDisplayValue(statistic, statValue);
            
            item.value = (float)statValue;
            item.valueFormatted = displayValue;
            item.type = statistic.type;
            statisticData.statistics.Add(item);
        }
        
        return statisticData;
    }
    */
    
    
    public static GameCommunityAchievementData GetProfileAchievementData(
        string appPackCode, string app_content_state) {
        
        LogUtil.Log("GameCommunityAchievementData:appPackCode:", appPackCode);
        LogUtil.Log("GameCommunityAchievementData:app_content_state:", app_content_state);
        
        GameCommunityAchievementData achievementData = new GameCommunityAchievementData();
        //string app_state = AppContentStates.Instance.GetById(app_content_state).app_states[0];
        
        foreach (GameAchievement achievement in GameAchievements.Instance.GetListByPackByAppContentState(appPackCode, app_content_state)) {
                    
            if (achievement.active) {
            
                GameCommunityAchievementItem item = new GameCommunityAchievementItem();
                
                //achievement.description 
                //  = GameAchievements.Instance.FormatAchievementTags(
                //      app_state, 
                //      app_content_state, 
                //      achievement.description);
                        
                item.code = achievement.code;
                item.displayName = achievement.display_name;
                item.description = achievement.description;
                //item.completed = achievement;
                
            
                LogUtil.Log("GetProfileAchievementData:achievement.code:", achievement.code);
                LogUtil.Log("GetProfileAchievementData:app_content_state:", app_content_state);
                string achievementCode = achievement.code + "-" + app_content_state;
                LogUtil.Log("GetProfileAchievementData:achievementCode:", achievementCode);
            
                bool completed = GameProfileAchievements.Current.GetAchievementValue(achievementCode);
            
                LogUtil.Log("GetProfileAchievementData:completed:", completed);
                LogUtil.Log("GetProfileAchievementData:type:", achievement.type);
            
                item.completed = completed;
                item.type = achievement.data.type;
                item.points = achievement.data.points;
                achievementData.achievements.Add(item);
            }
        }
        
        return achievementData;
    }
    
    public static GameCommunityStatisticData GetProfileStatisticData(
        string appPackCode, string app_content_state) {
        
        LogUtil.Log("GetProfileStatisticData:app_content_state:", app_content_state);
        
        GameCommunityStatisticData statisticData = new GameCommunityStatisticData();
        
        /*
        foreach(GameStatistic statistic in GameStatistics.Instance.GetListByPackAndContentState(
            appPackCode, app_content_state)) {
            
            if(statistic.active) {
                
                GameCommunityStatisticItem item = new GameCommunityStatisticItem();
                        
                item.code = statistic.code;
                item.displayName = statistic.display_name;
                item.description = statistic.description;           
            
                LogUtil.Log("GetProfileStatisticData:statistic.code:", statistic.code);
                LogUtil.Log("GetProfileStatisticData:app_content_state:", app_content_state);
                string statisticCode = statistic.code + "-" + app_content_state;
                LogUtil.Log("GetProfileStatisticData:statisticCode:", statisticCode);
            
                double statValue = GameProfileStatistics.Current.GetStatisticValue(statisticCode);
            
                LogUtil.Log("GetProfileStatisticData:statValue:", statValue);
                string displayValue = GameStatistics.Instance.GetStatisticDisplayValue(statistic, statValue);
                
                LogUtil.Log("GetProfileStatisticData:displayValue:", displayValue);
                LogUtil.Log("GetProfileStatisticData:type:", statistic.type);
            
                item.value = (float)statValue;
                item.valueFormatted = displayValue;
                item.type = statistic.type;
                statisticData.statistics.Add(item);
            }
        }
        */
        
        return statisticData;
    }
    
    // --------------------------------------------------------------

    // SYNC

    // sync to server/network
    
    public static void SyncProfileProgress() {
        SendSync();
    }
    
    public static void SendSync() {
        if (Instance != null) {
            Instance.sendSync();
        }
    }
        
    public void sendSync() {
        
        GameState.SaveProfile();
        
        if (AppConfigs.featureEnableFacebook) {
            
            if (isLoggedIn(SocialNetworkTypes.facebook)) {
            
                if (GameCommunityTrackingController.Instance.dataUpdates.achievements.ContainsKey(
                    AppConfigs.socialStatisticForFacebook)) {
                    // Facebook only allows one score so we will only send the 'high-score' stat
                    // that is a state high to low.
                
                    double statValue = GameProfileStatistics.Current.GetStatisticValue(
                        AppConfigs.socialStatisticForFacebook);
                
                    if (statValue > 0) {
                        SendScoreFacebook((int)statValue);
                    }
                }
            }
        }
        
        if (AppConfigs.featureEnableCustomLeaderboards) {
            SendSyncCustom();
        }        
        
        GameCommunityTrackingController.ResetUpdated();
        
        ProcessTrackers();
    }
    
    private void SendSyncCustom() {     
        
        // TODO send custom leaderboard

        string networkType = SocialNetworkTypes.facebook;
        
        string username = GameProfiles.Current.GetNetworkValueUsername(networkType);//.GetSocialNetworkUserName();
        
        if (string.IsNullOrEmpty(username) && !Application.isEditor) {
            return;
        }
        
        syncData.gameId = AppConfigs.socialGameCommunityAppId;
        syncData.profileId = GameProfiles.Current.uuid;
        
        // TODO: gamecenter, check others
        syncData.networkUser.appId = AppConfigs.socialGameCommunityAppId;
        syncData.networkUser.authCode = GameProfiles.Current.GetNetworkValueToken(networkType);
        syncData.networkUser.fullName = GameProfiles.Current.GetNetworkValueName(networkType);
        syncData.networkUser.key = GameProfiles.Current.GetNetworkValueType(networkType);

        // TODO: gamecenter, check others
        syncData.networkUser.networkCode = networkType;
        syncData.networkUser.networkName = "Facebook";
        syncData.networkUser.passwordHash = GameProfiles.Current.GetNetworkValueToken(networkType);
        syncData.networkUser.userId = GameProfiles.Current.GetNetworkValueId(networkType);
        syncData.networkUser.userName = GameProfiles.Current.GetNetworkValueUsername(networkType);
        
        
        // add updated statistics
        foreach (GameStatistic statistic in GameStatistics.Instance.GetAll()) {
            if (GameCommunityTrackingController.Instance.dataUpdates.statistics.ContainsKey(statistic.code)) {
                GameCommunitySyncProfileStatistic stat = new GameCommunitySyncProfileStatistic();
                stat.code = statistic.code;
                stat.statistic_value = GameCommunityTrackingController.Instance.dataUpdates.statistics[statistic.code];
                stat.absolute_value = GameProfileStatistics.Current.GetStatisticValue(stat.code);
                if (stat.statistic_value > 0) {
                    syncData.profileStatistics.statistics.Add(stat);
                }
            }
        }
        
        // achievements
        foreach (GameAchievement achievement in GameAchievements.Instance.GetAll()) {
            if (GameCommunityTrackingController.Instance.dataUpdates.achievements.ContainsKey(achievement.code)) {
                GameCommunitySyncProfileAchievement achieve = new GameCommunitySyncProfileAchievement();
                bool completed = GameProfileAchievements.Current.GetAchievementValue(achieve.code);
                achieve.code = achievement.code;
                achieve.completed = completed;
                achieve.achievement_value = GameCommunityTrackingController.Instance.dataUpdates.achievements[achievement.code];
                achieve.absolute_value = completed ? 1.0f : 0.0f;
                if (achieve.achievement_value == 1.0f) {
                    syncData.profileAchievements.achievements.Add(achieve);
                }
            }
        }
        
        GameCommunityTrackingController.SyncSystemAttributes();
        
        // profile game data attributes
        
        GameCommunityTrackingController.SetSyncProfileGameDataAttributes(GameProfiles.Current.attributes);
        
        // profile attributes
        
        foreach (KeyValuePair<string, DataAttribute> pair in GameProfileTrackers.Current.attributes) {
            syncData.profileAttributes.Add(
            pair.Value.code, 
            pair.Value.val
            );
        }
        
        // profile game attributes
        
        // data attributes - extra
        
        GameCommunityService.SyncData(syncData);
        
        // TODO add fall back to save locally
        syncData = new GameCommunitySyncData();
    }
    
    private void SendScoreFacebook(int score) {
        LogUtil.Log("Sending scores with Facebook");
        if (score > 0) {
            SocialNetworks.PostScoreFacebook(score);
        }
    }
    
    // --------------------------------------------------------------

    // LIKES
    
    public static void LikeCurrentApp(string networkType) {
        GameCommunityController.LikeCurrentApp(networkType);
    }
    
    public static void LikeUrl(string networkType, string urlToLike) {
        GameCommunityController.LikeUrl(networkType, urlToLike);
    }

    // --------------------------------------------------------------

    // UI

    // UI - GAME COMMUNITY
        
    public static void ShowGameCommunity() {
        GameCommunityUIController.ShowGameCommunity(); 
    }

    public static void HideGameCommunity() {
        GameCommunityUIController.HideGameCommunity();
    }

    //
    
    public static void ShowGameCommunityLogin() {
        GameCommunityUIPanelLogin.ShowGameCommunityLogin();
        GameCommunity.TrackGameView("Login", "game-community-login");
        GameCommunity.TrackGameEvent("game-community-login", "action", 1);
    }
    
    public static void HideGameCommunityLogin() {
        GameCommunityUIPanelLogin.HideGameCommunityLogin();
    }

    // SHARES
    
    public static void ShowSharesCenter() {
        GameCommunityUIController.ShowSharesCenter();
    }
    
    public static void HideSharesCenter() {
        GameCommunityUIController.HideSharesCenter();
    }
    
    // BROADCAST REPLAY
    
    public static void ShowBroadcastRecordPlayShare() {
        GameCommunityUIController.ShowBroadcastRecordPlayShare();
    }
    
    public static void HideBroadcastRecordPlayShare() {
        GameCommunityUIController.HideBroadcastRecordPlayShare();
    }

    // ACTIONS

    public static void ShowActionTools() {
        GameCommunityUIController.ShowActionTools();
    }
    
    public static void HideActionTools() {
        GameCommunityUIController.HideActionTools();
    }

    //

    public static void ShowActionAppRate() {
        GameCommunityUIController.ShowActionAppRate();
    }
    
    public static void HideActionAppRate() {
        GameCommunityUIController.HideActionAppRate();
    }
    
    // --------------------------------------------------------------

    // TRACKING
    
    //
    
    public static void ProcessTrackers() {
        GameCommunityTrackingController.ProcessTrackers();
    }
    
    // views
    
    public static void TrackGameView(string title, string code) {
        TrackView("Game: " + title, AppConfigs.appUrlShortCode + "/" + code);
    }
    
    public static void TrackGameEvent(string action, string label, int val) {
        TrackEvent("game", "Game Action: " + action, label, val);
    }
    
    public static void TrackView(string title, string url) {
        GameCommunityTrackingController.TrackView(title, url);
    }
    
    // events
    
    public static void TrackEvent(string category, string action, string label, int val) {
        GameCommunityTrackingController.TrackEvent(category, action, label, val);
    }   

}

