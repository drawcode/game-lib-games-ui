#define DEV
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
#else
using UnityEngine.UI;
#endif

using Engine.Events;
using Engine.Utility;
using Engine.UI;
using Engine.Game.App;
using Engine.Game.Data;

public class UIPanelOverlayPrepare : UIPanelBase {
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    // OVERVIEW

    public UILabel labelOverviewTip;
    public UILabel labelOverviewType;
    public UILabel labelOverviewStatus;

    public UILabel labelTipTitle;
    public UILabel labelTipDescription;
    public UILabel labelTipType;

    public UIImageButton buttonReady;

    public UIImageButton buttonTipNext;
#else
    // OVERVIEW

    public Text labelOverviewTip;
    public Text labelOverviewType;
    public Text labelOverviewStatus;

    public Text labelTipTitle;
    public Text labelTipDescription;
    public Text labelTipType;

    public Button buttonReady;

    public Button buttonTipNext;
#endif

    public static UIPanelOverlayPrepare Instance;

    public GameObject containerOverview;
    public GameObject containerOverviewGameplayTips;
    public GameObject containerOverviewGeneralTips;
    public GameObject containerTutorial;

    public GameObject containerTips;
    public GameObject containerTipsMode;
    public GameObject containerTipsGameplay;
    public GameObject containerTipsGeneral;

    public GameObject containerLoader;
    public GameObject containerLoaderSpinner;

    public string loadingLevelDisplay = "Loading Level...";

    //public UIPanelTips tips

    // GLOBAL

    public AppOverviewFlowState flowState = AppOverviewFlowState.GeneralTips;

    // THE LEVEL-LOAD PREPARE / TIPS OVERLAY -- the last screen a player sees that was still NGUI
    // (contexts: context-remaining-ngui-ugui-inventory.md, context-panel-overlay-prepare.md).
    //
    // FLIPPED 2026-09-07, in the three-commit staging rule 115 describes: the view was authored
    // and colour-verified against baselines/level-load-prepare-tips-red-backer.png with the key
    // left EMPTY (iter 22), and this iteration exercised the things a static render cannot --
    // a real level load, the slide in, the READY click, tip cycling and the item-loaded handoff.
    //
    // Migrating it is also the standing fix for the open "header + menu cover the loader" defect
    // (context-3f-pause-loader-levelload.md item D): the NGUI overlay drew UNDER every toolkit
    // view, so the header and the outgoing menu sat on top of it for the ~1.5s between
    // initLevelCo and onGameStarted. toolkitSortOrder below puts it above the chrome band.
    //
    // Backing out is UIPlatform.toolkitViewsEnabled (global) or "" here (this screen only).
    public override string toolkitViewKey {
        get {
            return BaseUIPanel.panelOverlayPrepare;
        }
    }

    // Above the chrome band (10000). The header and the menu screen are only dismissed at the
    // late onGameStarted, ~1.5s after this overlay appears, so anything below chrome is buried
    // for that whole window.
    public override int toolkitSortOrder {
        get {
            return UILayers.overlay;
        }
    }

    // Scene-resident and enabled at level load, long before the overlay is shown -- the case
    // preloading exists for. Harmless while toolkitViewKey is "".
    public override bool toolkitPreloadView {
        get {
            return true;
        }
    }

    // The six labels this panel writes are declared inside the `#if USE_UI_NGUI_2_7` branch, so
    // they are UILabel and BindElements can never bind them (rule 91). The toolkit side is
    // therefore written BY ELEMENT NAME, and every write is remembered so BindElements can
    // replay it -- the panel sets the loading text long before the async view load returns.
    protected Dictionary<string, string> viewTextPending = new Dictionary<string, string>();

    protected virtual void SetViewLabel(string elementName, string value) {

        viewTextPending[elementName] = value;

        UIUtil.SetLabelValue(UIUtil.ResolveDeep(viewRoot, elementName), value);
    }

