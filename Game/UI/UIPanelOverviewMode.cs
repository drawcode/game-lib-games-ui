#define DEV
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Engine.Events;
using Engine.UI;
using Engine.Utility;
using Engine.Game.App;
using Engine.Game.Data;

public class UIPanelOverviewMode : UIPanelBase {
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    // OVERVIEW

    public UILabel labelOverviewTip;
    public UILabel labelOverviewType;
    public UILabel labelOverviewStatus;
    public UIImageButton buttonOverviewReady;
    public UIImageButton buttonOverviewTutorial;
    public UIImageButton buttonOverviewTips;
    public UIImageButton buttonOverviewMode;
    public UILabel labelOverviewTeamEnemy;
#else
    // B10: agnostic UIRef handles (was UGUI), the BaseGameHUD pattern. Unbound (null) until
    // something binds them by name; every UIUtil call no-ops on a null ref.
    // OVERVIEW

    public Engine.UI.UIRef labelOverviewTip;
    public Engine.UI.UIRef labelOverviewType;
    public Engine.UI.UIRef labelOverviewStatus;
    public Engine.UI.UIRef buttonOverviewReady;
    public Engine.UI.UIRef buttonOverviewTutorial;
    public Engine.UI.UIRef buttonOverviewTips;
    public Engine.UI.UIRef buttonOverviewMode;
    public Engine.UI.UIRef labelOverviewTeamEnemy;
#endif

    public static UIPanelOverviewMode Instance;
    public GameObject containerOverview;
    public GameObject containerOverviewGameplayTips;
    public GameObject containerTutorial;
    public GameObject containerTips;
    public GameObject containerTipsMode;
    public GameObject containerTipsGameplay;

    //public UIPanelTips tips

    // GLOBAL

    public AppOverviewFlowState flowState = AppOverviewFlowState.Mode;

    // THE MODE OVERVIEW / READY SCREEN as a toolkit view (iter 29). The same M.A.N. chassis as
    // UIPanelOverlayPrepare, and the same seams, for the same reasons (see that class):
    //   * labels live in the #if NGUI branch, so the view is written BY ELEMENT NAME and every
    //     write is replayed after the async load;
    //   * suppression hides PanelOverview/Container, one level below containerOverview, because
    //     ShowOverview/HideOverview show, hide and tween containerOverview itself;
    //   * suppression is re-asserted every frame and restored on free (kill switch).
    public override string toolkitViewKey {
        get {
            return BaseUIPanel.panelOverviewMode;
        }
    }

    // Above the chrome band, like the prepare overlay it follows.
    public override int toolkitSortOrder {
        get {
            return UILayers.overlay;
        }
    }

    public override bool toolkitPreloadView {
        get {
            return true;
        }
    }

    protected Dictionary<string, string> viewTextPending = new Dictionary<string, string>();

    protected virtual void SetViewLabel(string elementName, string value) {

        viewTextPending[elementName] = value;

        UIUtil.SetLabelValue(UIUtil.ResolveDeep(viewRoot, elementName), value);
    }

    public override void BindElements(UIRef root) {

        base.BindElements(root);

        foreach(KeyValuePair<string, string> pair in viewTextPending) {
            UIUtil.SetLabelValue(UIUtil.ResolveDeep(root, pair.Key), pair.Value);
        }
    }

    // The legacy overview slides in from the bottom (AnimateInBottom(containerOverview)).
    protected override void ShowToolkitViewSlide() {
        TweenUtil.ShowObjectBottom(viewRoot, toolkitShowPreset);
    }

    protected override void HideToolkitViewSlide() {
        TweenUtil.HideObjectBottom(viewRoot, toolkitHidePreset);
    }

    private Transform legacyOverviewContent;
    private readonly List<GameObject> suppressedLegacy = new List<GameObject>();

    protected override void SuppressLegacyView() {
        // NOT base: that hides panelContainer, which the panel's own AnimateIn re-shows.
        ReassertLegacySuppression();
    }

