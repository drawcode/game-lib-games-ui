using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Engine.Data.Json;
using Engine.Events;
using Engine.Networking;

public class GameCommunityUIPanelLeaderboards : UIAppPanelBaseList {
    
    
    public GameObject listItemPrefab;
    public GameCommunityLeaderboardData leaderboardDataFull = null;
    public GameCommunityLeaderboardData leaderboardDataFriends = null;
    public static GameCommunityUIPanelLeaderboards Instance;
    
    void Awake() {      
        
        if (Instance != null && this != Instance) {
            //There is already a copy of this script running
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
    }
    
    public override void OnEnable() {

        base.OnEnable();

        Messenger.AddListener(
            GameCommunityMessages.gameCommunityReady, 
            OnGameCommunityReady);
        
        Messenger<GameCommunityLeaderboardData>.AddListener(
            GameCommunityMessages.gameCommunityLeaderboardData, 
            OnGameCommunityLeaderboards);
        
        Messenger<GameCommunityLeaderboardData>.AddListener(
            GameCommunityMessages.gameCommunityLeaderboardFriendData, 
            OnGameCommunityLeaderboardFriends);
    }
    
    public override void OnDisable() {

        base.OnDisable();

        Messenger.RemoveListener(
            GameCommunityMessages.gameCommunityReady, 
            OnGameCommunityReady);      
        
        Messenger<GameCommunityLeaderboardData>.RemoveListener(
            GameCommunityMessages.gameCommunityLeaderboardData, 
            OnGameCommunityLeaderboards);
        
        Messenger<GameCommunityLeaderboardData>.RemoveListener(
            GameCommunityMessages.gameCommunityLeaderboardFriendData, 
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
    
    public override void Start() {
        Reset();
    }
    
    public override void Init() {
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
        if (Instance != null) {
            Instance.loadDataFull();
        }
    }
    
    public static void LoadDataFriends() {
        if (Instance != null) {
            Instance.loadDataFriends();
        }
    }
    
    public void loadDataFull() {        
        Reset();
        
        if (Application.isEditor) {
            GameNetworks.ParseTestScoresFacebook(GameNetworks.testFacebookScoresResult);
        }
        else {
            GameCommunity.RequestLeaderboardsDefaultCode(1, 25, GameCommunity.GameLeaderboardType.ALL);
        }
    }
    
    public void loadDataFriends() { 
        Reset();
        
        if (Application.isEditor) {
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
            
            if (leaderboardData != null) {
            
                int i = 0;
                
                List<GameCommunityLeaderboardItem> leaderboardItems = new List<GameCommunityLeaderboardItem>();
                
                if (leaderboardData.leaderboards.ContainsKey("high-score")) {
                    leaderboardItems = leaderboardData.leaderboards["high-score"];
                }
                
                foreach (GameCommunityLeaderboardItem leaderboardItem in leaderboardItems) {
                                    
                    GameObject item = NGUITools.AddChild(listGridRoot, listItemPrefab);
                    item.name = "AStatisticItem" + i;               
                    
                    item.transform.FindChild("LabelUsername").GetComponent<UILabel>().text = leaderboardItem.name;
                    UILabel labelUsername = item.transform.FindChild("LabelName").GetComponent<UILabel>();
                    if (leaderboardItem.name != leaderboardItem.username) {
                        labelUsername.text = leaderboardItem.username;
                    }
                    else {
                        labelUsername.text = "";                    
                    }
                    
                    string displayValue = leaderboardItem.value.ToString("N0");
                    
                    item.transform.FindChild("LabelValue").GetComponent<UILabel>().text = displayValue;     
                    
                    UITexture profilePic = item.transform.FindChild("TextureProfilePic").GetComponent<UITexture>();
                    
                    GameCommunityUIController.LoadFacebookProfileImage(leaderboardItem.userId, profilePic, 48, 48, i + 1);
                                    
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

