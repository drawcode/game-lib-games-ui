using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class GameCommunityStatisticCodes {
    public static string points = "points";
    public static string highScore = "high-score";
    public static string totalScore = "total-score";
    public static string timePlayed = "time-played";
    public static string timesPlayed = "times-played";
    public static string shotsMade = "shots-made";
    public static string shotsMissed = "shots-missed";
    public static string topRank = "top-rank";
    public static string currentRank = "current-rank";
    public static string currentRankTotal = "current-rank-total";
    
}

public class GameCommunityLeaderboardData : DataObjectItem {
    public string network = "";
    public string appNamespace = "";
    public string appId = "";
    public string appName = "";
    public int page = 1;
    public int pageSize = 25;
    public int totalCount = 25;
    
    public int totalPages {
        get {
            return (int)Math.Ceiling((double)totalCount / (double)pageSize);
        }
    }
    
    public Dictionary<string,List<GameCommunityLeaderboardItem>> leaderboards = null;
    
    public GameCommunityLeaderboardData() {
        Reset();
    }
    
    public override void Reset() {
        base.Reset();
        network = "";
        appNamespace = "";
        appId = "";
        appName = "";
        leaderboards = new Dictionary<string, List<GameCommunityLeaderboardItem>>();
    }
}

public class GameCommunitySyncDataUpdates {
    
    public Dictionary<string, float> statistics = new Dictionary<string, float>();
    public Dictionary<string, float> achievements = new Dictionary<string, float>();
    public Dictionary<string, DataAttribute> profileGameDataAttributes = new Dictionary<string, DataAttribute>();
    public Dictionary<string, object> profileAttributes = new Dictionary<string, object>();
    public Dictionary<string, object> gameProfileAttributes = new Dictionary<string, object>();
    public Dictionary<string, object> dataAttributes = new Dictionary<string, object>();
        
    public GameCommunitySyncDataUpdates() {
        Reset();
    }
    
    public void Reset() {
        if (statistics != null) {
            statistics.Clear();
        }
        else {
            statistics = new Dictionary<string, float>();
        }
        
        if (achievements != null) {
            achievements.Clear();
        }
        else {
            achievements = new Dictionary<string, float>();
        }       
        
        if (profileGameDataAttributes != null) {
            profileGameDataAttributes.Clear();
        }
        else {
            profileGameDataAttributes = new Dictionary<string, DataAttribute>();
        }
        
        if (profileAttributes != null) {
            profileAttributes.Clear();
        }
        else {
            profileAttributes = new Dictionary<string, object>();
        }
        
        if (gameProfileAttributes != null) {
            gameProfileAttributes.Clear();
        }
        else {
            gameProfileAttributes = new Dictionary<string, object>();
        }
        
        if (dataAttributes != null) {
            dataAttributes.Clear();
        }
        else {
            dataAttributes = new Dictionary<string, object>();
        }
    }
    
    public void SetStatistic(string code, float val) {
        if (!statistics.ContainsKey(code)) {
            statistics.Add(code, val);
        }
        else {
            statistics[code] = val;
        }
    }
    
    public void SetAchievement(string code, bool completed) {
        SetAchievement(code, 1.0f);
    }
    
    public void SetAchievement(string code, float val) {
        if (!achievements.ContainsKey(code)) {
            achievements.Add(code, val);
        }
        else {
            achievements[code] = val;
        }
    }
    
    public void SetProfileGameDataAttribute(string code, DataAttribute dataAttribute) {
        if (!profileGameDataAttributes.ContainsKey(code)) {
            profileGameDataAttributes.Add(code, dataAttribute);
        }
        else {
            profileGameDataAttributes[code] = dataAttribute;
        }
    }
    
    public void SetProfileGameDataAttributes(Dictionary<string, DataAttribute> dataAttributes) {
        profileGameDataAttributes.Clear();
        foreach (KeyValuePair<string, DataAttribute> pair in dataAttributes) {
            profileGameDataAttributes.Add(pair.Key, pair.Value);
        }
    }
    
