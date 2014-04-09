using System;
using System.Collections;
using System.Collections.Generic;

#if !UNITY_WEBPLAYER

using System.IO;

#endif

using UnityEngine;
using Engine.Data.Json;
using Engine.Networking;

namespace Gameverses {

    public class GameversesContentConfig {
        public static string contentCacheAssetBundles = "bundles";
        public static string contentCacheVersion = "version";
        public static string contentCacheData = "data";
        public static string contentCacheUserData = "userdata";
        public static string contentCacheShared = "shared";
        public static string contentCacheAll = "all";
        public static string contentCacheTrackers = "trackers";
        public static string contentCacheScenes = "scenes";
        public static string contentCachePacks = "packs";
        public static string currentContentPackCode = "default";
    }

    public class GameversesContentItemStatus {
        public double itemSize = 0;
        public double itemProgress = 0;
        public string url = "";

        public bool downloaded = false;

        public double percentageCompleted {
            get {
                return itemProgress;
            }
        }

        public bool completed {
            get {
                return percentageCompleted == 1 ? true : false;
            }
        }
    }

    public class GameversesContentItemAccess : DataObjectItem {
        public bool globalItem = true;
        public string code = "";
        public string profileId = "";
        public string receipt = "";
        public string platform = "ios-storekit";
        public int quantity = 1;
        public string productCode = "";
    }

    public class GameversesContentItemAccessDictionary : DataObjectItem {

        public Dictionary<string, GameversesContentItemAccess> accessItems
            = new Dictionary<string, GameversesContentItemAccess>();

        public void CheckDictionary() {
            if (accessItems == null)
                accessItems = new Dictionary<string, GameversesContentItemAccess>();
        }

        public bool CheckAccess(string key) {
            CheckDictionary();
            bool hasAccess = accessItems.ContainsKey(key);
            if (key.ToLower() == "default") {

                //|| !GameProducts.enableProductLocks) {
                hasAccess = true;
            }
            LogUtil.Log("CheckAccess:: key: " + key + " hasAccess: " + hasAccess);
            return hasAccess;
        }

        public GameversesContentItemAccess GetContentAccess(string key) {
            CheckDictionary();
            if (CheckAccess(key)) {
                if (accessItems != null) {
                    if (accessItems.ContainsKey(key)) {
                        return accessItems[key];
                    }
                }
            }
            return null;
        }

        public void SetContentAccess(string key) {
            CheckDictionary();
            GameversesContentItemAccess itemAccess;

            if (CheckAccess(key) && accessItems.ContainsKey(key)) {
                itemAccess = GetContentAccess(key);
                itemAccess.code = key;
                itemAccess.globalItem = true;
                itemAccess.platform = "";//GamePacks.currentPacksPlatform;
                itemAccess.productCode = key;
                itemAccess.profileId = "";//GameProfiles.Current.username;
                itemAccess.quantity = 1;
                itemAccess.receipt = "";
                SetContentAccess(itemAccess);
            }
            else {
                itemAccess = new GameversesContentItemAccess();
                itemAccess.code = key;
                itemAccess.globalItem = true;
                itemAccess.platform = "";//GamePacks.currentPacksPlatform;
                itemAccess.productCode = key;
                itemAccess.profileId = "";//GameProfiles.Current.username;
                itemAccess.quantity = 1;
                itemAccess.receipt = "";
                SetContentAccess(itemAccess);
            }
        }

        public void SetContentAccess(GameversesContentItemAccess itemAccess) {
            CheckDictionary();

            if (CheckAccess(itemAccess.code)) {
                accessItems[itemAccess.code] = itemAccess;
            }
            else {
                accessItems.Add(itemAccess.code, itemAccess);
            }
        }

        public void SetContentAccessTransaction(string key, string productId, string receipt, int quantity, bool save) {
            GameversesContentItemAccess itemAccess = GetContentAccess(key);
            if (itemAccess != null) {
                itemAccess.receipt = receipt;
                itemAccess.productCode = productId;
                itemAccess.quantity = quantity;
                SetContentAccess(itemAccess);
                if (save) {
                    Save();
                }
            }
        }

        public void Save() {
            CheckDictionary();
            string contentItemAccessString = "";
            string settingKey = "ssg-cal";
            contentItemAccessString = JsonMapper.ToJson(accessItems);

            //LogUtil.Log("Save: access:" + contentItemAccessString);
            SystemPrefUtil.SetLocalSettingString(settingKey, contentItemAccessString);
            SystemPrefUtil.Save();
        }

        public void Load() {
            string settingKey = "ssg-cal";
            if (SystemPrefUtil.HasLocalSetting(settingKey)) {

                // Load from persistence
                string keyValue = SystemPrefUtil.GetLocalSettingString(settingKey);

                //LogUtil.Log("Load: access:" + keyValue);
                accessItems = JsonMapper.ToObject<Dictionary<string, GameversesContentItemAccess>>(keyValue);
                CheckDictionary();
            }
        }
    }

    // RESPONSES

    //{"info": "", "status": "", "error": 0, "action": "sx-2012-pack-1", "message": "Success!", "data":
    // {"download_urls": ["https://s3.amazonaws.com/game-supasupacross/1.1/ios/sx-2012-pack-1.unity3d?Signature=rJ%2Fe863up9wgAutleNY%2F%2B7OSy%2BU%3D&Expires=1332496714&AWSAccessKeyId=0YAPDVPCN85QV96YR382"], "access_allowed": true, "date_modified": "2012-03-21T10:58:34.919000", "udid": "[udid]", "tags": ["test", "fun"], "content": "this is \"real\"...", "url": "ffff", "version": "1.1", "increment": 1, "active": true, "date_created": "2012-03-21T10:58:34.919000", "type": "application/octet-stream"}}

    public class DownloadableGameversesContentItem {
        public List<string> download_urls = new List<string>();

        //public DateTime date_modified = DateTime.Now;
        //public string udid = "";
        //public List<string> tags = new List<string>();
        //public string content = "";
        //public string url = "";
        //public string code = "";
        //public string version = "1.1";
        //public double increment = 3;
        //public bool active = true;
        //public DateTime date_created = DateTime.Now;
        //public string type = "application/octet-stream";
        //public bool access_allowed = true;
    }