    private void ReassertLegacySuppression() {

        if(!isToolkitPanel) {
            return;
        }

        if(legacyOverviewContent == null && containerOverview != null) {
            legacyOverviewContent = containerOverview.transform.Find("Container");
        }

        Transform t = legacyOverviewContent;

        if(t == null || !t.gameObject.activeSelf) {
            return;
        }

        t.gameObject.Hide();

        if(!suppressedLegacy.Contains(t.gameObject)) {
            suppressedLegacy.Add(t.gameObject);
        }
    }

    protected override void FreeToolkitView() {

        foreach(GameObject go in suppressedLegacy) {
            if(go != null) {
                go.Show();
            }
        }

        suppressedLegacy.Clear();
        legacyOverviewContent = null;
        lastTipStatus = null;
        viewLabelTipStatus.Clear();
        tipsCacheValid = false;

        base.FreeToolkitView();
    }

    // "Tip n of m" is written by the active UIPanelTips into its own NGUI label; mirror it from
    // the component's state, localized.
    //
    // This runs every frame the view exists, and used to cost ~190 B/frame (measured): a
    // GetComponentsInChildren array, plus TrOrDefault's params array, two boxed ints and the
    // formatted string — all to produce the same text as last frame. Now the tips components are
    // cached (see RefreshTipsCache) and the text is formatted only when what it is made of
    // changes: (index, total, locale). The view label's CURRENT text is part of the skip test, so
    // an element that came back blank after a view teardown, or that someone else wrote, refills.
    private string lastTipStatus;
    private int lastTipStatusIndex;
    private int lastTipStatusTotal;
    private string lastTipStatusLocale;
    private readonly UIViewLabel viewLabelTipStatus = new UIViewLabel("LabelCurrentTipStatus");

    // The UIPanelTips under containerTips, in GetComponentsInChildren order (so "first activeSelf"
    // picks the same one it always did). Rebuilt when the container or its child count changes,
    // when a cached entry has been destroyed, and on every show (AnimateIn) — the cases where the
    // set of tips components can actually differ.
    private readonly List<UIPanelTips> tipsCache = new List<UIPanelTips>();
    private GameObject tipsCacheContainer;
    private int tipsCacheChildCount = -1;
    private bool tipsCacheValid;

    private void RefreshTipsCache() {

        bool valid = tipsCacheValid
            && tipsCacheContainer == containerTips
            && tipsCacheChildCount == containerTips.transform.childCount;

        for(int i = 0; valid && i < tipsCache.Count; i++) {
            if(tipsCache[i] == null) {
                valid = false;
            }
        }

        if(valid) {
            return;
        }

        // Include inactive: suppression deactivates the legacy Container above the tips, so
        // activeInHierarchy is false for all of them. The shown one is the activeSelf one.
        containerTips.GetComponentsInChildren<UIPanelTips>(true, tipsCache);
        tipsCacheContainer = containerTips;
        tipsCacheChildCount = containerTips.transform.childCount;
        tipsCacheValid = true;
    }

    private void UpdateToolkitTipStatus() {

        if(containerTips == null) {
            return;
        }

        RefreshTipsCache();

        UIPanelTips active = null;

        for(int i = 0; i < tipsCache.Count; i++) {
            if(tipsCache[i].gameObject.activeSelf) {
                active = tipsCache[i];
                break;
            }
        }

        // No tips set matches most content states (ShowTipsObject shows none), and then the legacy
        // screen shows ContainerTips/LabelCurrentTipStatus as AUTHORED: "Tip 1 of 3" (measured).
        int index = active != null ? active.currentTipIndex + 1 : 1;
        int total = active != null ? active.tipsTotal : 3;
        string locale = Engine.Game.App.BaseApp.L10n.CurrentCode;

        if(lastTipStatus != null
            && index == lastTipStatusIndex
            && total == lastTipStatusTotal
            && locale == lastTipStatusLocale
            && viewLabelTipStatus.Shows(viewRoot, lastTipStatus)) {
            return;
        }

        // TrOrDefault, not Tr: this lib ships to games that don't have the key, and Tr would put
        // the raw key on screen there -- the English format is the fallback.
        string status = Engine.Game.App.BaseApp.L10n.TrOrDefault("game_ui_overview_mode_tip_status",
            "Tip {0} of {1}", index, total);

        lastTipStatusIndex = index;
        lastTipStatusTotal = total;
        lastTipStatusLocale = locale;

        if(status == lastTipStatus && viewLabelTipStatus.Shows(viewRoot, status)) {
            return;
        }

        lastTipStatus = status;
        SetViewLabel("LabelCurrentTipStatus", status);
    }

