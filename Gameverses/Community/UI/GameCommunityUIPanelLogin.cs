using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// using Engine.Data.Json;
using Engine.Events;
using Engine.Networking;

using UnityEngine.UI;

public class GameCommunityUIPanelLogin : UIAppPanelBaseList {

    public static GameCommunityUIPanelLogin Instance;
    public int currentPage = 1;
    public int currentPageSize = 25;

    public GameObject listItemPrefabLeaderboard;
    public GameObject listItemPrefabLoading;
    public GameObject container;
    public GameObject containerLoggedInButtons;
    public GameObject containerNonLoggedInButtons;
    public GameCommunityLeaderboardData leaderboardDataFull = null;

#if USE_UI_NGUI || USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    public UILabel labelTitle;
    public UILabel labelStatus;
    public UIImageButton buttonLoginPanelLike;
    public UIImageButton buttonLoginPanelJoin;
    public UIImageButton buttonLoginPanelClose;
    public UIImageButton buttonLoginPanelNoThanks;
#else
    public GameObject labelTitle;
    public GameObject labelStatus;
    public Button buttonLoginPanelLike;
    public Button buttonLoginPanelJoin;
    public Button buttonLoginPanelClose;
    public Button buttonLoginPanelNoThanks;
#endif

    public override void Awake() {
        base.Awake();

        if(Instance != null && this != Instance) {
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

        Messenger<GameCommunityNetworkUser>.AddListener(
            GameCommunityMessages.gameCommunityLoggedIn, OnProfileLoggedIn);

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

        Messenger<GameCommunityNetworkUser>.RemoveListener(
            GameCommunityMessages.gameCommunityLoggedIn, OnProfileLoggedIn);

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

        string networkType = SocialNetworkTypes.facebook;

        if(GameCommunity.IsLoggedIn(networkType)) {
            HideNonLoggedInButtons();
            ShowLoggedInButtons();
        }
        else {
            ShowNonLoggedInButtons();
            HideLoggedInButtons();
        }
    }

    void SetLoginStatus() {
        string networkType = SocialNetworkTypes.facebook;

        if(GameCommunity.IsLoggedIn(networkType)) {
            if(labelStatus != null) {
                UIUtil.SetLabelValue(labelStatus, "Logged in, GAME ON!");
            }
        }
        else {
            if(labelStatus != null) {
                UIUtil.SetLabelValue(labelStatus, "Logged in, GAME ON!");
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

    void Login(string networkType) {

        HideNonLoggedInButtons();

        GameCommunityUIPanelLoading.ShowGameCommunityLoading(
            "Loading...",
            "Logging into Facebook."
        );

        if(!GameCommunity.IsLoggedIn(networkType)) {
            GameCommunity.Login(networkType);
        }
    }

    public override void OnButtonClickEventHandler(string buttonName) {

        string networkType = SocialNetworkTypes.facebook;

        if(buttonName == buttonLoginPanelJoin.name) {

            Login(networkType);

        }
        else if(buttonName == buttonLoginPanelLike.name) {

#if SOCIAL_USE_FACEBOOK
            GameCommunity.LikeUrl(networkType, AppConfigs.socialFacebookBrandPage);
#endif

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
            UIUtil.SetLabelValue(labelTitle, title);
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
        if(listGridRoot != null) {
            listGridRoot.DestroyChildren();
        }
    }

    public void LoadLoadingItem() {

        ClearList();

        if(listItemPrefabLoading != null) {

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
                GameObject item = NGUITools.AddChild(listGridRoot, listItemPrefabLoading);
#else
            GameObject item = GameObjectHelper.CreateGameObject(
                listItemPrefabLoading, Vector3.zero, Quaternion.identity, false);
            // NGUITools.AddChild(listGridRoot, listItemPrefab);

            item.transform.parent = listGridRoot.transform;
            item.ResetLocalPosition();
#endif
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

        if(listGridRoot != null) {
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

            ListReposition();

            //if(listGridRoot.transform.parent.gameObject.GetComponent<UIDraggablePanel>() == null) {
            //    yield break;
            //}

            //yield return new WaitForEndOfFrame();
            //try {
            //    UIDraggablePanel draggablePanel = listGridRoot.transform.parent.gameObject.GetComponent<UIDraggablePanel>();
            //    if(draggablePanel != null) {
            //        draggablePanel.ResetPosition();
            //    }
            //}
            //catch(Exception e) {
            //    LogUtil.Log(e);
            //    yield break;
            //}
            //RepositionList()

            //yield return new WaitForEndOfFrame();

            if(leaderboardData != null) {

                int i = 0;

                List<GameCommunityLeaderboardItem> leaderboardItems = new List<GameCommunityLeaderboardItem>();

                if(leaderboardData.leaderboards.ContainsKey("high-score")) {
                    leaderboardItems = leaderboardData.leaderboards["high-score"];
                }

                foreach(GameCommunityLeaderboardItem leaderboardItem in leaderboardItems) {

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
                GameObject item = NGUITools.AddChild(listGridRoot, listItemPrefabLeaderboard);
#else
                    GameObject item = GameObjectHelper.CreateGameObject(
                        listItemPrefabLeaderboard, Vector3.zero, Quaternion.identity, false);
                    // NGUITools.AddChild(listGridRoot, listItemPrefab);

                    item.transform.parent = listGridRoot.transform;
                    item.ResetLocalPosition();
#endif

                    item.name = "AStatisticItem" + i;

                    Transform labelUsernameObject = item.transform.Find("LabelUsername");

                    if(labelUsernameObject != null) {
                        UIUtil.SetLabelValue(labelUsernameObject.gameObject, leaderboardItem.username);
                    }

                    Transform labelNameObject = item.transform.Find("LabelName");

                    if(labelNameObject != null) {
                        UIUtil.SetLabelValue(labelNameObject.gameObject, "#" + (i + 1) * currentPage);
                    }

                    //if(leaderboardItem.name != leaderboardItem.username) {
                    //  labelUsername.text = leaderboardItem.username;
                    //}
                    //else {
                    //  labelUsername.text = "";                    
                    //}

                    string url = String.Format("http://graph.facebook.com/{0}/picture", leaderboardItem.username);
                    string displayValue = leaderboardItem.value.ToString("N0");

                    Transform labelValueObject = item.transform.Find("LabelValue");

                    if(labelValueObject != null) {
                        UIUtil.SetLabelValue(labelValueObject.gameObject, displayValue);
                    }

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
                    //UITexture profilePic = item.transform.Find("TextureProfilePic").GetComponent<UITexture>();

                    //GameCommunityUIController.LoadUITextureImage(profilePic, url, 48, 48, i + 1);
#else 

#endif

                    i++;
                }
            }

            yield return new WaitForEndOfFrame();
            ListReposition(listGrid, listGridRoot);
            yield return new WaitForEndOfFrame();

            if(!container.activeInHierarchy) {
                GameCommunity.HideGameCommunityLogin();
                yield break;
            }
        }
    }
}