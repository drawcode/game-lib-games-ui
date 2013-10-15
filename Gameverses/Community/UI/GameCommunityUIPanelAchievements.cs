using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Engine.Data.Json;
using Engine.Events;
using Engine.Networking;

public class GameCommunityUIPanelAchievements : MonoBehaviour {
	
	public GameObject listGridRoot;
    public GameObject listItemPrefab;
	
	public UILabel labelPoints;
	
	public List<GameAchievement> currentAchievements;
			
	public static GameCommunityUIPanelAchievements Instance;
	
	void Awake () {		
		
        if (Instance != null && this != Instance) {
            //There is already a copy of this script running
            Destroy(gameObject);
            return;
        }
		
        Instance = this;
	}
	
	void OnEnable() {
		Messenger.AddListener(GameCommunityPlatformMessages.gameCommunityReady, OnGameCommunityReady);
	}
	
	void OnDisable() {
		Messenger.RemoveListener(GameCommunityPlatformMessages.gameCommunityReady, OnGameCommunityReady);
	}
	
	void OnGameCommunityReady() {
		Init();	
	}	
	
	public void Start() {
		
	}
	
	public void Init() {
		currentAchievements = new List<GameAchievement>();
		LoadData();
	}
		
	public List<GameAchievement> GetAchievements() {				
		currentAchievements = GameAchievements.Instance.GetAll();				
		return currentAchievements;
	}
	
	
	public void LoadData() {
				
		StartCoroutine(LoadDataCo());
	}
	
	IEnumerator LoadDataCo() {
		
		if (listGridRoot != null) {

			listGridRoot.DestroyChildren();
			
	        yield return new WaitForEndOfFrame();
	        listGridRoot.transform.parent.gameObject.GetComponent<UIDraggablePanel>().ResetPosition();
	        yield return new WaitForEndOfFrame();
		
			List<GameAchievement> achievements = GetAchievements();
			
			int i = 0;
			
			int totalPoints = 0;
			
	        foreach(GameAchievement achievement in achievements) {
				
				GameObject item = NGUITools.AddChild(listGridRoot, listItemPrefab);
	            item.name = "AchievementItem" + i;
								
				// TODO ACHIEVEMENTS
				//achievement.description 
				//	= GameAchievements.Instance.FormatAchievementTags(
				//		GamePacks.Current.code,
				//		AppContentStates.Current.code, 
				//		achievement.description);
				
	            item.transform.FindChild("LabelName").GetComponent<UILabel>().text 
					= achievement.display_name;
	            item.transform.FindChild("LabelDescription").GetComponent<UILabel>().text 
					= achievement.description;
				
				Transform icon = item.transform.FindChild("Icon");
				UISprite iconSprite = null;
				
				if(icon != null) {
					GameObject iconObject = icon.gameObject;	
					iconSprite = iconObject.GetComponent<UISprite>();	
				}
				
				string achievementCode = achievement.code;
				
				bool completed 
					= GameProfileAchievements.Current.CheckIfAttributeExists(achievementCode);
				
				if(completed) {
					completed = GameProfileAchievements.Current.GetAchievementValue(achievementCode);
				}
				
				string points = "";
				
				if(completed) {
					int currentPoints = achievement.points;
					totalPoints += currentPoints;
					points = currentPoints.ToString();
					
					if(iconSprite != null) {
						iconSprite.alpha = 1f;
					}
				}	
				else {
					if(iconSprite != null) {
						iconSprite.alpha = .33f;
					}
				}
				
				if (achievement.points > 0 && completed) {
					points = "+" + points;
				}
					
				item.transform.FindChild("LabelValue").GetComponent<UILabel>().text = points;			
											
				i++;
	        }
			
			if(labelPoints != null) {
				string formatted = totalPoints.ToString("N0");
				if(totalPoints > 0) {
					formatted = "+" + formatted;
				}
				labelPoints.text = formatted;
			}
			
	        yield return new WaitForEndOfFrame();
	        listGridRoot.GetComponent<UIGrid>().Reposition();
	        yield return new WaitForEndOfFrame();
	        listGridRoot.transform.parent.gameObject.GetComponent<UIDraggablePanel>().ResetPosition();
	        yield return new WaitForEndOfFrame();
			
        }
	}
	
}
