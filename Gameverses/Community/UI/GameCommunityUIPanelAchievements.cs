using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Engine.Data.Json;
using Engine.Events;
using Engine.Networking;

public class GameCommunityUIPanelAchievements : UIAppPanelBaseList {

    public GameObject listItemPrefab;
    public UILabel labelPoints;
    public List<GameAchievement> currentAchievements;
    public static GameCommunityUIPanelAchievements Instance;

    public override void Awake() {
        base.Awake();

        if(Instance != null && this != Instance) {
            //There is already a copy of this script running
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public override void OnEnable() {
        base.OnEnable();
        Messenger.AddListener(GameCommunityMessages.gameCommunityReady, OnGameCommunityReady);
    }

    public override void OnDisable() {
        base.OnDisable();
        Messenger.RemoveListener(GameCommunityMessages.gameCommunityReady, OnGameCommunityReady);
    }

    void OnGameCommunityReady() {
        Init();
    }

    public override void Start() {

    }

    public override void Init() {
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

        if(listGridRoot != null) {

            listGridRoot.DestroyChildren();

            yield return new WaitForEndOfFrame();
            listGridRoot.transform.parent.gameObject.GetComponent<UIDraggablePanel>().ResetPosition();
            yield return new WaitForEndOfFrame();

            List<GameAchievement> achievements = GetAchievements();

            int i = 0;

            double totalPoints = 0;

            foreach(GameAchievement achievement in achievements) {

                GameObject item = NGUITools.AddChild(listGridRoot, listItemPrefab);
                item.name = "AchievementItem" + i;

                // TODO ACHIEVEMENTS
                //achievement.description 
                //  = GameAchievements.Instance.FormatAchievementTags(
                //      GamePacks.Current.code,
                //      AppContentStates.Current.code, 
                //      achievement.description);

                item.transform.Find("LabelName").GetComponent<UILabel>().text
                    = achievement.display_name;
                item.transform.Find("LabelDescription").GetComponent<UILabel>().text
                    = achievement.description;

                Transform icon = item.transform.Find("Icon");
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
                    double currentPoints = achievement.data.points;
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

                if(achievement.data.points > 0 && completed) {
                    points = "+" + points;
                }

                item.transform.Find("LabelValue").GetComponent<UILabel>().text = points;

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