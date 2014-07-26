using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using UnityEngine;

using Engine.Data.Json;
using Engine.Events;
using Engine.Utility;

public class GameCloudSyncMessages {
    
    public static string cloudSyncStart = "cloud-sync-start";
    public static string cloudSyncSuccess = "cloud-sync-success";
    public static string cloudSyncError = "cloud-sync-error";
}

public class BaseGameCloudSyncResult : GameDataObject {
    
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

public class GameCloudSyncResult : BaseGameCloudSyncResult {        
    
    public virtual GameCloudSyncObject data {
        get {
            return Get<GameCloudSyncObject>(BaseDataObjectKeys.data);
        }
        
        set {
            Set<GameCloudSyncObject>(BaseDataObjectKeys.data, value);
        }
    } 
}

public class GameCloudSyncType {
    public static string upload = "upload";
    public static string upload_delta = "upload-delta";
    public static string download = "download";
    public static string download_delta = "download-delta";
}

public class GameCloudSyncFileObject : GameDataObject {

    // code 
    // path
    // url 
    // content
    // data_type

    public GameCloudSyncFileObject() {
        data_type = GameCloudSyncType.upload;
    }
}

public class GameCloudSyncObject : GameDataObject {

    // uuid
    // email
    // code
    // username

    public GameCloudSyncObject() {
        Reset();
    }
    
    public virtual Dictionary<string, GameCloudSyncFileObject> files {
        get {
            return Get<Dictionary<string, GameCloudSyncFileObject>>(BaseDataObjectKeys.files);
        }
        
        set {
            Set<Dictionary<string, GameCloudSyncFileObject>>(BaseDataObjectKeys.files, value);
        }
    } 

    public void Reset() {
        email = "";
        
        if(files == null)
            files = new Dictionary<string, GameCloudSyncFileObject>();
        else 
            files.Clear();
    }

    public void SetFile(string code, string path, string content) {

        GameCloudSyncFileObject fileObject = null;

        if(files.ContainsKey(code)) {
            fileObject = files[code];
        }

        if(fileObject == null) {
            fileObject = new GameCloudSyncFileObject();
        }

        fileObject.code = code;
        fileObject.path = path;
        fileObject.content = content;

        files.Set(code, fileObject);
    }

}

public class GameCloudSyncGame : GameDataObject {
    // game_id
    // 
}

public class GameCloudSyncNetwork : GameDataObject {

    // network_id
    // username
}

public class GameCloudSync : GameObjectBehavior { 
    
    public static bool gameCloudSyncEnabled = AppConfigs.gameCloudSyncEnabled;
    public static bool gameCloudSyncTestingEnabled = AppConfigs.gameCloudSyncTestingEnabled;
    
    // Only one GameCloudSync can exist. We use a singleton pattern to enforce this.
    private static GameCloudSync _instance = null;
    
    public static GameCloudSync Instance {
        get {
            if (!_instance) {
                
                // check if an ObjectPoolManager is already available in the scene graph
                _instance = FindObjectOfType(typeof(GameCloudSync)) as GameCloudSync;
                
                // nope, create a new one
                if (!_instance) {
                    var obj = new GameObject("_GameCloudSync");
                    _instance = obj.AddComponent<GameCloudSync>();
                }
            }
            
            return _instance;
        }
    }

    //

    public GameCloudSyncObject profileSyncObject;
    public GameCloudSyncObject gameSyncObject;

    //
    
    void Start() {
        Init();
    }
    
    void OnEnable() {
        
        
    }
    
    void OnDisable() {
        
    }    
    
    void Init() {  
        Reset();
    }


    void Reset() {
        
        if(profileSyncObject == null) {
            profileSyncObject = new GameCloudSyncObject();
        }

        if(gameSyncObject == null) {
            gameSyncObject = new GameCloudSyncObject();
        }

        profileSyncObject.Reset();
        gameSyncObject.Reset();
    }

    // EVENTS

    void OnGameCloudSyncStart(GameCloudSyncResult result) {

    }
    
    void OnGameCloudSyncSuccess(GameCloudSyncResult result) {
        
    }
    
    void OnGameCloudSyncError(GameCloudSyncResult result) {
        
    }

    //
    //

}
