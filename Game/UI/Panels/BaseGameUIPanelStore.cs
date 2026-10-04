// The legacy "satchel" store: Clothing / Weapons / Powerups / Stats / Achievements / Quests lists.
//
// RESTORED 2026-10-03 (UI Toolkit batch B12, owner iter-30: "restore it under the toolkit rather
// than leave it commented"). History, game-lib-games before the 2019 split into games-ui:
//   39e44ba 2014-06-22 "Update UI to basely common game ui and app features." -- added, in the same
//           commit as BaseGameUIPanelProducts (the generic product list this game ships as its store).
//   8cb7d41 2014-06-29 "Update weapon systems, add new weapons and data. Updates to character
//           flows." -- wrapped the whole file in /* */ while GameProfileCharacters changed under it
//           (SetCurrentCharacterCode -> SetCurrentCharacterProfileCode; SetCurrentCharacterCostumeCode
//           dropped). The store logic moved into each game's own app-level GameUIPanelStore.
//   553453b 2016-11-06 "Update UI flows." -- one line edited INSIDE the comment.
//   6622fd8 / d08eec8 2019-03-30 -- moved to game-lib-games-ui still commented.
//
// WHY THE DEFINE. Other games on these libs (crazy-launch, racer-kart, runner-crazy, pop-crazy,
// odity-dream) each declare their own `enum GameUIPanelStoreListType` and a self-contained
// `GameUIPanelStore : GameUIPanelBase` in app code. Compiling this file unconditionally would give
// every one of them a duplicate-type error (CS0101) on the next lib pull, so it compiles only when a
// game asks for it with ENABLE_FEATURE_UI_STORE_LEGACY. It does not collide with this game's live
// store (GameUIPanelProducts): its Instance is typed to this base, it registers no panel code, it
// claims no toolkitViewKey, and its messenger handlers only answer to its own className.
//
// No prefab or scene object exists for it, so it has no view to convert: the CODE path only (NGUI
// fields under the NGUI #if with Engine.UI.UIRef in the #else, rows written through UIUtil, tweens
// through UITweenerUtil's agnostic path).
#if ENABLE_FEATURE_UI_STORE_LEGACY
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Engine.Events;
using Engine.Game.App.BaseApp;

public enum GameUIPanelStoreListType {
    Clothing,
    Weapons,
    Powerups,
    Stats,
    Achievements,
    Quests,
    All
}

public class BaseGameUIPanelStore : GameUIPanelBase {

    // Typed to the base, not the original `GameUIPanelStore`: no concrete class exists in the libs,
    // and the games that have one derive it from GameUIPanelBase, not from this.
    public static BaseGameUIPanelStore Instance;

    public GameObject listItemStatisticPrefab;
    public GameObject listItemAchievementPrefab;
    public GameObject listItemQuestPrefab;
    public GameObject listItemPowerupPrefab;
    public GameObject listItemClothingPrefab;
    public GameObject listItemWeaponPrefab;

    public GameObject containerMain;
    public GameObject containerList;

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    public UIImageButton buttonSatchelClothing;
    public UIImageButton buttonSatchelWeapons;
    public UIImageButton buttonSatchelPowerups;
    public UIImageButton buttonSatchelQuests;
    public UIImageButton buttonSatchelStats;
    public UIImageButton buttonSatchelTrophies;

    public UIImageButton buttonClose;
#else
    // Agnostic handles, bound at runtime by name.
    public Engine.UI.UIRef buttonSatchelClothing;
    public Engine.UI.UIRef buttonSatchelWeapons;
    public Engine.UI.UIRef buttonSatchelPowerups;
    public Engine.UI.UIRef buttonSatchelQuests;
    public Engine.UI.UIRef buttonSatchelStats;
    public Engine.UI.UIRef buttonSatchelTrophies;

    public Engine.UI.UIRef buttonClose;
#endif

    public GameUIPanelStoreListType panelListType = GameUIPanelStoreListType.Clothing;

    public string productCodeUse = "character-bot-1";
    public string productTypeUse = "default";
    public string productCharacterUse = "bot";

    public static bool isInst {
        get {
            if(Instance != null) {
                return true;
            }
            return false;
        }
    }

    public override void Awake() {
        base.Awake();

        if(Instance == null) {
            Instance = this;
        }
    }

    public override void Start() {
        Init();
    }

