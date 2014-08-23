using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Engine.Data.Json;
using Engine.Events;
using Engine.Networking;

public class GameCommunityUIPanelLogin : UIAppPanelBaseList {
	
	
    public GameObject listItemPrefabLeaderboard;
    public GameObject listItemPrefabLoading;
	
	public GameObject container;
	public GameObject containerLoggedInButtons;
	public GameObject containerNonLoggedInButtons;
	
	public GameCommunityLeaderboardData leaderboardDataFull = null;
	
	public UILabel labelTitle;
	public UILabel labelStatus;
	
	public UIImageButton buttonLoginPanelLike;
	public UIImageButton buttonLoginPanelJoin;
	public UIImageButton buttonLoginPanelClose;
	public UIImageButton buttonLoginPanelNoThanks;
			
	public static GameCommunityUIPanelLogin Instance;
	
	public int currentPage = 1;
	public int currentPageSize = 25;
		
	void Awake () {		
		
        if (Instance != null && this != Instance) {
            //There is already a copy of this script running
            Destroy(gameObject);
            return;
        }
		
        Instance = this;
		
		ClearList();
		HideNonLoggedInButtons();		
		HideLoggedInButtons();
	}
	
	public override void OnEnable() {
		
		Messenger<string>.AddListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);
		
		Messenger.AddListener(
			GameCommunityMessages.gameCommunityReady, 
			OnGameCommunityReady);
		
		Messenger<GameCommunityLeaderboardData>.AddListener(
			GameCommunityMessages.gameCommunityLeaderboardData, 
			OnGameCommunityLeaderboards);
		
		Messenger<GameCommunityNetworkUser>.AddListener(GameCommunityMessages.gameCommunityLoggedIn, OnProfileLoggedIn);
		