    /*
    public class GameversesDownloadableContentItemResponse : GameversesBaseObjectResponse {
        public GameversesDownloadableContentItem data = new GameversesDownloadableContentItem();

        public GameversesDownloadableContentItemResponse() {
            Reset();
        }

        public override void Reset() {
            base.Reset();
            data = new GameversesDownloadableContentItem();
        }
    }
    */

    public class GameversesBaseObjectResponse {
        public string info = "";
        public string status = "";
        public string code = "0";
        public string action = "";
        public string message = "Success";

        public GameversesBaseObjectResponse() {
            Reset();
        }

        public virtual void Reset() {
            info = "";
            status = "";
            code = "0";
            action = "";
            message = "Success";
        }
    }

    // CONTENT SYSTEM

    //"info": "ssg_ssc_1_1", "status": "", "code": "0", "action": "sx-2012-pack-1", "message": "Success!", "data": {"download_urls": ["http://s3.amazonaws.com/game-supasupacross/1.1/ios/sx-2012-pack-1.unity3d?Signature=9VJYzvaLZjeVcakz4DBDDg51Fwo%3D&Expires=1332704684&AWSAccessKeyId=0YAPDVPCN85QV96YR382"]}

    public class GameversesContentMessages {
        public static string ContentFileDownloadSuccess = "content-file-download-success";
        public static string ContentFileDownloadError = "content-file-download-error";
        public static string ContentFileDownloadStarted = "content-file-download-started";

        public static string ContentSetSuccess = "content-set-success";
        public static string ContentSetError = "content-set-error";
        public static string ContentSetStarted = "content-set-started";

        public static string ContentSetDownloadSuccess = "content-set-download-success";
        public static string ContentSetDownloadError = "content-set-download-error";
        public static string ContentSetDownloadStarted = "content-set-download-started";

        public static string ContentItemDownloadSuccess = "content-item-download-success";
        public static string ContentItemDownloadError = "content-item-download-error";
        public static string ContentItemDownloadStarted = "content-item-download-started";

        public static string ContentItemVerifySuccess = "content-item-verify-success";
        public static string ContentItemVerifyError = "content-item-verify-error";
        public static string ContentItemVerifyStarted = "content-item-verify-started";

        public static string ContentItemPrepareSuccess = "content-item-prepare-success";
        public static string ContentItemPrepareError = "content-item-prepare-error";
        public static string ContentItemPrepareStarted = "content-item-prepare-started";

        public static string ContentItemLoadSuccess = "content-item-load-success";
        public static string ContentItemLoadError = "content-item-load-error";
        public static string ContentItemLoadStarted = "content-item-load-started";

        public static string ContentItemProgress = "content-item-progress";
    }

    public class GameversesContentEndpoints {
        public static string contentVerification = "http://v3.gameverses.com/api/v1/en/file/{0}/{1}/{2}/{3}"; // 0 = game, version, platform, pack
        public static string contentDownloadPrimary = "http://s3.amazonaws.com/static/{0}/{1}/{2}/{3}";
        public static string contentDownloadAmazon = "http://s3.amazonaws.com/{0}/{1}/{2}/{3}";
        public static string contentDownloadFileAsset = "http://v3.gameverses.com/api/v1/en/file/{0}/{1}/{2}/{3}"; // 0 = game, version, platform, pack;
        public static string contentSyncContentSet = "http://v3.gameverses.com/api/v1/sync/en/content-list/{0}/{1}/{2}/{3}"; // 0 = game, version, platform, pack;
    }

    public class GameversesContentItem {
        public string uid = "";
        public string name = "";
        public int version = 0;
        public AssetBundle bundle;
    }

    public class GameversesContentItemError {
        public string uid = "";
        public string name = "";
        public string message = "";
        public GameversesContentItem contentItem;
    }

    public class GameversesContents {
        private static volatile GameversesContents instance;
        private static System.Object syncRoot = new System.Object();

        public static GameversesContents Instance {
            get {
                if (instance == null) {
                    lock (syncRoot) {
                        if (instance == null)
                            instance = new GameversesContents();
                    }
                }
                return instance;
            }
        }

        public string contentUrlRoot = GameversesConfig.currentContentUrlRoot;
        public string contentUrlCDN = GameversesConfig.currentContentUrlCDN;

        public List<GameversesContentItem> contentItemList = new List<GameversesContentItem>();
        public GameversesContentItem currentContentItem = new GameversesContentItem();

        public AssetBundle bundle;

        public WWW downloader;
        public WWW verifier;

        //public GameversesDownloadableContentItem dlcItem;
        public GameversesContentItemStatus contentItemStatus;

        public bool downloadInProgress = false;

        public GameversesContentItemAccessDictionary contentItemAccess;
        public string persistenceFolder = "";
        public string streamingAssetsFolder = "";

        public string appCachePath = "";
        public string appCachePathAssetBundles = "";
        public string appCachePathTrackers = "";
        public string appCachePathPacks = "";
        public string appCachePathShared = "";
        public string appCachePathAll = "";
        public string appCachePathAllShared = "";
        public string appCachePathAllSharedUserData = "";
        public string appCachePathSharedPacks = "";
        public string appCachePathSharedTrackers = "";
        public string appCachePathAllSharedTrackers = "";
        public string appCachePathAllPlatform = "";
        public string appCachePathAllPlatformPacks = "";
        public string appCachePathAllPlatformData = "";
        public string appCachePathData = "";
        public string appCacheVersionPath = "";
        public string appCachePlatformPath = "";

        public string appShipCachePath = "";
        public string appShipCachePathAssetBundles = "";
        public string appShipCachePathTrackers = "";
        public string appShipCachePathPacks = "";
        public string appShipCachePathShared = "";
        public string appShipCachePathAll = "";
        public string appShipCachePathData = "";
        public string appShipCacheVersionPath = "";
        public string appShipCachePlatformPath = "";
        public string appShipCachePathPlatformPath = "";

        public int currentIncrement = GameversesConfig.currentContentIncrement;
        public string currentVersion = GameversesConfig.currentContentVersion;
        public string currentRootAppCode = GameversesConfig.currentContentRootAppCode;
        public string currentAppCode = GameversesConfig.currentContentAppCode;
        public string currentPackCode = GameversesConfig.currentContentPackCode;
        public string currentPlatformCode = GameversesConfig.currentContentPlatformCode;

        public List<string> packPaths;
        public List<string> packPathsVersionedShared;
        public List<string> packPathsVersioned;

        public Dictionary<string, string> fileHashLookup = null;

        public GameversesContents() {

            //InitConfig();
        }

