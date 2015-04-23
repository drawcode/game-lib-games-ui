using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using Engine.Data.Json;
using Engine.Events;
using Engine.Networking;

using Prime31;

public enum GameCommunityItemProgress {
    NotStarted,
    Started,
    Completed
}

public class GameCommunitySocialController : GameObjectBehavior {
    
    public static GameCommunitySocialController Instance;
    public float currentTimeBlock = 0.0f;
    public float actionInterval = 1.0f;
    public Material photoMaterial;
    string fileName;
    string filePath;
    string currentMessageFacebook = "";
    string currentMessageTwitter = "";
    GameCommunityItemProgress facebookPhotoUploadProgress = GameCommunityItemProgress.NotStarted;
    GameCommunityItemProgress twitterPhotoUploadProgress = GameCommunityItemProgress.NotStarted;

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
                
        Messenger<string>.AddListener(SocialNetworksMessages.socialLoggedIn, OnSocialNetworkLoggedIn);
                
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
        
        Messenger<string>.RemoveListener(SocialNetworksMessages.socialLoggedIn, OnSocialNetworkLoggedIn);
        
        #if UNITY_ANDROID || UNITY_IPHONE
        //TwitterManager.loginSucceededEvent -= twitterLoginSucceededEvent;
        //TwitterManager.loginFailedEvent -= twitterLoginFailedEvent;
        TwitterManager.requestDidFinishEvent -= twitterRequestDidFinishEvent;
        