    public override void Init() {
        base.Init();
        loadData();

        AnimateIn();
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

        // OnDisable chains to base so UIPanelBase.FreeToolkitView runs (the Base*-layer chain rule).
        // OnEnable does NOT: UIPanelBase.OnEnable re-adds EVENT_BUTTON_CLICK, which this panel already
        // subscribes, so chaining it would fire every click twice. RemoveListener is idempotent.
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

        // UIUtil.IsButtonClicked has an overload per field type (UIImageButton / UIRef) and is
        // null-safe, where the original `buttonName == buttonSatchelClothing.name` threw on an
        // unassigned button.
        if(UIUtil.IsButtonClicked(buttonSatchelClothing, buttonName)) {
            changeList(GameUIPanelStoreListType.Clothing);
        }
        else if(buttonName.IndexOf("ButtonSatchelClothing$") > -1) {

            // Use costume

            productCodeUse = "";
            productTypeUse = "";
            productCharacterUse = "";

            string[] commandActionParams = buttonName.Replace("ButtonSatchelClothing$", "").Split('$');

            if(commandActionParams.Length > 0)
                productTypeUse = commandActionParams[0];
            if(commandActionParams.Length > 1)
                productCodeUse = commandActionParams[1];
            if(commandActionParams.Length > 2)
                productCharacterUse = commandActionParams[2];

            string weaponType = "ranged";
            if(productCharacterUse == "...") {
                weaponType = "melee";
            }

            if(!string.IsNullOrEmpty(productTypeUse)
                && !string.IsNullOrEmpty(productCodeUse)
                && !string.IsNullOrEmpty(productCharacterUse)) {

                GameProfileCharacters.Current.SetCurrentCharacterProfileCode(productCharacterUse);

                // TODO CHECK if can use or buy.. for now grant power and control
                // and access beyond all virtual currency bounds...
                //GameProfileCharacters.Current.SetCharacterCode(productCodeUse);

                if(productTypeUse == "costume") {

                    GameCharacterSkin skin = GameCharacterSkins.Instance.GetById(productCodeUse);

                    if(skin != null) {
                        GameCharacterSkinItemRPG rpg = skin.GetGameCharacterSkinByData(productCharacterUse, weaponType);
                        if(rpg != null) {
                            ////GameProfileCharacters.Current.SetCurrentCharacterCostumeCode(rpg.prefab);
                        }
                    }
                }
            }
        }
        else if(UIUtil.IsButtonClicked(buttonSatchelWeapons, buttonName)) {
            changeList(GameUIPanelStoreListType.Weapons);
        }
        else if(UIUtil.IsButtonClicked(buttonSatchelPowerups, buttonName)) {
            changeList(GameUIPanelStoreListType.Powerups);
        }
        else if(UIUtil.IsButtonClicked(buttonSatchelStats, buttonName)) {
            //changeList(GameUIPanelStoreListType.Stats);
            if(GameUIPanelStatistics.Instance != null) {
                GameUIPanelStatistics.Instance.AnimateIn();
            }
        }
        else if(UIUtil.IsButtonClicked(buttonSatchelTrophies, buttonName)) {
            changeList(GameUIPanelStoreListType.Achievements);
        }
        else if(UIUtil.IsButtonClicked(buttonSatchelQuests, buttonName)) {
            changeList(GameUIPanelStoreListType.Quests);
        }
    }

    public static void ChangeList(GameUIPanelStoreListType listType) {
        if(isInst) {
            Instance.changeList(listType);
        }
    }

    public virtual void changeList(GameUIPanelStoreListType listType) {
        panelListType = listType;
        loadData();
        AnimateInList();
    }

    public virtual void loadData() {
        StartCoroutine(loadDataCo());
    }

    IEnumerator loadDataCo() {

        LogUtil.Log("LoadDataCo");

        if(listGridRoot != null) {

            listGridRoot.DestroyChildren();

            yield return new WaitForEndOfFrame();

            if(panelListType == GameUIPanelStoreListType.Clothing) {
                loadDataClothing();
            }
            else if(panelListType == GameUIPanelStoreListType.Weapons) {
                loadDataWeapons();
            }
            else if(panelListType == GameUIPanelStoreListType.Powerups) {
                loadDataPowerups();
            }
            else if(panelListType == GameUIPanelStoreListType.Stats) {
                loadDataStatistics();
            }
            else if(panelListType == GameUIPanelStoreListType.Achievements) {
                loadDataAchievements();
            }
            else if(panelListType == GameUIPanelStoreListType.Quests) {
                loadDataQuests();
            }

            yield return new WaitForEndOfFrame();
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
            UIGrid grid = listGridRoot.GetComponent<UIGrid>();
            if(grid != null) {
                grid.Reposition();
            }
#endif
            yield return new WaitForEndOfFrame();
        }
    }

