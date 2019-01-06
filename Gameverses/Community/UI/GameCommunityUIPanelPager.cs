using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

// using Engine.Data.Json;
using Engine.Events;
using Engine.Networking;

public class GameCommunityUIPanelPager : UIAppPanelBaseList {

    // displays pager for next/previous and pages/totals

    public string pagerCode = "all";
    public static GameCommunityUIPanelPager Instance;

    // next
    public GameObject nextObject;
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    public UIImageButton buttonNext;
#else
    public GameObject buttonNext;
#endif

    // previous
    public GameObject previousObject;
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    public UIImageButton buttonPrevious;
#else
    public GameObject buttonPrevious;
#endif

    // logged
    public GameObject infoObject;
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    public UILabel labelInfo;
    public UILabel labelInfoMore;
#else
    public GameObject labelInfo;
    public GameObject labelInfoMore;
#endif
    public int currentPage = 1;
    public int currentPageSize = 25;
    public int currentTotalCount = 25;
    public int currentStart = 1;
    public int currentEnd = 25;


#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
#else

#endif

    public int currentTotalPages {
        get {
            return (int)Math.Ceiling((double)currentTotalCount / (double)currentPageSize);
        }
    }

    public override void Awake() {
        base.Awake();

        if(Instance != null && this != Instance) {
            //There is already a copy of this script running
            //Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public override void OnEnable() {
        Messenger<string>.AddListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);

        Messenger.AddListener(GameCommunityMessages.gameCommunityReady, OnGameCommunityReady);
        Messenger<GameCommunityNetworkUser>.AddListener(GameCommunityMessages.gameCommunityLoggedIn, OnProfileLoggedIn);
    }

    public override void OnDisable() {
        Messenger<string>.RemoveListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);

        Messenger.RemoveListener(GameCommunityMessages.gameCommunityReady, OnGameCommunityReady);
        Messenger<GameCommunityNetworkUser>.RemoveListener(GameCommunityMessages.gameCommunityLoggedIn, OnProfileLoggedIn);
    }

    void OnGameCommunityReady() {
        // gameCommunity is ready, use this instead of Start() as it has 
        // some parallel coroutine operations at start.
        Init();
    }

    public override void Init() {
        UpdateState();
    }

    public override void Start() {
        HideObjects();
    }

    public void UpdateState() {

        currentEnd = currentPage * currentPageSize;
        currentStart = currentEnd - currentPageSize + 1;

        if(buttonNext != null) {
            if(currentEnd > currentTotalCount) {
                buttonNext.gameObject.Hide();
            }
            else {
                buttonNext.gameObject.Show();
            }
        }

        if(buttonPrevious != null) {
            if(currentPage < 2) {
                buttonPrevious.gameObject.Hide();
            }
            else {
                buttonPrevious.gameObject.Show();
            }
        }

        if(labelInfo != null) {

            UIUtil.SetLabelValue(
                labelInfo,
                String.Format("SHOWING {0}-{1} of {2}",
                currentStart.ToString("N0"), 
                currentEnd.ToString("N0"), 
                currentTotalCount.ToString("N0")));
        }

        if(labelInfoMore != null) {

            UIUtil.SetLabelValue(
                labelInfoMore,
                String.Format("PAGE {0} of {1}",
                currentPage.ToString("N0"), 
                currentTotalPages.ToString("N0")));
        }
    }

    void HideObjects() {

    }

    public static void ShowNextObject() {
        if(Instance != null) {
            Instance.showNextObject();
        }
    }

    public static void HideNextObject() {
        if(Instance != null) {
            Instance.hideNextObject();
        }
    }

    public void showNextObject() {
        if(nextObject != null) {
            nextObject.Show();
        }
    }

    public void hideNextObject() {
        if(nextObject != null) {
            nextObject.Hide();
        }
    }

    void Update() {
        UpdateState();
    }

    void OnProfileLoggedIn(GameCommunityNetworkUser user) {

        LogUtil.Log("GameCommunityUIPanelPager: OnProfileLoggedIn: network:" + user.network
            + " username:" + user.username
            + " name:" + user.name
            + " first_name:" + user.first_name
            + " user_id:" + user.user_id);

        UpdateState();
    }

    public override void OnButtonClickEventHandler(string buttonName) {

        if(buttonNext != null) {
            if(buttonName == buttonNext.name) {
                LoadLeaderboardPage(currentPage + 1);
            }
        }

        if(buttonPrevious != null) {
            if(buttonName == buttonPrevious.name) {
                LoadLeaderboardPage(currentPage - 1);
            }
        }
    }

    public void LoadLeaderboardPage(int page) {

        if(page > currentTotalPages) {
            page = currentTotalPages;
            currentPage = page;
        }

        if(page < 1) {
            page = 1;
            currentPage = page;
        }

        if(page != currentPage) {
            currentPage = page;
            //GameCommunityUIPanelAll.Instance.currentPage = currentPage;
            //GameCommunityUIPanelAll.Instance.LoadLoadingItem();
            //AppViewerUIPanelProgress.Instance.currentPage = currentPage;
            //AppViewerUIPanelProgress.Instance.LoadLoadingItem();
            GameCommunity.RequestLeaderboardsDefaultCode(
                currentPage, currentPageSize, GameCommunity.GameLeaderboardType.ALL);
        }
    }
}