    protected virtual void SetViewObjectVisible(UIRef root, string elementName, bool visible) {

        UIRef element = UIUtil.ResolveDeep(root, elementName);

        if(visible) {
            UIUtil.ShowObject(element);
        }
        else {
            UIUtil.HideObject(element);
        }
    }

    public override void BindElements(UIRef root) {

        base.BindElements(root);

        foreach(KeyValuePair<string, string> pair in viewTextPending) {
            UIUtil.SetLabelValue(UIUtil.ResolveDeep(root, pair.Key), pair.Value);
        }

        SetViewObjectVisible(root, "ButtonGameInitFinish", buttonPlayVisible);
    }

    // ShowButtonPlay/HideButtonPlay toggle the legacy button's GameObject; the toolkit element
    // has none, so the state is mirrored here and replayed by BindElements.
    protected bool buttonPlayVisible = false;

    // SLIDE
    //
    // The legacy overlay enters from the BOTTOM -- PanelOverview is parked at y = -3500 in the
    // scene and ShowOverview tweens it up (AnimateInBottom(containerOverview)). The default view
    // slide is from the TOP, which would have this screen enter the wrong way round. Time.timeScale
    // is 1 here (measured in a real level load), so unlike pause this can be an animated slide
    // rather than ShowViewInPlace.
    protected override void ShowToolkitViewSlide() {
        TweenUtil.ShowObjectBottom(viewRoot, toolkitShowPreset);
    }

    protected override void HideToolkitViewSlide() {
        TweenUtil.HideObjectBottom(viewRoot, toolkitHidePreset);
    }

    // ONLY THIS PANEL'S OWN DISMISSAL MAY TAKE THE VIEW OUT
    //
    // The prepare overlay is on screen DURING a level load, and the level-load flow animates every
    // other panel OUT while it is up. Three routes reach this panel's AnimateOut, none of them
    // meaning "put the loader away":
    //
    //   * BaseUIController.HideAllPanels / HideAllPanelsNow sweep FindObjectsOfType(UIPanelBase)
    //     and call AnimateOut on every one of them -- including this panel;
    //   * showUIPanelActionsCo runs that sweep for whatever panel is shown next;
    //   * this panel carries UIPanelBaseTypes.typeDialogHUD, so every other dialog of that type
    //     broadcasts uiPanelAnimateOutClassType at it as IT animates in.
    //
    // Measured in play: the sweep lands one frame after UIPanelOverviewMode animates in, mid-load
    // ("PROBE outClassType from=UIPanelOverviewMode type=type-dialog-hud f=216", view hidden the
    // same frame).
    //
    // In legacy all three were INVISIBLE here: panelContainer and all ten panelLeft/Right/Top/
    // Bottom/Center objects are null on this panel (measured), so AnimateOut had nothing to hide.
    // What the player sees is containerOverview, and that is shown and hidden by ShowOverview /
    // HideOverview instead. A toolkit view IS driven by AnimateOut (HideToolkitViewSlide +
    // HidePanel), so the sweep blanked the entire level-load screen -- the white transition flash
    // and nothing else, for the whole load.
    //
    // So AnimateOut only does something when THIS panel is being dismissed: hideAll(), which is
    // UIPanelOverlayPrepare.HideAll() from the READY click (BaseUIController) and from
    // initLevelFinishCo. Everything else stays the no-op it has always been.
    private bool dismissing = false;

    public override void AnimateOut(float time, float delay) {

        if(!dismissing) {
            return;
        }

        base.AnimateOut(time, delay);
    }

    // SUPPRESSION
    //
    // The default UIPanelBase.SuppressLegacyView hides panelContainer -- and this panel's
    // panelContainer is NULL (measured in play; the scene wires containerOverview and nothing
    // else), so the default is a NO-OP here and all 104 legacy widgets would draw underneath the
    // toolkit view.
    //
    // What it hides instead is PanelOverview/Container, one level BELOW containerOverview, and the
    // level matters: ShowOverview/HideOverview Show(), Hide() and tween PanelOverview ITSELF, so
    // suppressing that object would be undone by the panel's own next show. Nothing anywhere
    // touches the Container child, and hiding it deactivates every descendant -- which is also what
    // takes the legacy ButtonGameInitFinish and ButtonTipNext COLLIDERS out of UICamera's pick
    // list. Hiding only the widgets would leave those live and pickable over a screen they no
    // longer draw (suppressed-ngui-widgets-keep-colliders).
    private Transform legacyOverviewContent;

