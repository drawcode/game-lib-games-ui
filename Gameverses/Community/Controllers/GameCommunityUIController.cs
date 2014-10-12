using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class GameCommunityUIShares {
    public static string shareCenter = "share-center";
}

public class GameCommunityUIController : UIPanelBase {
    
    public static GameCommunityUIController Instance;
    
    public GameObject containerShares;   
            
    public void Awake() {
        
        if (Instance != null && this != Instance) {
            //There is already a copy of this script running
            //Destroy(gameObject);
            return;
        }
        
        Instance = this;    
    }
    
    
    public static bool isInst {
        get {
            if (Instance != null) {
                return true;
            }
            return false;
        }
    }
    
    public override void OnEnable() {
        base.OnEnable();
    }
    
    public override void OnDisable() {
        base.OnDisable();
    }
    
    public override void Start() {
        base.Start();
    }

    public override void Init() {

        base.Init();
    }
    
    // -------------------------------------------------------------------
    
    // GAME COMMUNITY
    
    public static void ShowGameCommunity() {
        //GameCommunityUIPanelAll.ShowGameCommunity();
        //////AppViewerUIController.NavigateProgress();
        
        GameCommunity.TrackGameView("Game Community", "game-community");
        GameCommunity.TrackGameEvent("game-community", "action", 1);
        //ProcessTrackers();
    }
    
    public static void HideGameCommunity() {
        //GameCommunityUIPanelAll.HideGameCommunity();
        if(Instance != null) {
            Instance.hideGameCommunityPanels();
        }
    }
    
    public virtual void hideGameCommunityPanels() {        
        //Debug.Log("hideGameCommunity");
        
        UIPanelCommunityBroadcast.HideAll();
        UIPanelCommunityCamera.HideAll();
        UIPanelCommunityBackground.HideBackground();
        
        UIPanelCommunityBroadcast.HideBroadcastRecordPlayShare();
        //GameUIController.HideBroadcastRecordingReplayShare();        
    }
    
    //
    
    public static void ShowGameCommunityLogin() {
        GameCommunityUIPanelLogin.ShowGameCommunityLogin();
        GameCommunity.TrackGameView("Login", "game-community-login");
        GameCommunity.TrackGameEvent("game-community-login", "action", 1);
    }
    
    public static void HideGameCommunityLogin() {
        GameCommunityUIPanelLogin.HideGameCommunityLogin();
    }


    //

    public static void ShowBroadcastRecordPlayShare() {
        UIPanelCommunityBroadcast.ShowBroadcastRecordPlayShare();
    }
    
    public static void HideBroadcastRecordPlayShare() {
        UIPanelCommunityBroadcast.HideBroadcastRecordPlayShare();
    }

    //

    // SHARES
    
    public static void ShowSharesCenter() {
        UIPanelCommunityShare.ShowSharesCenter();
    }
    
    public static void HideSharesCenter() {
        UIPanelCommunityShare.HideSharesCenter();
    }
        
    // ACTIONS
    
    public static void ShowActionTools() {
        UIPanelCommunityShare.ShowActionTools();
    }
    
    public static void HideActionTools() {
        UIPanelCommunityShare.HideActionTools();
    }

    //

    public static void ShowActionAppRate() {
        UIPanelCommunityShare.ShowActionAppRate();
    }
    
    public static void HideActionAppRate() {
        UIPanelCommunityShare.HideActionAppRate();
    }

    // -------------------------------------------------------------------

    //
    
    public static void LoadFacebookProfileImageByUsername(
        string username, UITexture textureSpriteProfilePicture, int width, int height, float delay) {
        if (Instance != null) {
            string url = String.Format("http://graph.facebook.com/{0}/picture", username);      
            Instance.loadUITextureImage(
                textureSpriteProfilePicture, url, width, height, delay);
        }
    }
    
    public static void LoadFacebookProfileImage(
        string userId, UITexture textureSpriteProfilePicture, int width, int height, float delay) {
        if (Instance != null) {
            string url = String.Format("http://graph.facebook.com/{0}/picture", userId);        
            Instance.loadUITextureImage(
                textureSpriteProfilePicture, url, width, height, delay);
        }
    }
    
    public static void LoadUITextureImage(
        UITexture textureSprite, string url, int width, int height, float delay) {
        if (Instance != null) {
            Instance.loadUITextureImage(textureSprite, url, width, height, delay);
        }
    }
    
    public void loadUITextureImage(
        UITexture textureSprite, string url, int width, int height, float delay) {
        StartCoroutine(LoadUITextureImageCo(textureSprite, url, width, height, delay));     
    }
    
    public IEnumerator loadUITextureImageCo(
        UITexture textureSprite, string url, int width, int height, float delay) {
        yield return StartCoroutine(LoadUITextureImageCo(textureSprite, url, width, height, delay));        
    }
    
    public static IEnumerator LoadFacebookProfileImageCo(
        string userId, UITexture textureSpriteProfilePicture, int width, int height, float delay) {
        if (Instance != null) {
            string url = String.Format("http://graph.facebook.com/{0}/picture", userId);        
            yield return Instance.StartCoroutine(LoadUITextureImageCo(
                textureSpriteProfilePicture, url, width, height, delay));
        }
    }
    
    // -------------------------------------------------------------------
        
    public static List<string> urls404 = new List<string>();
    
    public static IEnumerator LoadUITextureImageCo(UITexture textureSprite, string url, int width, int height, float delay) {
                
        if (!string.IsNullOrEmpty(url) && !urls404.Contains(url)) {
            
            urls404.Add(url);
            
            yield return new WaitForSeconds(delay * .5f);
                                    
            Texture2D tex = null;
            
            if (textureSprite != null) {                            
                
                if (tex == null) {
                    tex = new Texture2D(48, 48, TextureFormat.RGB24, true);
                    
                    WWW www = new WWW(url);
                    yield return www;
            
                    if (www.error != null) {
                        //LogUtil.Log("Error loading image:" + www.error);
                    }
                    else {                      
                        www.LoadImageIntoTexture(tex);
                        if (tex != null) {
                            textureSprite.mainTexture = tex;
                        }
                                
                    }
                    www.Dispose();
                    www = null;
                                
                }
                
                if (textureSprite != null) {
                    Vector3 imageScale = textureSprite.transform.localScale.WithX(width).WithY(height);
                    textureSprite.transform.localScale = imageScale;
                }
            }
        }
    }
    
}
