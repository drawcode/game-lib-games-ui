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

    // The MODE CAPTION above them is the same defect from the driver side (iter 18's
    // widget-drivers-left-behind class). UpdateDisplay writes the content state's display_name
    // into `labelContentStateDisplayName`, which is declared inside the `#if USE_UI_NGUI_2_7`
    // branch -- a UILabel, so BindElements can never bind it and SuppressLegacyView has already
    // hidden the widget it writes to. The toolkit element therefore sat on the authored
    // placeholder "ARCADE MODE" for EVERY mode, including Missions and Coop.
    //
    // Written by name here, alongside the untouched legacy write, exactly as the level meta is.
    public static string labelNameGameMode = "LabelGameMode";

    // The RUN'S NUMBERS have the same problem one layer down. GameUIPanelResultsArcade /
    // ...Challenge write them through BaseGameUIPanelResultsBase, whose totalScore/totalScores/
    // totalCoins/totalKills fields are declared inside the `#if USE_UI_NGUI_2_7` branch — they are
    // UILabel, so BindElements can never bind them and a toolkit view never sees a value. Every
    // arcade round therefore ended on the authored placeholder "0" while the legacy panel showed
    // the real score.
    //
    // Those UILabels' legacy GameObject names ARE the toolkit element names (verified live:
    // totalScore -> LabelTotalPointsValue, totalScores -> LabelScoresValue, totalCoins ->
    // LabelCoinsCollectedValue, totalKills -> LabelKillsValue), which is the converter's naming
    // contract, so the values bridge BY NAME. Same shape as the level meta above, including the
    // wait for the async view load.

    public static string labelNameScores = "LabelScoresValue";
    public static string labelNameKills = "LabelKillsValue";
    public static string labelNameCoins = "LabelCoinsCollectedValue";
    public static string labelNameTotalPoints = "LabelTotalPointsValue";
    public static string labelNameTimeRunning = "LabelTimeRunningValue";
    public static string labelNameTotalXP = "LabelTotalXPValue";

    // Held so AnimateIn can replay them: UpdateDisplay is driven from the end-of-level coroutine
    // and can run while this panel is still inactive, which is exactly when there is no view to
    // write into.
    public GamePlayerRuntimeData lastRuntimeData = null;
    public float lastTimeTotal = 0f;

    public virtual void UpdateResultValues(
        GamePlayerRuntimeData runtimeData, float timeTotal) {

        lastRuntimeData = runtimeData;
        lastTimeTotal = timeTotal;

        if(runtimeData == null || string.IsNullOrEmpty(toolkitViewKey)) {
            return;
        }

        // StartCoroutine throws on an inactive GameObject, and an inactive panel has no view to
        // wait for — the AnimateIn replay covers that case.

        if(!gameObject.activeInHierarchy) {
            WriteResultValues();
            return;
        }

        StartCoroutine(UpdateResultValuesCo());
    }

    public virtual IEnumerator UpdateResultValuesCo() {

        for(int waitFrames = 0; waitFrames < 60 && !isToolkitPanel; waitFrames++) {
            yield return null;
        }

        WriteResultValues();
    }

    public virtual void WriteResultValues() {

        if(!isToolkitPanel || lastRuntimeData == null) {
            return;
        }

        UIUtil.UpdateLabelObject(
            viewRoot, labelNameScores, lastRuntimeData.scores.ToString("N0"));
        UIUtil.UpdateLabelObject(
            viewRoot, labelNameKills, lastRuntimeData.kills.ToString("N0"));
        UIUtil.UpdateLabelObject(
            viewRoot, labelNameCoins, lastRuntimeData.coins.ToString("N0"));
        UIUtil.UpdateLabelObject(
            viewRoot, labelNameTotalPoints, lastRuntimeData.totalScoreValue.ToString("N0"));
        UIUtil.UpdateLabelObject(
            viewRoot, labelNameTimeRunning,
            FormatUtil.GetFormattedTimeHoursMinutesSecondsMs((double)lastTimeTotal));

        // XP is not on the runtime data — GameRPG owns it, and its own labelXPValue is another
        // UILabel in the legacy branch, so the toolkit's XP field has the same gap. Read it from
        // the monitor rather than leaving a second placeholder on screen.
        //
        // currentTotalScore, NOT lastTotalScore: `last` is the tween cursor GameRPG counts UP
        // from and it initialises to the sentinel -1, which is what the toolkit XP field showed
        // when this first went in. Skip the write entirely while the monitor still holds a
        // sentinel, so the authored placeholder stands rather than a negative number.

        if(GameRPGMonitor.Instance != null
            && GameRPGMonitor.Instance.currentTotalScore >= 0) {
            UIUtil.UpdateLabelObject(
                viewRoot, labelNameTotalXP,
                GameRPGMonitor.Instance.currentTotalScore.ToString("N0"));
        }
    }

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

        string gameModeDisplay = GetGameModeDisplay();

        if(isToolkitPanel) {
            UIUtil.UpdateLabelObject(viewRoot, labelNameLevelCode, levelCodeDisplay);
            UIUtil.UpdateLabelObject(viewRoot, labelNameWorldCode, worldCodeDisplay);

            // Empty only when the content state has not resolved; leave the authored caption
            // standing rather than blanking the title.
            if(!string.IsNullOrEmpty(gameModeDisplay)) {
                UIUtil.UpdateLabelObject(viewRoot, labelNameGameMode, gameModeDisplay);
            }

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

    // Verbatim, NOT upper-cased: this mirrors what the legacy UILabel renders
    // ("Arcade Mode", "Missions Mode"), which is the migration contract. The view's authored
    // "ARCADE MODE" was never what the legacy panel showed.
    public virtual string GetGameModeDisplay() {

        if(AppContentStates.Current == null) {
            return "";
        }

        return AppContentStates.Current.display_name;
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

        // The loops above reach the LEGACY labels only; mirror the same numbers into the toolkit
        // view by name.

        UpdateResultValues(runtimeData, timeTotal);
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

        // Replay the run's numbers: UpdateDisplay may have run while this panel was inactive, or
        // before the toolkit view finished loading.

        UpdateResultValues(lastRuntimeData, lastTimeTotal);

        // Same replay for the CAPTIONS. UpdateLevelMeta's inactive branch writes straight through
        // with isToolkitPanel still false, so the level code, world code and mode caption land on
        // the legacy widgets only and the toolkit view keeps its authored placeholders. Nothing
        // re-ran them once the view arrived until here.

        UpdateLevelMeta();

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