    private readonly List<GameObject> suppressedLegacy = new List<GameObject>();

    private Transform ResolveLegacyOverviewContent() {

        if(legacyOverviewContent == null && containerOverview != null) {
            legacyOverviewContent = containerOverview.transform.Find("Container");
        }

        return legacyOverviewContent;
    }

    protected override void SuppressLegacyView() {

        // NOT base.SuppressLegacyView() -- see above, panelContainer is null on this panel.
        ReassertLegacySuppression();
    }

    // CONTINUOUS, not one-shot (the HUD's iter-7 rule): the level-load flow re-enters this panel on
    // every restart and re-runs ShowOverview -> ShowTipsObject, so anything re-activated after the
    // first sweep would come back on top of the view and stay there.
    private void ReassertLegacySuppression() {

        if(!isToolkitPanel) {
            return;
        }

        Transform t = ResolveLegacyOverviewContent();

        if(t == null || !t.gameObject.activeSelf) {
            return;
        }

        t.gameObject.Hide();

        // Tracked once, so the restore list cannot grow across re-activations.
        if(!suppressedLegacy.Contains(t.gameObject)) {
            suppressedLegacy.Add(t.gameObject);
        }
    }

    public virtual void Update() {

        if(isToolkitPanel) {
            ReassertLegacySuppression();
        }
    }

    // Restore-on-free, so the UIPlatform.toolkitViewsEnabled kill switch returns a working legacy
    // overlay rather than an empty chassis (rule 116).
    protected override void FreeToolkitView() {

        foreach(GameObject go in suppressedLegacy) {
            if(go != null) {
                go.Show();
            }
        }

        suppressedLegacy.Clear();

        base.FreeToolkitView();
    }

    public override void Awake() {
        base.Awake();

        if(Instance != null && this != Instance) {
            //There is already a copy of this script running
            //Destroy(gameObject);
            return;
        }

        Instance = this;
        
        HideCamera();

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
    }

    public override void OnDisable() {

        base.OnDisable();

        Messenger.RemoveListener(GameDraggableEditorMessages.GameLevelItemsLoaded, OnGameLevelItemsLoadedHandler);

        Messenger<string>.RemoveListener(UIPanelTipsMessages.tipsCycle, OnTipsCycleHandler);
    }

    string lastTipObjectName = "";

    void OnTipsCycleHandler(string objName) {
        if(objName != lastTipObjectName) {
            lastTipObjectName = objName;

            //if(flowState == AppOverviewFlowState.GameplayTips) {
            //    ChangeTipsState(AppOverviewFlowState.Mode);
            //}
            //else {            
            ChangeTipsState(AppOverviewFlowState.GeneralTips);
            //}
        }
    }

    public override void OnButtonClickEventHandler(string buttonName) {

        if(UIUtil.IsButtonClicked(buttonTipNext, buttonName)) {

            CancelInvoke("ShowOverviewTip");
            ShowOverviewTip();
        }
        /*
        if (UIUtil.IsButtonClicked(buttonOverviewReady, buttonName)) {
            Ready();
        }
        else if (UIUtil.IsButtonClicked(buttonOverviewMode, buttonName)) {
            ChangeTipsState(AppOverviewFlowState.Mode);
        }
        else if (UIUtil.IsButtonClicked(buttonOverviewTutorial, buttonName)) {
            ShowTutorial();
        }
        else if (UIUtil.IsButtonClicked(buttonOverviewTips, buttonName)) {
            ChangeTipsState(AppOverviewFlowState.GameplayTips);
        } 
        */
    }