    public void SetProfileAttribute(string code, object val) {
        if (!profileAttributes.ContainsKey(code)) {
            profileAttributes.Add(code, val);
        }
        else {
            profileAttributes[code] = val;
        }
    }
    
    public void SetGameProfileAttribute(string code, object val) {
        if (!gameProfileAttributes.ContainsKey(code)) {
            gameProfileAttributes.Add(code, val);
        }
        else {
            gameProfileAttributes[code] = val;
        }
    }
    
    public void SetDataAttribute(string code, object val) {
        if (!dataAttributes.ContainsKey(code)) {
            dataAttributes.Add(code, val);
        }
        else {
            dataAttributes[code] = val;
        }
    }
}

public class GameCommunityStatisticItem {
    public string code = "";
    public string displayName = "";
    public string description = "";
    public float value = 0f;
    public string valueFormatted = "";
    public string urlImage = "";
    public string type = "";
}

public class GameCommunityStatisticData : DataObjectItem {
    public List<GameCommunityStatisticItem> statistics = new List<GameCommunityStatisticItem>();
}

public class GameCommunityAchievementItem {
    public string code = "";
    public string displayName = "";
    public string description = "";
    public bool completed = false;
    public int points = 1;
    public string urlImage = "";
    public string type = "";
}

public class GameCommunityAchievementData : DataObjectItem {
    public List<GameCommunityAchievementItem> achievements = new List<GameCommunityAchievementItem>();
}

public class GameCommunityLeaderboardItem {
    public string code = "";
    public string username = "";
    public string userId = "";
    public string name = "";
    public string network = "";
    public float value = 0f;
    public string valueFormatted = "";
    public string urlImage = "";
    public string type = "";
    public int rank = 999;
    public int rankChange = 0;
    public int rankTotal = 0;
    public DateTime dateUpdated;
}

public class GameCommunitySyncUser {
    public string networkName = "";
    public string networkCode = "";
    public string userName = "Player";
    public string fullName = "";
    public string userId = "";
    public string passwordHash = "";
    public string authCode = "";
    public string appId = "";
    public string key = "";
}

public class GameCommunitySyncProfileStatistic {
    public string code = "";
    public double statistic_value = 0;
    public double absolute_value = 0;
    
}

public class GameCommunitySyncProfileStatistics {
    public List<GameCommunitySyncProfileStatistic> statistics = new List<GameCommunitySyncProfileStatistic>();
    
}

public class GameCommunitySyncProfileAchievement {
    public string code = "";
    public bool completed = false;
    public double achievement_value = 0;
    public double absolute_value = 0;
    
}

public class GameCommunitySyncProfileAchievements {
    public List<GameCommunitySyncProfileAchievement> achievements = new List<GameCommunitySyncProfileAchievement>();
}

public class GameCommunitySyncData {
    public string profileId = "";
    public string gameId = "";
    public GameCommunitySyncUser networkUser = new GameCommunitySyncUser();
    public GameCommunitySyncProfileStatistics profileStatistics = new GameCommunitySyncProfileStatistics();
    public GameCommunitySyncProfileAchievements profileAchievements = new GameCommunitySyncProfileAchievements();
    public Dictionary<string, DataAttribute> dataAttributes = new Dictionary<string, DataAttribute>(); // extra props
    public Dictionary<string, object> gameProfileAttributes = new Dictionary<string, object>();
    public Dictionary<string, object> profileAttributes = new Dictionary<string, object>();
    public Dictionary<string, object> data = new Dictionary<string, object>(); 
}

public class GameCommunityNetworkUser {
    public string username = "";
    public string name = "";
    public string first_name = "";
    public string network = "";
    public string user_id = "";
}

public class GameCommunityMessages {
    public static string gameCommunityLoggedIn = "game-community-network-loggedin";
    public static string gameCommunityReady = "game-community-ready";
    public static string gameCommunityLeaderboardData = "game-community-leaderboard-data";
    public static string gameCommunityLeaderboardUserData = "game-community-leaderboard-user-data";
    public static string gameCommunityLeaderboardFriendData = "game-community-leaderboard-friend-data";
    public static string gameCommunitySyncComplete = "game-community-sync-complete";
    public static string gameCommunitySyncError = "game-community-sync-error";
    public static string gameCommunityResultMessage = "game-community-result-message";
    public static string gameCommunityLoginDefer = "game-community-login-defer";
}

