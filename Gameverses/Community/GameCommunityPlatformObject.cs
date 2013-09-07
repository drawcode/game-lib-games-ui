using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Engine.Data.Json;
using Engine.Events;
using Engine.Networking;

/*
 
 GAME COMMUNITY PLATFORM
 - requires in unity the prime31 social plugins
 - requires app id that is categorized as a 'Game' on facebook to enable facebook scores/achievement apis
 
 HOWTO
 - configure settings here in GameCommunityPlatformConfig and attach to a root global object called 'GameCommunityPlatform' or a single object that
    will not be destroyed DontDestroyOnLoad(gameObject);

*/

public class GameCommunityPlatformObject : MonoBehaviour {
	
	public string note = "See GameCommunityConfig for settings...";
	
	[NonSerialized]
	public GameCommunityPlatformGlobal platformGlobal;
	
	[NonSerialized]
	public static GameCommunityPlatformObject Instance;
		    
	public void Awake() {
		
        if (Instance != null && this != Instance) {
            //There is already a copy of this script running
            //Destroy(gameObject);
            return;
        }
		
        Instance = this;
		
		DontDestroyOnLoad(gameObject);	
		
		// Add all objects needed to hook it up!		
		platformGlobal = gameObject.AddComponent<GameCommunityPlatformGlobal>();
	}
	
	void Start() {	
		
	}	
	
	void OnEnable() {
		//Messenger<string>.AddListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);
	}
	
	void OnDisable() {
		//Messenger<string>.RemoveListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);
	}
	// Quits the player when the user hits escape

	
	void Update () {
#if !UNITY_IPHONE    
		// quit on android and pc
		if (Input.GetKey(KeyCode.Escape)) {
        	Application.Quit();
    	}
#endif
	}
	
}

