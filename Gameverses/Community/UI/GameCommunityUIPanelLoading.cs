using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Engine.Data.Json;
using Engine.Events;
using Engine.Networking;

public class GameCommunityUIPanelLoading : UIAppPanelBaseList {
		
	public UILabel labelTitle;
	public UILabel labelStatus;
	
	public GameObject container;
	
	public UIImageButton buttonCloseLoading;
	
	public static GameCommunityUIPanelLoading Instance = null;
		
	void Awake () {		
		
        if (Instance != null && this != Instance) {
            //There is already a copy of this script running
            Destroy(gameObject);
            return;
        }
		
        Instance = this;
	}
	
	public override void OnEnable() {
		
		Messenger<string>.AddListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);
		
		Messenger.AddListener(
			GameCommunityPlatformMessages.gameCommunityReady, 
			OnGameCommunityReady);
		
		Messenger<GameCommunityLeaderboardData>.AddListener(
			GameCommunityPlatformMessages.gameCommunityLeaderboardData, 
			OnGameCommunityLeaderboards);
		
		Messenger<GameCommunityNetworkUser>.AddListener(GameCommunityPlatformMessages.gameCommunityLoggedIn, OnProfileLoggedIn);
		
		Messenger.AddListener(
			GameCommunityPlatformMessages.gameCommunityLoginDefer, 
			OnGameCommunityLoginDefer);		
	}
	
	public override void OnDisable() {
		
		Messenger<string>.RemoveListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);
		
		Messenger.RemoveListener(
			GameCommunityPlatformMessages.gameCommunityReady, 
			OnGameCommunityReady);		
		
		Messenger<GameCommunityLeaderboardData>.RemoveListener(
			GameCommunityPlatformMessages.gameCommunityLeaderboardData, 
			OnGameCommunityLeaderboards);	
		
		Messenger<GameCommunityNetworkUser>.RemoveListener(GameCommunityPlatformMessages.gameCommunityLoggedIn, OnProfileLoggedIn);
		
		Messenger.RemoveListener(
			GameCommunityPlatformMessages.gameCommunityLoginDefer, 
			OnGameCommunityLoginDefer);		
	}
	
	void OnGameCommunityLoginDefer() {
		
	}
	
	void OnProfileLoggedIn(GameCommunityNetworkUser user) {
		
	}
	
	void OnGameCommunityReady() {
		Init();	
	}
	
	void OnGameCommunityLeaderboards(GameCommunityLeaderboardData leaderboardData) {
		
	}
	
	public static void SetLoadingMessage(string title, string message){
		if(Instance != null) {
			Instance.setLoadingMessage(title, message);
		}
	}
	
	void setLoadingMessage(string title, string message) {
		if(labelTitle != null) {
			labelTitle.text = title;
		}
		if(labelStatus != null) {
			labelStatus.text = message;
		}
	}
	
	void OnButtonClickEventHandler(string buttonName) {
		
		if(buttonName == buttonCloseLoading.name) {
			
			HideGameCommunityLoading();
			
		}	
	}
		
	public static void ShowGameCommunityLoading(string title, string message) {
		SetLoadingMessage(title, message);
		ShowGameCommunityLoading();
	}
	
	public static void ShowGameCommunityLoading() {
		if(Instance != null) {
			Instance.showGameCommunityLoading();
		}
	}
	
	public static void HideGameCommunityLoading() {
		if(Instance != null) {
			Instance.hideGameCommunityLoading();
		}
	}
	
	public void showGameCommunityLoading() {		
		container.Show();
		GameCommunityUIPanelLogin.HideGameCommunityLogin();
	}
	
	public void hideGameCommunityLoading() {
		container.Hide();
	}
	
	public override void Start() {
		Reset();
	}
	
	public override void Init() {
		
	}
	
	public void Reset() {
		
	}	
		
	public void LoadData() {
		
	}	
}

