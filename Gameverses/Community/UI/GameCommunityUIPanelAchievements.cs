using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// using Engine.Data.Json;
using Engine.Events;
using Engine.Networking;

public class GameCommunityUIPanelAchievements : UIAppPanelBaseList {

    public GameObject listItemPrefab;

    public List<GameAchievement> currentAchievements;
    public static GameCommunityUIPanelAchievements Instance;

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    public UILabel labelPoints;
#else
    public GameObject labelPoints;
#endif

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

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
            yield return new WaitForEndOfFrame();
            listGridRoot.transform.parent.gameObject.GetComponent<UIDraggablePanel>().ResetPosition();
#endif
            yield return new WaitForEndOfFrame();

            List<GameAchievement> achievements = GetAchievements();

            int i = 0;

            double totalPoints = 0;

            foreach(GameAchievement achievement in achievements) {

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
                GameObject item = NGUITools.AddChild(listGridRoot, listItemPrefab);
#else
                GameObject item = GameObjectHelper.CreateGameObject(
                    listItemPrefab, Vector3.zero, Quaternion.identity, false);
                // NGUITools.AddChild(listGridRoot, listItemPrefab);

                item.transform.parent = listGridRoot.transform;
                item.ResetLocalPosition();
#endif

                item.name = "AchievementItem" + i;

                Transform labelItemName = item.transform.Find("LabelName");

                if(labelItemName != null) {
                    UIUtil.SetLabelValue(labelItemName.gameObject, achievement.display_name);
                }

                Transform labelItemDescription = item.transform.Find("LabelDescription");

                if(labelItemName != null) {
                    UIUtil.SetLabelValue(labelItemDescription.gameObject, achievement.display_name);
                }


                // TODO ACHIEVEMENTS
                //achievement.description 
                //  = GameAchievements.Instance.FormatAchievementTags(
                //      GamePacks.Current.code,
                //      AppContentStates.Current.code, 
                //      achievement.description);

                Transform iconItem = item.transform.Find("Icon");

                GameObject iconSprite = null;

                if(iconItem != null) {

                    GameObject iconObject = iconItem.gameObject;

                    iconSprite = iconObject;
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
                        SpriteUtil.SetColorAlpha(iconSprite, 1f);
                    }
                }
                else {
                    if(iconSprite != null) {
                        SpriteUtil.SetColorAlpha(iconSprite, .33f);
                    }
                }

                if(achievement.data.points > 0 && completed) {
                    points = "+" + points;
                }

                UIUtil.UpdateLabelObject(item.transform, "LabelValue", points);

                i++;
            }

            if(labelPoints != null) {

                string formatted = totalPoints.ToString("N0");

                if(totalPoints > 0) {
                    formatted = "+" + formatted;
                }

                UIUtil.SetLabelValue(labelPoints, formatted);
            }

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
            yield return new WaitForEndOfFrame();
            listGridRoot.GetComponent<UIGrid>().Reposition();
            yield return new WaitForEndOfFrame();
            listGridRoot.transform.parent.gameObject.GetComponent<UIDraggablePanel>().ResetPosition();
#endif
            yield return new WaitForEndOfFrame();

        }
    }
}