        public void InitConfig() {
            contentUrlRoot = GameversesConfig.currentContentUrlRoot;
            contentUrlCDN = GameversesConfig.currentContentUrlCDN;

            currentIncrement = GameversesConfig.currentContentIncrement;
            currentVersion = GameversesConfig.currentContentVersion;
            currentRootAppCode = GameversesConfig.currentContentRootAppCode;
            currentAppCode = GameversesConfig.currentContentAppCode;
            currentPackCode = GameversesConfig.currentContentPackCode;
            currentPlatformCode = GameversesConfig.currentContentPlatformCode;

            currentPlatformCode = GetCurrentPlatformCode();

            //Debug.Log("Contents:currentPlatformCode:" + currentPlatformCode);

            contentItemAccess = new GameversesContentItemAccessDictionary();
            contentItemAccess.Load();

            packPaths = new List<string>();
            packPathsVersioned = new List<string>();
            packPathsVersionedShared = new List<string>();

            fileHashLookup = new Dictionary<string, string>();
        }

        public string GetCurrentPlatformCode() {
#if UNITY_IPHONE
			return "ios";
#elif UNITY_ANDROID
			return "android";
#else
            return "desktop";
#endif
        }

        public void ChangePackAndLoadMainScene(string pack) {

            //GamePacks.Instance.ChangeCurrentGamePack(pack);
            //GameLevels.Instance.ChangeCurrentGameLevel(pack + "-main");
            // scene bundle based with unity caching
            //GameversesContents.Instance.LoadSceneOrDownloadScenePackAndLoad(GamePacks.Current.code);
        }

        public bool CheckGlobalContentAccess(string pack) {
            if (contentItemAccess.CheckAccess(pack)) {
                return true;
            }
            return false;
        }

        public void SaveGlobalContentAccess() {
            contentItemAccess.Save();
        }

        public void SetGlobalContentAccess(string pack) {

            //pack = pack.Replace(GamePacks.currentGameBundle + ".", "");
            contentItemAccess.SetContentAccess(pack);
            contentItemAccess.SetContentAccess(pack.Replace("-", "_"));
            contentItemAccess.SetContentAccess(pack.Replace("_", "-"));

            LogUtil.Log("GameStore::SetContentAccessPermissions pack :" + pack);
            LogUtil.Log("GameStore::SetContentAccessPermissions pack _ :" + pack.Replace("-", "_"));
            LogUtil.Log("GameStore::SetContentAccessPermissions pack - :" + pack.Replace("_", "-"));
            LogUtil.Log("GameStore::SetContentAccessPermissions pack - :" + pack.Replace("_", "-"));
            contentItemAccess.Save();
        }

        public void SetContentAccessTransaction(string key, string productId, string receipt, int quantity, bool save) {
            contentItemAccess.SetContentAccessTransaction(key, productId, receipt, quantity, save);
        }

        // ----------------------------------------------------------------------------------
        // HANDLERS

        private void HandleDownloadAssetBundleCallback(WebRequests.ResponseObject response) {
            response = HandleResponseObject(response);

            if (response.validResponse) {
                LogUtil.Log("SUCCESSFUL DOWNLOAD");

                string dataToParse = response.data;

                LogUtil.Log("dataToParse:" + dataToParse);

                if (!string.IsNullOrEmpty(dataToParse)) {
                    try {

                        //GameversesDownloadableContentItemResponse responseData = JsonMapper.ToObject<GameversesDownloadableContentItemResponse>(dataToParse);
                        //dlcItem = responseData.data;
                    }
                    catch (Exception e) {

                        //serverError = true;
                        LogUtil.Log("Parsing error:" + e.Message + e.StackTrace + e.Source);
                    }

                    /*
                    if(dlcItem != null) {
                        List<string> downloadUrls = dlcItem.download_urls;

                        foreach(string url in downloadUrls) {
                            GameMessenger<string>.Broadcast(GameversesContentMessages.ContentItemVerifySuccess, "Content verified, downloading and loading pack." );
                            CoroutineUtil.Start(SceneLoadFromCacheOrDownloadCo(url));
                            break;
                        }
                    }
                    else {
                        serverError = true;
                    }
                    */
                }
                else {

                    //serverError = true;
                }
            }
            else {

                // There was a problem with the response.
                LogUtil.Log("NON-SUCCESSFUL DOWNLOAD");

                //serverError = true;
            }

            //if(serverError) {
            //	Reset();
            ////	GameMessenger<string>.Broadcast(Gameverses.ContentMessages.ContentItemVerifyError, "Error on server, please try again.");
            //}
        }

        private void HandleDownloadableContentInfoCallback(WebRequests.ResponseObject response) {
            response = HandleResponseObject(response);

           // bool serverError = false;

            if (response.validResponse) {
                LogUtil.Log("SUCCESSFUL DOWNLOAD");

                string dataToParse = response.data;

                LogUtil.Log("dataToParse:" + dataToParse);

                if (!string.IsNullOrEmpty(dataToParse)) {
                    /*
                    try {
                        DownloadableContentItemResponse responseData = JsonMapper.ToObject<DownloadableContentItemResponse>(dataToParse);
                        dlcItem = responseData.data;
                    }
                    catch(Exception e) {
                        serverError = true;
                        LogUtil.Log("Parsing error:" + e.Message + e.StackTrace + e.Source);
                    }

                    if(dlcItem != null) {
                        List<string> downloadUrls = dlcItem.download_urls;

                        foreach(string url in downloadUrls) {
                            GameMessenger<string>.Broadcast(GameversesContentMessages.ContentItemVerifySuccess, "Content verified, downloading and loading pack." );
                            CoroutineUtil.Start(GameversesContents.Instance.SceneLoadFromCacheOrDownloadCo(url));
                            break;
                        }
                    }
                    else {
                        serverError = true;
                    }
                    */
                }
                else {

                    //serverError = true;
                }
            }
            else {

                // There was a problem with the response.
                //LogUtil.Log("NON-SUCCESSFUL DOWNLOAD");
                //serverError = true;
            }

            //if(serverError) {
            //Reset();
            //GameMessenger<string>.Broadcast(GameversesContentMessages.ContentItemVerifyError, "Error on server, please try again.");
            //}
        }

        //HandleDownloadableContentSetSyncCallback

