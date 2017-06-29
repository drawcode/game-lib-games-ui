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

    public override void Awake() {
        base.Awake();

        if(Instance != null && this != Instance) {
            //There is already a copy of this script running
            Destroy(gameObject);
            return;
        }

        Instance = this;
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

    }

    void OnProfileLoggedIn(GameCommunityNetworkUser user) {

    }

    void OnGameCommunityReady() {
        Init();
    }

    void OnGameCommunityLeaderboards(GameCommunityLeaderboardData leaderboardData) {

    }

    public static void SetLoadingMessage(string title, string message) {
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

    public override void OnButtonClickEventHandler(string buttonName) {

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