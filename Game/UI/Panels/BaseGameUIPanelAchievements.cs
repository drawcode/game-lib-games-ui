using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Engine.Events;
using UnityEngine.UI;

public class BaseGameUIPanelAchievements : GameUIPanelBase {

    public GameObject listItemAchievementPrefab;

    public static GameUIPanelAchievements Instance;

    public override void Awake() {
        base.Awake();

    }

    public static bool isInst {
        get {
            if(Instance != null) {
                return true;
            }
            return false;
        }
    }

    public override void Start() {
        Init();
    }

    public override void Init() {
        base.Init();
    }

    public override void OnEnable() {

        Messenger<string>.AddListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);

        Messenger<string>.AddListener(
            UIControllerMessages.uiPanelAnimateIn,
            OnUIControllerPanelAnimateIn);

        Messenger<string>.AddListener(
            UIControllerMessages.uiPanelAnimateOut,
            OnUIControllerPanelAnimateOut);

        Messenger<string, string>.AddListener(
            UIControllerMessages.uiPanelAnimateType,
            OnUIControllerPanelAnimateType);
    }

    public override void OnDisable() {

        Messenger<string>.RemoveListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);

        Messenger<string>.RemoveListener(
            UIControllerMessages.uiPanelAnimateIn,
            OnUIControllerPanelAnimateIn);

        Messenger<string>.RemoveListener(
            UIControllerMessages.uiPanelAnimateOut,
            OnUIControllerPanelAnimateOut);

        Messenger<string, string>.RemoveListener(
            UIControllerMessages.uiPanelAnimateType,
            OnUIControllerPanelAnimateType);
        // Chain to base so UIPanelBase.OnDisable -> FreeToolkitView runs when this panel is
        // pooled away, else the toolkit view leaks once the panel has one. Phase-3 migration
        // prerequisite (same fix the settings/header/footer bases got in 3A/3B).
        base.OnDisable();
    }

    public override void OnUIControllerPanelAnimateIn(string classNameTo) {
        if(className == classNameTo) {
            AnimateIn();
        }
    }

    public override void OnUIControllerPanelAnimateOut(string classNameTo) {
        if(className == classNameTo) {
            AnimateOut();
        }
    }

    public override void OnUIControllerPanelAnimateType(string classNameTo, string code) {
        if(className == classNameTo) {
            //
        }
    }

    public override void OnButtonClickEventHandler(string buttonName) {
        //LogUtil.Log("OnButtonClickEventHandler: " + buttonName);

    }

    public static void LoadData() {
        if(GameUIPanelAchievements.Instance != null) {
            GameUIPanelAchievements.Instance.loadData();
        }
    }

    public virtual void loadData() {
        StartCoroutine(loadDataCo());
    }

    IEnumerator loadDataCo() {

        // Toolkit (list wave): wait for the async view, then rebuild rows from the template.
        // The legacy grid path below stays for the kill switch.
        if(!string.IsNullOrEmpty(toolkitViewKey)) {

            for(int waitFrames = 0; waitFrames < 60 && !isToolkitPanel; waitFrames++) {
                yield return null;
            }

            if(isToolkitPanel) {
                loadDataAchievementsToolkit();
                yield break;
            }
        }


        LogUtil.Log("LoadDataCo");

        if(listGridRoot != null) {
            listGridRoot.DestroyChildren();

            yield return new WaitForEndOfFrame();

            loadDataAchievements();

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
	        yield return new WaitForEndOfFrame();
	        listGridRoot.GetComponent<UIGrid>().Reposition();
#endif

            yield return new WaitForEndOfFrame();
        }
    }

    // Rows through the platform: rebuild AchievementItem{i} from the view template with the
    // same completion/points logic as the legacy grid path below (icon alpha 1.0 completed /
    // 0.33 incomplete over the card's own trophy tint).
    public virtual void loadDataAchievementsToolkit() {

        UIUtil.ClearListItems(viewRoot, "AchievementList");

        int i = 0;

        foreach(GameAchievement achievement in GameAchievements.Instance.GetAll()) {

            Engine.UI.UIRef item = UIUtil.AddListItem(
                viewRoot, "AchievementList", "AchievementItemTemplate", "AchievementItem" + i);

            UIUtil.UpdateLabelObject(item, "LabelName", achievement.display_name);
            UIUtil.UpdateLabelObject(item, "LabelDescription", achievement.description);

            bool completed = false;

            bool hasValue = GameProfileAchievements.Current.CheckIfAttributeExists(achievement.code);

            if(hasValue) {
                completed = GameProfileAchievements.Current.GetAchievementValue(achievement.code);
            }

            if(!hasValue) {
                completed = GameProfileAchievements.Current.GetAchievementValue(achievement.code + "_" + achievement.pack_code);
            }

            string points = "";

            if(completed) {
                double currentPoints = achievement.data.points;

                if(GameConfigs.useCoinRewardsForAchievements) {
                    currentPoints *= (int)GameConfigs.coinRewardAchievementPoint;
                }

                points = "+" + currentPoints.ToString();
            }

            UIUtil.UpdateLabelObject(item, "LabelPoints", points);

            UIUtil.SetSpriteColor(
                UIUtil.ResolveDeep(item, "Icon"),
                new Color(1f, 0.9751f, 0.6392f, completed ? 1f : 0.33f));

            i++;
        }
    }

    public virtual void loadDataAchievements() {

        LogUtil.Log("Load Achievements:");

        List<GameAchievement> achievements = GameAchievements.Instance.GetAll();

        LogUtil.Log("Load Achievements: achievements.Count: " + achievements.Count);

        int i = 0;

        double totalPoints = 0;

        foreach(GameAchievement achievement in achievements) {

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
                GameObject item = NGUITools.AddChild(listGridRoot, listItemAchievementPrefab);
#else
            GameObject item = GameObjectHelper.CreateGameObject(
                listItemAchievementPrefab, Vector3.zero, Quaternion.identity, false);
            // NGUITools.AddChild(listGridRoot, listItemPrefab);
            item.transform.parent = listGridRoot.transform;
            item.ResetLocalPosition();
#endif
            item.name = "AchievementItem" + i;

            UIUtil.UpdateLabelObject(item.transform, "Container/LabelName", achievement.display_name);
            UIUtil.UpdateLabelObject(item.transform, "Container/LabelDescription", achievement.description);

            Transform iconItem = item.transform.Find("Container/Icon");

            GameObject iconSprite = null;

            if(iconItem != null) {

                GameObject iconObject = iconItem.gameObject;

                iconSprite = iconObject;
            }

            bool completed = false;

            bool hasValue = GameProfileAchievements.Current.CheckIfAttributeExists(achievement.code);

            if(hasValue) {
                completed = GameProfileAchievements.Current.GetAchievementValue(achievement.code);
            }

            if(!hasValue) {
                completed = GameProfileAchievements.Current.GetAchievementValue(achievement.code + "_" + achievement.pack_code);
            }

            string points = "";

            if(completed) {
                double currentPoints = achievement.data.points;
                totalPoints += currentPoints;

                if(GameConfigs.useCoinRewardsForAchievements) {
                    currentPoints *= (int)GameConfigs.coinRewardAchievementPoint;
                }

                points = "+" + currentPoints.ToString();

                if(iconSprite != null) {
                    SpriteUtil.SetColorAlpha(iconSprite, 1f);
                }
                //item.transform.FindChild("Container/ContainerComplete").gameObject.Show(); 
            }
            else {
                if(iconSprite != null) {
                    SpriteUtil.SetColorAlpha(iconSprite, .33f);
                }
                //item.transform.FindChild("Container/ContainerComplete").gameObject.Hide(); 
            }

            UIUtil.UpdateLabelObject(item.transform, "Container/LabelPoints", points);

            // Get trophy icon

            i++;
        }

        //if(labelPoints != null) {
        //	labelPoints.text = totalPoints.ToString("N0");
        //}
    }

    public virtual void ClearList() {
        if(listGridRoot != null) {
            listGridRoot.DestroyChildren();
        }
    }

    public override void HandleShow() {
        base.HandleShow();

        buttonDisplayState = UIPanelButtonsDisplayState.GameNetworks;
        // No character here — see BaseGameUIPanelStatistics: it collides with the
        // GameNetworks button grid in the same column.
        characterDisplayState = UIPanelCharacterDisplayState.None;
        backgroundDisplayState = UIPanelBackgroundDisplayState.PanelBacker;
    }

    public override void AnimateIn() {

        base.AnimateIn();

        UIPanelCommunityBroadcast.HideBroadcastRecordPlayShare();

        loadData();
    }

    public override void AnimateOut() {

        base.AnimateOut();

        ClearList();
    }
}