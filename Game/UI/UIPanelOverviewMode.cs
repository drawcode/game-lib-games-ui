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
        tipsStageApplied = -1;
        vsCardFilled = false;
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
        ReleaseVsCard();

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

        if(vsLevelCardEnabled) {
            index = tipsSlide + 1;
            total = TipsSlideCount();
        }
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

        if(index != lastTipStatusIndex && !vsLevelCardEnabled) {
            ShowDataTip();
        }

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
        else if(buttonName == elementTipsClose) {
            CloseTips();
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

        // The HUD's M.A.N. 9000 tap brings this screen back over a LIVE round, and its READY only
        // resumes it. Re-running the level-start player sequence there rebuilt the player's runtime
        // data and started a warp fade-out from 0 to 0 that never finished, which latched
        // effectWarpEnabled: no movement for the rest of the round (iter 31 device report).
        if(GameController.isInst && GameController.Instance.levelPlayerReadied) {
            HideAll();
            return;
        }

        if(GameController.isInst) {
            GameController.Instance.levelPlayerReadied = true;
        }

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

        ShowDataTip();
        ResetTipsCycle();

        tipsStage = GameController.isInst && GameController.Instance.levelPlayerReadied
            ? tipsStageTips : tipsStageAll;
        tipsStageApplied = -1;

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
            UpdateTipsCycle();
            UpdateToolkitTipStatus();
            ApplyTipsStage();
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

        if(TipsCycleTakesInput()) {
            AdvanceTipsSlide();
            return;
        }

        UIPanelTips tips = ToolkitTipsNeedingInput();

        if(tips != null) {
            StartCoroutine(tips.OnInputTapCo(gesture));
        }
    }

    void OnToolkitTipsSwipe(SwipeGesture gesture) {

        if(TipsCycleTakesInput()) {
            AdvanceTipsSlide();
            return;
        }

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

    // TIPS FIRST (iter 33, owner: "on the M.A.N. 9000 overlay with action tips, only show the
    // READY screen after the tips are closed"). Over a live round (levelPlayerReadied: the HUD
    // tap) the screen opens on the tips with OK in READY's slot; OK hides the tips and brings
    // READY up. The pre-round screen keeps tips + READY together: READY is its "click to begin".
    // Applied by element name from Update, so a view that binds late still gets the stage.
    const string elementTipsClose = "ButtonOverviewTipsClose";

    static readonly string[] tipsStageTipElements = {
        "TipsIcon", "LabelTipsHowTo", "LabelCurrentTipStatus", "LabelNextSteps", "NextIcon"
    };

    static readonly string[] tipsStageReadyElements = {
        "ButtonGameReady", "LabelOverviewStatus"
    };

    const int tipsStageAll = 0;     // pre-round: tips + READY together
    const int tipsStageTips = 1;    // live round: tips + OK
    const int tipsStageReady = 2;   // live round, tips closed: READY only

    private int tipsStage = tipsStageAll;
    private int tipsStageApplied = -1;

    public void CloseTips() {

        if(tipsStage != tipsStageTips) {
            return;
        }

        tipsStage = tipsStageReady;
    }

    private void ApplyTipsStage() {

        if(tipsStage == tipsStageApplied || viewRoot == null || !viewRoot.alive) {
            return;
        }

        tipsStageApplied = tipsStage;

        foreach(string name in tipsStageReadyElements) {
            SetViewElementShown(name, tipsStage != tipsStageTips);
        }

        foreach(string name in tipsStageTipElements) {
            SetViewElementShown(name, tipsStage != tipsStageReady);
        }

        SetViewElementShown(elementTipsClose, tipsStage == tipsStageTips);

        // The arcade card / data tip follow the stage on the next UpdateToolkitArcadeTip.
        arcadeTipApplied = -1;
    }

    private void SetViewElementShown(string name, bool shown) {

        UIRef el = UIUtil.ResolveDeep(viewRoot, name);

        if(shown) {
            UIUtil.ShowObject(el);
        }
        else {
            UIUtil.HideObject(el);
        }
    }

    // DATA TIP (iter 31, owner: the M.A.N. 9000 tap showed no tips). The legacy per-mode tip
    // cards were never ported, so outside arcade tip 1 the screen was empty. A tip from the data
    // the level-load overlay rotates (AppContentTips), written by element name so it lands even
    // before the async view binds (SetViewLabel replays it).
    const string elementDataTip = "DataTip";

    List<AppContentTip> dataTips;

    public virtual void ShowDataTip() {

        if(dataTips == null) {
            dataTips = AppContentTips.Instance.items;
        }

        if(dataTips == null || dataTips.Count == 0) {
            return;
        }

        AppContentTip tip = dataTips[UnityEngine.Random.Range(0, dataTips.Count)];

        string type = tip.keys != null && tip.keys.Count > 0
            ? Engine.Game.App.BaseApp.L10n.TrOrDefault(
                "game_ui_overlay_prepare_" + tip.keys[0] + "_tips", tip.keys[0] + " Tips")
            : "";

        SetViewLabel("LabelDataTipType", type);
        SetViewLabel("LabelDataTipTitle", tip.display_name);
        SetViewLabel("LabelDataTipDescription", tip.description);
    }

    // ----------------------------------------------------------------------------------------
    // TIPS CYCLE + VS CARD (iter 34, owner: "the READY screen should have tips to cycle, and when
    // it shows who you are fighting it should be your bot on the left, then the enemies of the
    // level on the right (enemy types)")
    //
    // Slides: 0 = the VS card (the ArcadeTip1 group, now on every mode), 1..n = the data tips in
    // order. They advance every tipsCycleSeconds (unscaled: the screen pauses the round) and on a
    // tap/swipe, and "Tip n of m" counts them. The cycle stops once the tips are closed (READY
    // only). The VS card: the player's current bot on the left, the level's enemy types on the
    // right (BaseAIController.GetLevelCharacterCodes, the roster the round spawns from, most
    // likely first, up to vsEnemyMax). Both sides are UIRenderSnapshot renders -- one render per
    // model, no live camera -- instead of the legacy RotatorPlayer / RotatorEnemy bots, which
    // only exist under the ARCADE tips set and showed a team-coloured bot, not the level's
    // enemies. Enemy display names are raw codes in the data ("zombie-dog-1"), so the enemy side
    // is pictures only; the player's bot name is its brand display name.
    //
    // Kill switch: vsLevelCardEnabled = false restores the legacy arcade-only card, the legacy
    // tips index and the random data tip, unchanged.

    public static bool vsLevelCardEnabled = true;
    public static float tipsCycleSeconds = 6f;
    public static int vsEnemyMax = 4;
    public static float vsPlayerYaw = 155f;
    public static float vsEnemyYaw = 205f;

    // Owner, 2026-10-31: "the enemies should be standing together more so you can see them teamed
    // up". One snapshot of all the level's enemy types in a formation - the first at the front
    // centre, the rest flanking it a step behind, overlapping a little - at their real relative
    // sizes, instead of one tile per type, each fitted to fill its own cell (which spread them over
    // a 2 x 2 grid and drew a marine at a third of a dog's height). Kill switch: false restores the
    // tiles. vsGroupOverlap is how much of the average width neighbours share; vsGroupDepth is how
    // far behind the flankers stand, as a share of the average depth; vsGroupLift raises them by a
    // share of the average height, like a back row in a team photo, so the group uses more of the
    // square picture than one row would.
    public static bool vsEnemyGroupEnabled = true;
    public static float vsGroupOverlap = .45f;
    public static float vsGroupDepth = .6f;
    public static float vsGroupLift = .2f;
    public static int vsGroupSnapshotSize = 384;

    const string elementVsPlayerImage = "VsPlayerImage";
    const string elementVsEnemies = "VsEnemies";
    const string vsEnemyElementPrefix = "VsEnemy";
    const string vsEnemiesOneClass = "ovm-vs-enemies--one";
    const string vsEnemiesThreeClass = "ovm-vs-enemies--three";
    const string vsEnemiesGroupClass = "ovm-vs-enemies--group";
    const string vsEnemyGroupSnapshotPrefix = "vs-enemies:";
    const char vsEnemyGroupSeparator = '+';
    const string vsPlayerSnapshotPrefix = "vs-player:";

    static readonly string[] vsLegacyElements = {
        elementArcadePlayerStage, elementArcadeEnemyStage, "LabelTeamEnemy"
    };

    private int tipsSlide;
    private float tipsSlideAt;
    private bool vsCardFilled;
    private int vsCardGeneration;
    private string vsPlayerSnapshotCode;
    private Engine.UI.UIRenderSnapshot vsSnapshots;
    private Engine.UI.UIRenderSnapshot vsGroupSnapshots;

    private int TipsSlideCount() {

        if(dataTips == null) {
            dataTips = AppContentTips.Instance.items;
        }

        return 1 + (dataTips != null ? dataTips.Count : 0);
    }

    private void ResetTipsCycle() {
        tipsSlide = 0;
        tipsSlideAt = Time.unscaledTime + tipsCycleSeconds;
        vsCardFilled = false;
        arcadeTipApplied = -1;
    }

    private bool TipsCycleTakesInput() {
        return vsLevelCardEnabled && isToolkitPanel && isVisible && tipsStage != tipsStageReady;
    }

    private void UpdateTipsCycle() {

        if(!vsLevelCardEnabled || !isVisible || tipsStage == tipsStageReady) {
            return;
        }

        if(Time.unscaledTime >= tipsSlideAt) {
            AdvanceTipsSlide();
        }
    }

    private void AdvanceTipsSlide() {

        int count = TipsSlideCount();

        tipsSlide = count > 0 ? (tipsSlide + 1) % count : 0;
        tipsSlideAt = Time.unscaledTime + tipsCycleSeconds;

        if(tipsSlide > 0) {
            ShowDataTipAt(tipsSlide - 1);
        }

        arcadeTipApplied = -1;
    }

    private void ShowDataTipAt(int i) {

        if(dataTips == null || i < 0 || i >= dataTips.Count) {
            return;
        }

        AppContentTip tip = dataTips[i];

        string type = tip.keys != null && tip.keys.Count > 0
            ? Engine.Game.App.BaseApp.L10n.TrOrDefault(
                "game_ui_overlay_prepare_" + tip.keys[0] + "_tips", tip.keys[0] + " Tips")
            : "";

        SetViewLabel("LabelDataTipType", type);
        SetViewLabel("LabelDataTipTitle", tip.display_name);
        SetViewLabel("LabelDataTipDescription", tip.description);
    }

    private void FillVsCard() {

        vsCardFilled = true;
        int generation = ++vsCardGeneration;

        foreach(string name in vsLegacyElements) {
            SetViewElementShown(name, false);
        }

        if(GameWorlds.Current != null) {
            SetViewLabel("LabelTitle", GameWorlds.Current.display_name);
        }

        if(vsSnapshots == null) {
            vsSnapshots = new Engine.UI.UIRenderSnapshot();
            vsSnapshots.width = 192;
            vsSnapshots.height = 192;
        }

        // The player's bot can change between rounds (colours too): render it fresh each fill.
        string characterCode = GetVsPlayerCharacterCode();
        GameCharacter character = string.IsNullOrEmpty(characterCode)
            ? null : GameCharacters.Instance.GetById(characterCode);

        if(character != null) {
            SetViewLabel("LabelBlurbPlayer", character.display_name);
        }

        if(vsPlayerSnapshotCode != null) {
            vsSnapshots.Release(vsPlayerSnapshotCode);
        }

        vsPlayerSnapshotCode = vsPlayerSnapshotPrefix + characterCode;

        UIRef playerImage = UIUtil.ResolveDeep(viewRoot, elementVsPlayerImage);
        UIUtil.ClearImageTexture(playerImage);
        UIUtil.ShowObject(playerImage);

        vsSnapshots.Request(vsPlayerSnapshotCode, SpawnVsPlayer, (code, texture) => {
            if(generation == vsCardGeneration && texture != null && viewRoot != null && viewRoot.alive) {
                UIUtil.SetImageTexture(UIUtil.ResolveDeep(viewRoot, elementVsPlayerImage), texture);
            }
        });

        List<string> enemies = BaseAIController.GetLevelCharacterCodes(GameActorType.enemy, vsEnemyMax);

        UIRef enemiesGroup = UIUtil.ResolveDeep(viewRoot, elementVsEnemies);
        UIUtil.ShowObject(enemiesGroup);

        bool group = vsEnemyGroupEnabled && enemies.Count > 1;

        UIUtil.SetClass(enemiesGroup, vsEnemiesOneClass, enemies.Count == 1);
        UIUtil.SetClass(enemiesGroup, vsEnemiesThreeClass, !group && enemies.Count == 3);
        UIUtil.SetClass(enemiesGroup, vsEnemiesGroupClass, group);

        if(group) {
            FillVsEnemyGroup(enemies, generation);
            return;
        }

        for(int i = 0; i < vsEnemyMax; i++) {

            UIRef slot = UIUtil.ResolveDeep(viewRoot, vsEnemyElementPrefix + (i + 1));

            if(i >= enemies.Count) {
                UIUtil.HideObject(slot);
                continue;
            }

            UIUtil.ClearImageTexture(slot);
            UIUtil.ShowObject(slot);

            string slotName = vsEnemyElementPrefix + (i + 1);

            vsSnapshots.Request(enemies[i], SpawnVsEnemy, (code, texture) => {
                if(generation == vsCardGeneration && texture != null && viewRoot != null && viewRoot.alive) {
                    UIUtil.SetImageTexture(UIUtil.ResolveDeep(viewRoot, slotName), texture);
                }
            });
        }
    }

    // The whole enemy side as one picture in the first slot; the other slots stay hidden.
    private void FillVsEnemyGroup(List<string> enemies, int generation) {

        if(vsGroupSnapshots == null) {
            vsGroupSnapshots = new Engine.UI.UIRenderSnapshot();
            vsGroupSnapshots.width = vsGroupSnapshotSize;
            vsGroupSnapshots.height = vsGroupSnapshotSize;
        }

        for(int i = 1; i < vsEnemyMax; i++) {
            UIUtil.HideObject(UIUtil.ResolveDeep(viewRoot, vsEnemyElementPrefix + (i + 1)));
        }

        string slotName = vsEnemyElementPrefix + 1;
        UIRef slot = UIUtil.ResolveDeep(viewRoot, slotName);
        UIUtil.ClearImageTexture(slot);
        UIUtil.ShowObject(slot);

        string code = vsEnemyGroupSnapshotPrefix + string.Join(vsEnemyGroupSeparator.ToString(), enemies);

        vsGroupSnapshots.Request(code, SpawnVsEnemyGroup, (c, texture) => {
            if(generation == vsCardGeneration && texture != null && viewRoot != null && viewRoot.alive) {
                UIUtil.SetImageTexture(UIUtil.ResolveDeep(viewRoot, slotName), texture);
            }
        });
    }

    // The stage camera looks along +Z with Y up, so x is across the picture and +z is away from
    // the camera. Positions come from each model's fitted bounds, so a dog and a marine stand at
    // the spacing their real widths need.
    static GameObject SpawnVsEnemyGroup(GameObject parent, string code) {

        string[] codes = code.Substring(vsEnemyGroupSnapshotPrefix.Length).Split(vsEnemyGroupSeparator);

        GameObject formation = new GameObject("vs-enemy-group");
        formation.transform.parent = parent.transform;
        formation.transform.localPosition = Vector3.zero;

        List<GameObject> models = new List<GameObject>();
        List<Bounds> bounds = new List<Bounds>();

        foreach(string characterCode in codes) {

            GameObject go = SpawnVsModel(formation, characterCode, vsEnemyYaw);

            if(go == null) {
                continue;
            }

            // The body only: particles and flat renderers (blob shadows) are dropped from the
            // render anyway (UIRenderSnapshot.skipFlatRenderers), and a wide blob would space the
            // models apart.
            bool found = false;
            Bounds b = new Bounds();

            foreach(Renderer r in go.GetComponentsInChildren<Renderer>(true)) {

                if(r is ParticleSystemRenderer) {
                    continue;
                }

                Bounds rb = DrawnBounds(r);
                Vector3 size = rb.size;

                if(size.y < .1f * Mathf.Max(size.x, size.z)) {
                    continue;
                }

                if(found) {
                    b.Encapsulate(rb);
                }
                else {
                    b = rb;
                    found = true;
                }
            }

            if(!found) {
                UnityEngine.Object.Destroy(go);
                continue;
            }

            models.Add(go);
            bounds.Add(b);
        }

        if(models.Count == 0) {
            UnityEngine.Object.Destroy(formation);
            return null;
        }

        float width = 0f;
        float depth = 0f;
        float height = 0f;

        foreach(Bounds b in bounds) {
            width += b.size.x;
            depth += b.size.z;
            height += b.size.y;
        }

        width /= bounds.Count;
        depth /= bounds.Count;
        height /= bounds.Count;

        float step = width * (1f - vsGroupOverlap);

        // 0 front centre; then left, right, further left: 1 -> -1, 2 -> +1, 3 -> -2.
        for(int i = 0; i < models.Count; i++) {

            int ring = (i + 1) / 2;
            float side = i % 2 == 1 ? -1f : 1f;
            float x = ring * side * step;
            float y = i == 0 ? 0f : height * vsGroupLift;
            float z = i == 0 ? 0f : depth * vsGroupDepth;

            // Move the model's bounds centre (x), feet (y) and front face (z) onto its spot.
            Vector3 spot = formation.transform.position + new Vector3(x, y, z);
            models[i].transform.position += new Vector3(
                spot.x - bounds[i].center.x, spot.y - bounds[i].min.y, spot.z - bounds[i].min.z);
        }

        return formation;
    }

    // What the renderer draws in its current pose, in world space: the stage frames this (the
    // UIRenderStage.PosedBounds rule), and a skinned renderer's own bounds hold every pose of the
    // rig, which spaced the models apart.
    static Bounds DrawnBounds(Renderer r) {

        SkinnedMeshRenderer smr = r as SkinnedMeshRenderer;

        if(smr == null || smr.sharedMesh == null) {
            return r.bounds;
        }

        Mesh baked = new Mesh();
        smr.BakeMesh(baked, true);

        Bounds local = baked.bounds;
        UnityEngine.Object.Destroy(baked);

        Transform t = smr.transform;
        Vector3 c = local.center;
        Vector3 e = local.extents;
        Bounds world = new Bounds(t.TransformPoint(c - e), Vector3.zero);

        for(int i = 1; i < 8; i++) {
            world.Encapsulate(t.TransformPoint(c + new Vector3(
                (i & 1) == 0 ? -e.x : e.x,
                (i & 2) == 0 ? -e.y : e.y,
                (i & 4) == 0 ? -e.z : e.z)));
        }

        return world;
    }

    private void ReleaseVsCard() {

        vsCardFilled = false;
        vsCardGeneration++;
        vsPlayerSnapshotCode = null;

        if(vsSnapshots != null) {
            vsSnapshots.Clear();
        }

        if(vsGroupSnapshots != null) {
            vsGroupSnapshots.Clear();
        }
    }

    // The profile's current character (GameCustomPlayerContainer.LoadPlayer's lookup).
    static string GetVsPlayerCharacterCode() {

        Engine.Game.App.BaseApp.GameProfileCharacterItem item = GameProfileCharacters.Current != null
            ? GameProfileCharacters.Current.GetCurrentCharacter() : null;

        if(item == null || item.code == BaseDataObjectKeys.defaultKey) {
            return ProfileConfigs.defaultGameCharacterCode;
        }

        return item.characterCode;
    }

    static GameObject SpawnVsModel(GameObject parent, string characterCode, float yaw) {

        GameCharacter character = GameCharacters.Instance.GetById(characterCode);

        if(character == null || character.data == null) {
            return null;
        }

        GameDataModel model = character.data.GetModel();

        if(model == null || string.IsNullOrEmpty(model.code)) {
            return null;
        }

        // Unpooled: the snapshot destroys what it spawns.
        GameObject go = AppContentAssets.LoadAsset(model.code, Vector3.zero, Quaternion.identity, false);

        if(go == null) {
            return null;
        }

        go.transform.parent = parent.transform;
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

        FitSkinnedBounds(go);

        return go;
    }

    // The stage frames renderer.bounds, and a skinned mesh's bounds are its authored localBounds:
    // the 3DRT bots ship them padded ~3x (marine-1: 2.99 x 2.91 x 3.44 local around a ~1 unit
    // model), so they rendered at a third of the size of the older models. Bake the posed mesh
    // and set localBounds to what it really covers. The spawned copy is thrown away after the
    // render, so changing its bounds costs nothing.
    static void FitSkinnedBounds(GameObject go) {

        Mesh baked = null;

        foreach(SkinnedMeshRenderer smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true)) {

            if(smr.sharedMesh == null) {
                continue;
            }

            if(baked == null) {
                baked = new Mesh();
            }

            // vertices come out in the renderer's own space (unscaled: useScale false)
            smr.BakeMesh(baked, false);

            Vector3[] vertices = baked.vertices;

            if(vertices.Length == 0) {
                continue;
            }

            // localBounds are in the root bone's space when it has one
            Transform boundsSpace = smr.rootBone != null ? smr.rootBone : smr.transform;
            Matrix4x4 toBounds = boundsSpace.worldToLocalMatrix
                * Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one);

            Bounds b = new Bounds(toBounds.MultiplyPoint3x4(vertices[0]), Vector3.zero);

            for(int i = 1; i < vertices.Length; i++) {
                b.Encapsulate(toBounds.MultiplyPoint3x4(vertices[i]));
            }

            smr.localBounds = b;
        }

        if(baked != null) {
            UnityEngine.Object.Destroy(baked);
        }
    }

    // The player's bot in the profile's colours/textures (the hero + default custom type reads
    // them from the profile, synchronously, in Change).
    static GameObject SpawnVsPlayer(GameObject parent, string code) {

        GameObject go = SpawnVsModel(parent, GetVsPlayerCharacterCode(), vsPlayerYaw);

        if(go == null) {
            return null;
        }

        Engine.Game.App.BaseApp.GameProfileCharacterItem item = GameProfileCharacters.Current != null
            ? GameProfileCharacters.Current.GetCurrentCharacter() : null;

        GameCustomCharacterData custom = new GameCustomCharacterData();
        custom.actorType = GameCustomActorTypes.heroType;
        custom.type = GameCustomTypes.defaultType;

        if(item != null) {
            custom.characterCode = item.code;
        }

        GameCustomPlayer customPlayer = go.GetOrSet<GameCustomPlayer>();

        if(customPlayer != null) {
            customPlayer.Change(custom);
        }

        return go;
    }

    static GameObject SpawnVsEnemy(GameObject parent, string code) {
        return SpawnVsModel(parent, code, vsEnemyYaw);
    }

    private void UpdateToolkitArcadeTip() {

        UIPanelTips active = activeTipsSet;

        if(active != arcadeTipsSetChecked) {
            arcadeTipsSetChecked = active;
            arcadeTipsSetIsArcade = active != null
                && active.name.Contains(AppContentStateMeta.appContentStateGameArcade);
        }

        bool show = arcadeTipsSetIsArcade && active != null && active.currentTipIndex == 0
            && tipsStage != tipsStageReady;

        if(vsLevelCardEnabled) {
            show = tipsSlide == 0 && tipsStage != tipsStageReady;
        }

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

            // The data tip fills the screen whenever the arcade card does not -- except once the
            // tips are closed (READY only).
            UIRef dataTip = UIUtil.ResolveDeep(viewRoot, elementDataTip);

            if(show || tipsStage == tipsStageReady) {
                UIUtil.HideObject(dataTip);
            }
            else {
                UIUtil.ShowObject(dataTip);
            }
        }

        if(show && vsLevelCardEnabled && !vsCardFilled && viewRoot != null && viewRoot.alive) {
            FillVsCard();
        }

        bool want3D = show && isVisible && !vsLevelCardEnabled;

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