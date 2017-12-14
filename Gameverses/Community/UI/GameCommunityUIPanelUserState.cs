using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

// using Engine.Data.Json;
using Engine.Events;
using Engine.Networking;

public class GameCommunityUIPanelUserState : UIAppPanelBaseList {

    // switches between login button and facebook network info

    public static GameCommunityUIPanelUserState Instance;

    // not logged
    public GameObject notLoggedInObject;
    public UIImageButton buttonFacebookLogin;

    // logged
    public GameObject loggedInObject;
    public UILabel labelUsername;
    public UILabel labelFirstname;
    public UILabel labelScore;
    public UILabel labelRank;
    public UITexture textureSpriteProfilePicture;
    bool imageLoaded = false;

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
        UpdateLoginState();
    }

    public override void Start() {
        if(notLoggedInObject != null) {
            notLoggedInObject.gameObject.Hide();
        }
        if(loggedInObject != null) {
            loggedInObject.gameObject.Hide();
        }
    }

    void Update() {
        UpdateLoginState();
    }

    void OnProfileLoggedIn(GameCommunityNetworkUser user) {

        LogUtil.Log("GameCommunityUIPanelUserState: OnProfileLoggedIn: network:" + user.network
            + " username:" + user.username
            + " name:" + user.name
            + " first_name:" + user.first_name
            + " user_id:" + user.user_id);

        UpdateLoginState();
    }

    void UpdateLoginState() {

        string networkType = SocialNetworkTypes.facebook;

        if(GameCommunity.IsLoggedIn(networkType)) {
            if(notLoggedInObject != null) {
                notLoggedInObject.gameObject.Hide();
            }
            if(loggedInObject != null) {
                loggedInObject.gameObject.Show();
            }
            LoadProfileInfo(networkType);
        }
        else {

            if(notLoggedInObject != null) {
                notLoggedInObject.gameObject.Show();
            }
            if(loggedInObject != null) {
                loggedInObject.gameObject.Hide();
            }
        }
    }

    public override void OnButtonClickEventHandler(string buttonName) {

        if(buttonFacebookLogin != null) {
            if(buttonName == buttonFacebookLogin.name) {
                GameCommunity.Login(SocialNetworkTypes.facebook);
            }
        }
    }

    public void LoadProfileInfo(string networkType) {
        if(GameCommunity.IsLoggedIn(networkType)) {

            string username = GameProfiles.Current.GetNetworkValueUsername(networkType);//GameCommunity.networkUserName;
            string firstname = GameProfiles.Current.GetNetworkValueFirstName(networkType);//GameCommunity.networkFirstName;
            int highScore = GameCommunity.GetHighScore();
            int rank = GameCommunity.GetRank();
            int rankTotal = GameCommunity.GetRankTotal();
            string rankDisplay = "UNRANKED";

            if(rank > 0) {

                rankDisplay = "#" + rank.ToString("N0");

                if(rankTotal > 0 && rankTotal > rank) {
                    rankDisplay += " of " + rankTotal;
                }

                if(labelFirstname != null) {
                    labelFirstname.gameObject.Hide();
                }

                if(labelRank != null) {
                    labelRank.gameObject.Show();
                }
            }
            else {

                if(labelFirstname != null) {
                    labelFirstname.gameObject.Show();
                }

                if(labelRank != null) {
                    labelRank.gameObject.Hide();
                }
            }

            if(labelRank != null) {
                labelRank.text = rankDisplay;
            }

            if(labelScore != null) {
                labelScore.text = highScore.ToString("N0");
            }

            if(labelUsername != null) {
                labelUsername.text = username;
            }

            if(labelFirstname != null) {
                if(username != firstname) {
                    labelFirstname.text = firstname;
                }
                else {
                    labelFirstname.text = "";
                }
            }
            if(networkType == SocialNetworkTypes.facebook) {
                LoadFacebookProfileImage(username);
            }
        }

    }

    public void LoadFacebookProfileImage(string username) {
        if(!imageLoaded) {
            GameCommunityUIController.LoadFacebookProfileImageByUsername(username, textureSpriteProfilePicture, 48, 48, 1);
            imageLoaded = true;
        }
    }
}