    public override void Awake() {
        base.Awake();

        if(Instance != null && this != Instance) {
            //There is already a copy of this script running
            //Destroy(gameObject);
            return;
        }

        Instance = this;

        panelTypes.Add(UIPanelBaseTypes.typeDialogHUD);
    }

    public static bool isInst {
        get {
            if(Instance != null) {
                return true;
            }
            return false;
        }
    }

    public override void Init() {
        base.Init();

        Ready();
    }

    public override void Start() {
        Init();
    }

    // EVENTS

    public override void OnEnable() {

        base.OnEnable();

        Messenger.AddListener(GameDraggableEditorMessages.GameLevelItemsLoaded, OnGameLevelItemsLoadedHandler);

        Messenger<string>.AddListener(UIPanelTipsMessages.tipsCycle, OnTipsCycleHandler);

        Messenger<string>.AddListener(GameMessages.gameInitLevelEnd, OnGameInitLevelEnd);
    }

    public override void OnDisable() {

        base.OnDisable();

        Messenger.RemoveListener(GameDraggableEditorMessages.GameLevelItemsLoaded, OnGameLevelItemsLoadedHandler);

        Messenger<string>.RemoveListener(UIPanelTipsMessages.tipsCycle, OnTipsCycleHandler);

        Messenger<string>.RemoveListener(GameMessages.gameInitLevelEnd, OnGameInitLevelEnd);
    }

    void OnGameInitLevelEnd(string levelCode) {
        ShowTipsObjectMode();
    }

    string lastTipObjectName = "";

    void OnTipsCycleHandler(string objName) {
        if(objName != lastTipObjectName) {
            lastTipObjectName = objName;

            if(flowState == AppOverviewFlowState.GameplayTips) {
                ChangeTipsState(AppOverviewFlowState.Mode);
            }
            else {
                ChangeTipsState(AppOverviewFlowState.GameplayTips);
            }

        }

        GameCustomController.BroadcastCustomSync();
    }

    public override void OnButtonClickEventHandler(string buttonName) {
        if(UIUtil.IsButtonClicked(buttonOverviewReady, buttonName)) {
            Ready();
        }
        else if(UIUtil.IsButtonClicked(buttonOverviewMode, buttonName)) {
            ChangeTipsState(AppOverviewFlowState.Mode);
        }
        else if(UIUtil.IsButtonClicked(buttonOverviewTutorial, buttonName)) {
            ShowTutorial();
        }
        else if(UIUtil.IsButtonClicked(buttonOverviewTips, buttonName)) {
            ChangeTipsState(AppOverviewFlowState.GameplayTips);
        }
    }

    public void ContentPause() {
        GameController.GameRunningStateContent();
    }

    public void ContentRun() {
        GameController.GameRunningStateRun();
    }

    public void Ready() {

        // The round goes live here, so the level-load sequence is over even if its finish
        // coroutine never completed -- release its flag and its overlay rather than carry them
        // into the round (iter 25, items 21/22).
        if(GameController.isInst) {
            GameController.Instance.releaseLevelInitializing();
        }

        Messenger.Broadcast(GameMessages.gameLevelPlayerReady);

        HideAll();
    }

    public void ChangeTipsState(AppOverviewFlowState flowStateTo) {
        flowState = flowStateTo;
        UpdateTipsStates();
    }