        private void HandleDownloadableContentSetSyncCallback(WebRequests.ResponseObject response) {
            response = HandleResponseObject(response);

           // bool serverError = false;

            /*
            if(response.validResponse) {
                LogUtil.Log("HandleDownloadableContentSetSyncCallback valid");

                string dataToParse = response.data;

                LogUtil.Log("dataToParse:" + dataToParse);

                if(!string.IsNullOrEmpty(dataToParse)) {
                    try {
                        DownloadableContentItemResponse responseData = JsonMapper.ToObject<DownloadableContentItemResponse>(dataToParse);
                        dlcItem = responseData.data;
                    }
                    catch(Exception e) {
                        serverError = true;
                        LogUtil.Log("Parsing error:" + e.Message + e.StackTrace + e.Source);
                    }

                    if(dlcItem != null) {
                        List<string> downloadUrls = dlcItem.download_urls;

                        foreach(string url in downloadUrls) {
                            GameMessenger<string>.Broadcast(GameversesContentMessages.ContentItemVerifySuccess, "Content verified, downloading and loading pack." );
                            CoroutineUtil.Start(Contents.SceneLoadFromCacheOrDownloadCo(url));
                            break;
                        }
                    }
                    else {
                        serverError = true;
                    }
                }
                else {
                    serverError = true;
                }
            }
            else {

                // There was a problem with the response.
                LogUtil.Log("NON-SUCCESSFUL DOWNLOAD");
                serverError = true;
            }

            if(serverError) {
                Reset();
                GameMessenger<string>.Broadcast(GameversesContentMessages.ContentItemVerifyError, "Error on server, please try again.");
            }
            */
        }

        private void HandleDownloadableFileCallback(WebRequests.ResponseObject response) {
            /*
            response = HandleResponseObject(response);

            bool serverError = false;

            if(response.validResponse) {
                LogUtil.Log("Successful verfication download...");

                string dataToParse = response.data;

                LogUtil.Log("dataToParse:" + dataToParse);

                if(!string.IsNullOrEmpty(dataToParse)) {
                    try {
                        DownloadableContentItemResponse responseData = JsonMapper.ToObject<DownloadableContentItemResponse>(dataToParse);
                        dlcItem = responseData.data;
                    }
                    catch(Exception e) {
                        serverError = true;
                        LogUtil.Log("Parsing error:" + e.Message + e.StackTrace + e.Source);
                    }

                    if(dlcItem != null) {
                        List<string> downloadUrls = dlcItem.download_urls;

                        if(downloadUrls.Count > 0) {
                            GameMessenger<string>.Broadcast(GameversesContentMessages.ContentItemVerifySuccess, "Content verified, downloading and loading pack." );

                            //Debug.Log("url:" + url);
                            //CoroutineUtil.Start(Contents.SceneLoadFromCacheOrDownloadCo(url));
                            //WebRequests.Instance.Request(
                        }
                    }
                    else {
                        serverError = true;
                    }
                }
                else {
                    serverError = true;
                }
            }
            else {

                // There was a problem with the response.
                LogUtil.Log("NON-SUCCESSFUL DOWNLOAD");
                serverError = true;
            }

            if(serverError) {
                Reset();
                GameMessenger<string>.Broadcast(GameversesContentMessages.ContentItemVerifyError, "Error on server, please try again.");
            }
            */
        }

        //public WebRequests.ResponseObject HandleResponseObjectAssetBundle(WebRequests.ResponseObject responseObject) {
        //}

        public WebRequests.ResponseObject HandleResponseObject(WebRequests.ResponseObject responseObject) {
            bool serverError = false;

            // Manages common response object parsing to get to object
            if (responseObject.www.text != null) {
                JsonData data = JsonMapper.ToObject(responseObject.www.text);

                if (data.IsObject) {
                    string code = (string)data["code"];
                    string message = (string)data["message"];
                    /*
                    JsonData dataValue = null;
                    if(data["data"] != null) {
                        if(data["data"].IsObject) {
                            dataValue = data["data"];
                        }
                    }
                    try{
                        responseObject.error = Convert.ToInt32(code);
                    }
                    catch(Exception e) {
                        responseObject.error = 1;
                        LogUtil.Log("ERROR: " + e.Message + e.StackTrace + e.Source);
                    }
                    */
                    responseObject.message = message;
                    responseObject.code = code;

                    LogUtil.Log("STATUS/CODE:" + code);
                    LogUtil.Log("STATUS/CODE MESSAGE:" + message);

                    if (code == "0") {
                        LogUtil.Log("STATUS/DATA NODE:" + data);

                        LogUtil.Log("dataValue:" + data["data"]);
                        LogUtil.Log("responseObject.www.text:" + responseObject.www.text);

                        responseObject.data = responseObject.www.text;
                        responseObject.dataValue = data["data"];
                        responseObject.validResponse = true;
                    }
                    else {
                        LogUtil.Log("ERROR - Good response but problem with data, see message.");
                        serverError = true;
                    }
                }
            }
            else {
                LogUtil.Log("ERROR - NO DATA");
                serverError = true;
            }

            if (serverError) {
                responseObject.validResponse = false;
                Reset();
                GameMessenger<string>.Broadcast(GameversesContentMessages.ContentItemVerifyError, "Error receiving a server response, please try again.");
            }

            return responseObject;
        }

        // ----------------------------------------------------------------------------------
        // REQUESTS

        public void RequestDownloadableContent(string pack) {

            //RequestDownloadableContent(
            //	GamePacks.currentPacksGame,
            //	GamePacks.currentPacksVersion,
            //	GamePacks.currentPacksPlatform,
            //	pack);
        }

        public void RequestDownloadableContent(string game, string version, string platform, string pack) {

            //glob.ShowLoadingIndicator();

            Dictionary<string, object> data = new Dictionary<string, object>();
            string udid = UniqueUtil.Instance.currentUniqueId;

            data.Add("device_id", udid);
            data.Add("app_id", "gameverses-neeches");
            data.Add("auth", "gameverses-neeches");

            downloadInProgress = true;

            string url = GetDownloadContentItemUrl(game, version, platform, pack);
            WebRequests.Instance.Request(WebRequests.RequestType.HTTP_POST, url, data, HandleDownloadableContentInfoCallback);
            contentItemStatus = new GameversesContentItemStatus();

            GameMessenger<string>.Broadcast(GameversesContentMessages.ContentItemVerifyStarted, "Verifying content access...");
        }

