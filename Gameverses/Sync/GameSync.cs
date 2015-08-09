using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

using UnityEngine;

using Engine.Data.Json;
using Engine.Events;
using Engine.Utility;

public class GameSyncMessages {
    
    public static string cloudSyncStart = "cloud-sync-start";
    public static string cloudSyncSuccess = "cloud-sync-success";
    public static string cloudSyncError = "cloud-sync-error";
}

public class BaseGameSyncResult : GameDataObject {
    
    public virtual GameDataObject info {
        get {
            return Get<GameDataObject>(BaseDataObjectKeys.info);
        }
        
        set {
            Set<GameDataObject>(BaseDataObjectKeys.info, value);
        }
    }
    
    public virtual int error {
        get {
            return Get<int>(BaseDataObjectKeys.error);
        }
        
        set {
            Set<int>(BaseDataObjectKeys.error, value);
        }
    } 
}

public class GameSyncResult : BaseGameSyncResult {        
    
    public virtual GameSyncObject data {
        get {
            return Get<GameSyncObject>(BaseDataObjectKeys.data);
        }
        
        set {
            Set<GameSyncObject>(BaseDataObjectKeys.data, value);
        }
    } 
}

public class GameSyncType {
    public static string upload = "upload";
    public static string upload_delta = "upload-delta";
    public static string download = "download";
    public static string download_delta = "download-delta";
}

public class GameSyncFileObject : GameDataObject {

    // code 
    // path
    // url 
    // content
    // data_type
    // hash

    public GameSyncFileObject() {
        data_type = GameSyncType.upload;
    }
}

public class GameSyncObject : GameDataObject {

    // uuid
    // email
    // code
    // username

    public GameSyncObject() {
        Reset();
    }
    
    public virtual Dictionary<string, GameSyncFileObject> files {
        get {
            return Get<Dictionary<string, GameSyncFileObject>>(BaseDataObjectKeys.files);
        }
        
        set {
            Set<Dictionary<string, GameSyncFileObject>>(BaseDataObjectKeys.files, value);
        }
    }

    public override void Reset() {
        base.Reset();

        email = "";
        
        if (files == null)
            files = new Dictionary<string, GameSyncFileObject>();
        else 
            files.Clear();
    }

    public void SetFile(string code, string path, string content) {

        GameSyncFileObject fileObject = null;

        if (files.ContainsKey(code)) {
            fileObject = files[code];
        }

        if (fileObject == null) {
            fileObject = new GameSyncFileObject();
        }

        fileObject.code = code;
        fileObject.path = path;
        fileObject.content = content;

        files.Set(code, fileObject);
    }

}

public class GameSyncGame : GameDataObject {
    // game_id
    // 
}

public class GameSyncNetwork : GameDataObject {

    // network_id
    // username
}

public class GameSyncActions {
    public static string syncProfile = "sync/profile/";
}


public class GameSync : GameObjectBehavior { 
    
    public static bool gameCloudSyncEnabled = AppConfigs.gameCloudSyncEnabled;
    public static bool gameCloudSyncTestingEnabled = AppConfigs.gameCloudSyncTestingEnabled;
    
    // Only one GameSync can exist. We use a singleton pattern to enforce this.
    private static GameSync _instance = null;
    
    public static GameSync Instance {
        get {
            if (!_instance) {
                
                // check if an ObjectPoolManager is already available in the scene graph
                _instance = FindObjectOfType(typeof(GameSync)) as GameSync;
                
                // nope, create a new one
                if (!_instance) {
                    var obj = new GameObject("_GameSync");
                    _instance = obj.AddComponent<GameSync>();
                }
            }
            
            return _instance;
        }
    }

    //

    public GameSyncObject profileSyncObject;
    public GameSyncObject gameSyncObject;

    //
    
    void Start() {
        Init();
    }
    
    void OnEnable() {        
        
        Messenger<WWWs.RequestItem>.AddListener(
            WWWs.StatusMessages.success, OnWWWRequestItemSuccess);  
        
        Messenger<WWWs.RequestItem>.AddListener(
            WWWs.StatusMessages.started, OnWWWRequestItemStarted);  

        Messenger<WWWs.RequestItem,string>.AddListener(
            WWWs.StatusMessages.error, OnWWWRequestItemError);        
    }
    
    void OnDisable() {
        
        Messenger<WWWs.RequestItem>.RemoveListener(
            WWWs.StatusMessages.success, OnWWWRequestItemSuccess);  
        
        Messenger<WWWs.RequestItem>.RemoveListener(
            WWWs.StatusMessages.started, OnWWWRequestItemStarted);  
        
        Messenger<WWWs.RequestItem,string>.RemoveListener(
            WWWs.StatusMessages.error, OnWWWRequestItemError);         
        
    }
    
    void Init() {  
        Reset();
    }

    void Reset() {

        ResetProfileSyncObject();
        ResetGameSyncObject();
    }

    //