public class GameCommunitySyncDataResponse : DataObjectItem {
    public string message = "";
    public string data = "";
        
    public GameCommunitySyncDataResponse() {
        Reset();
    }
    
    public override void Reset() {
        base.Reset();
        message = "";
        data = "";
    }
}

public class GameCommunityMessageResult : DataObjectItem {
    public string title = "";
    public string message = "";
    public string data = "";
    public List<object> dataObjects = null;
    public List<GameObject> gameObjects = null;
        
    public GameCommunityMessageResult() {
        Reset();
    }
    
    public override void Reset() {
        base.Reset();
        message = "";
        data = "";
        dataObjects = new List<object>();
        gameObjects = new List<GameObject>();
    }
}

// TRACKING

public class GameCommunityTrackerReports : DataObjectItem {
    public List<GameCommunityTrackerReport> trackerReports = new List<GameCommunityTrackerReport>();
    public GameCommunityTrackerReport currentTrackerReport;
    private string key = "tracker-reports";
    
    public GameCommunityTrackerReports() {
        Reset();
    }
    
    public override void Reset() {
        base.Reset();
        trackerReports = new List<GameCommunityTrackerReport>();
        Load();
    }
    
    public void SetTrackerReport(GameCommunityTrackerReport trackerReport) {
        if (trackerReports != null) {
            if (!trackerReports.Contains(trackerReport)) {
                trackerReports.Add(trackerReport);
            }
        }
    }
    
    public void SetTrackerView(GameCommunityTrackerView trackerView) {
        if (currentTrackerReport != null)
            currentTrackerReport.SetTrackerView(trackerView);
    }
    
    public void SetTrackerEvent(GameCommunityTrackerEvent trackerEvent) {
        if (currentTrackerReport != null)
            currentTrackerReport.SetTrackerEvent(trackerEvent);
    }
    
    public void Save() {
        SaveData(ContentPaths.appCachePathAllSharedUserData, key, trackerReports);
    }
    
    public void Load() {
        trackerReports = LoadData<List<GameCommunityTrackerReport>>(ContentPaths.appCachePathAllSharedUserData, key);
        currentTrackerReport = new GameCommunityTrackerReport();
        SetTrackerReport(currentTrackerReport);
    }
    
    public GameCommunityTrackerReports Copy() {
        GameCommunityTrackerReports copy = new GameCommunityTrackerReports();
        
        copy.attributes = attributes;
        copy.currentTrackerReport = currentTrackerReport;
        copy.key = key;
        copy.trackerReports = trackerReports;
        
        return copy;    
    }
}

public class GameCommunityTrackerReport : DataObjectItem {
    public List<GameCommunityTrackerView> trackerViews;
    public List<GameCommunityTrackerEvent> trackerEvents;
    
    public GameCommunityTrackerReport() {
        Reset();
    }
    
    public override void Reset() {
        base.Reset();
        trackerViews = new List<GameCommunityTrackerView>();
        trackerEvents = new List<GameCommunityTrackerEvent>();      
    }
            
    public void SetTrackerView(GameCommunityTrackerView trackerView) {
        if (!trackerViews.Contains(trackerView) 
            && trackerViews.Count < 99) {
            trackerViews.Add(trackerView);
        }
        // TODO purge rather than discard filled.
    }
                
    public void SetTrackerEvent(GameCommunityTrackerEvent trackerEvent) {
        if (!trackerEvents.Contains(trackerEvent) 
            && trackerViews.Count < 99) {
            trackerEvents.Add(trackerEvent);
        }
    }
}

public class GameCommunityTrackerView {
    public string title = "main";
    public string url = "main";
    
    public GameCommunityTrackerView() {
    }
    
    public GameCommunityTrackerView(string titleTo, string urlTo) {
        title = titleTo;
        url = urlTo;
    }
}

