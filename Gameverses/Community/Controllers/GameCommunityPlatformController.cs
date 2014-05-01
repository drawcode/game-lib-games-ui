using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
#if !UNITY_WEBPLAYER
using System.IO;
#endif

using Engine.Events;

public class GameCommunityPlatformController : GameObjectBehavior {
	
	public static GameCommunityPlatformController Instance;
	
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
			GameCommunityPlatformMessages.gameCommunityReady, 
			OnGameCommunityReady);
		
		Messenger.AddListener(SocialNetworksMessages.socialLoggedIn, OnProfileLoggedIn);
		
		Messenger<string, string, object>.AddListener(SocialNetworksMessages.socialProfileData, OnProfileData);
		
		Messenger<GameCommunityLeaderboardData>.AddListener(
			GameCommunityPlatformMessages.gameCommunityLeaderboardData, 
			OnLeaderboardData);
		
		Messenger<GameCommunityLeaderboardData>.AddListener(
			GameCommunityPlatformMessages.gameCommunityLeaderboardUserData, 
			OnProfileLeaderboardData);
	}
	
	void OnDisable() {
		
		Messenger.RemoveListener(
			GameCommunityPlatformMessages.gameCommunityReady, 
			OnGameCommunityReady);				
		
		Messenger.RemoveListener(SocialNetworksMessages.socialLoggedIn, OnProfileLoggedIn);
		
		Messenger<string, string, object>.RemoveListener(SocialNetworksMessages.socialProfileData, OnProfileData);	
		
		Messenger<GameCommunityLeaderboardData>.AddListener(
			GameCommunityPlatformMessages.gameCommunityLeaderboardData, 
			OnLeaderboardData);
		
		Messenger<GameCommunityLeaderboardData>.RemoveListener(
			GameCommunityPlatformMessages.gameCommunityLeaderboardUserData, 
			OnProfileLeaderboardData);
	}
	
	void Init() {
		
	}	
	
	void OnGameCommunityReady() {
		
		// test tracking
		GameCommunity.TrackGameView("Loaded", "loaded");
		GameCommunity.ProcessTrackers();
		
	}
		
	void OnProfileLoggedIn() {
		
		Debug.Log("GameCommunityPlatformController: OnProfileLoggedIn");
		
		// If they logged in to like
		
		if(GameCommunityPlatformController.Instance.likeActionClicked) {
			GameCommunity.LikeCurrentApp();
		}
		
		GameCommunity.TrackGameView("Facebook Logged In", "logged-in");
		
		GameCommunity.SyncProfileProgress();
	}
	
	void OnProfileData(string networkType, string dataType, object data) {
		
		LogUtil.Log("OnProfileData: networkType: ", networkType);
		LogUtil.Log("OnProfileData: dataType: ", dataType);
		LogUtil.Log("OnProfileData: data: ", data);
		
		
		GameCommunityUIPanelLoading.HideGameCommunityLoading();
		
		if(networkType == SocialNetworkTypes.facebook) {
						
			if(dataType == SocialNetworkDataTypes.profile) {
				
				// TODO move to tracker...
				var dict = data as Dictionary<string,object>;
				
				if(dict != null) {
					
					string userId = "";
					if(dict.ContainsKey("id")) {
						userId = dict["id"].ToString();
					}
					LogUtil.Log("OnProfileData: userId:" + userId);
					
					string name = "";
					if(dict.ContainsKey("name")) {
						name = dict["name"].ToString();
					}
					LogUtil.Log("OnProfileData: name:" + name);
					
					string username = "";
					if(dict.ContainsKey("username")) {
						username = dict["username"].ToString();
					}
					LogUtil.Log("OnProfileData: username:" + username);
					
					string first_name = "";
					if(dict.ContainsKey("first_name")) {
						first_name = dict["first_name"].ToString();
					}
					LogUtil.Log("OnProfileData: first_name:" + first_name);
										
					string gender = "";
					if(dict.ContainsKey("gender")) {
						gender = dict["gender"].ToString();
					}
					LogUtil.Log("OnProfileData: gender:" + gender);
					
					string dob = "";
					if(dict.ContainsKey("birthday")) {
						dob = dict["birthday"].ToString();
					}
					LogUtil.Log("OnProfileData: dob:" + dob);
					
					string location = "";
					string locationCity = "";
					string locationState = "";					
					if(dict.ContainsKey("location")) {
						var loc = dict["location"] as Dictionary<string,object>;
						if(loc != null) {
							if(loc.ContainsKey("name")) {
								string locName = loc["name"].ToString();
								if(!string.IsNullOrEmpty(locName)) {
									if(locName.IndexOf(',') > -1) {
										string[] arrLocation = locName.Split(',');
										locationCity = arrLocation[0].Trim();
										if(arrLocation.Length > 0) {
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
					
					LogUtil.Log("OnProfileData: location:" + location);
					LogUtil.Log("OnProfileData: locationCity:" + locationCity);
					LogUtil.Log("OnProfileData: locationState:" + locationState);
					
					string locale = "en_US";
					if(dict.ContainsKey("locale")) {
						locale = dict["locale"].ToString();
					}
					LogUtil.Log("OnProfileData: locale:" + locale);
					
					LogUtil.Log("OnProfileData ht: userId:" + userId + " name:" + name + " username:" + username + " first_name:" + first_name);
					
					GameProfiles.Current.SetSocialNetworkProfileState(networkType, userId);
					GameProfiles.Current.SetSocialNetworkName(name);
					GameProfiles.Current.SetSocialNetworkUserName(username);
					GameProfiles.Current.SetSocialNetworkFirstName(first_name);
					
					if(!string.IsNullOrEmpty(dob)) {
						GameProfileTrackers.Current.SetDOB(dob);
					}
					
					if(!string.IsNullOrEmpty(gender)) {
						GameProfileTrackers.Current.SetGender(gender);
					}
					
					if(!string.IsNullOrEmpty(location)) {
						GameProfileTrackers.Current.SetLocation(location);
					}
					
					if(!string.IsNullOrEmpty(locationCity)) {
						GameProfileTrackers.Current.SetLocationCity(locationCity);
					}					
					
					if(!string.IsNullOrEmpty(locationState)) {
						GameProfileTrackers.Current.SetLocationState(locationState);
					}
					
					if(!string.IsNullOrEmpty(locale)) {
						GameProfileTrackers.Current.SetLocale(locale);
					}
					
					//GameProfiles.Current.SetSocialNetworkAuthTokenApp(SocialNetworks.Instance.GetAccessTokenUserFacebook());
					GameProfiles.Current.SetSocialNetworkAuthTokenUser(SocialNetworks.GetAccessTokenUserFacebook());
					
					GameCommunityPlatformState.SaveProfile();
					
					GameCommunityNetworkUser user = new GameCommunityNetworkUser();
					user.username = username;
					user.name = name;
					user.first_name = first_name;
					user.user_id = userId;
					user.network = networkType;
					
					GameCommunity.ResetLoggingIn();
					
					Messenger<GameCommunityNetworkUser>.Broadcast(
						GameCommunityPlatformMessages.gameCommunityLoggedIn,
						user);
					
				}				
			}
		}
	}
	
	void OnLeaderboardData(GameCommunityLeaderboardData leaderboardData) {
		
		// update latest rank
		
		if(leaderboardData == null) {
			return;
		}
		
		int totalRank = leaderboardData.totalCount;
		if(totalRank > 0) {
			GameCommunity.SetRankTotal(totalRank);
		}
		
		// go get user leaderboard for rest of ranks
		
		string username = GameProfiles.Current.GetSocialNetworkUserName();
		if(!string.IsNullOrEmpty(username)) {
			GameCommunity.RequestLeaderboardUser(
				GameCommunityStatisticCodes.highScore, 
				1, 1000, GameCommunity.GameLeaderboardType.ALL, username);
		}
		
		
		GameCommunity.TrackGameView("Leaderboard Data Loaded", "leaderboard-data-loaded");
	}
	
	void OnProfileLeaderboardData(GameCommunityLeaderboardData leaderboardData) {
		// update all ranks for current stats for the user
		if(leaderboardData != null) {
			foreach(KeyValuePair<string,List<GameCommunityLeaderboardItem>> leaderboard in leaderboardData.leaderboards) {
				foreach(GameCommunityLeaderboardItem item in leaderboard.Value) {
					if(!string.IsNullOrEmpty(item.username)) {
						if(item.username.ToLower() == GameProfiles.Current.GetSocialNetworkUserName().ToLower()) {
							// update this rank
							
							GameCommunity.SetRankStat(item.code, item.rank);
							GameCommunity.SetRankTotalStat(item.code, item.rankTotal);
						}
					}
				}
				GameCommunityPlatformState.SaveProfile();
			}
		}
		
		GameCommunity.TrackGameView("Leaderboard Gamer Data Loaded", "leaderboard-gamer-data-loaded");
	}
	
	public static void SendResultMessage(string title, string message) {
		if(Instance != null) {
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
		if(Instance != null) {
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
		if(Instance != null) {
			Instance.sendResultMessage(result);
		}
	}
	
	public void sendResultMessage(GameCommunityMessageResult result) {
		Messenger<GameCommunityMessageResult>.Broadcast(
			GameCommunityPlatformMessages.gameCommunityResultMessage, result);
	}
	
	// LIKES
	
	public static void LikeCurrentApp() {
		if(Instance != null) {
			Instance.likeThisApp();
		}
	}
		
	public void likeThisApp() {
		string urlToLike = AppConfigs.socialFacebookLikeDefaultUrl;
		likeUrl(urlToLike);
	}
	
	public static void LikeUrl(string urlToLike) {
		if(Instance != null) {
			Instance.likeUrl(urlToLike);
		}
	}
	
	public bool likeActionClicked = false;
	
	public void likeUrl(string urlToLike) {
		
		//if(Application.isEditor) {
			// not avail in web/desktop yet but web is easy with js library,
		//	Platforms.ShowWebView("", urlToLike);
		//}
		//else {
			if(GameCommunity.IsLoggedIn() || Application.isEditor) {
				likeActionClicked = false;
				GameCommunityPlatformSocialController.LikeUrl(urlToLike);
				
				GameCommunityUIPanelLoading.ShowGameCommunityLoading("THANK YOU", AppConfigs.appGameDisplayName + " successfully liked!");
				
				Invoke("HideLikeThanks", 4f);
			}
			else {
				likeActionClicked = true;
				GameCommunity.Login();
			}
		//}
	}
	
	private void HideLikeThanks() {
		GameCommunityUIPanelLoading.HideGameCommunityLoading();
	}
	
	public void LeaderboardsShowOnline() {
		Platforms.ShowWebView(
			AppConfigs.appGameDisplayName 
			+ " Online Leaderboards + Competitions", 
			AppConfigs.appUrlWeb
			);
		
		GameCommunity.TrackGameView("Game Community Online", "online-community");
		GameCommunity.TrackGameEvent("online-community", "load", 1);
	}
}
