using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using Engine.Data.Json;
using Engine.Events;
using Engine.Networking;

public class GameCommunitySocialController : GameObjectBehavior {
    
    public static GameCommunitySocialController Instance;
    public float currentTimeBlock = 0.0f;
    public float actionInterval = 1.0f;

    public Material photoMaterial;
    string fileName;
    string filePath;

    string currentMessageFacebook = "";
    string currentMessageTwitter = "";

    public void Awake() {
        
        if (Instance != null && this != Instance) {
            //There is already a copy of this script running
            //Destroy(gameObject);
            return;
        }
        
        Instance = this;
        
        Init();
    }
    
    void OnEnable() {
        Messenger<GameCommunityMessageResult>.AddListener(
            GameCommunityMessages.gameCommunityResultMessage, OnGameCommunityResultMessage);
        
        
        #if UNITY_ANDROID || UNITY_IPHONE
        //TwitterManager.loginSucceededEvent += twitterLoginSucceededEvent;
        //TwitterManager.loginFailedEvent += twitterLoginFailedEvent;
        TwitterManager.requestDidFinishEvent += twitterRequestDidFinishEvent;
        
        TwitterManager.requestDidFailEvent += twitterRequestDidFailEvent;
        TwitterManager.tweetSheetCompletedEvent += twitterTweetSheetCompletedEvent;
        //TwitterManager.tweetSheetFailedEvent += twitterTweetSheetFailedEvent;
        #endif
    }
    
    void OnDisable() {
        Messenger<GameCommunityMessageResult>.RemoveListener(
            GameCommunityMessages.gameCommunityResultMessage, OnGameCommunityResultMessage); 

        
        #if UNITY_ANDROID || UNITY_IPHONE
        //TwitterManager.loginSucceededEvent -= twitterLoginSucceededEvent;
        //TwitterManager.loginFailedEvent -= twitterLoginFailedEvent;
        TwitterManager.requestDidFinishEvent -= twitterRequestDidFinishEvent;
        
        TwitterManager.requestDidFailEvent -= twitterRequestDidFailEvent;
        TwitterManager.tweetSheetCompletedEvent -= twitterTweetSheetCompletedEvent;
        //TwitterManager.tweetSheetFailedEvent -= twitterTweetSheetFailedEvent;
        #endif
    }

    void twitterTweetSheetCompletedEvent(bool completed) {

        if(completed) {
            GameCommunityController.SendResultMessage(
                Locos.Get(LocoKeys.social_twitter_upload_success_title), 
                Locos.Get(LocoKeys.social_twitter_upload_success_message));
        }
        else {
            GameCommunityController.SendResultMessage(
                Locos.Get(LocoKeys.social_twitter_upload_error_title), 
                Locos.Get(LocoKeys.social_twitter_upload_error_message));
        }
    }

    void twitterRequestDidFinishEvent(object data) {

        GameCommunityController.SendResultMessage(
            Locos.Get(LocoKeys.social_twitter_upload_success_title), 
            Locos.Get(LocoKeys.social_twitter_upload_success_message));

    }
    
    void twitterRequestDidFailEvent(object data) {
        
        GameCommunityController.SendResultMessage(
            Locos.Get(LocoKeys.social_twitter_upload_error_title), 
            Locos.Get(LocoKeys.social_twitter_upload_error_message));
        
    }

    void OnGameCommunityResultMessage(GameCommunityMessageResult result) {
        
        // show message from title, message in alert
        //GameCommunityUIPanelLoading.ShowGameCommunityLoading(
        //    result.title, 
        //    result.message
        //);      

        if (result == null) {
            return;
        }

        if (string.IsNullOrEmpty(result.title)) {
            return;
        }

        if (result.title.ToLower().Contains("error")) {
            UINotificationDisplay.QueueError(result.title, result.message);            
        }
        else {
            UINotificationDisplay.QueueInfo(result.title, result.message);
        }
    }
    
    void Init() {
        
    }
    
    void Start() {  
        initSocial();
    }
    
    public void initSocial() {      
        //Invoke("initFacebook", 2);    
        //Invoke("initTwitter", 4);
    }

    public static void LikeUrl(string networkType, string url) {
        if (Instance != null) {
            Instance.likeUrl(networkType, url);
        }
    }
    
    public void likeUrl(string networkType, string url) {   

        if (networkType == SocialNetworkTypes.facebook) {
            // like facebook
            SocialNetworks.LikeUrlFacebook(url);
        }
        else if (networkType == SocialNetworkTypes.gameverses) { 
                
            // like custom
        }
        else if (networkType == SocialNetworkTypes.twitter) {
        
            // like other
        }
    }

    public static void CaptureCameraPhoto(string key, Camera cam, Material photoPlaceholderMaterial) {
        if (Instance != null) {
            Instance.captureCameraPhoto(key, cam, photoPlaceholderMaterial);
        }
    }
    
