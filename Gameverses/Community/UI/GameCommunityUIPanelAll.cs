using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Engine.Data.Json;
using Engine.Events;
using Engine.Networking;

public class GameCommunityUIPanelAll : UIAppPanelBaseList {
    
    
    public GameObject listItemPrefabStatistic;
    public GameObject listItemPrefabAchievement;
    public GameObject listItemPrefabLeaderboard;
    public GameObject listItemPrefabLoading;
    public GameObject listItemPrefabPaging;
    public GameObject containerLists;
    public GameCommunityLeaderboardData leaderboardDataFull = null;
    public GameCommunityLeaderboardData leaderboardDataFriends = null;
    public GameCommunityStatisticData statisticData = null;
    public UILabel labelTitle;
    public UIImageButton buttonFull;
    public UIImageButton buttonFriends;
    public UIImageButton buttonStats;
    public UIImageButton buttonOnline;
    public UIImageButton buttonClose;
    public LeaderboardFilterType leaderboardType = LeaderboardFilterType.FULL;
    public static GameCommunityUIPanelAll Instance;
    public GameCommunityUIPanelPager pagerObject;
    public int currentPage = 1;
    public int currentPageSize = 25;
    
    public enum LeaderboardFilterType {
        FULL,
        FRIENDS
    }
    
    void Awake() {      
        
        if (Instance != null && this != Instance) {
            //There is already a copy of this script running
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        
        ClearList();
    }
    
    public override void OnEnable() {

        base.OnEnable();
        
        Messenger<string>.AddListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);
        
        Messenger.AddListener(
            GameCommunityMessages.gameCommunityReady, 
            OnGameCommunityReady);
        
        Messenger<GameCommunityLeaderboardData>.AddListener(
            GameCommunityMessages.gameCommunityLeaderboardData, 
            OnGameCommunityLeaderboards);
        
        Messenger<GameCommunityLeaderboardData>.AddListener(
            GameCommunityMessages.gameCommunityLeaderboardFriendData, 
            OnGameCommunityLeaderboardFriends);     
        
        Messenger<GameCommunityNetworkUser>.AddListener(GameCommunityMessages.gameCommunityLoggedIn, OnProfileLoggedIn);
    }
    
    public override void OnDisable() {

        base.OnDisable();
        
        Messenger<string>.RemoveListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);
        
        Messenger.RemoveListener(
            GameCommunityMessages.gameCommunityReady, 
            OnGameCommunityReady);      
        
        Messenger<GameCommunityLeaderboardData>.RemoveListener(
            GameCommunityMessages.gameCommunityLeaderboardData, 
            OnGameCommunityLeaderboards);
        
        Messenger<GameCommunityLeaderboardData>.RemoveListener(
            GameCommunityMessages.gameCommunityLeaderboardFriendData, 
            OnGameCommunityLeaderboardFriends);     
        
