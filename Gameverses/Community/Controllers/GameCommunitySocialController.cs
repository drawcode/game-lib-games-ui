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
    }
    
    void OnDisable() {
        Messenger<GameCommunityMessageResult>.RemoveListener(
            GameCommunityMessages.gameCommunityResultMessage, OnGameCommunityResultMessage);        
    }
    
    void OnGameCommunityResultMessage(GameCommunityMessageResult result) {
        
        // show message from title, message in alert
        //GameCommunityUIPanelLoading.ShowGameCommunityLoading(
        //    result.title, 
        //    result.message
        //);      

        if(result == null) {
            return;
        }

        if(string.IsNullOrEmpty(result.title)) {
            return;
        }


        if(result.title.ToLower().Contains("error")) {
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
    
    /*
    public void FindSocialNetworkFacebookObject() {     
        if(socialNetworkFacebookObject == null) {
            socialNetworkFacebookObject = GameObject.Find("SocialNetworkFacebook");
            if(socialNetworkFacebookObject == null) {
                socialNetworkFacebookObject = new GameObject("SocialNetworkFacebook");
                DontDestroyOnLoad(socialNetworkFacebookObject);
            }
        }
    }
    
    public void FindSocialNetworkTwitterObject() {      
        if(socialNetworkTwitterObject == null) {
            socialNetworkTwitterObject = GameObject.Find("SocialNetworkTwitter");
            if(socialNetworkTwitterObject == null) {
                socialNetworkTwitterObject = new GameObject("SocialNetworkTwitter");
                DontDestroyOnLoad(socialNetworkTwitterObject);
            }
        }
    }
    
    public void FindFacebookManagerObject() {       
        FindSocialNetworkFacebookObject();      
        if(facebookManager == null) {
            facebookManager = socialNetworkFacebookObject.GetComponent<FacebookManager>();
            facebookEventListener = socialNetworkFacebookObject.GetComponent<FacebookEventListener>();
            
            if(facebookManager == null || facebookEventListener == null) {
                
                //LogUtil.Log("initFacebook:adding:" + facebookManager);
                
                facebookManager = socialNetworkFacebookObject.AddComponent<FacebookManager>();
                facebookEventListener = socialNetworkFacebookObject.AddComponent<FacebookEventListener>();
            }
        }
    }
    
    public void FindTwitterManagerObject() {
        
        FindSocialNetworkTwitterObject();
        
        if(twitterManager == null) {
            
            twitterManager = socialNetworkTwitterObject.GetComponent<TwitterManager>();
            
            #if UNITY_IPHONE
            twitterEventListener = socialNetworkTwitterObject.GetComponent<TwitterEventListener>();
            #elif UNITY_ANDROID
            twitterEventListener = socialNetworkTwitterObject.GetComponent<TwitterAndroidEventListener>();
            #endif
            
            if(twitterManager == null) {
                
                //LogUtil.Log("initTwitter:adding:" + twitterManager);
                
                twitterManager = socialNetworkTwitterObject.AddComponent<TwitterManager>();
                #if UNITY_IPHONE
                twitterEventListener = socialNetworkTwitterObject.AddComponent<TwitterEventListener>();
                #elif UNITY_ANDROID
                twitterEventListener = socialNetworkTwitterObject.AddComponent<TwitterAndroidEventListener>();
                #endif
            }
        }
    }
    
    
    public void initFacebook() {
        
        //LogUtil.Log("initFacebook:" + facebookManager);
    
        FindFacebookManagerObject();
                        
    #if UNITY_IPHONE
        FacebookBinding.init(FACEBOOK_APP_ID);
    #elif UNITY_ANDROID
        FacebookAndroid.init(FACEBOOK_APP_ID);
    #endif
        
        initFacebookEvents();
    }
    public void initFacebookEvents() {
        // [CH] Uncomment this to test initial user workflow.
        //FacebookBinding.logout();
        if(photoMaterial != null) {
            photoMaterial = new Material(photoMaterial); // [CH] Copy the material so we don't destroy the original material.
        }
        FacebookManager.loginSucceededEvent += () =>
        {
            //LogUtil.Log("[CH] Login succeeded! uploading photo....");
            uploadPhotoToFacebook();
        };
    }
    
    public void initTwitter() {
        
        FindTwitterManagerObject();
                
    #if UNITY_IPHONE
        TwitterBinding.init(TWITTER_KEY, TWITTER_SECRET);
    #elif UNITY_ANDROID
        TwitterAndroid.init(TWITTER_KEY, TWITTER_SECRET);
    #endif
        
        initTwitterEvents();
    }
    
    public void initTwitterEvents() {

        TwitterManager.twitterPost += () =>
        {
            //LogUtil.Log("[CH] Post uploaded!");
            GameCommunityController.SendResultMessage(
                twitterPostSuccessTitle, twitterPostSuccessMessage);
        };

        TwitterManager.twitterPostFailed += errorMessage =>
        {
            //LogUtil.Log("[CH] Post error!");
            // [CH] Display the ingame gui. At this point in time no gui is displayed.
            GUIInGame.SetActive(true);
            alwaysOnController.refreshInterface();
        };
    }
*/
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
        //LogUtil.Log("[CH] looking for:"+www.url);
        yield return www;

        if (www.error != null) {
            //LogUtil.Log("Cannot load file"+www.error+" Path:"+www.url);
        }
        www.LoadImageIntoTexture(tex);
        
        if (photoMaterial != null) {
            photoMaterial.mainTexture = tex;
        
            //AppViewerUIController.Instance.HidePhotoCompleteActionUI();
            //AppViewerUIController.Instance.ShowPhotoShareActionUI();
        
            updatePhotoPreview();
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
        if (!GameCommunity.IsLoggedIn(SocialNetworkTypes.facebook)) {
            GameCommunity.Login(SocialNetworkTypes.facebook);
        }
        else {      
            //LogUtil.Log("We have a valid session.");
            uploadCurrentPhotoToFacebook();
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
        if (SocialNetworks.IsTwitterAvailable()) {
#if UNITY_EDITOR
            displayPendingUploadAnimation();
#endif
            uploadCurrentPhotoToTwitter();
        }
        else {
            
            GameCommunityController.SendResultMessage(
                AppConfigs.stringTwitterDisabledTitle, 
                AppConfigs.stringTwitterDisabledMessage);
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
        var encoder = new ImageJPGEncoder(tex, 75.0f);
        encoder.doEncoding();
        var bytes = encoder.GetBytes();     
        
        displayPendingUploadAnimation();

        Facebook.instance.postImage(
            bytes, AppConfigs.stringFacebookPostMessage, onFacebookUploadComplete);     
    }

    public void onFacebookUploadComplete(string error, object result) {        

        if (error != null) {
            //LogUtil.LogError( error );
                        
            GameCommunityController.SendResultMessage(
                AppConfigs.stringFacebookUploadErrorTitle, 
                AppConfigs.stringFacebookUploadErrorMessage + error);
        }
        else {          
            GameCommunityController.SendResultMessage(
                AppConfigs.stringFacebookUploadSuccessTitle, 
                AppConfigs.stringFacebookUploadSuccessMessage);
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
            AppConfigs.stringTwitterPostMessage, filePathToUpload);
    }

    public void displayPendingSaveAnimation() {
        GameCommunityController.SendResultMessage(
            "SAVING", AppConfigs.stringPendingCreatingScreenshot);
    }

    public void displayPendingUploadAnimation() {
        GameCommunityController.SendResultMessage(
            "UPLOADING", AppConfigs.stringPendingUploadingPost);

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
        string name = AppConfigs.stringSavedPhotoTitleDefault;  
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
            AppConfigs.stringLibraryPhotoSavedTitle, 
            AppConfigs.stringLibraryPhotoSavedMessage);
    }
}