    public void UpdateTipsStates() {

        if(flowState == AppOverviewFlowState.GameplayTips) {
            ShowTipsObjectGameplay();
        }
        else {
            ShowTipsObjectMode();
        }
    }

    public void ShowTipsObjectGameplay() {
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
        UIUtil.HideButton(buttonOverviewTips);
        UIUtil.ShowButton(buttonOverviewMode);
#else
        UIUtil.HideObject(buttonOverviewTips);
        UIUtil.ShowObject(buttonOverviewMode);
#endif
        ShowTipsObject("gameplay");
    }

    public void ShowTipsObjectMode() {
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
        UIUtil.HideButton(buttonOverviewMode);
        UIUtil.ShowButton(buttonOverviewTips);
#else
        UIUtil.HideObject(buttonOverviewMode);
        UIUtil.ShowObject(buttonOverviewTips);
#endif
        string currentAppContentState = AppContentStates.Current.code;
        ShowTipsObject(currentAppContentState);
    }

    public void HideTipsObjects() {
        if(containerTips != null) {
            foreach(UIPanelTips tips in containerTips.GetComponentsInChildren<UIPanelTips>(true)) {
                tips.gameObject.Hide();

                TweenUtil.FadeToObject(tips.gameObject, 0f, .4f, 0f);

                //UITweenerUtil.FadeTo(tips.gameObject, UITweener.Method.Linear, UITweener.Style.Once, .4f, 0f, 0f);
            }
        }
    }

    public void ShowTipsObject(string objName) {

        if(containerTips != null) {

            HideTipsObjects();

            foreach(UIPanelTips tips in containerTips.GetComponentsInChildren<UIPanelTips>(true)) {

                if(!string.IsNullOrEmpty(objName) && tips.name.Contains(objName)) {
                    tips.gameObject.Show();

                    TweenUtil.FadeToObject(tips.gameObject, 0f, 0f, 0f);
                    //UITweenerUtil.FadeTo(tips.gameObject, UITweener.Method.Linear, UITweener.Style.Once, 0f, 0f, 0f);

                    tips.ShowTipsFirst();

                    TweenUtil.FadeToObject(tips.gameObject, 1f, .5f, .6f);
                    //UITweenerUtil.FadeTo(tips.gameObject, UITweener.Method.Linear, UITweener.Style.Once, .5f, .6f, 1f);

                }
            }
        }
    }

    public void ShowTutorial() {

        HideStates();

        flowState = AppOverviewFlowState.Tutorial;

        UIPanelDialogBackground.ShowDefault();

        UIUtil.SetLabelValue(labelOverviewType, AppContentStates.Current.display_name);
        SetViewLabel("LabelOverviewType", AppContentStates.Current.display_name);

        //LogUtil.Log("UIPanelModeTypeChoice:ShowOverview:flowState:" + flowState);

        AnimateInBottom(containerTutorial);

        ContentPause();

        UIColors.UpdateColors();

    }

    public void HideTutorial() {

        AnimateOutBottom(containerOverview, 0f, 0f);

        ContentRun();
    }

    public void ShowTips() {

        HideStates();

        flowState = AppOverviewFlowState.GameplayTips;

        UIPanelDialogBackground.ShowDefault();

        UIUtil.SetLabelValue(labelOverviewType, AppContentStates.Current.display_name);
        SetViewLabel("LabelOverviewType", AppContentStates.Current.display_name);

        //LogUtil.Log("UIPanelModeTypeChoice:ShowOverview:flowState:" + flowState);

        AnimateInBottom(containerOverviewGameplayTips);

        ContentPause();

        UIColors.UpdateColors();

    }

    public void HideTips() {

        AnimateOutBottom(containerOverview, 0f, 0f);

        ContentRun();
    }

    public void OnGameLevelItemsLoadedHandler() {

        //LogUtil.Log("OnGameLevelItemsLoadedHandler");

        if(AppModeTypes.Instance.isAppModeTypeGameChoice) {

            //LogUtil.Log("OnGameLevelItemsLoadedHandler2");
        }
    }

