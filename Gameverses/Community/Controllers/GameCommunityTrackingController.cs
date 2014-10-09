using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

#if !UNITY_WEBPLAYER
using System.IO;
#endif
using Engine.Data.Json;
using Engine.Events;
using Engine.Networking;

public class GameCommunityTrackingController : GameObjectBehavior {
    
    public static GameCommunityTrackingController Instance;
    public static System.Object syncRoot = new System.Object();
    public float currentTimeBlock = 0.0f;
    public float actionInterval = 1.0f;
    public GameCommunitySyncDataUpdates dataUpdates;
    public GameCommunitySystemTracking systemTracking;
    public List<GoogleTracker> googleTrackers;
    public GameCommunityTrackerReports trackerReports;
    
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
        Messenger<string>.AddListener(SocialNetworksMessages.socialLoggedIn, OnProfileLoggedIn);
        Messenger<string, string, object>.AddListener(SocialNetworksMessages.socialProfileData, OnProfileData);
        
        Messenger<GameCommunityLeaderboardData>.AddListener(
            GameCommunityMessages.gameCommunityLeaderboardData, 
            OnLeaderboardData);
        
        Messenger<GameCommunityLeaderboardData>.AddListener(
            GameCommunityMessages.gameCommunityLeaderboardUserData, 
            OnProfileLeaderboardData);
        //socialLoggedIn
    }
    
    void OnDisable() {
        Messenger<string>.RemoveListener(SocialNetworksMessages.socialLoggedIn, OnProfileLoggedIn);
        Messenger<string, string, object>.RemoveListener(SocialNetworksMessages.socialProfileData, OnProfileData);  
        
        Messenger<GameCommunityLeaderboardData>.AddListener(
            GameCommunityMessages.gameCommunityLeaderboardData, 
            OnLeaderboardData);
        
        Messenger<GameCommunityLeaderboardData>.RemoveListener(
            GameCommunityMessages.gameCommunityLeaderboardUserData, 
            OnProfileLeaderboardData);
    }
    
    void Init() {
        if (systemTracking == null) {
            systemTracking = new GameCommunitySystemTracking();
        }
        
        if (dataUpdates == null) {
            dataUpdates = new GameCommunitySyncDataUpdates();
        }
        
        if (googleTrackers == null) {           
            googleTrackers = new List<GoogleTracker>();
        }
        
        if (googleTrackers.Count == 0) {
            
            if (Context.Current.hasNetworkAccess) {
            
                string[] accountIds = AppConfigs.trackingGoogleAccountIds;
                string[] domains = AppConfigs.trackingGoogleAccountDomains;
                
                string lastDomain = "";
                
                for (int i = 0; i < accountIds.Length; i++) {
                    if (domains.Length > i) {
                        lastDomain = domains[i];
                    }
                    
                    googleTrackers.Add(new GoogleTracker(accountIds[i], lastDomain, new GoogleTrackerUnity3DAnalyticsSession()));
                }
            }
        }
        
        if (trackerReports == null) {
            trackerReports = new GameCommunityTrackerReports();
        }
    }
    
    public static void ProcessTrackers() {
        if (Instance != null) {
            Instance.processTrackers();
        }
    }
    
    public void processTrackers() {
        if (Context.Current.hasNetworkAccess) {
            StartCoroutine(processTrackersCo());
        }
        else {
            if (trackerReports != null) {
                // Save to user data for later
                trackerReports.Save();
            }
        }
    }
    
    private bool processTrackersRunning = false;
    
    public IEnumerator processTrackersCo() {
                
        //LogUtil.Log("processTrackersCo");
        
        if (!processTrackersRunning && trackerReports != null) {
            processTrackersRunning = true;
            
            GameCommunityTrackerReports trackerReportsCopy = trackerReports.Copy();
            
            if (trackerReportsCopy.trackerReports != null) {
            
                foreach (GameCommunityTrackerReport trackerReport in trackerReportsCopy.trackerReports) {
                
                    // Views
                    
                    if (trackerReport.trackerViews != null) {
                        
                        //LogUtil.Log("processTrackersCo: trackerReport.trackerViews.Count" + trackerReport.trackerViews.Count);
                        
                        foreach (GameCommunityTrackerView trackerView in trackerReport.trackerViews) {
                            
                            // track google
                
                            foreach (GoogleTracker googleTracker in googleTrackers) {
                                if (googleTracker != null) {
                                    googleTracker.TrackPageView(trackerView.title, trackerView.url);
                                    yield return new WaitForSeconds(.2f);
                                }
                            }
                                    
                            // track custom
                            
                            // track other...
                            
                        }
                    }
                    
                    // Events           
                    
                    if (trackerReport.trackerEvents != null) {
                        
                        //LogUtil.Log("processTrackersCo: trackerReport.trackerEvents.Count" + trackerReport.trackerEvents.Count);
                        
                        foreach (GameCommunityTrackerEvent trackerEvent in trackerReport.trackerEvents) {
                            
                            // track google
                
                            foreach (GoogleTracker googleTracker in googleTrackers) {
                                if (googleTracker != null) {
                                    googleTracker.TrackEvent(
                                        trackerEvent.category, 
                                        trackerEvent.action, 
                                        trackerEvent.label, 
                                        trackerEvent.val);
                                    yield return new WaitForSeconds(.2f);
                                }
                            }
                                    
                            // track custom
                            
                            // track other...
                            
                        }
                    }   
                }
            }
                
            lock (syncRoot) {               
                trackerReports.Reset();
            }
            
            processTrackersRunning = false;
        }
        
        yield break;
    }
    
    public static void TrackView(string title, string url) {
        if (Instance != null) {
            Instance.trackView(title, url);
        }
    }
    
    string currentCheckPointTitle = "";
    string currentCheckPointUrl = "";
    
    public void trackView(string title, string url) {

        if (AppConfigs.analyticsNetworkEnabledGoogle) {

            LogUtil.Log("trackView: title:" + title + " url:" + url);
            if (trackerReports != null) {
                GameCommunityTrackerView trackerView = new GameCommunityTrackerView();
                trackerView.title = title;
                trackerView.url = url;
                trackerReports.SetTrackerView(trackerView);     
            }
            
            TestFlight.CheckpointExit(currentCheckPointTitle, currentCheckPointUrl);
            
            currentCheckPointTitle = title;     
            currentCheckPointUrl = url;     
            
            TestFlight.CheckpointEnter(currentCheckPointTitle, currentCheckPointUrl);
        }
    }
    
    // TODO .. add common events for adding game event, store event, ui event, load event etc.
        
    public static void TrackEvent(string category, string action, string label, int val) {
        if (Instance != null) {
            Instance.trackEvent(category, action, label, val);
        }
    }
        
    public static void TrackEvent(string category, string action) {
        if (Instance != null) {
            Instance.trackEvent(category, action, null, 1);
        }       
    }
    
    public void trackEvent(string category, string action, string label, int val) {
        LogUtil.Log("trackEvent: category:" + category + " action:" + action + " label:" + label + " val:" + val.ToString());
        
        if (trackerReports != null) {
            GameCommunityTrackerEvent trackerEvent = new GameCommunityTrackerEvent();
            trackerEvent.category = category;
            trackerEvent.action = action;
            trackerEvent.label = label;
            trackerEvent.val = val;
            trackerReports.SetTrackerEvent(trackerEvent);   
        }
        
        //TestFlight.CheckpointEvent(category, action, label, val.ToString());
    }
    
    public static void SyncSystemAttributes() {
        if (Instance != null) {
            Instance.syncSystemAttributes();
        }   
    }
    
    public void syncSystemAttributes() {
    
        // os
                        
        GameProfileTrackers.Current.SetAttributeStringValue(
            GameProfileTrackerAttributes.infoSystemOperatingSystem, 
            GameCommunitySystemTracking.Instance.operatingSystem);
        
        // platform
        
        GameProfileTrackers.Current.SetAttributeStringValue(
            GameProfileTrackerAttributes.infoSystemPlatformCode, 
            GameCommunitySystemTracking.Instance.platformCode);
                
        GameProfileTrackers.Current.SetAttributeStringValue(
            GameProfileTrackerAttributes.infoSystemPlatformCode, 
            GameCommunitySystemTracking.Instance.platform);
        
        // device
        
#if UNITY_IPHONE
        GameProfileTrackers.Current.SetAttributeStringValue(
            GameProfileTrackerAttributes.infoSystemDeviceGeneration, 
            iPhone.generation.ToString());
#endif
        GameProfileTrackers.Current.SetAttributeStringValue(
            GameProfileTrackerAttributes.infoSystemDeviceModel, 
            GameCommunitySystemTracking.Instance.deviceModel);      
        
        GameProfileTrackers.Current.SetAttributeStringValue(
            GameProfileTrackerAttributes.infoSystemDeviceName, 
            GameCommunitySystemTracking.Instance.deviceName);
        
        GameProfileTrackers.Current.SetAttributeStringValue(
            GameProfileTrackerAttributes.infoSystemDeviceType, 
            GameCommunitySystemTracking.Instance.deviceType);       
        
        GameProfileTrackers.Current.SetAttributeStringValue(
            GameProfileTrackerAttributes.infoSystemDeviceUniqueIdentifier, 
            GameCommunitySystemTracking.Instance.deviceUniqueIdentifier);
                
        GameProfileTrackers.Current.SetAttributeStringValue(
            GameProfileTrackerAttributes.infoSystemDeviceUniqueIdentifierUuid, 
            UniqueUtil.Instance.currentUniqueId);
                
        GameProfileTrackers.Current.SetAttributeStringValue(
            GameProfileTrackerAttributes.infoSystemProcessorType, 
            GameCommunitySystemTracking.Instance.processorType);
        
        GameProfileTrackers.Current.SetAttributeStringValue(
            GameProfileTrackerAttributes.infoSystemGraphicsDeviceName, 
            GameCommunitySystemTracking.Instance.graphicsDeviceName);
        
        /*
        GameProfileTrackers.Current.SetAttributeStringValue(
            GameProfileTrackerAttributes.infoSystemSupportsShadows, 
            GameCommunitySystemTracking.Instance.supportsShadows);
                
        GameProfileTrackers.Current.SetAttributeStringValue(
            GameProfileTrackerAttributes.infoSystemSupportsVibration, 
            GameCommunitySystemTracking.Instance.supportsVibration);    
            */  
        
        GameProfileTrackers.Current.SetAttributeStringValue(
            GameProfileTrackerAttributes.infoSystemScreenSize, 
            string.Format("{0}x{1}", Screen.width, Screen.height));
                
        GameProfileTrackers.Current.SetAttributeIntValue(
            GameProfileTrackerAttributes.infoSystemScreenWidth, 
            Screen.width);      
        
        GameProfileTrackers.Current.SetAttributeIntValue(
            GameProfileTrackerAttributes.infoSystemScreenHeight, 
            Screen.height);
        
        GameProfileTrackers.Current.SetAttributeDoubleValue(
            GameProfileTrackerAttributes.infoSystemScreenDpi, 
            Screen.dpi);
        
        GameState.SaveProfile();
        
        //
        
        
    }
    
    public static void ResetUpdated() {
        if (Instance != null) {
            Instance.resetUpdated();
        }
    }
    
    public void resetUpdated() {
        dataUpdates.Reset();
    }
    
    // statistics
    
    public static void SetSyncStatistic(string code, float val) {
        if (Instance != null) {
            Instance.setSyncStatistic(code, val);
        }
    }
    
    public void setSyncStatistic(string code, float val) {
        dataUpdates.SetStatistic(code, val);    
    }
    
    // achievements
    
    public static void SetSyncAchievement(string code, float val) {
        if (Instance != null) {
            Instance.setSyncAchievement(code, val);
        }
    }
    
    public void setSyncAchievement(string code, float val) {
        dataUpdates.SetAchievement(code, val);  
    }
    
    // profile game data attributes     
    
    public static void SetSyncProfileGameDataAttribute(string code, DataAttribute val) {
        if (Instance != null) {
            Instance.setSyncProfileGameDataAttribute(code, val);
        }
    }
    
    public void setSyncProfileGameDataAttribute(string code, DataAttribute dataAttribute) {
        dataUpdates.SetProfileGameDataAttribute(code, dataAttribute);
    }
    
    public static void SetSyncProfileGameDataAttributes(Dictionary<string, DataAttribute> dataAttributes) {
        if (Instance != null) {
            Instance.setSyncProfileGameDataAttributes(dataAttributes);
        }
    }
    
    public void setSyncProfileGameDataAttributes(Dictionary<string, DataAttribute> dataAttributes) {
        dataUpdates.SetProfileGameDataAttributes(dataAttributes);
    }
    
    // profile attributes
    
    public static void SetSyncProfileAttribute(string code, string val) {
        if (Instance != null) {
            Instance.setProfileAttribute(code, val);
        }
    }
    
    public void setProfileAttribute(string code, string val) {
        dataUpdates.SetProfileAttribute(code, val); 
    }
    
    // game profile attributes
    
    public static void SetSyncGameProfileAttribute(string code, string val) {
        if (Instance != null) {
            Instance.setGameProfileAttribute(code, val);
        }
    }
    
    public void setGameProfileAttribute(string code, string val) {
        dataUpdates.SetGameProfileAttribute(code, val); 
    }
    
    // data attributes
    
    public static void SetSyncDataAttribute(string code, string val) {
        if (Instance != null) {
            Instance.setDataAttribute(code, val);
        }
    }
    
    public void setDataAttribute(string code, string val) {
        dataUpdates.SetDataAttribute(code, val);    
    }
    
    // events to track from
    
    void OnProfileLoggedIn(string networkType) {
        
    }
    
    void OnProfileData(string networkType, string dataType, object data) {
        
        
    }
    
    void OnLeaderboardData(GameCommunityLeaderboardData leaderboardData) {
        
        
    }
    
    void OnProfileLeaderboardData(GameCommunityLeaderboardData leaderboardData) {
        
    }
}