    public void captureCameraPhoto(string key, Camera cam, Material photoPlaceholderMaterial) {        
        photoMaterial = photoPlaceholderMaterial;
        StartCoroutine(captureCameraPhotoCo(key, cam));
    }
    
    IEnumerator captureCameraPhotoCo(string key, Camera cam) {
        
        yield return new WaitForEndOfFrame();
        
        fileName = key.ToLower() + "-screenshot-" + System.DateTime.Now.Ticks.ToString() + ".png";
        filePath = Application.persistentDataPath + "/" + fileName;
        
        //File.Delete(filePath);
                
        RenderTexture renderTexture = RenderTexture.active;
        
        RenderTexture.active = cam.targetTexture;

        cam.Render();

        Texture2D tex = new Texture2D(cam.targetTexture.width, cam.targetTexture.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
        tex.Apply();
        
        RenderTexture.active = renderTexture;        

        while (File.Exists(filePath) == false) {
            yield return null;
        }

        WWW www = new WWW("file://" + filePath);
        
        yield return www;
        
        if (www.error != null) {
            //LogUtil.Log("Cannot load file"+www.error+" Path:"+www.url);
        }
        www.LoadImageIntoTexture(tex);
        
        if (photoMaterial != null) {
            photoMaterial.mainTexture = tex;
        }
        else {
            LogUtil.Log("Set photoMaterial property with read/write material/texture before taking photo");
        }
    }

    // TAKE PHOTO
        
    public static void TakePhoto(Material photoPlaceholderMaterial) {
        if (Instance != null) {
            Instance.takePhoto(photoPlaceholderMaterial);
        }
    }
    
    public void takePhoto(Material photoPlaceholderMaterial) {
        photoMaterial = photoPlaceholderMaterial;
        TakePhoto();
    }
    
    public static void TakePhoto() {
        if (Instance != null) {
            Instance.takePhoto();
        }
    }

    public void takePhoto() {
        StartCoroutine(takePhotoCo());
    }

    IEnumerator takePhotoCo() {
        //yield return new WaitForSeconds(0.5f);
        
        fileName = "screenshot.png";
        filePath = Application.persistentDataPath + "/" + fileName;
        
        File.Delete(filePath);

#if UNITY_EDITOR
        Application.CaptureScreenshot(filePath);
#else
        /**
         * For iOS you have to pass in just the name, while it seems like
         * an absolute path is necessary/ or it puts it in a location that is
         * different than persistentDataPath.
        **/
        Application.CaptureScreenshot(fileName);
#endif

        /**
         * We wait for a half second so that the photo can be taken, otherwise
         * we'll get the 'saving screenshot' screen on it. Hopefully this is long
         * enough for all devices. I think we can assume so. In the editor this
         * means that you won't see the 'saving screenshot' screen.
        **/

        //yield return new WaitForSeconds(0.5f);
        //displayPendingSaveAnimation();

        while (File.Exists(filePath) == false) {
            yield return null;
        }

        //LogUtil.Log("screen capture complete!");

        var tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        WWW www = new WWW("file://" + filePath);

        yield return www;

        if (www.error != null) {
            //LogUtil.Log("Cannot load file"+www.error+" Path:"+www.url);
        }
        www.LoadImageIntoTexture(tex);
        
        if (photoMaterial != null) {
            photoMaterial.mainTexture = tex;
        
            //AppViewerUIController.Instance.HidePhotoCompleteActionUI();
            //AppViewerUIController.Instance.ShowPhotoShareActionUI();
        
            //updatePhotoPreview();
        }
        else {
            LogUtil.Log("Set photoMaterial property with read/write material/texture before taking photo");
        }
    }

    public static void UpdatePhotoPreview() {
        if (Instance != null) {
            Instance.updatePhotoPreview();
        }
    }

    public void updatePhotoPreview() {
        // load this into the preview image texture.
        GameObject photoPreview = GameObject.Find("PhotoPreview");
        if (photoPreview != null) {
            UITexture uiTexture = photoPreview.GetComponent<UITexture>();
            if (uiTexture != null) {
                uiTexture.material = photoMaterial;
            }
        }
    }

    public static void StartPhotoUploadToFacebook() {
        if (Instance != null) {
            Instance.startFacebookPhotoUploadProcess();
        }
    }

    public void startFacebookPhotoUploadProcess() {

        bool loggedIn = GameCommunity.IsLoggedIn(SocialNetworkTypes.facebook);
        
        Debug.Log("GameCommunitySocialController:startFacebookPhotoUploadProcess:Logging in facebook: urlscheme:" + AppConfigs.appUrlScheme);

        Debug.Log("GameCommunitySocialController:startFacebookPhotoUploadProcess:" 
            + " loggedIn:" + loggedIn);

        if (loggedIn) {
            displayPendingUploadAnimation();
            uploadCurrentPhotoToFacebook();
        }
        else {      
            //LogUtil.Log("We have a valid session.");
            GameCommunity.Login(SocialNetworkTypes.facebook);
        }
    }
    
    //public static void UploadPhotoToTwitter() {
    //    if (Instance != null) {
    //        Instance.uploadPhotoToTwitter();
    //    }
    //}
    
    public void uploadPhotoToTwitter() {
        startTwitterPhotoUploadProcess();
    }
    
    public static void StartPhotoUploadToTwitter() {
        if (Instance != null) {
            Instance.startTwitterPhotoUploadProcess();
        }
    }

    public void startTwitterPhotoUploadProcess() {
                
        bool loggedIn = GameCommunity.IsLoggedIn(SocialNetworkTypes.twitter);

        if (loggedIn) {//SocialNetworks.IsTwitterAvailable()) {
            displayPendingUploadAnimation();
            uploadCurrentPhotoToTwitter();
        }
        else {

            if (SocialNetworks.IsTwitterAvailable()) {
                GameCommunity.Login(SocialNetworkTypes.twitter);
            
            }
            else {
            
                GameCommunityController.SendResultMessage(
                Locos.Get(LocoKeys.social_twitter_disabled_title), 
                Locos.Get(LocoKeys.social_twitter_disabled_message));
            }
        }
    }
    
    //public static void UploadCurrentPhotoToFacebook() {
    //    if (Instance != null) {
    //        Instance.uploadCurrentPhotoToFacebook();
    //    }
    //}
    
    public void uploadCurrentPhotoToFacebook() {
        uploadPhotoToFacebook(photoMaterial);
    }
    
    public void uploadPhotoToFacebook(Material photoMaterialPlaceholder) {
        
        Texture2D tex = (Texture2D)photoMaterialPlaceholder.mainTexture;
        uploadPhotoToFacebook(tex);
    }

    public void uploadPhotoToFacebook(Texture2D tex) {

        // normally, we would just encode the Texture to a PNG but Facebook does not like Unity created PNG's since 3.4.0 came out
        //var bytes = tex.EncodeToPNG();        
        //var tex = new Texture2D( Screen.width, Screen.height, TextureFormat.RGB24, false );
        //tex.ReadPixels( new Rect( 0, 0, Screen.width, Screen.height ), 0, 0, false );
        //var bytes = tex.EncodeToPNG();
        //Destroy( tex );
        
        // We will instead make a JPG and upload that
        var encoder = new ImageJPGEncoder(tex, 95.0f);
        encoder.doEncoding();
        var bytes = encoder.GetBytes();     
        
        displayPendingUploadAnimation();

        Facebook.instance.postImage(
            bytes, 
            currentMessageFacebook,
            onFacebookUploadComplete);     
    }

    public void onFacebookUploadComplete(string error, object result) {        

        if (error != null) {
            //LogUtil.LogError( error );
                        
            GameCommunityController.SendResultMessage(
                Locos.Get(LocoKeys.social_facebook_upload_error_title), 
                Locos.Get(LocoKeys.social_facebook_upload_error_message) + error);
        }
        else {          
            GameCommunityController.SendResultMessage(
                Locos.Get(LocoKeys.social_facebook_upload_success_title), 
                Locos.Get(LocoKeys.social_facebook_upload_success_message));
        }
    }

    // TWITTER
    
    public static void UploadCurrentPhotoToTwitter() {
        if (Instance != null) {
            Instance.uploadCurrentPhotoToTwitter();
        }
    }
    
    public void uploadCurrentPhotoToTwitter() {
        uploadPhotoToTwitter(filePath);
    }
    
    public void uploadPhotoToTwitter(string filePathToUpload) {
        SocialNetworks.ShowComposerTwitter(
            currentMessageTwitter, 
            filePathToUpload);
    }

    public void displayPendingSaveAnimation() {
        GameCommunityController.SendResultMessage(
            "SAVING", 
            Locos.Get(LocoKeys.social_photo_pending_creating_screenshot)
        );
    }

    public void displayPendingUploadAnimation() {
        GameCommunityController.SendResultMessage(
            "UPLOADING", 
            Locos.Get(LocoKeys.social_photo_pending_uploading_post)
        );

#if UNITY_EDITOR
        StartCoroutine(waitAndDisplayAlert());
#endif

    }

    IEnumerator waitAndDisplayAlert() {
        yield return new WaitForSeconds(2.0f);
        GameCommunityController.SendResultMessage(
            "UPLOAD", "Facebook/Twitter upload doesn't work on desktop currently.");
    }
    
    public static void SaveImageToLibraryDefault() {
        if (Instance != null) {
            Instance.saveImageToLibraryDefault();   
        }
    }
    
    public void saveImageToLibraryDefault() {
        string name = Locos.Get(LocoKeys.social_photo_saved_photo_title);  
        string filePath = Path.Combine(Application.persistentDataPath, "screenshot.png");
        saveImageToLibrary(name, filePath);
    }
    
    public static void SaveImageToLibrary(string name, string fileToSave) {
        if (Instance != null) {
            Instance.saveImageToLibrary(name, fileToSave);  
        }
    }

    public void saveImageToLibrary(string name, string fileToSave) {
#if UNITY_IPHONE
        EtceteraBinding.saveImageToPhotoAlbum(fileToSave);
#elif UNITY_ANDROID
        EtceteraAndroid.saveImageToGallery(fileToSave, name);
#endif  
        
        GameCommunityController.SendResultMessage(
            Locos.Get(LocoKeys.social_photo_library_photo_saved_title),
            Locos.Get(LocoKeys.social_photo_library_photo_saved_message)
        );
    }

    // GAME

    // COMMUNITY - RESULTS

    public string GetGameStateMessage(string networkType) {

        string message = "";
        string messagePre = "";//"results! " + currentPanel;
        string messagePost = "";//"results! " + currentPanel;

        string currentPanel = GameUIController.Instance.currentPanel;

        if(networkType == SocialNetworkTypes.facebook) {

            message = Locos.Get(LocoKeys.social_facebook_game_results_message);

            if(currentPanel == GameUIPanel.panelResults) {
                // 
                
                messagePre += "Score: " + GameController.CurrentGamePlayerController.runtimeData.totalScoreValue;
            }
            else if(currentPanel == GameUIPanel.panelCustomizeCharacter) {
                // 
                
                messagePre += "My character in Barrow Brainball. ";
            }
            else if(currentPanel == GameUIPanel.panelCustomizeCharacterColors) {
                // 
                
                messagePre += "My character colors in Barrow Brainball. ";
            }
            else if(currentPanel == GameUIPanel.panelCustomizeCharacterRPG) {
                // 
                
                messagePre += "My character skills in Barrow Brainball. ";
            }
            
            messagePost += "Play Barrow Brainball! " + 
                "on iTunes:https://itunes.apple.com/us/app/barrow-brainball/id725328485?ls=1&mt=8 " + "" +
                    "or Google Play:https://play.google.com/store/apps/details?id=com.barrow.barrowbrainball" + "" +
                    " or online: http://barrowbrainball.com/play ";
        }
        else if(networkType == SocialNetworkTypes.twitter) {
            message = Locos.Get(LocoKeys.social_twitter_game_results_message);
        }
        
        //Locos.Get(LocoKeys.social_twitter_post_message
        // TODO base on current game state / scores etc.

        message = messagePre + message + messagePost;
        
        return message;
    }

    // FACEBOOK

    public static void PostGameResultsFacebook() {
        
        if (Instance != null) {
            Instance.postGameResultsFacebook(
            );  
        }
    }
    
    public void postGameResultsFacebook() {
        
        postGameResultsFacebook(
            Locos.Get(LocoKeys.social_facebook_game_results_message),
            Locos.Get(LocoKeys.social_facebook_game_results_url),
            Locos.Get(LocoKeys.social_facebook_game_results_title),
            Locos.Get(LocoKeys.social_facebook_game_results_image_url),
            Locos.Get(LocoKeys.social_facebook_game_results_caption)
        );
    }

    public static void PostGameResultsFacebook(
        string message, string url, string title, string urlImage, string caption) {

        if (Instance != null) {
            Instance.postGameResultsFacebook(
                message,
                url,
                title,
                urlImage,
                caption
            );  
        }
    }
    
    public void postGameResultsFacebook(
        string message, string url, string title, string urlImage, string caption) {

        SocialNetworks.ShowLoginOrPostMessageFacebook(
            message,
            url,
            title,
            urlImage,
            caption
        );
    }

    // TWITTER
    
    public static void PostGameResultsTwitter() {
        if (Instance != null) {
            Instance.postGameResultsTwitter();  
        }
    }

    public void postGameResultsTwitter() {

        postGameResultsTwitter(
            Locos.Get(LocoKeys.social_twitter_game_results_message),
            Locos.Get(LocoKeys.social_twitter_game_results_image_url));
    }
    
    public static void PostGameResultsTwitter(
        string message, string urlImage) {
        
        if (Instance != null) {
            Instance.postGameResultsTwitter(
                message,
                urlImage
            );  
        }
    }
    
    public void postGameResultsTwitter(
        string message, string urlImage) {
        
        SocialNetworks.ShowLoginOrComposerTwitter(
            message,
            urlImage
        );
    }
}