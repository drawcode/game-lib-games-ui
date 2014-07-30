using System;
using System.Collections;
using System.Collections.Generic;
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

    public GameSyncFileObject() {
        data_type = GameSyncType.upload;
    }
}

public class GameSyncObject : GameDataObject {

    // uuid
    // email
    // code
    // username@@ryn@pcstein00BB--!!!

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

    public void Reset() {
        email = "";
        
        if(files == null)
            files = new Dictionary<string, GameSyncFileObject>();
        else 
            files.Clear();
    }

    public void SetFile(string code, string path, string content) {

        GameSyncFileObject fileObject = null;

        if(files.ContainsKey(code)) {
            fileObject = files[code];
        }

        if(fileObject == null) {
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
        
        
    }
    
    void OnDisable() {
        
    }    
    
    void Init() {  
        Reset();
    }


    void Reset() {
        
        if(profileSyncObject == null) {
            profileSyncObject = new GameSyncObject();
        }

        if(gameSyncObject == null) {
            gameSyncObject = new GameSyncObject();
        }

        profileSyncObject.Reset();
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

}