		Messenger.AddListener(
			GameCommunityMessages.gameCommunityLoginDefer, 
			OnGameCommunityLoginDefer);		
	}
	
	public override void OnDisable() {
		
		Messenger<string>.RemoveListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);
		
		Messenger.RemoveListener(
			GameCommunityMessages.gameCommunityReady, 
			OnGameCommunityReady);		
		
		Messenger<GameCommunityLeaderboardData>.RemoveListener(
			GameCommunityMessages.gameCommunityLeaderboardData, 
			OnGameCommunityLeaderboards);	
		
		Messenger<GameCommunityNetworkUser>.RemoveListener(GameCommunityMessages.gameCommunityLoggedIn, OnProfileLoggedIn);
		
		Messenger.RemoveListener(
			GameCommunityMessages.gameCommunityLoginDefer, 
			OnGameCommunityLoginDefer);		
	}
	
	void OnGameCommunityLoginDefer() {
		if(container != null) {
			if(!container.activeSelf) {
				return;
			}
		}
	}
	
	void OnProfileLoggedIn(GameCommunityNetworkUser user) {
		
		LogUtil.Log("GameCommunityUIPanelLogin: OnProfileLoggedIn: network:" + user.network 
			+ " username:" + user.username 
			+ " name:" + user.name 
			+ " first_name:" + user.first_name 
			+ " user_id:" + user.user_id);
		
		//GameCommunity.SetHighScore(GameCommunity.GetHighScore());
		//GameCommunity.SendSync();
		HideGameCommunityLogin();
	}
	
	void OnGameCommunityReady() {
		Init();	
	}
	
	void OnGameCommunityLeaderboards(GameCommunityLeaderboardData leaderboardData) {
		if(container != null) {
			if(!container.activeSelf) {
				return;
			}
		}
		
		leaderboardDataFull = leaderboardData;
		//if(leaderboardData.leaderboardCode
		StartCoroutine(LoadDataCo(leaderboardDataFull));
	}
	
	void ShowHideButtons() {
		
		SetLoginStatus();
		
		if(GameCommunity.IsLoggedIn()) {
			HideNonLoggedInButtons();
			ShowLoggedInButtons();
		}
		else {
			ShowNonLoggedInButtons();
			HideLoggedInButtons();
		}
	}
	
	void SetLoginStatus() {
		if(GameCommunity.IsLoggedIn()) {
			if(labelStatus != null) {
				labelStatus.text = "Logged in, GAME ON!";
			}
		}
		else {
			if(labelStatus != null) {
				labelStatus.text = "Logging in...";
			}
		}
	}
	
	void ShowNonLoggedInButtons() {
		if(containerNonLoggedInButtons != null) {
			containerNonLoggedInButtons.Show();
		}
	}
	
	void HideNonLoggedInButtons() {
		if(containerNonLoggedInButtons != null) {
			containerNonLoggedInButtons.Hide();
		}
	}
	
	void ShowLoggedInButtons() {
		if(containerLoggedInButtons != null) {
			containerLoggedInButtons.Show();
		}
	}
	
	void HideLoggedInButtons() {
		if(containerLoggedInButtons != null) {
			containerLoggedInButtons.Hide();
		}
	}
	
	
	
	void Login() {
		
		HideNonLoggedInButtons();
		
		GameCommunityUIPanelLoading.ShowGameCommunityLoading(
			"Loading...", 
			"Logging into Facebook."
		);
		
		if(!GameCommunity.IsLoggedIn()) {
			GameCommunity.Login();
		}
	}
	
	void OnButtonClickEventHandler(string buttonName) {
		
		if(buttonName == buttonLoginPanelJoin.name) {
			
			Login();
			
		}	
		else if(buttonName == buttonLoginPanelLike.name) {
			
			GameCommunity.LikeUrl(AppConfigs.socialFacebookBrandPage);
			
		}	
		else if(buttonName == buttonLoginPanelClose.name) {
			
			HideGameCommunityLogin();
			
			////AppViewerUIPanelOverlays.Instance.HidePanelSocialNetworkOverlay();
			
			GameCommunity.ProcessTrackers();
			
			Messenger.Broadcast(
				GameCommunityMessages.gameCommunityLoginDefer);
			
		}	
	}
	
	public static void ShowGameCommunityLogin() {
		if(Instance != null) {
			Instance.showGameCommunityLogin();
		}
	}
	
	public static void HideGameCommunityLogin() {
		if(Instance != null) {
			Instance.hideGameCommunityLogin();
		}
	}
	
	public void showGameCommunityLogin() {		
		//container.Show();
		ShowHideButtons();
		//Invoke("loadLeaderboardDataDefault",1);
	}
	
	private void loadLeaderboardDataDefault() {
		LoadDataFull();		
	}
	
	public void hideGameCommunityLogin() {
		//container.Hide();		
		GameCommunity.ProcessTrackers();
	}
	
	public override void Start() {
		Reset(false);
	}
	
	public override void Init() {
	}
	
	public void Reset() {
		Reset(true);
	}
	
	public void Reset(bool loadLoadingItem) {
		ClearList();
		currentPage = 1;
		leaderboardDataFull = new GameCommunityLeaderboardData();
		if(loadLoadingItem) {
			LoadLoadingItem();
		}
	}	
	
	public void SetTitle(string title) {
		if(labelTitle != null) {
			labelTitle.text = title;
		}
	}
	
	public void LoadData() {
		Reset();	
		loadDataFull();
	}
	
	public static void LoadDataFull() {
		if(Instance != null) {
			Instance.loadDataFull();
		}
	}
	
	public void loadDataFull() {		
		Reset();
		
		GameCommunity.RequestLeaderboardsDefaultCode(
			currentPage, currentPageSize, GameCommunity.GameLeaderboardType.ALL);
	}
		
	void ClearList() {		
		if (listGridRoot != null) {
			listGridRoot.DestroyChildren();
		}
	}
	
	public void LoadLoadingItem() {
		ClearList();
		if(listItemPrefabLoading != null) {
			NGUITools.AddChild(listGridRoot, listItemPrefabLoading);
		}
	}
	
	public void LoadData(GameCommunityLeaderboardData leaderboardData) {
		StartCoroutine(LoadDataCo(leaderboardData));		
	}
		
	public IEnumerator LoadDataCo(GameCommunityLeaderboardData leaderboardData) {
				
		
		if(!container.activeInHierarchy) {
			GameCommunity.HideGameCommunityLogin();
			yield break;
		}
						
		if (listGridRoot != null) {
			ClearList();			
			
			if(leaderboardData == null) {
				yield break;
			}
			
			if(leaderboardData.leaderboards.Count == 0) {
				yield break;	
			}
			
			if(listGridRoot.transform.parent == null) {
				yield break;
			}
			
			if(listGridRoot.transform.parent.gameObject.GetComponent<UIDraggablePanel>() == null) {
				yield break;
			}
			
	        yield return new WaitForEndOfFrame();
			try {
				UIDraggablePanel draggablePanel = listGridRoot.transform.parent.gameObject.GetComponent<UIDraggablePanel>();
				if(draggablePanel != null) {
	        		draggablePanel.ResetPosition();
				}
			}
			catch (Exception e) {
				LogUtil.Log(e);
				yield break;
			}
	        yield return new WaitForEndOfFrame();
			
			if(leaderboardData != null) {
			
				int i = 0;
				
				List<GameCommunityLeaderboardItem> leaderboardItems = new List<GameCommunityLeaderboardItem>();
				
				if(leaderboardData.leaderboards.ContainsKey("high-score")) {
					leaderboardItems = leaderboardData.leaderboards["high-score"];
				}
				
		        foreach(GameCommunityLeaderboardItem leaderboardItem in leaderboardItems) {
									
		            GameObject item = NGUITools.AddChild(listGridRoot, listItemPrefabLeaderboard);
		            item.name = "AStatisticItem" + i;				
					
		            item.transform.FindChild("LabelUsername").GetComponent<UILabel>().text = leaderboardItem.username;
					UILabel labelUsername = item.transform.FindChild("LabelName").GetComponent<UILabel>();
					labelUsername.text = "#" + (i + 1) * currentPage;
					
					//if(leaderboardItem.name != leaderboardItem.username) {
		            //	labelUsername.text = leaderboardItem.username;
					//}
					//else {
		            //	labelUsername.text = "";					
					//}
					
					string displayValue = leaderboardItem.value.ToString("N0");
					
					item.transform.FindChild("LabelValue").GetComponent<UILabel>().text = displayValue;		
					
					UITexture profilePic = item.transform.FindChild("TextureProfilePic").GetComponent<UITexture>();
				
					string url = String.Format("http://graph.facebook.com/{0}/picture", leaderboardItem.username);	
					GameCommunityUIController.LoadUITextureImage(profilePic, url, 48, 48, i + 1);
					
					i++;
		        }
			}
			
	        yield return new WaitForEndOfFrame();
	        listGridRoot.GetComponent<UIGrid>().Reposition();
	        yield return new WaitForEndOfFrame();	
	        listGridRoot.transform.parent.gameObject.GetComponent<UIDraggablePanel>().ResetPosition();
	        yield return new WaitForEndOfFrame();
			
			if(!container.activeInHierarchy) {
				GameCommunity.HideGameCommunityLogin();
				yield break;
			}
        }
	}
	
}