    public void OnWWWRequestItemSuccess(WWWs.RequestItem requestItem) {
        
        //Debug.Log("OnWWWRequestItemSuccess:" + requestItem.ToJson());

        //Dictionary<string,object> files = new Dictionary<string, object>();


        //foreach(var file in requestItem.

        //profileSyncObject.Add(key:, ValueType);

        if(requestItem.IsAction(GameSyncActions.syncProfile)) {
            
            Debug.Log("OnWWWRequestItemSuccess:" + requestItem.ToJson());
        }

    }

    public void HandleActionSyncProfile() {
        
    }
    
    //
    
    public void OnWWWRequestItemStarted(WWWs.RequestItem requestItem) {
        
        Debug.Log("OnWWWRequestItemStarted:" + requestItem.ToJson());
    }
    
    //
    
    public void OnWWWRequestItemError(WWWs.RequestItem requestItem, string error) {
        
        Debug.Log("OnWWWRequestItemError:" + requestItem.ToJson());
        Debug.Log("OnWWWRequestItemError:error:" + error);
    }

    //

    public static void ResetProfileSyncObject() {
        Instance.resetProfileSyncObject();
    }

    public void resetProfileSyncObject() {
        
        if (profileSyncObject == null) {
            profileSyncObject = new GameSyncObject();
        }
        profileSyncObject.Reset();
    }
    
    public static void ResetGameSyncObject() {
        Instance.resetGameSyncObject();
    }
    
    public void resetGameSyncObject() {
        
        if (gameSyncObject == null) {
            gameSyncObject = new GameSyncObject();
        }
        gameSyncObject.Reset();
    }

    // EVENTS

    void OnGameSyncStart(GameSyncResult result) {

    }
    
    void OnGameSyncSuccess(GameSyncResult result) {
        
    }
    
    void OnGameSyncError(GameSyncResult result) {
        
    }

    //
    //
    
    // SET PROFILE SYNC DATA BY META
    
    //
    
    public static void SetProfileSyncContent(string code, string path, string content) {
        Instance.setProfileSyncContent(code, path, content);
    }
    
    public void setProfileSyncContent(string code, string path, string content) {
        profileSyncObject.SetFile(code, path, content);
    }

    //
    //

    // SYNC PROFILE

    //

    public static void SyncProfile() {
        
        Instance.syncProfile();        
    }
        
    public void syncProfile() {

        string uuid = GameProfiles.Current.uuid;

        if (string.IsNullOrEmpty(uuid)) {

            GameState.LoadProfile();
            
            if (string.IsNullOrEmpty(uuid)) {

                GameProfiles.Current.uuid = UniqueUtil.Instance.CreateUUID4();
                GameState.SaveProfile();
            }
        }

        syncProfile(
                Path.Combine(GameversesConfig.apiPath, GameSyncActions.syncProfile),
            AppConfigs.gameCloudSyncKey, // key
            UniqueUtil.Instance.currentUniqueId, // uid
            GameProfiles.Current.uuid, // profile_id
            GameversesConfig.apiGameId, // game_id
            profileSyncObject); // encrypted/compressed profile file sets
    }

    //

    public static void SyncProfile(
        string url, 
        string key,
        string uid,
        string profile_id, 
        string game_id, 
        object data) {

        Instance.syncProfile(
            url, 
            key,
            uid,
            profile_id, 
            game_id, 
            data);        
    }

    public void syncProfile(
        string url, 
        string key,
        string uid,
        string profile_id, 
        string game_id, 
        object data) {

        if(string.IsNullOrEmpty(profile_id)) {
            return;
        }
        
        CoroutineUtil.Start(syncProfileCo(
            url, 
            key,
            uid,
            profile_id, 
            game_id, 
            data));
    }

    //
        
    public IEnumerator syncProfileCo(
        string url, 
        string key,
        string uid,
        string profile_id, 
        string game_id, 
        object data) {
        
        //yield return new WaitForEndOfFrame();
        
        WWWs.RequestItem requestItem = new WWWs.RequestItem();
        
        requestItem.url = url;// + "?r=" + UnityEngine.Random.Range(0,99999).ToString();
        //http://localhost:3330/api/v1/sync/profile/test

        requestItem.SetRequestType(WWWs.RequestType.post);
        requestItem.paramHash.Set("key", key);
        requestItem.paramHash.Set("uid", uid);
        requestItem.paramHash.Set("profile_id", profile_id);
        requestItem.paramHash.Set("game_id", game_id);

        requestItem.paramHash.Set("compressed", ProfileConfigs.useStorageCompression);
        requestItem.paramHash.Set("encrypted", ProfileConfigs.useStorageEncryption);

        string dataValue = "";//data.ToJson();
        if (data != null) {
            dataValue = data.ToJson();
            if (!string.IsNullOrEmpty(dataValue)) {
                dataValue = dataValue.TrimStart('"').TrimEnd('"').Replace("\\\"", "\"");
            }
        }
        
        requestItem.paramHash.Set("data", data);        
        
        Debug.Log("requestItem.paramHash" + requestItem.paramHash.ToJson());
        
        WWWs.Request(requestItem);
        //Debug.Log("" + requestItem.ToJson());

        yield return new WaitForEndOfFrame();
    }
}
