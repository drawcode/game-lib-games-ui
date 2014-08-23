using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

/*
 * 
 
 SAMPLE BASIC GAME COMMUNITY SCENE
  - Uses facebook login
  - Uses [app] platform profiles, statistics, achievements for 
    local persistence in streaming assets/documents cache
  - Get statistics lists
  - Get Facebook scores for app
  - Get Facebook scores for user and friends
  
  See advanced for custom online leaderboards and more stats, also achievements.
  
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
 
  GameCommunity.SendStatistics()
   - Send statistics not sent to all networks
   
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

using Engine.Data.Json;
using Engine.Events;
using Engine.Networking;

public class GameCommunityBasicSceneAppViewerUIButtonNames {	
	
	public static string ButtonSendStatistics = "ButtonSendStatistics";
	public static string ButtonLeaderboards = "ButtonLeaderboards";
	public static string ButtonLeaderboardFriends = "ButtonLeaderboardFriends";
	
	public static string ButtonStatistics = "ButtonStatistics";
	public static string ButtonStatisticHighScoreAddOne = "ButtonStatisticHighScoreAddOne";
	public static string ButtonStatisticHighScoreAddTen = "ButtonStatisticHighScoreAddTen";
	public static string ButtonCloseDialogs = "ButtonCloseDialogs";
	
	public static string ButtonMain = "ButtonMain";
}

public class GameCommunityBasicScene : GameObjectBehavior {
	
	public static GameCommunityBasicScene Instance;	
		    
	public void Awake() {
		
        if (Instance != null && this != Instance) {
            //There is already a copy of this script running
            //Destroy(gameObject);
            return;
        }
		
        Instance = this;	
	}
	
	// facebook uses an int for score for some reason, 
	// and only descending, no time, fastest time, lowest score etc.
	int highScore = 0;	
	
	public UILabel labelHighScore;	
	
	public GameObject containerMainObject;
	public GameObject containerStatisticsListObject;
	public GameObject containerLeaderboardsObject;
	public GameObject containerLeaderboardFriendsObject;
	
	void OnEnable() {
		//Messenger<string>.AddListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);
		
		//Messenger.AddListener(GameCommunityMessages.gameCommunityReady, OnGameCommunityReady);
		//Messenger<GameCommunityNetworkUser>.AddListener(GameCommunityMessages.gameCommunityLoggedIn, OnProfileLoggedIn);
	}
	
	void OnDisable() {
		//Messenger<string>.RemoveListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);
		
		//Messenger.RemoveListener(GameCommunityMessages.gameCommunityReady, OnGameCommunityReady);
		//Messenger<GameCommunityNetworkUser>.RemoveListener(GameCommunityMessages.gameCommunityLoggedIn, OnProfileLoggedIn);
	}
	
	void OnGameCommunityReady() {
		// gameCommunity is ready, use this instead of Start() as it has 
		// some parallel coroutine operations at start.
		//Init();	
	}	
	
	void Init() {		
		//UpdateHighScore();
	}
	
	void OnProfileLoggedIn(GameCommunityNetworkUser user) {
		
		// See PlatformProfileInfo and GameCommunityUIPanelUserState for login handling.
	}
		
	void Start() {
		
	}
		
	void UpdateHighScore() {
		highScore = GameCommunity.GetHighScore();
		
		if(labelHighScore != null) {
			labelHighScore.text = GameCommunity.GetHighScoreFormatted();
		}		
	}
	
	void Update() {
		
	}
	
	void OnButtonClickEventHandler(string buttonName) {
		
		if(buttonName 
			== GameCommunityBasicSceneAppViewerUIButtonNames.ButtonLeaderboards) {
			
			HideAllDialogs();
			
			ShowLeaderboards();
			
			GameCommunityUIPanelLeaderboards.LoadDataFull();
			
		}
		else if(buttonName 
			== GameCommunityBasicSceneAppViewerUIButtonNames.ButtonLeaderboardFriends) {
			
			HideAllDialogs();
			
			ShowLeaderboardFriends();
			
			GameCommunityUIPanelLeaderboards.LoadDataFriends();
			
		}
		else if(buttonName 
			== GameCommunityBasicSceneAppViewerUIButtonNames.ButtonStatistics) {
			
			HideAllDialogs();
			
			ShowStatisticsList();
			
			GameCommunityUIPanelStatistics.Instance.LoadData();
		}
		else if(buttonName 
			== GameCommunityBasicSceneAppViewerUIButtonNames.ButtonStatisticHighScoreAddOne) {
			
			// Here we are adding a score to the current.  IN actual use at the end of a level just send the
			// game score.  If higher than the current it will send to facebook.
			
			highScore += 1;
			
			GameCommunity.SetHighScore(highScore);
			
			UpdateHighScore();
			
			FinishGameSession();
			
		}
		else if(buttonName 
			== GameCommunityBasicSceneAppViewerUIButtonNames.ButtonStatisticHighScoreAddTen) {
			
			// Here we are adding a score to the current.  IN actual use at the end of a level just send the
			// game score.  If higher than the current it will send to facebook.
			
			highScore += 10;
			
			GameCommunity.SetHighScore(highScore);
			
			UpdateHighScore();
			
			FinishGameSession();
			
		}
		else if(buttonName 
			== GameCommunityBasicSceneAppViewerUIButtonNames.ButtonSendStatistics) {
			
			// Set high score automatically calls this, for other stats that may be set this method
			// is required after setting all stats for non facebook networks i.e. custom or gamecenter.
			
			GameCommunity.SyncProfileProgress();
			
		}
		else if(buttonName == GameCommunityBasicSceneAppViewerUIButtonNames.ButtonCloseDialogs) {
			
			HideAllDialogs();
			
			ShowMain();			
		}
	}
	
	public void FinishGameSession() {
		GameCommunity.SetStatisticValue("times-played", 1);
		GameCommunity.SetStatisticValue("time-played", UnityEngine.Random.Range(5f, 50f));
		GameCommunity.SetStatisticValue("shots-made", UnityEngine.Random.Range(2f, 15f));
		GameCommunity.SetStatisticValue("shots-missed", UnityEngine.Random.Range(0f, 8f));
		GameCommunity.SyncProfileProgress();
	}
		
	public void HideAllDialogs() {
		HideStatisticsList();
		HideLeaderboards();
		HideLeaderboardFriends();
		HideMain();
	}
	
	public void ShowMain() {
		containerMainObject.Show();
	}
	
	public void HideMain() {
		containerMainObject.Hide();
	}
		
	public void ShowStatisticsList() {
		containerStatisticsListObject.Show();
	}
	
	public void HideStatisticsList() {
		containerStatisticsListObject.Hide();
	}
		
	public void ShowLeaderboards() {
		containerLeaderboardsObject.Show();
	}
			
	public void HideLeaderboards() {
		containerLeaderboardsObject.Hide();
	}
		
	public void ShowLeaderboardFriends() {
		containerLeaderboardFriendsObject.Show();
	}
			
	public void HideLeaderboardFriends() {
		containerLeaderboardFriendsObject.Hide();
	}
	
}