    public void Ready() {
        //HideAll();
        HideStates();
    }

    public void ChangeTipsState(AppOverviewFlowState flowStateTo) {
        flowState = flowStateTo;
        UpdateTipsStates();
    }

    public void UpdateTipsStates() {

        if(flowState == AppOverviewFlowState.GeneralTips) {
            ShowTipsObjectGeneral();
        }
    }

    public void ShowTipsObjectGameplay() {
        //UIUtil.HideButton(buttonOverviewTips);
        //UIUtil.ShowButton(buttonOverviewMode);
        ShowTipsObject("gameplay");
    }

    public void ShowTipsObjectGeneral() {
        //UIUtil.HideButton(buttonOverviewTips);
        //UIUtil.ShowButton(buttonOverviewMode);
        ShowTipsObject("general");
    }

    public void ShowTipsObjectMode() {
        //UIUtil.HideButton(buttonOverviewMode);
        //UIUtil.ShowButton(buttonOverviewTips);
        //string currentAppContentState = AppContentStates.Current.code;
        //ShowTipsObject(currentAppContentState);
    }

    public void HideTipsObjects() {
        if(containerTips != null) {
            foreach(UIPanelTips tips in containerTips.GetComponentsInChildren<UIPanelTips>(true)) {
                tips.gameObject.Hide();
                                
                TweenUtil.FadeToObject(tips.gameObject, 0f, .4f, 0f);

                //UITweenerUtil.FadeTo(tips.gameObject, UITweener.Method.Linear, UITweener.Style.Once, .4f, 0f, 0f);
            }
            
            HideCamera();
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

            ShowCamera();
        }
    }

    public void ShowTutorial() {

        ShowCamera();

        HideStates();

        flowState = AppOverviewFlowState.Tutorial;

        UIPanelDialogBackground.ShowDefault();

        UIUtil.SetLabelValue(labelOverviewType, AppContentStates.Current.display_name);
        SetViewLabel("LabelOverviewType", AppContentStates.Current.display_name);

        //LogUtil.Log("UIPanelModeTypeChoice:ShowOverview:flowState:" + flowState);

        AnimateInBottom(containerTutorial);

        UIColors.UpdateColors();

    }

    public void HideTutorial() {

        AnimateOutBottom(containerOverview);

        HideCamera();
    }

    public void ShowTips() {

        ShowCamera();

        HideStates();

        flowState = AppOverviewFlowState.GeneralTips;

        UIPanelDialogBackground.ShowDefault();

        UIUtil.SetLabelValue(labelOverviewType, AppContentStates.Current.display_name);
        SetViewLabel("LabelOverviewType", AppContentStates.Current.display_name);

        //LogUtil.Log("UIPanelModeTypeChoice:ShowOverview:flowState:" + flowState);

        AnimateInBottom(containerOverviewGameplayTips);

        UIColors.UpdateColors();

    }

    public void HideTips() {

        AnimateOutBottom(containerOverview);

        HideCamera();
    }

    public void OnGameLevelItemsLoadedHandler() {

        //LogUtil.Log("OnGameLevelItemsLoadedHandler");

        if(AppModeTypes.Instance.isAppModeTypeGameChoice) {

            //LogUtil.Log("OnGameLevelItemsLoadedHandler2");
        }

        // TrOrDefault: other games on this shared lib keep the English literal.
        string readyToPlay = Engine.Game.App.BaseApp.L10n.TrOrDefault("game_ui_overlay_prepare_ready_to_play", "READY TO PLAY?");
        UIUtil.SetLabelValue(labelOverviewTip, readyToPlay);
        SetViewLabel("LabelOverviewTip", readyToPlay);
        ShowButtonPlay();
    }

    public void ShowButtonPlay() {
        if(buttonReady != null) {
            buttonReady.gameObject.Show();
        }

        buttonPlayVisible = true;
        SetViewObjectVisible(viewRoot, "ButtonGameInitFinish", true);

        HideLoaderSpinner();
    }

