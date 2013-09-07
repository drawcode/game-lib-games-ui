using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Engine.Data.Json;
using Engine.Events;
using Engine.Networking;

public class GameCommunityUIPanelLeaderboards : MonoBehaviour {
	
	public GameObject listGridRoot;
    public GameObject listItemPrefab;
	
	public GameCommunityLeaderboardData leaderboardDataFull = null;
	public GameCommunityLeaderboardData leaderboardDataFriends = null;
			
	public static GameCommunityUIPanelLeaderboards Instance;
	
	void Awake () {		
		
        if (Instance != null && this != Instance) {
            //There is already a copy of this script running
            Destroy(gameObject);
            return;
        }
		
        Instance = this;
	}
	
	void OnEnable() {
		Messenger.AddListener(
			GameCommunityPlatformMessages.gameCommunityReady, 
			OnGameCommunityReady);
		
		Messenger<GameCommunityLeaderboardData>.AddListener(
			GameCommunityPlatformMessages.gameCommunityLeaderboardData, 
			OnGameCommunityLeaderboards);
		
		Messenger<GameCommunityLeaderboardData>.AddListener(
			GameCommunityPlatformMessages.gameCommunityLeaderboardFriendData, 
			OnGameCommunityLeaderboardFriends);
	}
	
	void OnDisable() {
		Messenger.RemoveListener(
			GameCommunityPlatformMessages.gameCommunityReady, 
			OnGameCommunityReady);		
		
		Messenger<GameCommunityLeaderboardData>.RemoveListener(
			GameCommunityPlatformMessages.gameCommunityLeaderboardData, 
			OnGameCommunityLeaderboards);
		
		Messenger<GameCommunityLeaderboardData>.RemoveListener(
			GameCommunityPlatformMessages.gameCommunityLeaderboardFriendData, 
			OnGameCommunityLeaderboardFriends);
	}
	
	void OnGameCommunityReady() {
		Init();	
	}
	
	void OnGameCommunityLeaderboards(GameCommunityLeaderboardData leaderboardData) {
		
		leaderboardDataFull = leaderboardData;
		StartCoroutine(LoadDataCo(leaderboardDataFull));
	}
	
	void OnGameCommunityLeaderboardFriends(GameCommunityLeaderboardData leaderboardData) {
		
		leaderboardDataFriends = leaderboardData;
		StartCoroutine(LoadDataCo(leaderboardDataFriends));
	
	}
	
	public void Start() {		
		Reset();
	}
	
	public void Init() {
		LoadData();
	}
	
	public void Reset() {
		ClearList();
		leaderboardDataFull = new GameCommunityLeaderboardData();
		leaderboardDataFriends = new GameCommunityLeaderboardData();
	}
	
	public void LoadData() {
		Reset();
		LoadDataFull();		
	}
	
	public static void LoadDataFull() {
		if(Instance != null) {
			Instance.loadDataFull();
		}
	}
	
	public static void LoadDataFriends() {
		if(Instance != null) {
			Instance.loadDataFriends();
		}
	}
	
	public void loadDataFull() {		
		Reset();
		
		if(Application.isEditor) {
			GameNetworks.ParseTestScoresFacebook(GameNetworks.testFacebookScoresResult);
		}
		else {
			GameCommunity.RequestLeaderboardsDefaultCode(1, 25, GameCommunity.GameLeaderboardType.ALL);
		}
	}
	
	public void loadDataFriends() {	
		Reset();
		
		if(Application.isEditor) {
			GameNetworks.ParseTestScoresFacebook(GameNetworks.testFacebookScoresResult);
		}
		else {
			GameCommunity.RequestLeaderboardFriends();
		}
	}
	
	void ClearList() {		
		if (listGridRoot != null) {
			listGridRoot.DestroyChildren();
		}
	}
	
	public void LoadData(GameCommunityLeaderboardData leaderboardData) {
		StartCoroutine(LoadDataCo(leaderboardData));		
	}
	
	public IEnumerator LoadDataCo(GameCommunityLeaderboardData leaderboardData) {
						
		if (listGridRoot != null) {
			ClearList();
			
	        yield return new WaitForEndOfFrame();
	        listGridRoot.transform.parent.gameObject.GetComponent<UIDraggablePanel>().ResetPosition();
	        yield return new WaitForEndOfFrame();
			
			if(leaderboardData != null) {
			
				int i = 0;
				
				List<GameCommunityLeaderboardItem> leaderboardItems = new List<GameCommunityLeaderboardItem>();
				
				if(leaderboardData.leaderboards.ContainsKey("high-score")) {
					leaderboardItems = leaderboardData.leaderboards["high-score"];
				}
				
		        foreach(GameCommunityLeaderboardItem leaderboardItem in leaderboardItems) {
									
		            GameObject item = NGUITools.AddChild(listGridRoot, listItemPrefab);
		            item.name = "AStatisticItem" + i;				
					
		            item.transform.FindChild("LabelUsername").GetComponent<UILabel>().text = leaderboardItem.name;
					UILabel labelUsername = item.transform.FindChild("LabelName").GetComponent<UILabel>();
					if(leaderboardItem.name != leaderboardItem.username) {
		            	labelUsername.text = leaderboardItem.username;
					}
					else {
		            	labelUsername.text = "";					
					}
					
					string displayValue = leaderboardItem.value.ToString("N0");
					
					item.transform.FindChild("LabelValue").GetComponent<UILabel>().text = displayValue;		
					
					UITexture profilePic = item.transform.FindChild("TextureProfilePic").GetComponent<UITexture>();
					
					GameCommunityPlatformUIController.LoadFacebookProfileImage(leaderboardItem.userId, profilePic, 48, 48, i + 1);
									
					i++;
		        }
			}
			
	        yield return new WaitForEndOfFrame();
	        listGridRoot.GetComponent<UIGrid>().Reposition();
	        yield return new WaitForEndOfFrame();	
	        listGridRoot.transform.parent.gameObject.GetComponent<UIDraggablePanel>().ResetPosition();
	        yield return new WaitForEndOfFrame();
        }
	}
	
}

