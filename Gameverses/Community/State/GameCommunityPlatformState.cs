//#define STATE_RESOURCES
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

using UnityEngine;

using Engine.Data.Json;
using Engine.Events;
using Engine.Networking;

public class GameCommunityPlatformState 
{
	public GameProfile profile;
	public GameProfileStatistic profileStatistic;
	public GameProfileAchievement profileAchievement;
	public GameProfileTracker profileTracker;
	public GameProfileRPG profileRPG;
	
	private static volatile GameCommunityPlatformState instance;
	private static System.Object syncRoot = new System.Object();
	
	public Thread saveThread;
	
	public string KEY_PROFILE;
		
	public static GameCommunityPlatformState Instance {
		get {
			if (instance == null) {
				lock (syncRoot) {
					if (instance == null) 
						instance = new GameCommunityPlatformState();
				}
			}	
			return instance;
		}
	}
	
	private GameCommunityPlatformState() {
		InitState();
	}
	
	public void InitState() {
		KEY_PROFILE = "profile-v-1-0-2";
		
		profile = GameProfiles.Current;
		profileStatistic = GameProfileStatistics.Current;
		profileAchievement = GameProfileAchievements.Current;
		profileTracker = GameProfileTrackers.Current;
		profileRPG = GameProfileRPGs.Current;
		
		loadProfile();
		saveProfile();
	}
		
	// KEYS
	
	public string GetProfileKey(string username) {
		return KEY_PROFILE + "-" + System.Uri.EscapeUriString(username).ToLower();
	}
	
	public string GetProfileStatisticKey(string username) {
		return KEY_PROFILE + "-statistic" + "-" + System.Uri.EscapeUriString(username).ToLower();
	}
	
	public string GetProfileAchievementKey(string username) {
		return KEY_PROFILE + "-achievement" + "-" + System.Uri.EscapeUriString(username).ToLower();
	}
	
	public string GetProfileTrackerKey(string username) {
		return KEY_PROFILE + "-tracker" + "-" + System.Uri.EscapeUriString(username).ToLower();
	}
	
	public string GetProfileRPGKey(string username) {
		return KEY_PROFILE + "-rpg" + "-" + System.Uri.EscapeUriString(username).ToLower();
	}
		
	// PROFILE
	
	public static void SaveProfile() {
		if(Instance != null) {
			Instance.saveProfile();
		}
	}
	