        TwitterManager.requestDidFailEvent -= twitterRequestDidFailEvent;
        TwitterManager.tweetSheetCompletedEvent -= twitterTweetSheetCompletedEvent;
        //TwitterManager.tweetSheetFailedEvent -= twitterTweetSheetFailedEvent;
        #endif
    }

    void OnSocialNetworkLoggedIn(string networkType) {

        if (networkType == SocialNetworkTypes.facebook) {
            if (facebookPhotoUploadProgress == GameCommunityItemProgress.Started) {
                uploadCurrentPhotoToFacebook();
            }
        }
        else if (networkType == SocialNetworkTypes.twitter) {
            if (twitterPhotoUploadProgress == GameCommunityItemProgress.Started) {
                uploadCurrentPhotoToTwitter();
            }
        }
    }

    void twitterTweetSheetCompletedEvent(bool completed) {

        if (completed) {
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
        
        fileName = "screenshot.png";
        //fileName = key.ToLower() + "-screenshot-" + System.DateTime.Now.Ticks.ToString() + ".png";
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

    // TAKE PHOTO WEB

    public Texture2D imageData2D;

    public static void TakePhotoWeb() {
        if (Instance != null) {
            Instance.takePhotoWeb();
        }
    }

    public void takePhotoWeb() {
        StartCoroutine(takePhotoWebCo());
    }

    IEnumerator takePhotoWebCo() {
        yield return new WaitForEndOfFrame();
        #if UNITY_WEB_PLAYER
        string imageData64 = "";
        var newTexture = TakePhoto(Camera.main, Screen.width, Screen.height);
        LerpTexture(imageData2D, ref newTexture);
        imageData64 = System.Convert.ToBase64String(newTexture.EncodeToPNG());
        Application.ExternalEval("document.location.href='data:image/octet-stream;base64," + imageData64 + "'");
#endif
    }
    
    public static Texture2D TakePhoto(Camera cam, int width, int height) {
        if (Instance != null) {
            return Instance.takePhoto(cam, width, height);
        }
        return null;
    }

    public Texture2D takePhoto(Camera cam, int width, int height) {
        var renderTexture = new RenderTexture(width, height, 0);
        var targetTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
        cam.targetTexture = renderTexture;
        cam.Render();
        
        RenderTexture.active = renderTexture;
        targetTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        targetTexture.Apply();
        
        cam.targetTexture = null;
        RenderTexture.active = null;
        cam.ResetAspect();
        
        return targetTexture;
    }
    
    private static void LerpTexture(Texture2D alphaTexture, ref Texture2D texture) {
        var bgColors = alphaTexture.GetPixels();
        var tarCols = texture.GetPixels();
        for (var i = 0; i < tarCols.Length; i++)
            tarCols[i] = bgColors[i].a > 0.99f ? bgColors[i] : Color.Lerp(tarCols[i], bgColors[i], bgColors[i].a);
        texture.SetPixels(tarCols);
        texture.Apply();
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
        
        //Debug.Log("GameCommunitySocialController:startFacebookPhotoUploadProcess:Logging in facebook: urlscheme:" + AppConfigs.appUrlScheme);

        //Debug.Log("GameCommunitySocialController:startFacebookPhotoUploadProcess:" 
        //    + " loggedIn:" + loggedIn);

        facebookPhotoUploadProgress = GameCommunityItemProgress.Started;

        currentMessageFacebook = GetGameStateMessage(SocialNetworkTypes.facebook);

        if (loggedIn) {
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
        
        currentMessageTwitter = GetGameStateMessage(SocialNetworkTypes.twitter);

        twitterPhotoUploadProgress = GameCommunityItemProgress.Started;

        if (loggedIn) {//SocialNetworks.IsTwitterAvailable()) {
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

        byte[] bytes = GetImageBytes(tex);
        
        displayPendingUploadAnimation();

        SocialNetworks.PostMessageFacebook(
            currentMessageFacebook, bytes, onFacebookUploadComplete);
        
        facebookPhotoUploadProgress = GameCommunityItemProgress.Completed;
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
        uploadPhotoToTwitter(photoMaterial);
    }
    
    public void uploadPhotoToTwitter(string filePathToUpload) {

        displayPendingUploadAnimation();

        SocialNetworks.ShowComposerTwitter(
            currentMessageTwitter, 
            filePathToUpload);

        twitterPhotoUploadProgress = GameCommunityItemProgress.Completed;
    }

    public void uploadPhotoToTwitter(Material photoMaterialPlaceholder) {
        
        Texture2D tex = (Texture2D)photoMaterialPlaceholder.mainTexture;
        uploadPhotoToTwitter(tex);
    }
    
    public void uploadPhotoToTwitter(Texture2D tex) {

        byte[] bytes = GetImageBytes(tex);
        
        displayPendingUploadAnimation();

        SocialNetworks.PostMessageTwitter(currentMessageTwitter, bytes);
        
        twitterPhotoUploadProgress = GameCommunityItemProgress.Completed;
    }

    public byte[] GetImageBytes(Texture2D tex, float quality = 95.0f) {

        // normally, we would just encode the Texture to a PNG but Facebook does not like Unity created PNG's since 3.4.0 came out
        //var bytes = tex.EncodeToPNG();        
        //var tex = new Texture2D( Screen.width, Screen.height, TextureFormat.RGB24, false );
        //tex.ReadPixels( new Rect( 0, 0, Screen.width, Screen.height ), 0, 0, false );
        //var bytes = tex.EncodeToPNG();
        //Destroy( tex );

        var encoder = new ImageJPGEncoder(tex, quality);
        encoder.doEncoding();
        var bytes = encoder.GetBytes(); 
        return bytes;
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

    public string GetGameAdjective() {
        List<string> words = new List<string>();
        words.Add(Locos.Get(LocoKeys.game_action_adverb_1));
        words.Add(Locos.Get(LocoKeys.game_action_adverb_2));
        words.Add(Locos.Get(LocoKeys.game_action_adverb_3));
        words.Add(Locos.Get(LocoKeys.game_action_adverb_4));
        words.Add(Locos.Get(LocoKeys.game_action_adverb_5));
        words.Add(Locos.Get(LocoKeys.game_action_adverb_6));
        words.Add(Locos.Get(LocoKeys.game_action_adverb_7));
        
        words.Shuffle();
        
        string word = words[0];

        return word;
    }

    // GAME

    // COMMUNITY - RESULTS

    public string GetGameStateMessage(string networkType) {

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        if (networkType == SocialNetworkTypes.facebook) {

            sb.Append(getGameStatePanelMessage(networkType));
            sb.Append(" ");
            sb.Append(Locos.Get(LocoKeys.social_facebook_game_action_append));
        }
        else if (networkType == SocialNetworkTypes.twitter) {
                        
            sb.Append(getGameStatePanelMessage(networkType));
            sb.Append(" ");
            sb.Append(Locos.Get(LocoKeys.app_default_append));
        }
                
        return sb.ToString().Replace("  ", " ");
    }

    public string getGameStatePanelMessage(string networkType) {
        
        string currentPanel = GameUIController.Instance.currentPanel;
        
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        if (currentPanel == GameUIPanel.panelResults) {            // 
            
            if (AppContentStates.Instance.isAppContentStateGameTrainingChoiceQuiz) {
                
                sb.Append(Locos.Get(LocoKeys.game_action_results_choice_quiz_message));
                
            }
            else {

                string score = Locos.Get(LocoKeys.game_type_score);
                string scores = Locos.Get(LocoKeys.game_type_scores);
                string coins = Locos.Get(LocoKeys.game_type_coins);

                string adjective = GameCommunitySocialController.Instance.GetGameAdjective();

                sb.Append(adjective);
                sb.Append(" ");
                sb.Append(Locos.Get(LocoKeys.game_action_results_arcade_message));
                sb.Append(" ");
                sb.Append(score);
                sb.Append(": ");
                sb.Append(GameController.CurrentGamePlayerController.runtimeData.totalScoreValue);
                sb.Append(" ");
                sb.Append(scores);
                sb.Append(": ");
                sb.Append(GameController.CurrentGamePlayerController.runtimeData.scores);
                sb.Append(" ");
                sb.Append(coins);
                sb.Append(": ");
                sb.Append(GameController.CurrentGamePlayerController.runtimeData.coins);
            }
        }
        else if (currentPanel == GameUIPanel.panelCustomizeCharacter
            || currentPanel == GameUIPanel.panelCustomize 
            || currentPanel == GameUIPanel.panelEquipment) {
            // 
            sb.Append(Locos.Get(LocoKeys.game_action_panel_character_customize_message));
        }
        else if (currentPanel == GameUIPanel.panelCustomizeCharacterColors) {
            // 
            sb.Append(Locos.Get(LocoKeys.game_action_panel_character_colors_message));
        }
        else if (currentPanel == GameUIPanel.panelCustomizeCharacterRPG) {
            // 
            sb.Append(Locos.Get(LocoKeys.game_action_panel_character_rpg_message));
        }
        else if (currentPanel == GameUIPanel.panelAchievements) {
            // 
            sb.Append(Locos.Get(LocoKeys.game_action_panel_achievements_message));
        }
        else if (currentPanel == GameUIPanel.panelStatistics) {
            // 
            sb.Append(Locos.Get(LocoKeys.game_action_panel_statistics_message));
        }
        else {
            sb.Append(Locos.Get(LocoKeys.game_action_default_message));
        }

        if (sb.Length == 0) {
            sb.Append(Locos.Get(LocoKeys.game_action_default_message));
        }

        return sb.ToString();
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

    public void Update() {
    
        if (Input.GetKey(KeyCode.LeftControl)) {        
        
            if (Input.GetKey(KeyCode.LeftShift)) {
                if (Input.GetKeyDown(KeyCode.S)) {

                    TakePhotoWeb();
                }
            }
        }
    
    }
}