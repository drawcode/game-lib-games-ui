using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Engine.Game.App;
using Engine.Game.Data;



#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
#else
using UnityEngine.UI;
#endif

using Engine.Events;

public class BaseGameUIPanelResults : GameUIPanelBase {

    public static GameUIPanelResults Instance;

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    public UILabel labelContentStateDisplayName;
#else
    public Engine.UI.UIRef labelContentStateDisplayName; // 2.11: agnostic handle, bound by name
#endif

    public GameObject listItemPrefab;

    public GameObject containerModes;

    public override void Awake() {
        base.Awake();
    }

    public override void OnEnable() {

        Messenger<string>.AddListener(
            ButtonEvents.EVENT_BUTTON_CLICK,
            OnButtonClickEventHandler);

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

        Messenger<string>.RemoveListener(
            ButtonEvents.EVENT_BUTTON_CLICK,
            OnButtonClickEventHandler);

        Messenger<string>.RemoveListener(
            UIControllerMessages.uiPanelAnimateIn,
            OnUIControllerPanelAnimateIn);

        Messenger<string>.RemoveListener(
            UIControllerMessages.uiPanelAnimateOut,
            OnUIControllerPanelAnimateOut);

        Messenger<string, string>.RemoveListener(
            UIControllerMessages.uiPanelAnimateType,
            OnUIControllerPanelAnimateType);

        // Chain to base so UIPanelBase.OnDisable -> FreeToolkitView runs when this panel is pooled
        // away, else the toolkit view leaks once panel-results gets a toolkitViewKey. Phase-3
        // migration prerequisite, and the last member of the Results family still breaking the
        // chain — the other five were fixed with the list bases.
        //
        // OnDisable ONLY: UIPanelBase.OnEnable re-adds EVENT_BUTTON_CLICK -> OnButtonClickEventHandler,
        // which this panel already subscribes itself, so chaining OnEnable would fire every results
        // button click TWICE. RemoveListener is idempotent, so the OnDisable-only chain is safe.
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

    public static bool isInst {
        get {
            if(GameUIPanelResults.Instance != null) {
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

        loadData();
    }

    public override void OnButtonClickEventHandler(string buttonName) {
        //LogUtil.Log("OnButtonClickEventHandler: " + buttonName);
    }

    public virtual void ShowContentState() {

        if(containerModes != null) {

            foreach(GameObjectInactive inactive in
                containerModes.GetComponentsInChildren<GameObjectInactive>(true)) {

                if(inactive.type.IsEqualLowercase(BaseDataObjectKeys.app_content_state)) {

                    inactive.gameObject.Hide();
                }
            }

            foreach(GameObjectInactive inactive in
                containerModes.GetComponentsInChildren<GameObjectInactive>(true)) {

                if(AppContentStates.Current != null && AppContentStates.Current.code != null) {

                    if(inactive.code.ToLower() == AppContentStates.Current.code.ToLower()
                        && inactive.type.IsEqualLowercase(BaseDataObjectKeys.app_content_state)) {

                        inactive.gameObject.Show();
                    }
                }
            }
        }
    }

    // panel-results ships LabelWorldCode / LabelLevelCode as authored placeholder text
    // ("PLANET 426", "LEVEL 10-10") in BOTH backends, and nothing ever wrote to them, so
    // every round on every level ended under the same wrong caption. They sit in the shared
    // Buttons/Row/Level/LevelMeta group, not in a mode variant, and no serialised field
    // points at them, so they are written BY NAME the way the worlds panel writes its meta.

    public static string labelNameLevelCode = "LabelLevelCode";
    public static string labelNameWorldCode = "LabelWorldCode";

    public virtual void UpdateLevelMeta() {

        // UpdateDisplay is driven from the end-of-level coroutine in BaseGameController, and
        // StartCoroutine THROWS on an inactive GameObject -- which would take the rest of that
        // coroutine (stats, XP, the results show) with it. An inactive panel has no toolkit
        // view to wait for either, so write straight through.

        if(!gameObject.activeInHierarchy) {
            WriteLevelMeta();
            return;
        }

        StartCoroutine(UpdateLevelMetaCo());
    }

    public virtual void WriteLevelMeta() {

        string levelCodeDisplay = GetLevelCodeDisplay();
        string worldCodeDisplay = GetWorldCodeDisplay();

        if(isToolkitPanel) {
            UIUtil.UpdateLabelObject(viewRoot, labelNameLevelCode, levelCodeDisplay);
            UIUtil.UpdateLabelObject(viewRoot, labelNameWorldCode, worldCodeDisplay);
            return;
        }

        UIUtil.UpdateLabelObject(gameObject, labelNameLevelCode, levelCodeDisplay);
        UIUtil.UpdateLabelObject(gameObject, labelNameWorldCode, worldCodeDisplay);
    }

    public virtual IEnumerator UpdateLevelMetaCo() {

        // The toolkit view binds ASYNC, and UpdateDisplay is driven from the end-of-level
        // coroutine, which can beat the load. Same wait as BaseGameUIPanelWorlds.loadDataCo.

        if(!string.IsNullOrEmpty(toolkitViewKey)) {

            for(int waitFrames = 0; waitFrames < 60 && !isToolkitPanel; waitFrames++) {
                yield return null;
            }
        }

        WriteLevelMeta();
    }

    public virtual string GetLevelCodeDisplay() {

        if(GameLevels.Current == null
            || string.IsNullOrEmpty(GameLevels.Current.code)) {
            return "";
        }

        return ("level " + GameLevels.Current.code).ToUpper();
    }

    public virtual string GetWorldCodeDisplay() {

        // GameWorlds.Current is the world the PROFILE last selected, and it does not track
        // the level that was actually played -- measured live on level 1-1 (world-planet-z)
        // with GameWorlds.Current still reading "Planet 337: New Mars". Resolve the world
        // the level itself names; fall back to Current only when the level has no world.

        GameWorld world = null;

        if(GameLevels.Current != null
            && !string.IsNullOrEmpty(GameLevels.Current.world_code)) {
            world = GameWorlds.Instance.GetById(GameLevels.Current.world_code);
        }

        if(world == null) {
            world = GameWorlds.Current;
        }

        if(world == null) {
            return "";
        }

        string displayName = world.display_name;

        if(string.IsNullOrEmpty(displayName)) {
            return "";
        }

        // "Planet Z: Zedlands" -> "PLANET Z". This is the narrow subtitle next to the level
        // code; the full name belongs to the worlds panel.

        int separator = displayName.IndexOf(':');

        if(separator > 0) {
            displayName = displayName.Substring(0, separator);
        }

        return displayName.Trim().ToUpper();
    }

    public virtual void UpdateDisplay(
        GamePlayerRuntimeData runtimeData, float timeTotal) {

        ShowContentState();

        UpdateLevelMeta();

        UIUtil.SetLabelValue(
            labelContentStateDisplayName, AppContentStates.Current.display_name);

        if(AppContentStates.Instance.isAppContentStateGameChallenge) {

#if ENABLE_FEATURE_MODE_CHALLENGE
            foreach(GameUIPanelResultsChallenge result in containerModes.GetComponentsInChildren<GameUIPanelResultsChallenge>(true)) {
                result.UpdateDisplay(runtimeData, timeTotal);
            }
#endif
        }

#if ENABLE_FEATURE_MODE_TRAINING
        else if(AppContentStates.Instance.isAppContentStateGameTrainingChoiceQuiz) {
            //foreach(UIPanelResultsChoiceQuiz result in containerModes.GetComponentsInChildren<UIPanelResultsChoiceQuiz>(true)) {
            //    result.UpdateDisplay(runtimeData, timeTotal);
            //}
        }
        else if(AppContentStates.Instance.isAppContentStateGameTrainingCollectionSafety) {
            //foreach(UIPanelResultsCollectionSafety result in containerModes.GetComponentsInChildren<UIPanelResultsCollectionSafety>(true)) {
            //    result.UpdateDisplay(runtimeData, timeTotal);
            //}
        }
        else if(AppContentStates.Instance.isAppContentStateGameTrainingCollectionSmarts) {
            //foreach(UIPanelResultsCollectionSmarts result in containerModes.GetComponentsInChildren<UIPanelResultsCollectionSmarts>(true)) {
            //    result.UpdateDisplay(runtimeData, timeTotal);
            //}
        }

#endif

        else { // if(AppContentStates.Instance.isAppContentStateGameArcade) {
            foreach(GameUIPanelResultsArcade result in
                containerModes.GetComponentsInChildren<GameUIPanelResultsArcade>(true)) {

                result.UpdateDisplay(runtimeData, timeTotal);
            }
        }
    }

    public static void LoadData() {

        if(GameUIPanelResults.Instance != null) {

            GameUIPanelResults.Instance.loadData();
        }
    }

    public virtual void loadData() {
        StartCoroutine(loadDataCo());
    }

    IEnumerator loadDataCo() {

        yield return new WaitForSeconds(1f);

        ShowContentState();
    }

    public virtual void HandleItems() {

        // Handle by world

        string codeWorld = GameWorlds.Current.code;

        if(codeWorld.IsNullOrEmpty()) {
            return;
        }

        foreach(GameObjectInactive container in
            gameObject.GetList<GameObjectInactive>()) {

            if(container.type.IsEqualLowercase(BaseDataObjectKeys.display_items)) {

                foreach(GameObjectInactive item in
                    container.gameObject.GetList<GameObjectInactive>()) {

                    if(item.type.IsEqualLowercase(BaseDataObjectKeys.display_item)
                        && !item.type.IsEqualLowercase(BaseDataObjectKeys.display_items)) {

                        item.gameObject.HideChildren();
                    }
                }

                foreach(GameObjectData dataItem in
                    container.gameObject.GetList<GameObjectData>()) {

                    Dictionary<string, object> data = dataItem.ToDictionary();

                    string val = data.Get<string>(BaseDataObjectKeys.world);

                    if(val.IsEqualLowercase(codeWorld)) {

                        dataItem.gameObject.Show();
                    }
                }
            }
        }
    }

    public override void HandleShow() {
        base.HandleShow();

        backgroundDisplayState = UIPanelBackgroundDisplayState.PanelBacker;
        adDisplayState = UIPanelAdDisplayState.Video;
        characterDisplayState = UIPanelCharacterDisplayState.Character;
        buttonDisplayState = UIPanelButtonsDisplayState.GameNetworks;
    }

    public override void AnimateIn() {
        base.AnimateIn();

        HandleItems();

        loadData();

#if USE_GAME_LIB_GAMEVERSES
        GameCommunity.ShowSharesCenter();
#endif

        Messenger.Broadcast(GameMessages.gameResultsStart);
    }

    public override void AnimateOut() {

        base.AnimateOut();

        Messenger.Broadcast(GameMessages.gameResultsEnd);

#if USE_GAME_LIB_GAMEVERSES
        GameCommunity.HideSharesCenter();
#endif
    }
}