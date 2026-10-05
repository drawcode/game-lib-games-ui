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

        // A fresh view starts with the arcade group display: none (USS); re-apply on next Update.
        arcadeTipApplied = -1;
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

        // Stages off first (layers restored, RTs released), then the bots go back under their
        // legacy parents BEFORE the suppressed Container is shown again.
        UnstageAllLegacy3D();
        arcade3DStaged = false;
        arcadeTipApplied = -1;
        activeTipsSet = null;
        RestoreArcade3D();

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

        activeTipsSet = active;

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

        Messenger<TapGesture>.AddListener(FingerGesturesMessages.OnTap, OnToolkitTipsTap);
        Messenger<SwipeGesture>.AddListener(FingerGesturesMessages.OnSwipe, OnToolkitTipsSwipe);
    }

    public override void OnDisable() {

        base.OnDisable();

        Messenger.RemoveListener(GameDraggableEditorMessages.GameLevelItemsLoaded, OnGameLevelItemsLoadedHandler);

        Messenger<string>.RemoveListener(UIPanelTipsMessages.tipsCycle, OnTipsCycleHandler);

        Messenger<string>.RemoveListener(GameMessages.gameInitLevelEnd, OnGameInitLevelEnd);

        Messenger<TapGesture>.RemoveListener(FingerGesturesMessages.OnTap, OnToolkitTipsTap);
        Messenger<SwipeGesture>.RemoveListener(FingerGesturesMessages.OnSwipe, OnToolkitTipsSwipe);
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
            UpdateToolkitArcadeTip();
        }
        else if(arcade3DMoved) {
            // A FreeToolkitView that ran while this panel was being deactivated could not move
            // the bots back (Unity refuses hierarchy changes then); finish it now.
            RestoreArcade3D();
        }
    }

    // The toolkit hide lands here when the slide ends, so the bots stay in their RTs for the
    // whole slide-out and go inactive with the view.
    public override void HidePanel() {

        base.HidePanel();

        if(isToolkitPanel && !isVisible) {
            SetArcade3DActive(false);
            arcadeTipApplied = -1;
        }
    }

    // ----------------------------------------------------------------------------------------
    // TAPS ON THE TOOLKIT PATH
    //
    // The tips sets advance on a FingerGestures tap/swipe, but UIPanelTips only listens while it
    // is enabled, and the toolkit suppression deactivates PanelOverview/Container above every
    // tips set — so "TAP TO ADVANCE TO NEXT TIP" did nothing on the toolkit path. Forward the
    // gesture to the active set, only while it cannot hear it itself (no double advance on the
    // legacy path). The coroutine runs on this panel: the set's own GameObject is inactive.

    void OnToolkitTipsTap(TapGesture gesture) {

        UIPanelTips tips = ToolkitTipsNeedingInput();

        if(tips != null) {
            StartCoroutine(tips.OnInputTapCo(gesture));
        }
    }

    void OnToolkitTipsSwipe(SwipeGesture gesture) {

        UIPanelTips tips = ToolkitTipsNeedingInput();

        if(tips != null) {
            tips.OnInputSwipe(gesture);
        }
    }

    UIPanelTips ToolkitTipsNeedingInput() {

        if(!isToolkitPanel || !isVisible) {
            return null;
        }

        UIPanelTips tips = activeTipsSet;

        if(tips == null || tips.isActiveAndEnabled) {
            return null;
        }

        return tips;
    }

    // ----------------------------------------------------------------------------------------
    // ARCADE TIP 1 (D8, iter 36) — the title + VS cards of the arcade tips set's tip-1-start
    //
    // Legacy: PanelOverview/Container/Tips/ModeTips/app-content-state-game-arcade/Container/
    // ItemsCenter/tip-1-start (title, star, two cards, ACTION BOT / VS. / INSECTOIDS + MORE and two
    // live 3D bots). It shows only while the ARCADE set is the active UIPanelTips (ShowTipsObject
    // picks the set whose name contains the content-state code) and its currentTipIndex is 0. The
    // training quiz set has its own, different tip-1-start, so the set is tested by name.
    //
    // The flat parts are the view's ArcadeTip1 group (display: none by default), toggled on change.
    //
    // 3D: the bots live under RotatorPlayer / RotatorEnemy inside the suppressed (inactive)
    // Container. They are MOVED (world pose kept) under a holder on this panel, so they can be
    // active while every flat NGUI widget around them stays suppressed, and staged into the
    // PlayerStage / PlayerEnemyStage elements through StageLegacy3D. Active only while the view is
    // up (until HidePanel) and tip 1 shows; the stage camera follows that. FreeToolkitView unstages
    // and puts them back exactly (parent, sibling index, local pose, activeSelf) — deferred to the
    // next Update when the panel was mid-deactivation. Legacy enemy card has TWO loaders, so two
    // overlapping bots: rendered faithfully.
    //
    // LIGHT: render-stage lights are one shared light per layer (max of the visible stages). The
    // READY screen can be the only stage up (HUD coin hidden behind it), so these stages ask for a
    // light of their own instead of borrowing.
    //
    // Per frame: reference/int/bool compares only; GameObject.name (allocates) is read once per
    // change of the active set.

    public const string elementArcadeTip = "ArcadeTip1";
    public const string elementArcadePlayerStage = "PlayerStage";
    public const string elementArcadeEnemyStage = "PlayerEnemyStage";
    public const string legacyArcadeTipName = "tip-1-start";
    public const string legacyArcadePlayerRoot = "RotatorPlayer";
    public const string legacyArcadeEnemyRoot = "RotatorEnemy";

    public static float arcadeStageLightIntensity = 1.0f;

    private UIPanelTips activeTipsSet;
    private UIPanelTips arcadeTipsSetChecked;
    private bool arcadeTipsSetIsArcade;
    private int arcadeTipApplied = -1;      // -1 unknown, 0 hidden, 1 shown
    private bool arcade3DWanted;
    private bool arcade3DStaged;
    private int arcadeReframeFrames;

    private GameObject arcade3DHolder;
    private bool arcade3DMoved;
    private readonly List<Arcade3DRecord> arcade3DRecords = new List<Arcade3DRecord>();
    private UIRenderStageBinding arcadePlayerStage;
    private UIRenderStageBinding arcadeEnemyStage;

    private class Arcade3DRecord {
        public GameObject root;
        public Transform parent;
        public int siblingIndex;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;
        public bool activeSelf;
    }

    private static Transform FindDeepChild(Transform parent, string childName) {

        if(parent == null) {
            return null;
        }

        for(int i = 0; i < parent.childCount; i++) {

            Transform child = parent.GetChild(i);

            if(child.name == childName) {
                return child;
            }

            Transform found = FindDeepChild(child, childName);

            if(found != null) {
                return found;
            }
        }

        return null;
    }

    private void UpdateToolkitArcadeTip() {

        UIPanelTips active = activeTipsSet;

        if(active != arcadeTipsSetChecked) {
            arcadeTipsSetChecked = active;
            arcadeTipsSetIsArcade = active != null
                && active.name.Contains(AppContentStateMeta.appContentStateGameArcade);
        }

        bool show = arcadeTipsSetIsArcade && active != null && active.currentTipIndex == 0;
        int applied = show ? 1 : 0;

        if(applied != arcadeTipApplied && viewRoot != null && viewRoot.alive) {

            arcadeTipApplied = applied;

            UIRef group = UIUtil.ResolveDeep(viewRoot, elementArcadeTip);

            if(show) {
                UIUtil.ShowObject(group);
            }
            else {
                UIUtil.HideObject(group);
            }
        }

        bool want3D = show && isVisible;

        if(want3D && !arcade3DStaged) {
            SetupArcade3D(active);
        }

        if(want3D != arcade3DWanted) {
            arcade3DWanted = want3D;

            if(want3D) {
                SetArcade3DActive(true);
                arcadeReframeFrames = 2;
            }
            else {
                SetArcade3DActive(false);
            }
        }

        if(arcadeReframeFrames > 0 && --arcadeReframeFrames == 0) {

            if(arcadePlayerStage != null && arcadePlayerStage.isBound) {
                arcadePlayerStage.stage.Reframe();
            }

            if(arcadeEnemyStage != null && arcadeEnemyStage.isBound) {
                arcadeEnemyStage.stage.Reframe();
            }
        }
    }

    private void MoveArcade3D(Transform tipRoot, string rootName) {

        Transform t = FindDeepChild(tipRoot, rootName);

        if(t == null) {
            return;
        }

        Arcade3DRecord r = new Arcade3DRecord();
        r.root = t.gameObject;
        r.parent = t.parent;
        r.siblingIndex = t.GetSiblingIndex();
        r.localPosition = t.localPosition;
        r.localRotation = t.localRotation;
        r.localScale = t.localScale;
        r.activeSelf = t.gameObject.activeSelf;

        t.SetParent(arcade3DHolder.transform, true);
        arcade3DRecords.Add(r);
    }

    // Once per view (or after a free): move the bots out of the suppressed Container and stage them.
    private void SetupArcade3D(UIPanelTips arcadeTips) {

        arcade3DStaged = true;

        if(viewRoot == null || !viewRoot.alive) {
            arcade3DStaged = false;
            return;
        }

        if(!arcade3DMoved) {

            if(arcadeTips == null) {
                return;
            }

            Transform tipRoot = FindDeepChild(arcadeTips.transform, legacyArcadeTipName);

            if(tipRoot == null) {
                return;
            }

            if(arcade3DHolder == null) {
                arcade3DHolder = new GameObject("ToolkitArcadeTip3D");
                arcade3DHolder.transform.SetParent(transform, false);
            }

            arcade3DRecords.Clear();
            MoveArcade3D(tipRoot, legacyArcadePlayerRoot);
            MoveArcade3D(tipRoot, legacyArcadeEnemyRoot);
            arcade3DMoved = arcade3DRecords.Count > 0;
        }

        // Active (renderers on: the Container's legacy Hide() disabled them) for the Attach: the
        // stage frames the content's posed mesh bounds at bind time.
        SetArcade3DActive(true);

        for(int i = 0; i < arcade3DRecords.Count; i++) {

            GameObject root = arcade3DRecords[i].root;

            if(root == null) {
                continue;
            }

            GameObjectHelper.ShowRenderers(root);

            UIRenderStageBinding.Options o = new UIRenderStageBinding.Options();
            o.size = 256;
            o.framePadding = 1.15f;
            o.followContent = true;
            o.keepColliderLayers = false;
            o.lightIntensity = arcadeStageLightIntensity;

            if(root.name == legacyArcadePlayerRoot) {
                arcadePlayerStage = StageLegacy3D(root, elementArcadePlayerStage, o);
            }
            else {
                arcadeEnemyStage = StageLegacy3D(root, elementArcadeEnemyStage, o);
            }
        }

        arcadeReframeFrames = 2;
    }

    private void SetArcade3DActive(bool active) {

        if(arcade3DHolder != null && arcade3DHolder.activeSelf != active) {
            arcade3DHolder.SetActive(active);
        }

        if(!active) {
            return;
        }

        for(int i = 0; i < arcade3DRecords.Count; i++) {

            GameObject root = arcade3DRecords[i].root;

            if(root != null && !root.activeSelf) {
                root.SetActive(true);
            }
        }
    }

    // The loaders under the rotators may spawn their bot AFTER the stage attached (the loader's
    // Start runs on first activation, and it copies its own -- by then the stage's -- layer), so
    // the stage's Detach, which restores only the transforms it recorded, leaves those on the
    // stage layer and the legacy cameras could never see them again. Top-down (parents come
    // first in GetComponentsInChildren), anything still on the stage layer takes its parent's.
    private static void ReleaseStageLayer(Transform root) {

        int stageLayer = UIRenderStageBinding.DefaultLayer();

        if(stageLayer < 0 || root.gameObject.layer == stageLayer) {
            return;
        }

        Transform[] all = root.GetComponentsInChildren<Transform>(true);

        for(int i = 0; i < all.Length; i++) {

            Transform c = all[i];

            if(c != root && c.parent != null && c.gameObject.layer == stageLayer) {
                c.gameObject.layer = c.parent.gameObject.layer;
            }
        }
    }

    private void RestoreArcade3D() {

        if(!arcade3DMoved) {
            return;
        }

        if(this == null || !gameObject.activeInHierarchy) {
            return;
        }

        for(int i = 0; i < arcade3DRecords.Count; i++) {

            Arcade3DRecord r = arcade3DRecords[i];

            if(r.root == null || r.parent == null) {
                continue;
            }

            Transform t = r.root.transform;
            t.SetParent(r.parent, false);
            t.SetSiblingIndex(r.siblingIndex);
            t.localPosition = r.localPosition;
            t.localRotation = r.localRotation;
            t.localScale = r.localScale;

            if(r.root.activeSelf != r.activeSelf) {
                r.root.SetActive(r.activeSelf);
            }

            ReleaseStageLayer(t);
        }

        arcade3DRecords.Clear();
        arcade3DMoved = false;
        arcade3DWanted = false;
        arcadePlayerStage = null;
        arcadeEnemyStage = null;

        if(arcade3DHolder != null) {
            arcade3DHolder.SetActive(false);
        }
    }
}