        public void RequestDownloadableContentSetSync(string game, string version, string platform) {
            downloadInProgress = true;

            string url = GetContentSetUrl(game, version, platform);
            WebRequests.Instance.Request(WebRequests.RequestType.HTTP_GET, url, HandleDownloadableContentSetSyncCallback);

            GameMessenger<string>.Broadcast(GameversesContentMessages.ContentSetDownloadStarted, "Getting downloadable content access...");
        }

        public void RequestDownloadableFile(string url) {
            downloadInProgress = true;

            WebRequests.Instance.Request(WebRequests.RequestType.HTTP_GET, url, HandleDownloadableFileCallback);

            GameMessenger<string>.Broadcast(GameversesContentMessages.ContentFileDownloadStarted, "Started downloading..." + url);
        }

        // ----------------------------------------------------------------------------------
        // HELPERS

        public string GetDownloadContentItemUrl(string game, string buildVersion, string platform, string pack) {

            // add increment to the pack name
            //pack = pack + "-" + Convert.ToString(GamePacks.currentPacksIncrement);
            return String.Format(GameversesContentEndpoints.contentDownloadFileAsset, game, buildVersion, platform, pack);
        }

        public string GetContentSetUrl(string game, string buildVersion, string platform) {

            // add increment to the pack name
            //pack = pack + "-" + Convert.ToString(GamePacks.currentPacksIncrement);
            return String.Format(GameversesContentEndpoints.contentSyncContentSet, game, buildVersion, platform);
        }

        public void LoadSceneOrDownloadScenePackAndLoad(string pack) {

            //LoadSceneOrDownloadScenePackAndLoad(
            //	GamePacks.currentPacksGame,
            //	GamePacks.currentPacksVersion,
            //	GamePacks.currentPacksPlatform,
            //	pack);
        }

        public void LoadSceneOrDownloadScenePackAndLoad(string game, string buildVersion, string platform, string pack) {
            bool isDownloadableContent = IsDownloadableContent(pack);
            LogUtil.Log("isDownloadableContent:" + isDownloadableContent);

            //int version = GamePacks.currentPacksIncrement;

            //string url = GetDownloadContentItemUrl(game, buildVersion, platform, pack);

            ///string lastPackUrlValue = GetLastPackState(pack);

            //if(Caching.IsVersionCached(lastPackUrlValue, version)
            //	&& !string.IsNullOrEmpty(lastPackUrlValue)) {
            //	// Just load from the saved url
            //	//CoroutineUtil.Start(SceneLoadFromCacheOrDownloadCo(lastPackUrlValue));
            //}
            //else {
            //	// Do download verification and download
            //	RequestDownloadableContent(game, buildVersion, platform, pack);
            //}
        }

        //
        public bool IsDownloadableContent(string pack) {

            //if(pack.ToLower() == GamePacks.PACK_BOOK_DEFAULT.ToLower()) {
            //	return true;
            //}
            return false;
        }

        public void SetLastPackState(string packName, string url) {
            if (IsDownloadableContent(packName)) {
                string lastPackUrlKey = "last-pack-" + packName;
                string lastPackUrlValue = url;

                if (!string.IsNullOrEmpty(lastPackUrlValue)) {
                    SystemPrefUtil.SetLocalSettingString(lastPackUrlKey, lastPackUrlValue);
                    SystemPrefUtil.Save();
                }
            }
        }

        public string GetLastPackState(string packName) {
            if (IsDownloadableContent(packName)) {
                string lastPackUrlKey = "last-pack-" + packName;
                if (SystemPrefUtil.HasLocalSetting(lastPackUrlKey)) {
                    return SystemPrefUtil.GetLocalSettingString(lastPackUrlKey);
                }
            }
            return "";
        }

        public IEnumerator SceneLoadFromCacheOrDownloadCo(string url) {
            UnloadLevelBundle();

            //int version = GamePacks.currentPacksIncrement;
            //string packName = GamePacks.PACK_BOOK_DEFAULT;
            //string sceneName = GameLevels.Current.name;

            //LogUtil.Log("SceneLoadFromCacheOrDownloadCo: packName:" + packName);
            //LogUtil.Log("SceneLoadFromCacheOrDownloadCo: sceneName:" + sceneName);
            //LogUtil.Log("SceneLoadFromCacheOrDownloadCo: version:" + version);

            //GameversesContentItem contentItem = new GameversesContentItem();

            //contentItem.uid = sceneName; // hash this
            //contentItem.name = sceneName;
            //contentItem.version = version;

            //bool isDlc = false;
            bool ready = true;

            if (IsDownloadableContent("")) {

                //LogUtil.Log("SceneLoadFromCacheOrDownloadCo: " + packName);

                //LogUtil.Log("SceneLoadFromCacheOrDownloadCo: " + url);

                GameMessenger<string>.Broadcast(GameversesContentMessages.ContentItemDownloadStarted, url);

                downloadInProgress = true;

                downloader = WWW.LoadFromCacheOrDownload(url, 1);

                LogUtil.Log("downloader.progress: " + downloader.progress);

                yield return downloader;

                LogUtil.Log("downloader.progress2: " + downloader.progress);

                // Handle error
                if (downloader.error != null) {
                    LogUtil.LogError("Error downloading");
                    LogUtil.LogError(downloader.error);
                    LogUtil.LogError(url);
                    ready = false;
                    Reset();
                    GameMessenger<string>.Broadcast(GameversesContentMessages.ContentItemDownloadError, downloader.error);
                }
                else {

                    // In order to make the scene available from LoadLevel, we have to load the asset bundle.
                    // The AssetBundle class also lets you force unload all assets and file storage once it
                    // is no longer needed.

                    GameMessenger<string>.Broadcast(GameversesContentMessages.ContentItemPrepareStarted, "Content preparing...");

                    UnloadLevelBundle();

                    bundle = downloader.assetBundle;

                    //LogUtil.Log("LoadLevel" + sceneName);

                    //SetLastPackState(packName, url);

                    GameMessenger<string>.Broadcast(GameversesContentMessages.ContentItemPrepareSuccess, "Content prepared...");

                    // Load the level we have just downloaded
                }
            }

            if (ready) {

                //GameLoadingObject.Instance.LoadLevelHandler();
                Reset();
            }
            else {

                // Show download error...
                GameMessenger<string>.Broadcast(GameversesContentMessages.ContentItemDownloadError, "Error unloading pack, please try again.");
                Reset();
            }
        }