public class GameCommunityTrackerEvent {
    public string category = "main";
    public string action = "main";
    public string label = null;
    public int val = 1;
    
    public GameCommunityTrackerEvent() {
    }
    
    public GameCommunityTrackerEvent(string categoryTo, string actionTo, string labelTo, int valTo) {
        category = categoryTo;
        action = actionTo;
        label = labelTo;
        val = valTo;
    }
}

public class GameCommunitySystemTracking {
    public string deviceModel = "";
    public string deviceName = "";
    public string deviceType = "";
    public string deviceUniqueIdentifier = "";
    public string graphicsDeviceID = "";
    public string graphicsDeviceName = "";
    public string graphicsDeviceVendor = "";
    public string graphicsDeviceVendorID = "";
    public string graphicsDeviceVersion = "";
    public string graphicsMemorySize = "";
    public string graphicsPixelFillrate = "";
    public string graphicsShaderLevel = "";
    public string operatingSystem = "";
    public string processorCount = "";
    public string processorType = "";
    public string supportedRenderTargetCount = "";
    public string supportsAccelerometer = "";
    public string supportsGyroscope = "";
    public string supportsImageEffects = "";
    public string supportsLocationService = "";
    public string supportsRenderTextures = "";
    public string supportsShadows = "";
    public string supportsVertexPrograms = "";
    public string supportsVibration = "";
    public string systemMemorySize = "";
    private static volatile GameCommunitySystemTracking instance;
    private static System.Object syncRoot = new System.Object();
        
    public static GameCommunitySystemTracking Instance {
        get {
            if (instance == null) {
                lock (syncRoot) {
                    if (instance == null) 
                        instance = new GameCommunitySystemTracking();
                }
            }
        
            return instance;
        }
        set {
            instance = value;
        }
    }
    
    public GameCommunitySystemTracking() {
        Reset();
    }
    
    public string platformCode {
        get {
            #if UNITY_IPHONE
                    return "ios";
            #elif UNITY_ANDROID
                    return "android";
            #else 
            return "desktop";
            #endif  
        }
    }
        
    public string platform {
        get {
            return Application.platform.ToString();
        }
    }
    
    public void Reset() {
        deviceModel = SystemInfo.deviceModel;
        deviceName = SystemInfo.deviceName;
        deviceType = SystemInfo.deviceType.ToString();
        deviceUniqueIdentifier = UniqueUtil.Instance.currentUniqueId;// SystemInfo.deviceUniqueIdentifier;
        graphicsDeviceID = SystemInfo.graphicsDeviceID.ToString();
        graphicsDeviceName = SystemInfo.graphicsDeviceName;
        graphicsDeviceVendor = SystemInfo.graphicsDeviceVendor;
        graphicsDeviceVendorID = SystemInfo.graphicsDeviceVendorID.ToString();
        graphicsDeviceVersion = SystemInfo.graphicsDeviceVersion;
        graphicsMemorySize = SystemInfo.graphicsMemorySize.ToString("N0");
        graphicsPixelFillrate = SystemInfo.graphicsPixelFillrate.ToString("N0");
        graphicsShaderLevel = SystemInfo.graphicsShaderLevel.ToString("N0");
        operatingSystem = SystemInfo.operatingSystem;
        processorCount = SystemInfo.processorCount.ToString("N0");
        processorType = SystemInfo.processorType;
        supportedRenderTargetCount = SystemInfo.supportedRenderTargetCount.ToString("N0");
        supportsAccelerometer = SystemInfo.supportsAccelerometer.ToString();
        supportsGyroscope = SystemInfo.supportsGyroscope.ToString();
        supportsImageEffects = SystemInfo.supportsImageEffects.ToString();
        supportsLocationService = SystemInfo.supportsLocationService.ToString();
        supportsRenderTextures = SystemInfo.supportsRenderTextures.ToString();
        supportsShadows = SystemInfo.supportsShadows.ToString();
        supportsVertexPrograms = SystemInfo.supportsVertexPrograms.ToString();
        supportsVibration = SystemInfo.supportsVibration.ToString();
        systemMemorySize = SystemInfo.systemMemorySize.ToString();
    }
}