    // One row instance under listGridRoot: NGUITools.AddChild on NGUI, else the same
    // instantiate + reparent BaseGameUIPanelProducts.loadDataProductsItems uses.
    protected virtual GameObject AddListItem(GameObject prefab) {

        if(listGridRoot == null || prefab == null) {
            return null;
        }

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
        return NGUITools.AddChild(listGridRoot, prefab);
#else
        GameObject item = GameObjectHelper.CreateGameObject(
            prefab, Vector3.zero, Quaternion.identity, false);
        item.transform.parent = listGridRoot.transform;
        item.ResetLocalPosition();
        return item;
#endif
    }

    // The row's Container/Icon alpha, on whichever renderer it carries.
    protected virtual void SetItemIconAlpha(GameObject item, float alpha) {

        Transform iconTransform = item.transform.Find("Container/Icon");

        if(iconTransform == null) {
            return;
        }

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
        UISprite iconSprite = iconTransform.GetComponent<UISprite>();

        if(iconSprite != null) {
            iconSprite.alpha = alpha;
        }
#else
        SpriteUtil.SetColorAlpha(iconTransform.gameObject, alpha);
#endif
    }

    public virtual void loadDataQuests() {

    }

    public virtual void loadDataClothing() {
        loadDataProduct("character-skin");
    }

    public virtual void loadDataProduct(string type) {
        LogUtil.Log("Load loadDataProduct:" + type);

        List<GameProduct> products = GameProducts.Instance.GetListByType(type);

        LogUtil.Log("Load skins: products.Count: " + products.Count);

        int i = 0;

        foreach(GameProduct product in products) {

            GameObject item = AddListItem(listItemClothingPrefab);

            if(item == null) {
                break;
            }

            item.name = "WeaponItem" + i;

            GameProductInfo info = product.GetDefaultProductInfoByLocale();

            UIUtil.UpdateLabelObject(item.transform, "LabelName", info.display_name);
            UIUtil.UpdateLabelObject(item.transform, "LabelDescription", info.description);

            SetItemIconAlpha(item, 1f);
            // TODO change out image...

            // Update button action

            Transform buttonObject = item.transform.Find("Container/ButtonAction");

            if(buttonObject != null
                && UIUtil.IsButton(Engine.UI.UIRef.Of(buttonObject.gameObject))) {

                // TODO change to get from character skin
                string productType = "costume";
                string productCode = product.code;
                string productCharacter = "norah";

                productCode = productCode.Replace(productType + "-", "");

                if(productCode.IndexOf("jaime") > -1) {
                    productCharacter = "jaime";
                }

                buttonObject.name = "ButtonSatchelClothing$" + productType + "$" + productCode + "$" + productCharacter;
            }

            i++;
        }
    }

    public virtual void loadDataWeapons() {

        LogUtil.Log("Load Weapons:");

        List<GameWeapon> weapons = GameWeapons.Instance.GetAll();

        LogUtil.Log("Load weapons: weapons.Count: " + weapons.Count);

        int i = 0;

        foreach(GameWeapon weapon in weapons) {

            GameObject item = AddListItem(listItemWeaponPrefab);

            if(item == null) {
                break;
            }

            item.name = "WeaponItem" + i;

            UIUtil.UpdateLabelObject(item.transform, "LabelName", weapon.display_name);
            UIUtil.UpdateLabelObject(item.transform, "LabelDescription", weapon.description);

            SetItemIconAlpha(item, 1f);
            // TODO change out image...

            i++;
        }
    }

    public virtual void loadDataPowerups() {
        loadDataProduct("powerup");
    }

