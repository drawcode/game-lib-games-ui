//#define GAMECENTER_ENABLED
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Engine.Events;
using Engine.Networking;

public class GameCommunityPlatformGlobal : MonoBehaviour
{
	public static GameCommunityPlatformGlobal Instance;
	
	[NonSerialized]
	public GameNetworks gameNetworks;
	
	[NonSerialized]
	public SocialNetworks socialNetworks;
	
	[NonSerialized]
	public GameCommunityPlatformState state;	
	
	[NonSerialized]
	public GameCommunityPlatformController platformController;
	
	[NonSerialized]
	public GameCommunityPlatformService platformService;
	
	[NonSerialized]
	public GameCommunityPlatformUIController platformAppViewerUIController;
	
	[NonSerialized]
	public GameCommunityPlatformSocialController platformSocialController;
	
	[NonSerialized]
	public GameCommunityPlatformTrackingController platformTrackingController;
	
	[NonSerialized]
	public AudioSystem audioSystem;
				
    public void Awake() {
		
        if (Instance != null && this != Instance) {
            //There is already a copy of this script running
            Destroy(gameObject);
            return;
        }
		
        Instance = this;
				
		DontDestroyOnLoad(gameObject);					
	}
	
	public static bool isReady {
		get { return GameCommunityPlatformGlobal.Instance != null ? true : false; }
	}
	
	public IEnumerator Init() {
						
		InitNetwork();
		
		InitSocial();
		
		yield return StartCoroutine(InitContent());
		
		InitState();
		
		InitCommunity();
		
		InitAudio();
	
		Tester();
		
		yield break;
		
	}
	
	public void Tester() {
		//AppTester.RunTests();
	}	
	
	public void Start() {
		
		StartCoroutine(Init());	
	}	
	
	
	public void InitAudio() {
		audioSystem = gameObject.AddComponent<AudioSystem>();		
		
		//GameAudioEffects.audio_loop_intro_1 = "ui-music-intro";
		//GameAudioEffects.audio_loop_main_1 = "ui-music-loop";
		//GameAudioEffects.audio_loop_game_1 = "music-grim-graveyard";
		//GameAudioEffects.audio_loop_game_2 = "music-cracked-castle";
		//GameAudioEffects.audio_loop_game_3 = "music-electric-jungle";
		
		//audioSystem.PrepareIntroFileFromResources(GameAudioEffects.audio_loop_intro_1, false, (float)GameProfiles.Current.GetAudioMusicVolume());
		//audioSystem.PrepareLoopFileFromResources(GameAudioEffects.audio_loop_main_1, true, (float)GameProfiles.Current.GetAudioMusicVolume());
		
		//audioSystem.PrepareGameLapLoopFileFromResources(0, GameAudioEffects.audio_loop_game_1, true, (float)GameProfiles.Current.GetAudioMusicVolume());
		//audioSystem.PrepareGameLapLoopFileFromResources(1, GameAudioEffects.audio_loop_game_2, true, (float)GameProfiles.Current.GetAudioMusicVolume());
		//audioSystem.PrepareGameLapLoopFileFromResources(2, GameAudioEffects.audio_loop_game_3, true, (float)GameProfiles.Current.GetAudioMusicVolume());

		audioSystem.SetAmbienceVolume(GameProfiles.Current.GetAudioMusicVolume());
		audioSystem.SetEffectsVolume(GameProfiles.Current.GetAudioEffectsVolume());
		
		//audioSystem.StartAmbience();
//#if DEV			
		if(Application.isEditor) {
			audioSystem.SetAmbienceVolume(0);
			audioSystem.SetEffectsVolume(0);
		}
//#endif
				
		LogUtil.Log("GameGlobal InitAudio init...");
	}
	
	public IEnumerator InitContent() {
		
        // DEBUG TEMP

        //Debug.Log("CHECKING TIPS");
        //foreach(AppContentAction action in AppContentActions.Instance.GetAll()) {
        //	if(action.content_tooltips.Count > 0) {
        //		//Debug.Log("WE GOT TIPS!:" + action.content_tooltips.Count);
        //	}
        //}
				
		//yield return StartCoroutine(Contents.Instance.ProcessLoadCo());		
		
		//if(AppViewerAppController.Instance != null) {
        //	AppViewerAppController.Instance.ChangeAppContentStateByCode("pack-vidari-featured-1-initial");
		//}
		yield break;
	}
	
	public void InitNetwork() {	
		gameObject.AddComponent<WebRequests>();	
		
		// Wire up Testflight
		
		TestFlight.isEnabled = AppConfigs.trackingTestFlightEnable;
		TestFlight.TakeOff(AppConfigs.trackingTestFlightTeamToken);
	}
		
	public void InitState() {				
		try {
			state = GameCommunityPlatformState.Instance;
			LogUtil.Log("GameGlobal InitState init...");
		}
		catch (Exception e) {
			LogUtil.Log("GameGlobal could not be initialized..." + e.Message + e.StackTrace);
		}
	}
	
	public void InitCommunity() {		
		platformService = GameCommunityPlatformService.Instance;//gameObject.AddComponent<GameCommunityPlatformService>();
		platformController = gameObject.AddComponent<GameCommunityPlatformController>();
		platformAppViewerUIController = gameObject.AddComponent<GameCommunityPlatformUIController>();
		platformSocialController = gameObject.AddComponent<GameCommunityPlatformSocialController>();
		platformTrackingController = gameObject.AddComponent<GameCommunityPlatformTrackingController>();
		
		Messenger.Broadcast(GameCommunityPlatformMessages.gameCommunityReady);		
	}
	
	public void InitSocial() {
		socialNetworks = gameObject.AddComponent<SocialNetworks>();
		SocialNetworks.LoadSocialLibs();
				
		if(GameNetworks.gameNetworkiOSAppleGameCenterEnabled) {
#if UNITY_IPHONE			
			gameNetworks = gameObject.AddComponent<GameNetworks>();			
			gameNetworks.loadNetwork(GameNetworkType.IOS_GAME_CENTER);
#endif
		}
	}
	
	void OnEnable() {
		//Messenger<string>.AddListener(AlertDialogMessages.DIALOG_QUIT, OnQuitDialog);
	}
	
	void OnDisable() {
		//Messenger<string>.RemoveListener(AlertDialogMessages.DIALOG_QUIT, OnQuitDialog);
	}
	
	void Quit() {
		Application.Quit(); 
	}
}