    public void UpdateOverviewWorld() {

        GameWorld gameWorld = GameWorlds.Current;

        if(gameWorld == null) {
            return;
        }

        foreach(GameObjectInactive obj in containerOverview.GetList<GameObjectInactive>()) {

            if(obj.type == BaseDataObjectKeys.overview
                && obj.code == BaseDataObjectKeys.world) {

                foreach(GameObjectInactive world in containerOverview.GetList<GameObjectInactive>()) {
                    if(world.type == BaseDataObjectKeys.world) {
                        TweenUtil.HideObjectBottom(world.gameObject);
                    }
                }

                foreach(GameObjectInactive world in containerOverview.GetList<GameObjectInactive>()) {

                    if(world.code.IsEqualLowercase(gameWorld.code)) {
                        TweenUtil.ShowObjectBottom(world.gameObject);
                    }
                }
            }
        }
    }

    public void ShowOverview() {

        HideStates();

        // Update team display
        //LogUtil.Log("ShowOverview:");

        flowState = AppOverviewFlowState.Mode;

        UIPanelDialogBackground.ShowDefault();

        UpdateOverviewWorld();

        UIUtil.SetLabelValue(labelOverviewType, AppContentStates.Current.display_name);
        SetViewLabel("LabelOverviewType", AppContentStates.Current.display_name);

        AnimateInBottom(containerOverview);

        GameCustomController.BroadcastCustomSync();

        foreach(GameCustomPlayer customPlayer in gameObject.GetList<GameCustomPlayer>()) {

            if(customPlayer.isActorTypeEnemy) {

                GameTeam team = GameTeams.Current;

                if(team != null) {

                    UIUtil.SetLabelValue(labelOverviewTeamEnemy, team.display_name);

                    GameCustomCharacterData customInfo = new GameCustomCharacterData();
                    customInfo.actorType = GameCustomActorTypes.enemyType;
                    customInfo.presetColorCode = team.data.GetColorPreset().code;
                    customInfo.presetTextureCode = team.data.GetTexturePreset().code;
                    customInfo.type = GameCustomTypes.teamType;
                    customInfo.teamCode = team.code;

                    customPlayer.Load(customInfo);
                }
            }
        }

        ContentPause();

        UIColors.UpdateColors();
    }

    public void HideOverview() {

        AnimateOutBottom(containerOverview, 0f, 0f);

        ContentRun();
    }

    public void ShowCurrentState() {
        ShowOverview();
    }

    public void HideStates() {

        UIPanelDialogBackground.HideAll();

        UIPanelDialogRPGHealth.HideAll();
        UIPanelDialogRPGEnergy.HideAll();

        HideOverview();
        HideTutorial();
        HideTips();
    }

    // SHOW/LOAD

    public static void ShowDefault() {
        if(isInst) {
            Instance.AnimateIn();
        }
    }

    public static void HideAll() {
        if(isInst) {
            Instance.AnimateOut();
        } 
    }

    public void Reset() {
        flowState = AppOverviewFlowState.Mode;
    }

    public static void LoadData() {
        if(Instance != null) {
            Instance.loadData();
        }
    }

    public void loadData() {
        //LogUtil.Log("UIPanelModeTypeChoice:loadData");
        StartCoroutine(loadDataCo());
    }

    IEnumerator loadDataCo() {

        Reset();

        ShowCurrentState();

        UpdateTipsStates();

        yield return new WaitForSeconds(1f);
    }

    public override void AnimateIn() {
        base.AnimateIn();

        // A show is when the tips set may have been rebuilt; re-collect it on the next Update.
        tipsCacheValid = false;

        UIPanelDialogBackground.ShowDefault();

        loadData();
    }

    public override void AnimateOut() {
        base.AnimateOut();

        HideStates();
    }

    public void Update() {

        if(isToolkitPanel) {
            ReassertLegacySuppression();
            UpdateToolkitTipStatus();
        }
    }
}