    public virtual void loadDataStatistics() {

        LogUtil.Log("Load Statistics:");

        List<GameStatistic> statistics = GameStatistics.Instance.GetAll();

        LogUtil.Log("Load statistics: statistics.Count: " + statistics.Count);

        int i = 0;

        foreach(GameStatistic statistic in statistics) {

            GameObject item = AddListItem(listItemStatisticPrefab);

            if(item == null) {
                break;
            }

            item.name = "StatisticItem" + i;

            UIUtil.UpdateLabelObject(item.transform, "LabelName", statistic.display_name);
            UIUtil.UpdateLabelObject(item.transform, "LabelDescription", statistic.description);

            double statValue = GameProfileStatistics.Current.GetStatisticValue(statistic.code);
            string displayValue = GameStatistics.Instance.GetStatisticDisplayValue(statistic, statValue);

            UIUtil.UpdateLabelObject(item.transform, "LabelPoints", displayValue);

            i++;
        }
    }

    public virtual void loadDataAchievements() {

        LogUtil.Log("Load Achievements:");

        List<GameAchievement> achievements = GameAchievements.Instance.GetAll();

        LogUtil.Log("Load Achievements: achievements.Count: " + achievements.Count);

        int i = 0;

        int totalPoints = 0;

        foreach(GameAchievement achievement in achievements) {

            GameObject item = AddListItem(listItemAchievementPrefab);

            if(item == null) {
                break;
            }

            item.name = "AchievementItem" + i;

            UIUtil.UpdateLabelObject(item.transform, "LabelName", achievement.display_name);
            UIUtil.UpdateLabelObject(item.transform, "LabelDescription", achievement.description);

            bool completed = GameProfiles.Current.CheckIfAttributeExists(achievement.code);

            if(completed) {
                completed = GameProfileAchievements.Current.GetAchievementValue(achievement.code);
            }

            if(!completed) {
                completed = GameProfileAchievements.Current.GetAchievementValue(achievement.code + "_" + achievement.pack_code);
            }

            string points = "";

            if(completed) {
                // data.points is a double now (an int when this was written).
                int currentPoints = (int)achievement.data.points;
                totalPoints += currentPoints;
                points = "+" + currentPoints.ToString();

                SetItemIconAlpha(item, 1f);
            }
            else {
                SetItemIconAlpha(item, .33f);
            }

            UIUtil.UpdateLabelObject(item.transform, "LabelPoints", points);

            // Get trophy icon

            i++;
        }

        //if(labelPoints != null) {
        //	labelPoints.text = totalPoints.ToString("N0");
        //}
    }

    public virtual void ShowMain() {
        if(containerMain != null) {
            UITweenerUtil.MoveTo(containerMain,
                UITweener.Method.EaseInOut, UITweener.Style.Once, .3f, 0f, Vector3.zero.WithY(0));

            UITweenerUtil.FadeTo(containerMain,
                UITweener.Method.Linear, UITweener.Style.Once, .3f, 0f, 1f);

            panelMode = UIAppPanelMode.ModeMain;
        }
    }

    public virtual void HideMain() {
        if(containerMain != null) {
            UITweenerUtil.MoveTo(containerMain,
                UITweener.Method.EaseInOut, UITweener.Style.Once, .2f, 0f, Vector3.zero.WithY(bottomClosedY));

            UITweenerUtil.FadeTo(containerMain,
                UITweener.Method.Linear, UITweener.Style.Once, .2f, 0f, 0f);
        }
    }

    public virtual void ShowList() {
        if(containerList != null) {
            UITweenerUtil.MoveTo(containerList,
                UITweener.Method.EaseInOut, UITweener.Style.Once, .3f, 0f, Vector3.zero.WithY(0));

            UITweenerUtil.FadeTo(containerList,
                UITweener.Method.Linear, UITweener.Style.Once, .3f, 0f, 1f);

            panelMode = UIAppPanelMode.ModeList;
        }
    }

    public virtual void HideList() {
        if(containerList != null) {
            UITweenerUtil.MoveTo(containerList,
                UITweener.Method.EaseInOut, UITweener.Style.Once, .2f, 0f, Vector3.zero.WithY(bottomClosedY));

            UITweenerUtil.FadeTo(containerList,
                UITweener.Method.Linear, UITweener.Style.Once, .2f, 0f, 0f);
        }
    }

    public override void AnimateIn() {

        base.AnimateIn();

        AnimateInMain();
    }

    public virtual void AnimateInMain() {

        HideList();
        ShowMain();
    }

    public virtual void AnimateInList() {

        HideMain();
        ShowList();
    }

    public override void AnimateOut() {

        base.AnimateOut();

        HideMain();
        HideList();
    }
}
#endif