        public void LoadLevelBundle(string pack, int increment) {
#if !UNITY_WEBPLAYER
            string pathPack = PathUtil.Combine(appCachePathAllPlatformPacks, pack);

            //pathPack = PathUtil.Combine(pathPack, ContentConfig.contentCacheScenes);

            //GamePacks.Instance.ChangeCurrentGamePack(pack);
            if (Directory.Exists(pathPack)) {
                string pathUrl = PathUtil.Combine(pathPack, pack + "-" + increment.ToString() + ".unity3d");
                if (FileSystemUtil.CheckFileExists(pathUrl)) {
                    LoadLevelBundle("file://" + pathUrl);
                }
                else {

                    //Debug.Log("Pack file does not exist: " + pathUrl);
                }
            }
            else {

                //Debug.Log("Pack does not exist:" + pathPack);
            }
#endif
        }

        public void LoadLevelBundle(string sceneUrl) {

            //CoroutineUtil.Start(LoadLevelBundleCo(sceneUrl));
        }

        public IEnumerator LoadLevelBundleCo(string sceneUrl) {
            bool ready = true;

            downloader = new WWW(sceneUrl);

            downloadInProgress = true;

            LogUtil.Log("downloader.progress: " + downloader.progress);

            yield return downloader;

            LogUtil.Log("downloader.progress2: " + downloader.progress);

            // Handle error
            if (downloader.error != null) {
                LogUtil.LogError("Error downloading");
                LogUtil.LogError(downloader.error);
                LogUtil.LogError(sceneUrl);
                ready = false;
                Reset();
                GameMessenger<string>.Broadcast(GameversesContentMessages.ContentItemDownloadError, downloader.error);
            }
            else {

                // In order to make the scene available from LoadLevel, we have to load the asset bundle.
                // The AssetBundle class also lets you force unload all assets and file storage once it
                // is no longer needed.

                GameMessenger<string>.Broadcast(GameversesContentMessages.ContentItemPrepareStarted, "Content preparing...");

                UnloadLevelBundle();

                bundle = downloader.assetBundle;

                //LogUtil.Log("LoadLevel" + sceneName);

                //SetLastPackState(GamePacks.Current.code, sceneUrl);

                downloadInProgress = false;
                string sceneName = "";//GamePacks.Current.code + "-main";

                //AsyncOperation asyncLoad = Application.LoadLevelAsync(sceneName);
                Application.LoadLevel(sceneName);

                GameMessenger<string>.Broadcast(GameversesContentMessages.ContentItemPrepareSuccess, "Content prepared...");
            }

            Debug.Log("ready:" + ready);
        }

        public void UnloadLevelBundle(bool unloadAll) {
            if (bundle != null) {
                bundle.Unload(unloadAll);
            }
        }

        public void UnloadLevelBundle() {
            UnloadLevelBundle(false);
        }

        public void Reset() {
            downloader = null;
            contentItemStatus = new GameversesContentItemStatus();
            downloadInProgress = false;
        }

        public GameversesContentItemStatus ProgressStatus() {
            if (downloader != null && downloadInProgress) {
                if (downloader.isDone) {
                    contentItemStatus.downloaded = true;
                }

                LogUtil.Log("progress:" + downloader.progress);

                contentItemStatus.itemProgress = downloader.progress;
                contentItemStatus.url = downloader.url;
            }

            return contentItemStatus;
        }

        // ----------------------------------------------------------------------------------
        // FILE LOADING

        // Individual file downloading

        public void DownloadAndSaveFile(string url, string pathToSave) {
        }

        public void DownloadFile(string url, string pathToSave) {
        }

        // ----------------------------------------------------------------------------------
        // FILE SETS

        public void StartContentSystem() {

            //
            InitConfig();
        }

        public void LoadContentSet() {

            //Debug.Log("Contents::LoadContentSet");
        }

        // INIT and PREPARE

        public void InitCache() {
            InitConfig();
            SyncFolders();
        }

        //public IEnumerator InitCache() {
        // Initial cache
        //SyncFolders();

        // Get latest main content list from server

        // Sync any files missing from latest

        // Check that all content states have icons downloaded
        // }

        public IEnumerator DownloadLatestContentList() {

            // Get and check the md5 hash of main content list

            yield break;
        }

        public void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs, bool versioned) {
#if !UNITY_WEBPLAYER
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!dir.Exists) {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            if (!Directory.Exists(destDirName)) {

                //LogUtil.Log("Creating Directory: " + destDirName);
                Directory.CreateDirectory(destDirName);
            }

            FileInfo[] files = dir.GetFiles();

            foreach (FileInfo file in files) {
                if (file.Extension != ".meta"
                    && file.Extension != ".DS_Store") {
                    string temppath = PathUtil.Combine(destDirName, file.Name);

                    if (versioned) {
                        temppath = GetFullPathVersioned(file.FullName, temppath);
                    }

                    if (!FileSystemUtil.CheckFileExists(temppath) || Application.isEditor) {

                        //LogUtil.Log("copying ship file: " + file.FullName);
                        //LogUtil.Log("copying ship file to cache: " + temppath);

                        file.CopyTo(temppath, true);

                        //SystemHelper.SetNoBackupFlag(temppath);
                    }
                }
            }

            if (copySubDirs) {
                foreach (DirectoryInfo subdir in dirs) {
                    string temppath = PathUtil.Combine(destDirName, subdir.Name);

                    //LogUtil.Log("Copying Directory: " + temppath);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs, versioned);
                }
            }
#endif
        }

        public List<string> GetPackPathsNonVersioned() {
            LoadPackPaths();
            return packPaths;
        }

        public List<string> GetPackPathsVersioned() {
            LoadPackPaths();
            return packPathsVersioned;
        }

        public List<string> GetPackPathsVersionedShared() {
            LoadPackPaths();
            return packPathsVersionedShared;
        }