    public void HideButtonPlay() {
        if(buttonReady != null) {
            buttonReady.gameObject.Hide();
        }

        buttonPlayVisible = false;
        SetViewObjectVisible(viewRoot, "ButtonGameInitFinish", false);

        ShowLoaderSpinner();
    }

    public void ShowLoader() {

        ShowCamera();

        containerLoader.Show();
    }

    public void HideLoader() {

        containerLoader.Hide();

        HideCamera();
    }

    public void ShowLoaderSpinner() {
        
        containerLoaderSpinner.Show();
    }

    public void HideLoaderSpinner() {
        containerLoaderSpinner.Hide();
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

        containerOverview.Show();

        ShowCamera();

        ShowLoaderSpinner();

        UpdateOverviewWorld();

        if(containerLoader.Has<GameObjectImageFill>()) {
            GameObjectImageFill fill = containerLoader.Get<GameObjectImageFill>();
            fill.Reset();
        }

        // Update team display

        //LogUtil.Log("ShowOverview:");

        flowState = AppOverviewFlowState.GeneralTips;

        UIPanelDialogBackground.ShowDefault();

        UIUtil.SetLabelValue(labelOverviewType, AppContentStates.Current.display_name);
        SetViewLabel("LabelOverviewType", AppContentStates.Current.display_name);

        AnimateInBottom(containerOverview);

        UIColors.UpdateColors();

        InvokeRepeating("ShowOverviewTip", 0, 15);
    }

    List<AppContentTip> currentTips;
    AppContentTip currentTip;

    public void ShowOverviewTip() {

        if(currentTips == null) {
            currentTips = AppContentTips.Instance.items;
        }

        if(currentTips == null) {
            return;
        }

        if(currentTips.Count == 0) {
            return;
        }

        currentTips.Shuffle();

        currentTip = currentTips[0];

        UIUtil.SetLabelValue(labelTipTitle, currentTip.display_name);
        SetViewLabel("LabelTipTitle", currentTip.display_name);
        UIUtil.SetLabelValue(labelTipDescription, currentTip.description);
        SetViewLabel("LabelTipDescription", currentTip.description);
        // The view authors @loc:game_ui_overlay_prepare_action_tips; this overwrite used to put
        // English "action Tips" back over it. Keyed per tip category, English concat as default.
        string tipType = Engine.Game.App.BaseApp.L10n.TrOrDefault(
            "game_ui_overlay_prepare_" + currentTip.keys[0] + "_tips", currentTip.keys[0] + " Tips");
        UIUtil.SetLabelValue(labelTipType, tipType);
        SetViewLabel("LabelTipType", tipType);
    }

    public void HideOverview() {

        AnimateOutBottom(containerOverview);

        containerOverview.Hide();

        CancelInvoke("ShowOverviewTip");

        HideCamera();
    }

    public void ShowCurrentState() {
        ShowOverview();
    }

    public void HideStates() {

        HideButtonPlay();

        string loadingLevel = Engine.Game.App.BaseApp.L10n.TrOrDefault("game_ui_overlay_prepare_loading_level", loadingLevelDisplay);
        UIUtil.SetLabelValue(labelOverviewTip, loadingLevel);
        SetViewLabel("LabelOverviewTip", loadingLevel);

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
            Instance.showDefault();
        }
    }

    public void showDefault() {
        AnimateIn();
    }

    public static void HideAll() {
        if(isInst) {
            Instance.hideAll();
        }
    }

    public void hideAll() {

        dismissing = true;

        AnimateOut();

        dismissing = false;

        HideStates();
    }

    public void Reset() {
        flowState = AppOverviewFlowState.GeneralTips;
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

        yield return new WaitForSeconds(.1f);
    }

    public override void AnimateIn() {
        base.AnimateIn();

        loadData();
    }

    public override void AnimateOut() {
        base.AnimateOut();

        //HideStates();

        //HideCamera();
    }
}