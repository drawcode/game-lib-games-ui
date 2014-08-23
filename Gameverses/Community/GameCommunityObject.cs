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
 - configure settings here in GameCommunityConfig and attach to a root global object called 'GameCommunity' or a single object that
    will not be destroyed DontDestroyOnLoad(gameObject);

*/

public class GameCommunityObject : GameObjectBehavior {
	
	public string note = "See AppConfigs for settings...";	

	[NonSerialized]
	public static GameCommunityObject Instance;
		    
	public void Awake() {
		
        if (Instance != null && this != Instance) {
            //There is already a copy of this script running
            //Destroy(gameObject);
            return;
        }
		
        Instance = this;
		
		DontDestroyOnLoad(gameObject);	
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