	public void saveProfile() {
		//LogUtil.Log("SaveProfile: username: " + profile.username);
		
		string key = GetProfileKey(profile.username);
		string keyAchievement = GetProfileAchievementKey(profile.username);
		string keyStatistic = GetProfileStatisticKey(profile.username);
		string keyTracker = GetProfileTrackerKey(profile.username);
		string keyRPG = GetProfileRPGKey(profile.username);
		
		//LogUtil.Log("SaveProfile: key: " + key);
		//LogUtil.Log("SaveProfile: keyAchievement: " + keyAchievement);
		//LogUtil.Log("SaveProfile: keyStatistic: " + keyStatistic);
		//LogUtil.Log("SaveProfile: keyTracker: " + keyTracker);
		//LogUtil.Log("SaveProfile: keyRPG: " + keyRPG);
		
		string jsonString = JsonMapper.ToJson(profile);
		//LogUtil.Log("GameState::SaveProfile jsonString...." + jsonString);	
		
		string jsonStringAchievement = JsonMapper.ToJson(profileAchievement);
		//LogUtil.Log("GameState::SaveProfile jsonStringAchievement...." + jsonStringAchievement);	
		
		string jsonStringStatistic = JsonMapper.ToJson(profileStatistic);
		//LogUtil.Log("GameState::SaveProfile jsonStringStatistic...." + jsonStringStatistic);	
		
		string jsonStringTracker = JsonMapper.ToJson(profileTracker);
		//LogUtil.Log("GameState::SaveProfile jsonStringTracker...." + jsonStringTracker);
		
		string jsonStringRPG = JsonMapper.ToJson(profileRPG);
		//LogUtil.Log("GameState::SaveProfile jsonStringRPG...." + jsonStringRPG);
		
		
#if UNITY_WEBPLAYER || STATE_RESOURCES
		SystemPrefUtil.SetLocalSettingString(key, jsonString);
		SystemPrefUtil.Save();
		SystemPrefUtil.SetLocalSettingString(keyAchievement, jsonStringAchievement);
		SystemPrefUtil.Save();
		SystemPrefUtil.SetLocalSettingString(keyStatistic, jsonStringStatistic);
		SystemPrefUtil.Save();
		SystemPrefUtil.SetLocalSettingString(keyTracker, jsonStringTracker);
		SystemPrefUtil.Save();
		SystemPrefUtil.SetLocalSettingString(keyRPG, jsonStringRPG);
		SystemPrefUtil.Save();
#else
		if(!string.IsNullOrEmpty(ContentPaths.appCachePathAllSharedUserData)) {
			// general profile
			string persistentProfilePath = Path.Combine(
				ContentPaths.appCachePathAllSharedUserData,
				key + ".json"); 
			FileSystemUtil.WriteString(persistentProfilePath, jsonString);
			
			// achievements
			string persistentProfilePathAchievement = Path.Combine(
				ContentPaths.appCachePathAllSharedUserData,
				keyAchievement + ".json"); 
			FileSystemUtil.WriteString(persistentProfilePathAchievement, jsonStringAchievement);
			
			// statistics
			string persistentProfilePathStatistic = Path.Combine(
				ContentPaths.appCachePathAllSharedUserData,
				keyStatistic + ".json"); 
			FileSystemUtil.WriteString(persistentProfilePathStatistic, jsonStringStatistic);
			
			// trackers
			string persistentProfilePathTracker = Path.Combine(
				ContentPaths.appCachePathAllSharedUserData,
				keyTracker + ".json"); 
			FileSystemUtil.WriteString(persistentProfilePathTracker, jsonStringTracker);
			
			// rpg
			string persistentProfilePathRPG = Path.Combine(
				ContentPaths.appCachePathAllSharedUserData,
				keyRPG + ".json"); 
			FileSystemUtil.WriteString(persistentProfilePathRPG, jsonStringRPG);
						
			//LogUtil.Log("saveProfile: persistentProfilePath:" + persistentProfilePath);
			//LogUtil.Log("saveProfile: persistentProfilePathAchievement:" + persistentProfilePathAchievement);
			//LogUtil.Log("saveProfile: persistentProfilePathStatistic:" + persistentProfilePathStatistic);
			//LogUtil.Log("saveProfile: persistentProfilePathTracker:" + persistentProfilePathTracker);
			//LogUtil.Log("saveProfile: persistentProfilePathRPG:" + persistentProfilePathRPG);
		}
#endif
		
		//CoroutineUtil.Start(SaveProfileCo(profile));
	}
	
	public static void LoadProfile() {
		if(Instance != null) {
			Instance.loadProfile();	
		}
	}
	
