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

    // THE LEVEL-LOAD PREPARE / TIPS OVERLAY -- the last unconverted screen a player sees
    // (contexts: context-remaining-ngui-ugui-inventory.md). The view is authored
    // (Resources/ui/views/panel-overlay-prepare.uxml) and the bridge below is wired, but the
    // KEY IS STILL EMPTY, so nothing changes: EnsureToolkitView returns early on an empty key
    // and this panel keeps rendering NGUI. That is deliberate and is the same staging
    // UIPanelPause used -- this screen is the LEVEL-LOAD CRITICAL PATH, and the view's font
    // sizes and its seven stacked semi-transparent backers have not been measured against
    // baselines/level-load-prepare-tips-red-backer.png yet.
    //
    // TO FLIP: return BaseUIPanel.panelOverlayPrepare here, then drive a real level load and
    // A/B against that baseline. Migrating it is also the standing fix for the open
    // "header + menu cover the loader" defect (context-3f-pause-loader-levelload.md item D):
    // the NGUI overlay draws UNDER every toolkit view, and toolkitSortOrder below puts it
    // above the chrome band where it belongs.
    public override string toolkitViewKey {
        get {
            return "";
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

        UIUtil.SetLabelValue(labelOverviewTip, "READY TO PLAY?");
        SetViewLabel("LabelOverviewTip", "READY TO PLAY?");
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
        UIUtil.SetLabelValue(labelTipType, currentTip.keys[0] + " Tips");
        SetViewLabel("LabelTipType", currentTip.keys[0] + " Tips");
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

        UIUtil.SetLabelValue(labelOverviewTip, loadingLevelDisplay);
        SetViewLabel("LabelOverviewTip", loadingLevelDisplay);

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
        AnimateOut();
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