        Messenger<GameCommunityNetworkUser>.RemoveListener(GameCommunityMessages.gameCommunityLoggedIn, OnProfileLoggedIn);
    }
    
    void OnProfileLoggedIn(GameCommunityNetworkUser user) {
        
        LogUtil.Log("GameCommunityUIPanelAll: OnProfileLoggedIn: network:" + user.network 
            + " username:" + user.username 
            + " name:" + user.name 
            + " first_name:" + user.first_name 
            + " user_id:" + user.user_id);
        
        //GameCommunity.SetHighScore(GameCommunity.GetHighScore());
        //GameCommunity.SendStatistics();
        HideLoading();
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
    
    public override void OnButtonClickEventHandler(string buttonName) {
        
        if (buttonName == buttonFull.name) {
            
            GameCommunityUIPanelAll.LoadDataFull();
            
            SetTitle("FULL LEADERBOARD");
            
            GameCommunity.TrackGameView("Leaderboards Full", "leaderboards-full");
            GameCommunity.TrackGameEvent("leaderboards-full", "load", 1);
            
        }
        else if (buttonName == buttonFriends.name) {
                        
            GameCommunityUIPanelAll.LoadDataFriends();
                        
            SetTitle("FRIEND LEADERBOARD");
            
            GameCommunity.TrackGameView("Leaderboards Friends", "leaderboards-friends");
            GameCommunity.TrackGameEvent("leaderboards-friends", "load", 1);
            
        }
        else if (buttonName == buttonStats.name) {
                        
            GameCommunityUIPanelAll.LoadDataStatistics();
            
            SetTitle("MY STATISTICS");
            
            GameCommunity.TrackGameView("Statistics", "statistics");
            GameCommunity.TrackGameEvent("statistics", "load", 1);
            
        }
        else if (buttonName == buttonOnline.name) {
                        
            Platforms.ShowWebView(
                AppConfigs.appGameDisplayName 
                + " Online Leaderboards + Competitions", 
                AppConfigs.appUrlWeb
            );
            
            GameCommunity.TrackGameView("Game Community Online", "online-community");
            GameCommunity.TrackGameEvent("online-community", "load", 1);
            
        }
        else if (buttonName == buttonClose.name) {
            
            HideGameCommunity();
        }
        else if (buttonName.ToLower().IndexOf("leaderboard") > -1) {
            
            //ShowGameCommunity();
        }       
    }
    
    public void ShowLeaderboardFull() {     
        GameCommunityUIPanelAll.ShowGameCommunity();
        
        GameCommunity.TrackGameView("Leaderboards Full", "leaderboards-full");
        GameCommunity.TrackGameEvent("leaderboards-full", "load", 1);
    }
    
    public static void ShowGameCommunity() {
        if (Instance != null) {
            Instance.showGameCommunity();
        }
    }
    
    public static void HideGameCommunity() {
        if (Instance != null) {
            Instance.hideGameCommunity();
        }
    }
    
    public void showGameCommunity() {   
        containerLists.Show();
        
        if (Context.Current.hasNetworkAccess) {
            Invoke("loadLeaderboardDataDefault", 1);
        }
    }
    
    private void loadLeaderboardDataDefault() {
        LoadDataFull();     
    }
    
    public void hideGameCommunity() {
        containerLists.Hide();
        GameCommunityUIPanelLoading.HideGameCommunityLoading();
        GameCommunity.ProcessTrackers();
    }
    
    public override void Start() {
        Reset(false);
    }
    
    public override void Init() {
        //LoadData();
    }
        
    public void Reset() {
        Reset(true);
    }
        
    public void Reset(bool loadLoadingItem) {
        ClearList();
        currentPage = 1;
        leaderboardDataFull = new GameCommunityLeaderboardData();
        leaderboardDataFriends = new GameCommunityLeaderboardData();
        statisticData = new GameCommunityStatisticData();
        if (loadLoadingItem) {
            LoadLoadingItem();
        }
    }
    
    public void SetTitle(string title) {
        if (labelTitle != null) {
            labelTitle.text = title;
        }
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
    
    public static void LoadDataStatistics() {
        if (Instance != null) {
            Instance.loadDataStatistics();
        }
    }
    
    public void loadDataFull() {        
        Reset();
        
        leaderboardType = LeaderboardFilterType.FULL;
        
        GameCommunityUIPanelLoading.ShowGameCommunityLoading("Loading Leaderboards", "Fetching full leaderboard from server");
        
        GameCommunity.RequestLeaderboardsDefaultCode(
            currentPage, currentPageSize, GameCommunity.GameLeaderboardType.ALL);
    }
        
    public void HideLoading() {
        GameCommunityUIPanelLoading.HideGameCommunityLoading();
    }
    
    public void loadDataFriends() { 
        Reset();

        string networkType = SocialNetworkTypes.facebook;
        
        leaderboardType = LeaderboardFilterType.FRIENDS;
        
        GameCommunityUIPanelLoading.ShowGameCommunityLoading(
            "Loading Friend Scores", 
            "You must be logged into Facebook for this feature.");
        
        Invoke("HideLoading", 3);
        
        if (!GameCommunity.IsLoggedIn(networkType) && !Application.isEditor) {
            GameCommunity.Login(networkType);
        }
        else {          
            if (Application.isEditor) {
                GameNetworks.ParseTestScoresFacebook(GameNetworks.testFacebookScoresResult);
            }
            else {
                GameCommunity.RequestLeaderboardFriends();
            }
        }       
    }
    
    public void loadDataStatistics() {  
        Reset();
        
        statisticData = GameCommunity.GetProfileStatisticData(GamePacks.Current.code, AppContentStates.Current.code);
        LoadData(statisticData);
    }
    
    void ClearList() {      
        if (listGridRoot != null) {
            listGridRoot.DestroyChildren();
        }
    }
    
    public void LoadLoadingItem() {
        ClearList();
        if (listItemPrefabLoading != null) {
            NGUITools.AddChild(listGridRoot, listItemPrefabLoading);
        }
    }
    
    public void LoadData(GameCommunityLeaderboardData leaderboardData) {
        StartCoroutine(LoadDataCo(leaderboardData));        
    }
    
    public void LoadData(GameCommunityStatisticData statData) {
        StartCoroutine(LoadDataCo(statData));       
    }
    
    public IEnumerator LoadDataCo(GameCommunityLeaderboardData leaderboardData) {
                
        if (!containerLists.activeInHierarchy) {
            GameCommunityUIPanelAll.HideGameCommunity();
            yield break;
        }
                        
        if (listGridRoot != null) {
            ClearList();            
            
            if (leaderboardData == null) {
                yield break;
            }
            
            if (leaderboardData.leaderboards.Count == 0) {
                yield break;    
            }
            
            if (listGridRoot.transform.parent == null) {
                yield break;
            }
            
            if (listGridRoot.transform.parent.gameObject.GetComponent<UIDraggablePanel>() == null) {
                yield break;
            }
            
            yield return new WaitForEndOfFrame();
            try {
                foreach (UIDraggablePanel panel in listGridRoot.transform.parent.gameObject.GetComponentsInChildren<UIDraggablePanel>()) {
                    //listGridRoot.transform.parent.gameObject.GetComponent<UIDraggablePanel>().ResetPosition();
                    panel.ResetPosition();
                    break;
                }
            }
            catch (Exception e) {
                LogUtil.Log(e);
                yield break;
            }
            yield return new WaitForEndOfFrame();
            
            if (leaderboardData != null) {
            
                int i = 0;
                
                List<GameCommunityLeaderboardItem> leaderboardItems = new List<GameCommunityLeaderboardItem>();
                
                if (leaderboardData.leaderboards.ContainsKey("high-score")) {
                    leaderboardItems = leaderboardData.leaderboards["high-score"];
                }
                
                foreach (GameCommunityLeaderboardItem leaderboardItem in leaderboardItems) {
                                    
                    GameObject item = NGUITools.AddChild(listGridRoot, listItemPrefabLeaderboard);
                    item.name = "AStatisticItem" + i;               
                                        
                    string displayValue = leaderboardItem.valueFormatted;               
                    item.transform.Find("LabelValue").GetComponent<UILabel>().text = displayValue; 
                    
                    item.transform.Find("LabelUsername").GetComponent<UILabel>().text = leaderboardItem.username;
                    UILabel labelRank = item.transform.Find("LabelName").GetComponent<UILabel>();
                    int rank = (((i) + ((currentPage * currentPageSize) - currentPageSize)) + 1);
                    leaderboardItem.rank = rank;
                    string rankText = "#" + rank.ToString("N0");
                    labelRank.text = rankText;
                    
                    
                    UITexture profilePic = item.transform.Find("TextureProfilePic").GetComponent<UITexture>();
                    
                    //if (leaderboardItem.network == GameProfiles.Current.GetSocialNetworkType() 
                    //    && leaderboardItem.username == GameProfiles.Current.GetSocialNetworkUserName()) {
                    //    GameCommunity.SetRank((double)leaderboardItem.rank);
                    //}
                
                    string url = String.Format("http://graph.facebook.com/{0}/picture", leaderboardItem.username);  
                    GameCommunityUIController.LoadUITextureImage(profilePic, url, 48, 48, i + 1);
                    
                    i++;
                }
                                    
                if (leaderboardType == LeaderboardFilterType.FULL) {
                    GameObject itemPager = NGUITools.AddChild(listGridRoot, listItemPrefabPaging);
                    itemPager.name = "AStatisticItem" + i++;
                    
                    foreach (GameCommunityUIPanelPager pager in itemPager.GetComponentsInChildren<GameCommunityUIPanelPager>()) {
                        pager.currentPageSize = currentPageSize;
                        pager.currentPage = currentPage;
                        pager.currentTotalCount = leaderboardData.totalCount;
                        pager.UpdateState();
                        break;
                    }       
                }
            }
            
            yield return new WaitForEndOfFrame();
            listGridRoot.GetComponent<UIGrid>().Reposition();
            yield return new WaitForEndOfFrame();   
            foreach (UIDraggablePanel panel in listGridRoot.transform.parent.gameObject.GetComponentsInChildren<UIDraggablePanel>()) {
                panel.ResetPosition();
                break;
            }
            yield return new WaitForEndOfFrame();
            
                    
            if (!containerLists.activeInHierarchy) {
                GameCommunityUIPanelAll.HideGameCommunity();
                yield break;
            }
        }
        
        HideLoading();
    }
    
    IEnumerator LoadDataCo(GameCommunityStatisticData statisticData) {
                
        if (!containerLists.activeInHierarchy) {
            GameCommunityUIPanelAll.HideGameCommunity();
            yield break;
        }
                    
        GameCommunityUIPanelLoading.ShowGameCommunityLoading("Loading Statistics", "Loading local statistics and progress...");
        if (listGridRoot != null) {
            ClearList();
            
            if (statisticData == null) {
                yield break;
            }
            
            if (statisticData.statistics.Count == 0) {
                yield break;
            }
            
            if (listGridRoot.transform.parent == null) {
                yield break;
            }
            
            if (listGridRoot.transform.parent.gameObject.GetComponent<UIDraggablePanel>() == null) {
                yield break;
            }
            
            
            yield return new WaitForEndOfFrame();
            
            try {
                listGridRoot.transform.parent.gameObject.GetComponent<UIDraggablePanel>().ResetPosition();
            }
            catch (Exception e) {
                LogUtil.Log(e);
                yield break;
            }
            yield return new WaitForEndOfFrame();
                    
            int i = 0;
            
            foreach (GameCommunityStatisticItem statistic in statisticData.statistics) {
                AddListItem(statistic, i);
                i++;
            }
            
            // shot percentage
            double shotsMade = GameCommunity.GetStatisticValueDouble("shots-made");
            double shotsMissed = GameCommunity.GetStatisticValueDouble("shots-missed");
            double totalShots = shotsMade + shotsMissed;
            
            string valueTotalShotsFormatted = (totalShots).ToString("N0");
            
            AddListItem(
                CreateRuntimeStatistic(
                    "total-shots", 
                    "Total Shots", 
                    "Total shots taken", 
                    "int", 
                    valueTotalShotsFormatted), 
                i++);
            
            double shotPercentageValue = shotsMade / (shotsMade + shotsMissed);
                        
            if (shotPercentageValue < 0) {
                shotPercentageValue = 0;
            }           
            
            shotPercentageValue = (float)Mathf.Clamp((float)shotPercentageValue, 0, 1);
            
            string valueFormatted = (shotPercentageValue).ToString("P2");
            if (valueFormatted == "NaN") {
                valueFormatted = "0%";
            }
            
            AddListItem(
                CreateRuntimeStatistic(
                    "shot-percentage", 
                    "Shot Percentage", 
                    "Percentage of shots made", 
                    "percent", 
                    valueFormatted), 
                i++);
            
            yield return new WaitForEndOfFrame();
            listGridRoot.GetComponent<UIGrid>().Reposition();
            yield return new WaitForEndOfFrame();   
            listGridRoot.transform.parent.gameObject.GetComponent<UIDraggablePanel>().ResetPosition();
            yield return new WaitForEndOfFrame();   
            
            if (!containerLists.activeInHierarchy) {
                GameCommunityUIPanelAll.HideGameCommunity();
                yield break;
            }
            
        }
        HideLoading();
    }
    
    public GameCommunityStatisticItem CreateRuntimeStatistic(string code, string displayName, string description, string type, string valueFormatted) {
        GameCommunityStatisticItem statItem = new GameCommunityStatisticItem();
        
        statItem.code = code;
        statItem.displayName = displayName;
        statItem.description = description;
        statItem.type = type;
        statItem.valueFormatted = valueFormatted;
                
        return statItem;
    }
    
    public void AddListItem(GameCommunityStatisticItem statistic, int increment) {
                                
        GameObject item = NGUITools.AddChild(listGridRoot, listItemPrefabStatistic);
        item.name = "AStatisticItem" + increment;               
        
        item.transform.Find("LabelName").GetComponent<UILabel>().text = statistic.displayName;
        item.transform.Find("LabelDescription").GetComponent<UILabel>().text = statistic.description;              
        item.transform.Find("LabelValue").GetComponent<UILabel>().text = statistic.valueFormatted;     
    }
    
}

