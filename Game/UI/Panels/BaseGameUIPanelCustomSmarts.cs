using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
#else
using UnityEngine.UI;
#endif

using Engine.Events;

#if ENABLE_FEATURE_CHARACTER_CUSTOMIZE_SMARTS

public class BaseGameUIPanelCustomSmarts : GameUIPanelBase {

    public static GameUIPanelCustomSmarts Instance;

    public GameObject listItemPrefab;

    public GameObject helmetObject;
    public GameObject helmetObjectRotator;

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    public UIImageButton buttonClose;
    public UILabel labelPlaySmartScore;
#else
    public Engine.UI.UIRef buttonClose; // 2.11: agnostic handle, bound by name	
    public Engine.UI.UIRef labelPlaySmartScore; // 2.11: agnostic handle, bound by name
#endif

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

        // B7 (2026-10-03): chain to the base. UIPanelBase.OnDisable is what calls FreeToolkitView;
        // without it the view leaks its PanelRenderer and the kill switch cannot restore legacy.
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

    public override void Start() {
        Init();
    }

    public override void Init() {
        base.Init();

        loadData();

        double score = GameProfileRPGs.Current.GetGamePlayerProgressXP();

        SetScore(score);
    }

    public virtual void LoadDefault() {
        LoadData();
    }

    public static void ShowDefault() {
        if(GameUIPanelCustomSmarts.Instance != null) {
            GameUIPanelCustomSmarts.Instance.AnimateIn();
            GameUIPanelCustomSmarts.Instance.LoadDefault();
        }
    }

    public override void OnButtonClickEventHandler(string buttonName) {
        //LogUtil.Log("OnButtonClickEventHandler: " + buttonName);
    }

    public static void LoadData() {
        if(GameUIPanelCustomSmarts.Instance != null) {
            GameUIPanelCustomSmarts.Instance.loadData();
        }
    }

    public virtual void loadData() {
        StartCoroutine(loadDataCo());
    }

    IEnumerator loadDataCo() {

        InputSystem.Instance.currentDraggableGameObject = helmetObjectRotator;

        yield return new WaitForSeconds(1f);
    }

    public static void HideAll() {
        if(GameUIPanelCustomSmarts.Instance != null) {
            GameUIPanelCustomSmarts.Instance.AnimateOut();
        }
    }

    public virtual void SetScore(double score) {

        string val = score.ToString("N0", Engine.Game.App.BaseApp.L10n.NumberFormat);

        UIUtil.SetLabelValue(labelPlaySmartScore, val);

        // B7: the toolkit copy. Under USE_UI_NGUI_2_7 the field above is a UILabel and can never
        // bind to the view, so the view's label is written BY NAME, and remembered for the replay
        // in BindElements (Init calls this before the async view exists).
        toolkitScoreText = val;

        SetToolkitScore();
    }

    // ----------------------------------------------------------------------------------------
    // B7 (2026-10-03): UI Toolkit hosting -- Resources/ui/views/panel-custom-smarts.uxml.
    //
    // The key is BaseUIPanel.panelCustomSmartsCode ("panel-custom-smarts", the prefab's name), NOT
    // panelCustomSmarts, whose value "panelcustom-smarts" is a typo kept for compatibility.
    // Flat panel: nothing under panelContainer has to survive, so the default whole-container
    // suppression applies. Buttons route by name through the GLOBAL handler
    // (ButtonGameEquipmentRoom -> ShowEquipment, ButtonGameModeTraining -> training).
    public override string toolkitViewKey {
        get {
            return BaseUIPanel.panelCustomSmartsCode;
        }
    }

    // The bind target of labelPlaySmartScore (also aliased in Resources/ui/binds/panel-custom-smarts.json
    // for builds without NGUI, where the field is a UIRef).
    public const string elementScoreValue = "LabelValue";

    protected string toolkitScoreText = null;

    // Runs from LoadToolkitView's continuation, every time a view is (re)built.
    public override void BindElements(Engine.UI.UIRef root) {

        base.BindElements(root);

        SetToolkitScore();
    }

    protected virtual void SetToolkitScore() {

        if(!isToolkitPanel || toolkitScoreText == null) {
            return;
        }

        UIUtil.SetLabelValue(UIUtil.ResolveDeep(viewRoot, elementScoreValue), toolkitScoreText);
    }

    public override void AnimateIn() {

        base.AnimateIn();

        //ShowPanelDefault();
    }

    public override void AnimateOut() {

        base.AnimateOut();

        InputSystem.Instance.currentDraggableGameObject = null;

        //HidePanelDefault();
    }

    public virtual void Update() {

        if(GameConfigs.isGameRunning) {
            return;
        }

        if(!isVisible) {
            return;
        }
    }
}
#endif