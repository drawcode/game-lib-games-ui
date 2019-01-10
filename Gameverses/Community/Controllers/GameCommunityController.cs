using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

#if !UNITY_WEBPLAYER
using System.IO;
#endif
using Engine.Events; 


public class GameCommunityController : GameObjectBehavior {
    
    public static GameCommunityController Instance;
    public float currentTimeBlock = 0.0f;
    public float actionInterval = 1.0f;
        
    public void Awake() {
        
        if (Instance != null && this != Instance) {
            //There is already a copy of this script running
            //Destroy(gameObject);
            return;
        }
        
        Instance = this;
        
        Init();
    }
    
    void OnEnable() {
        
        Messenger.AddListener(
            GameCommunityMessages.gameCommunityReady, 
            OnGameCommunityReady);
        
        Messenger<string>.AddListener(SocialNetworksMessages.socialLoggedIn, OnProfileLoggedIn);
        
        Messenger<string, string, object>.AddListener(SocialNetworksMessages.socialProfileData, OnProfileData);
        
        Messenger<GameCommunityLeaderboardData>.AddListener(
            GameCommunityMessages.gameCommunityLeaderboardData, 
            OnLeaderboardData);
        
        Messenger<GameCommunityLeaderboardData>.AddListener(
            GameCommunityMessages.gameCommunityLeaderboardUserData, 
            OnProfileLeaderboardData);
    }
    
    void OnDisable() {
        
        Messenger.RemoveListener(
            GameCommunityMessages.gameCommunityReady, 
            OnGameCommunityReady);              
        
        Messenger<string>.RemoveListener(SocialNetworksMessages.socialLoggedIn, OnProfileLoggedIn);
        
        Messenger<string, string, object>.RemoveListener(SocialNetworksMessages.socialProfileData, OnProfileData);  
        
        Messenger<GameCommunityLeaderboardData>.AddListener(
            GameCommunityMessages.gameCommunityLeaderboardData, 
            OnLeaderboardData);
        
        Messenger<GameCommunityLeaderboardData>.RemoveListener(
            GameCommunityMessages.gameCommunityLeaderboardUserData, 
            OnProfileLeaderboardData);
    }
    
    void Init() {
        
    }
    
    void OnGameCommunityReady() {
        
        // test tracking
        GameCommunity.TrackGameView("Loaded", "loaded");
        GameCommunity.ProcessTrackers();
        
    }
        
    void OnProfileLoggedIn(string networkType) {
        
        Debug.Log("GameCommunityController: OnProfileLoggedIn");

        if (networkType == SocialNetworkTypes.facebook) {
            // If they logged in to like
            
            if (GameCommunityController.Instance.likeActionClicked) {
                GameCommunity.LikeCurrentApp(SocialNetworkTypes.facebook);
            }
            
            GameCommunity.TrackGameView("Facebook Logged In", "logged-in");
            
            GameCommunity.SyncProfileProgress();
        }
    }
    