	public void loadProfile() {
		string key = GetProfileKey(profile.username);
		string keyAchievement = GetProfileAchievementKey(profile.username);
		string keyStatistic = GetProfileStatisticKey(profile.username);
		string keyTracker = GetProfileTrackerKey(profile.username);
		string keyRPG = GetProfileRPGKey(profile.username);
		
		LogUtil.Log("LoadProfile: username: " + profile.username);
		LogUtil.Log("LoadProfile: key: " + key);
		
		string data = "";
		string dataAchievement = "";
		string dataStatistic = "";
		string dataTracker = "";
		string dataRPG = "";
#if UNITY_WEBPLAYER || STATE_RESOURCES
		data = SystemPrefUtil.GetLocalSettingString(key);
		dataAchievement = SystemPrefUtil.GetLocalSettingString(keyAchievement);
		dataStatistic = SystemPrefUtil.GetLocalSettingString(keyStatistic);
		dataTracker = SystemPrefUtil.GetLocalSettingString(keyTracker);
		dataRPG = SystemPrefUtil.GetLocalSettingString(keyRPG);
#else
		// general profile
		string persistentProfilePath = Path.Combine(
			ContentPaths.appCachePathAllSharedUserData,
			key + ".json"); 
		data = FileSystemUtil.ReadString(persistentProfilePath);
		
		// achievements
		string persistentProfilePathAchievement = Path.Combine(
			ContentPaths.appCachePathAllSharedUserData,
			keyAchievement + ".json"); 
		dataAchievement = FileSystemUtil.ReadString(persistentProfilePathAchievement);
		
		// statistics
		string persistentProfilePathStatistic = Path.Combine(
			ContentPaths.appCachePathAllSharedUserData,
			keyStatistic + ".json"); 
		dataStatistic = FileSystemUtil.ReadString(persistentProfilePathStatistic);
		
		// trackers
		string persistentProfilePathTracker = Path.Combine(
			ContentPaths.appCachePathAllSharedUserData,
			keyTracker + ".json"); 
		dataTracker = FileSystemUtil.ReadString(persistentProfilePathTracker);
		
		// rpg
		string persistentProfilePathRPG = Path.Combine(
			ContentPaths.appCachePathAllSharedUserData,
			keyRPG + ".json"); 
		dataRPG = FileSystemUtil.ReadString(persistentProfilePathRPG);
		
		//LogUtil.Log("saveProfile: persistentProfilePath:" + persistentProfilePath);
		//LogUtil.Log("saveProfile: persistentProfilePathAchievement:" + persistentProfilePathAchievement);
		//LogUtil.Log("saveProfile: persistentProfilePathStatistic:" + persistentProfilePathStatistic);
		//LogUtil.Log("saveProfile: persistentProfilePathTracker:" + persistentProfilePathTracker);
		//LogUtil.Log("saveProfile: persistentProfilePathRPG:" + persistentProfilePathRPG);
		
#endif
		if(!string.IsNullOrEmpty(data)) {
			profile = JsonMapper.ToObject<GameProfile>(data);
			profile.loginCount++;
			profile.SyncAccessPermissions();
		}
		
		if(!string.IsNullOrEmpty(dataAchievement)) {
			profileAchievement = JsonMapper.ToObject<GameProfileAchievement>(dataAchievement);
		}
		
		if(!string.IsNullOrEmpty(dataStatistic)) {
			profileStatistic = JsonMapper.ToObject<GameProfileStatistic>(dataStatistic);
		}
				
		if(!string.IsNullOrEmpty(dataTracker)) {
			profileTracker = JsonMapper.ToObject<GameProfileTracker>(dataTracker);
		}		
				
		if(!string.IsNullOrEmpty(dataRPG)) {
			profileRPG = JsonMapper.ToObject<GameProfileRPG>(dataRPG);
		}

		GameProfiles.Current = profile;
		GameProfileAchievements.Current = profileAchievement;
		GameProfileStatistics.Current = profileStatistic;
		GameProfileTrackers.Current = profileTracker;
		GameProfileRPGs.Current = profileRPG;
	}
		
	public void ChangeUser(string username) {
		ChangeUser(username, false);
	}
		
	public void ChangeUser(string username, bool keepExisting) {
		//LogUtil.Log("ChangeUser: username: " + username);
		//LogUtil.Log("ChangeUser: key: " + GetProfileKey(username));
		
		if(profile.username != username) {
						
			string originalProfileUser = profile.username.ToLower();
			
			if(originalProfileUser.ToLower() == "player" // == profile.GetDefaultPlayer().ToLower()
			   || keepExisting) {
				// Keep all progress from default player if they decide to log into gamecenter
				// Has cheating problems but can be resolved after bug.
				profile.ChangeUserNoReset(username);
			}
			else {
				profile.ChangeUser(username);
			}
			
			loadProfile();
			saveProfile();	
		}
		
		if(profileAchievement != null) {
			profileAchievement.username = profile.username;
		}
		
		if(profileStatistic != null) {
			profileStatistic.username = profile.username;
		}
				
		if(profileTracker != null) {
			profileTracker.username = profile.username;
		}
				
		if(profileRPG != null) {
			profileRPG.username = profile.username;
		}
	}
}