        public void LoadPackPaths() {
            ////Debug.Log("LoadPackPaths:appCachePathPacks:" + appCachePathPacks);
            ////Debug.Log("LoadPackPaths:appCachePathAllPlatformPacks:" + appCachePathAllPlatformPacks);

            if (packPaths.Count == 0) {

                //Debug.Log("Loading packPathsNONVersioned: " + appCachePathPacks);

                if (!string.IsNullOrEmpty(appCachePathPacks)) {
#if !UNITY_WEBPLAYER
                    foreach (string path in Directory.GetDirectories(appCachePathPacks)) {
                        string pathToAdd = PathUtil.Combine(appCachePathPacks, path);
                        if (!string.IsNullOrEmpty(pathToAdd)) {
                            if (!packPaths.Contains(pathToAdd)) {
                                packPaths.Add(pathToAdd);

                                //Debug.Log("Adding packPathsNONVersioned: pathToAdd:" + pathToAdd);
                            }
                        }
                    }
#endif
                }
            }

            if (packPathsVersionedShared.Count == 0) {

                //Debug.Log("Loading packPathsVersionedShared: " + appCachePathSharedPacks);

                if (!string.IsNullOrEmpty(appCachePathSharedPacks)) {
#if !UNITY_WEBPLAYER
                    foreach (string path in Directory.GetDirectories(appCachePathSharedPacks)) {
                        string pathToAdd = PathUtil.Combine(appCachePathSharedPacks, path);
                        if (!string.IsNullOrEmpty(pathToAdd)) {
                            if (!packPathsVersionedShared.Contains(pathToAdd)) {
                                packPathsVersionedShared.Add(pathToAdd);

                                //Debug.Log("Adding packPathsVersionedShared: pathToAdd:" + pathToAdd);
                            }
                        }
                    }
#endif
                }
            }

            if (packPathsVersioned.Count == 0) {

                //Debug.Log("Loading packPathsVersioned: " + appCachePathAllPlatformPacks);
                if (!string.IsNullOrEmpty(appCachePathAllPlatformPacks)) {
#if !UNITY_WEBPLAYER
                    foreach (string path in Directory.GetDirectories(appCachePathAllPlatformPacks)) {
                        string pathToAdd = PathUtil.Combine(appCachePathAllPlatformPacks, path);
                        if (!string.IsNullOrEmpty(pathToAdd)) {
                            if (!packPathsVersioned.Contains(pathToAdd)) {
                                packPathsVersioned.Add(pathToAdd);

                                //Debug.Log("Adding packPathsVersioned: pathToAdd:" + pathToAdd);
                            }
                        }
                    }
#endif
                }
            }
        }

        public string GetFileDataFromPersistentCache(string path, bool versioned, bool absolute) {
            string fileData = "";
            string pathPart = path;

            string pathToCopy = "";
            pathToCopy = PathUtil.Combine(GameversesContents.Instance.appShipCacheVersionPath, pathPart);

            if (!absolute) {
                path = PathUtil.Combine(GameversesContents.Instance.appCacheVersionPath, path);

                //shipPath = PathUtil.Combine(ContentPaths.appShipCacheVersionPath, path);
            }
            string pathVersioned = path;

            if (versioned) {
                pathVersioned = GameversesContents.Instance.GetFullPathVersioned(pathToCopy, pathVersioned);
            }

            ////Debug.Log("LoadDataFromPersistent:path:" + path);
            ////Debug.Log("LoadDataFromPersistent:pathVersioned:" + pathVersioned );

            if (!FileSystemUtil.CheckFileExists(pathVersioned) && !absolute) {

                // copy from streaming assets

                if (FileSystemUtil.CheckFileExists(pathToCopy)) {
                    FileSystemUtil.CopyFile(pathToCopy, pathVersioned);
                }
                else {
                    return "";
                }
            }
            fileData = FileSystemUtil.ReadString(pathVersioned);
            return fileData;
        }

        public void SyncFolders() {
            ////Debug.Log("Contents::SyncFolders");

            persistenceFolder = Application.persistentDataPath;
            streamingAssetsFolder = Application.streamingAssetsPath;

            // //Debug.Log("persistenceFolder: " + persistenceFolder);
            // //Debug.Log("streamingAssetsFolder: " + streamingAssetsFolder);

            string pathRoot = PathUtil.Combine(persistenceFolder, currentRootAppCode);
            string pathShipRoot = PathUtil.Combine(streamingAssetsFolder, currentRootAppCode);
#if !UNITY_WEBPLAYER
            if (!Directory.Exists(pathRoot)) {
                Directory.CreateDirectory(pathRoot);
            }
#endif
            string pathRootAppend = PathUtil.Combine(pathRoot, currentAppCode);
            string pathShipRootAppend = PathUtil.Combine(pathShipRoot, currentAppCode);
#if !UNITY_WEBPLAYER
            if (!Directory.Exists(pathRootAppend)) {
                Directory.CreateDirectory(pathRootAppend);
            }
#endif

            appCachePath = pathRootAppend;
            appShipCachePath = pathShipRootAppend;

            appCacheVersionPath = PathUtil.Combine(appCachePath, GameversesContents.Instance.currentVersion);
            appShipCacheVersionPath = PathUtil.Combine(appShipCachePath, GameversesContentConfig.contentCacheVersion);

            appCachePathAll = PathUtil.Combine(appCachePath, GameversesContentConfig.contentCacheAll);
            appShipCachePathAll = PathUtil.Combine(appShipCachePath, GameversesContentConfig.contentCacheAll);
            appCachePathAllShared = PathUtil.Combine(appCachePathAll, GameversesContentConfig.contentCacheShared);
            appCachePathAllSharedTrackers = PathUtil.Combine(appCachePathAllShared, GameversesContentConfig.contentCacheTrackers);
            appCachePathAllSharedUserData = PathUtil.Combine(appCachePathAllShared, GameversesContentConfig.contentCacheUserData);

            appCachePathAllPlatform = PathUtil.Combine(appCachePathAll, GetCurrentPlatformCode());
            appCachePathAllPlatformPacks = PathUtil.Combine(appCachePathAllPlatform, GameversesContentConfig.contentCachePacks);
            appCachePathAllPlatformData = PathUtil.Combine(appCachePathAllPlatform, GameversesContentConfig.contentCacheData);

            ////Debug.Log("appCachePath: " + appCachePath);
            ////Debug.Log("appShipCachePath: " + appShipCachePath);

            ////Debug.Log("appCacheVersionPath: " + appCacheVersionPath);
            ////Debug.Log("appShipCacheVersionPath: " + appShipCacheVersionPath);

            ////Debug.Log("appCachePathAll: " + appCachePathAll);
            ////Debug.Log("appShipCachePathAll: " + appShipCachePathAll);
						
#if !UNITY_WEBPLAYER
            if (!Directory.Exists(appCachePath)) {
                Directory.CreateDirectory(appCachePath);
            }

            if (!Directory.Exists(appCacheVersionPath)) {
                Directory.CreateDirectory(appCacheVersionPath);
            }

            if (!Directory.Exists(appShipCachePathAll)) {
                Directory.CreateDirectory(appShipCachePathAll);
            }
#endif

            appCachePlatformPath = PathUtil.Combine(appCacheVersionPath, GetCurrentPlatformCode());
            appShipCachePlatformPath = PathUtil.Combine(appShipCacheVersionPath, GetCurrentPlatformCode());
#if !UNITY_WEBPLAYER
            if (!Directory.Exists(appCachePlatformPath)) {
                Directory.CreateDirectory(appCachePlatformPath);
            }
#endif

            appCachePathData = PathUtil.Combine(appCacheVersionPath, GameversesContentConfig.contentCacheData);
            appCachePathShared = PathUtil.Combine(appCacheVersionPath, GameversesContentConfig.contentCacheShared);
            appCachePathSharedPacks = PathUtil.Combine(appCachePathShared, GameversesContentConfig.contentCachePacks);
            appCachePathSharedTrackers = PathUtil.Combine(appCachePathShared, GameversesContentConfig.contentCacheTrackers);
            appCachePathPacks = PathUtil.Combine(appCachePlatformPath, GameversesContentConfig.contentCachePacks);

            //appCachePathAllPlatformPacks
#if !UNITY_WEBPLAYER
            if (!Directory.Exists(appCachePathShared)) {
                Directory.CreateDirectory(appCachePathShared);
            }

            if (!Directory.Exists(appCachePathData)) {
                Directory.CreateDirectory(appCachePathData);
            }
#endif

            appShipCachePathData = PathUtil.Combine(appShipCacheVersionPath, GameversesContentConfig.contentCacheData);
            appShipCachePathShared = PathUtil.Combine(appShipCacheVersionPath, GameversesContentConfig.contentCacheShared);

            DirectoryCopy(appShipCachePlatformPath, appCachePlatformPath, true, true);

            //DirectoryCopy(appShipCachePathPacks, appCachePathPacks, true);
            DirectoryCopy(appShipCachePathData, appCachePathData, true, true);
            DirectoryCopy(appShipCachePathShared, appCachePathShared, true, true);
            DirectoryCopy(appShipCachePathAll, appCachePathAll, true, false);  // files in all/shared are not versioned...
        }