    void OnProfileData(string networkType, string dataType, object data) {
        
        Debug.Log("OnProfileData: networkType: " + networkType);
        Debug.Log("OnProfileData: dataType: " + dataType);
        Debug.Log("OnProfileData: data: " + data);        
        
        GameCommunityUIPanelLoading.HideGameCommunityLoading();
        
        if (networkType == SocialNetworkTypes.facebook) {
                        
            if (dataType == SocialNetworkDataTypes.profile) {
                
                // TODO move to tracker...
                var dict = data as Dictionary<string,object>;
                
                if (dict != null) {
                    
                    string userId = "";
                    if (dict.ContainsKey("id")) {
                        userId = dict["id"].ToString();
                    }
                    Debug.Log("OnProfileData: userId:" + userId);
                    
                    string name = "";
                    if (dict.ContainsKey("name")) {
                        name = dict["name"].ToString();
                    }
                    Debug.Log("OnProfileData: name:" + name);
                    
                    string username = "";
                    if (dict.ContainsKey("username")) {
                        username = dict["username"].ToString();
                    }
                    Debug.Log("OnProfileData: username:" + username);
                    
                    string first_name = "";
                    if (dict.ContainsKey("first_name")) {
                        first_name = dict["first_name"].ToString();
                    }
                    Debug.Log("OnProfileData: first_name:" + first_name);
                                        
                    string gender = "";
                    if (dict.ContainsKey("gender")) {
                        gender = dict["gender"].ToString();
                    }
                    Debug.Log("OnProfileData: gender:" + gender);
                    
                    string dob = "";
                    if (dict.ContainsKey("birthday")) {
                        dob = dict["birthday"].ToString();
                    }
                    Debug.Log("OnProfileData: dob:" + dob);
                    
                    string location = "";
                    string locationCity = "";
                    string locationState = "";                  
                    if (dict.ContainsKey("location")) {
                        var loc = dict["location"] as Dictionary<string,object>;
                        if (loc != null) {
                            if (loc.ContainsKey("name")) {
                                string locName = loc["name"].ToString();
                                if (!string.IsNullOrEmpty(locName)) {
                                    if (locName.IndexOf(',') > -1) {
                                        string[] arrLocation = locName.Split(',');
                                        locationCity = arrLocation[0].Trim();
                                        if (arrLocation.Length > 0) {
                                            locationState = arrLocation[1].Trim();
                                        }
                                    }
                                    else {
                                        location = locName;
                                    }
                                }
                            }
                        }
                    }
                    
                    Debug.Log("OnProfileData: location:" + location);
                    Debug.Log("OnProfileData: locationCity:" + locationCity);
                    Debug.Log("OnProfileData: locationState:" + locationState);
                    
                    string locale = "en_US";
                    if (dict.ContainsKey("locale")) {
                        locale = dict["locale"].ToString();
                    }
                    Debug.Log("OnProfileData: locale:" + locale);
                    
                    Debug.Log("OnProfileData ht: userId:" + userId + 
                        " name:" + name + 
                        " username:" + username + 
                        " first_name:" + first_name);

                    //GameProfiles.Current.SetThirdPartyNetworkUser();
                    GameProfiles.Current.SetNetworkValueId(networkType, userId);
                    GameProfiles.Current.SetNetworkValueName(networkType, name);
                    GameProfiles.Current.SetNetworkValueUsername(networkType, username);
                    GameProfiles.Current.SetNetworkValueFirstName(networkType, first_name);
                    GameProfiles.Current.SetNetworkValueType(networkType, networkType);

                    if (!string.IsNullOrEmpty(dob)) {
                        GameProfileTrackers.Current.SetDOB(dob);
                    }
                    
                    if (!string.IsNullOrEmpty(gender)) {
                        GameProfileTrackers.Current.SetGender(gender);
                    }
                    
                    if (!string.IsNullOrEmpty(location)) {
                        GameProfileTrackers.Current.SetLocation(location);
                    }
                    
                    if (!string.IsNullOrEmpty(locationCity)) {
                        GameProfileTrackers.Current.SetLocationCity(locationCity);
                    }                   
                    
                    if (!string.IsNullOrEmpty(locationState)) {
                        GameProfileTrackers.Current.SetLocationState(locationState);
                    }
                    
                    if (!string.IsNullOrEmpty(locale)) {
                        GameProfileTrackers.Current.SetLocale(locale);
                    }

                    GameProfiles.Current.SetNetworkValueToken(
                        networkType, SocialNetworks.GetAccessTokenUserFacebook());
                    
                    GameState.SaveProfile();
                    
                    GameCommunityNetworkUser user = new GameCommunityNetworkUser();
                    user.username = username;
                    user.name = name;
                    user.first_name = first_name;
                    user.user_id = userId;
                    user.network = networkType;
                    
                    GameCommunity.ResetLoggingIn(networkType);
                    
                    Messenger<GameCommunityNetworkUser>.Broadcast(
                        GameCommunityMessages.gameCommunityLoggedIn,
                        user);
                    
                }               
            }
        }
    }
    
    void OnLeaderboardData(GameCommunityLeaderboardData leaderboardData) {
        
        // update latest rank
        
        if (leaderboardData == null) {
            return;
        }
        
        int totalRank = leaderboardData.totalCount;
        if (totalRank > 0) {
            GameCommunity.SetRankTotal(totalRank);
        }
        
        // go get user leaderboard for rest of ranks
        
        string username = GameProfiles.Current.GetNetworkValueUsername(SocialNetworkTypes.facebook);///.GetSocialNetworkUserName();
        if (!string.IsNullOrEmpty(username)) {
            GameCommunity.RequestLeaderboardUser(
                GameCommunityStatisticCodes.highScore, 
                1, 1000, GameCommunity.GameLeaderboardType.ALL, username);
        }
        
        
        GameCommunity.TrackGameView("Leaderboard Data Loaded", "leaderboard-data-loaded");
    }
    
    void OnProfileLeaderboardData(GameCommunityLeaderboardData leaderboardData) {
        // update all ranks for current stats for the user
        if (leaderboardData != null) {
            foreach (KeyValuePair<string,List<GameCommunityLeaderboardItem>> leaderboard in leaderboardData.leaderboards) {
                foreach (GameCommunityLeaderboardItem item in leaderboard.Value) {
                    if (!string.IsNullOrEmpty(item.username)) {
                        if (item.username.ToLower() == 
                            GameProfiles.Current.GetNetworkValueUsername(SocialNetworkTypes.facebook).ToLower()) {
                            // update this rank
                            
                            GameCommunity.SetRankStat(item.code, item.rank);
                            GameCommunity.SetRankTotalStat(item.code, item.rankTotal);
                        }
                    }
                }
                GameState.SaveProfile();
            }
        }
        
        GameCommunity.TrackGameView("Leaderboard Gamer Data Loaded", "leaderboard-gamer-data-loaded");
    }
    
    public static void SendResultMessage(string title, string message) {
        if (Instance != null) {
            Instance.sendResultMessage(title, message);
        }
    }
    
    public void sendResultMessage(string title, string message) {
        sendResultMessage(title, message, "", null, null);
    }
    
    public static void SendResultMessage(
        string title, 
        string message, 
        string data, 
        List<object> dataObjects, 
        List<GameObject> gameObjects) {
        if (Instance != null) {
            Instance.sendResultMessage(title, message, data, dataObjects, gameObjects);
        }
    }
    
    public void sendResultMessage(
        string title,
        string message,
        string data,
        List<object> dataObjects,
        List<GameObject> gameObjects) {
        GameCommunityMessageResult result = new GameCommunityMessageResult();
        result.title = title;
        result.message = message;
        result.data = data;
        result.dataObjects = dataObjects;
        result.gameObjects = gameObjects;       
        sendResultMessage(result);

    }
    
    public static void SendResultMessage(GameCommunityMessageResult result) {
        if (Instance != null) {
            Instance.sendResultMessage(result);
        }
    }
    
    public void sendResultMessage(GameCommunityMessageResult result) {
        Messenger<GameCommunityMessageResult>.Broadcast(
            GameCommunityMessages.gameCommunityResultMessage, result);
    }
    
    // LIKES
    
    public static void LikeCurrentApp(string networkType) {
        if (Instance != null) {
            Instance.likeThisApp(networkType);
        }
    }
        
    public void likeThisApp(string networkType) {
        string urlToLike = AppConfigs.socialFacebookLikeDefaultUrl;
        likeUrl(networkType, urlToLike);
    }
    
    public static void LikeUrl(string networkType, string urlToLike) {
        if (Instance != null) {
            Instance.likeUrl(networkType, urlToLike);
        }
    }
    
    public bool likeActionClicked = false;
    
    public void likeUrl(string networkType, string urlToLike) {
        
        //if(Application.isEditor) {
        // not avail in web/desktop yet but web is easy with js library,
        //  Platforms.ShowWebView("", urlToLike);
        //}
        //else {
        if (GameCommunity.IsLoggedIn(networkType) || Application.isEditor) {

            likeActionClicked = false;

            GameCommunitySocialController.LikeUrl(networkType, urlToLike);
            
            // TODO loco localize
            
            GameCommunityUIPanelLoading.ShowGameCommunityLoading(
                    "THANK YOU", AppConfigs.appGameDisplayName + " successfully liked!");
                
            Invoke("HideLikeThanks", 4f);
        }
        else {
            likeActionClicked = true;
            GameCommunity.Login(networkType);
        }
        //}
    }
    
    private void HideLikeThanks() {
        GameCommunityUIPanelLoading.HideGameCommunityLoading();
    }
    
    public void LeaderboardsShowOnline() {

        // TODO loco localize

        Platforms.ShowWebView(
            AppConfigs.appGameDisplayName 
            + " Online Leaderboards + Competitions", 
            AppConfigs.appUrlWeb
        );
        
        GameCommunity.TrackGameView("Game Community Online", "online-community");
        GameCommunity.TrackGameEvent("online-community", "load", 1);
    }
}