        public string GetFullPathVersioned(string fullPath) {
            string fileHash = "";//CryptoUtil.CalculateMD5HashFromFile(fullPath);
            return GetFileVersioned(fullPath, fileHash);
        }

        public string GetFullPathVersioned(string hashPath, string pathToVersion) {
            if (FileSystemUtil.CheckFileExists(hashPath)) {
                string fileHash = "";//CryptoUtil.CalculateMD5HashFromFile(hashPath);
                return GetFileVersioned(pathToVersion, fileHash);
            }
            else {
                return hashPath;
            }
        }

        public string GetFileVersioned(string path, string hash) {
            string fileVersioned = "";
            if (!string.IsNullOrEmpty(path)) {
                string[] arrpath = path.Split('/');

                fileVersioned = path;

                if (arrpath != null) {
                    string filepart = arrpath[arrpath.Length - 1];
                    string arttpathrest = path.Replace(filepart, "");
                    string[] arrfilepart = filepart.Split('.');
                    string ext = arrfilepart[arrfilepart.Length - 1];
                    string filepartbare = filepart.Replace("." + ext, "");

                    string appVersion = currentVersion.Replace(".", "-");
                    string appIncrement = currentIncrement.ToString();

                    fileVersioned = PathUtil.Combine(arttpathrest, filepartbare + "-" + appVersion + "-" + appIncrement + "-" + hash + "." + ext);
                }
            }

            return fileVersioned;
        }

        public void CheckContentSetFiles() {

            //Debug.Log("Contents::CheckContentSetFiles");

            SyncFolders();
        }

        public void ContentServerSync() {

            // hit /sync url to see what to update
            // Save local if exists, else pull local
        }

        /*
        public static IEnumerator SceneLoadFromCacheOrDownloadCo(string packName, string sceneName) {
            int version = GamePacks.currentPacksIncrement;
            string url = "https://s3.amazonaws.com/game-supasupacross/1.1/ios/" + packName + ".unity3d";

            LogUtil.Log("SceneLoadFromCacheOrDownloadCo: " + url);

            var downloadProgress = WWW.LoadFromCacheOrDownload(url, version);

            yield return downloadProgress;

            // Handle error
            if (downloadProgress.error != null)
            {
                //GameMessenger<ContentItemError>.Broadcast(GameversesContentMessages.ContentItemDownloadError,
                                                      //contentItemError);

                LogUtil.LogError("Error downloading");
            }
            else {

                // In order to make the scene available from LoadLevel, we have to load the asset bundle.
                // The AssetBundle class also lets you force unload all assets and file storage once it
                // is no longer needed.
                Contents.lastBundle = downloadProgress.assetBundle;

                //contentItem.bundle = bundle;

                //contentItemList.Add(contentItem);

                // Load the level we have just downloaded
                Application.LoadLevel (sceneName);
            }
        }

        public IEnumerator PrepareSceneLoadFromCacheOrDownloadCoroutine(string sceneName, int version) {
            ContentItem contentItem = new ContentItem();

            contentItem.uid = sceneName; // hash this
            contentItem.name = sceneName;
            contentItem.version = version;

            var downloadProgress = WWW.LoadFromCacheOrDownload("Streamed-" + sceneName + ".unity3d", version);

            yield return downloadProgress;

            // Handle error
            if (downloadProgress.error != null)
            {
                ContentItemError contentItemError = new ContentItemError();
                contentItemError.contentItem = contentItem;
                contentItemError.name = sceneName;
                contentItemError.message = downloadProgress.error;
                GameMessenger<ContentItemError>.Broadcast(GameversesContentMessages.ContentItemDownloadError,
                                                      contentItemError);
            }
            else {

                // In order to make the scene available from LoadLevel, we have to load the asset bundle.
                // The AssetBundle class also lets you force unload all assets and file storage once it
                // is no longer needed.
                AssetBundle bundle = downloadProgress.assetBundle;
                contentItem.bundle = bundle;

                contentItemList.Add(contentItem);

                // Load the level we have just downloaded
                //Application.LoadLevel ("Level1");
            }
        }
        */
    }
}