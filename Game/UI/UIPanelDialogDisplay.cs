#define DEV
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
#else
using UnityEngine.UI;
#endif

using Engine.Events;
using Engine.UI;
using Engine.Utility;

public class UIPanelDialogDisplay : UIPanelBase {
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    public UILabel labelTitle;
    public UILabel labelDescription;
    public UIImageButton buttonDialogOk;
    public UIImageButton buttonDialogCancel;
    public UIImageButton buttonDialogGo;
    public UIImageButton buttonDialogNext;
#else
    public Text labelTitle;
    public Text labelDescription;
    public Button buttonDialogOk;
    public Button buttonDialogCancel;
    public Button buttonDialogGo;
    public Button buttonDialogNext;
#endif

    public static UIPanelDialogDisplay Instance;

    // ----------------------------------------------------------------------------------------
    // TOOLKIT (wave 3F — the dialog family, after the pause overlay)

    // SCENE-RESIDENT singleton, like UIPanelPause: BaseUIController does not catalog-load this
    // panel and there is no panel-dialog-display.prefab anywhere in the project. The panel key
    // exists only to name the view (Resources/ui/views/panel-dialog-display.uxml).
    public override string toolkitViewKey {
        get {
            return BaseUIPanel.panelDialogDisplay;
        }
    }

    // Enabled at level load, long before anything goes out of bounds — so build the view THEN,
    // not in the frame the dialog is asked for. Same reasoning as the pause overlay, and the case
    // toolkitPreloadView exists for.
    public override bool toolkitPreloadView {
        get {
            return true;
        }
    }

    // A modal over live gameplay: it must draw ABOVE the in-game HUD, which loads earlier.
    public override int toolkitSortOrder {
        get {
            return UILayers.overlay;
        }
    }

    // Legacy enters from the BOTTOM — backgroundDisplayState is None, so centerEnterDirection
    // resolves to Bottom, and the scene parks Center at y -3000 (below). The base default slides
    // down from the top, which is the flow-panel choreography, not this one's.
    //
    // Unlike pause this does NOT want ShowViewInPlace: out-of-bounds runs through
    // gameRunningStateContent(), whose default timeScale is 1, so the scaled-time tween pump is
    // still turning and a real slide plays.
    protected override void ShowToolkitViewSlide() {
        TweenUtil.ShowObjectBottom(viewRoot, toolkitShowPreset);
    }

    protected override void HideToolkitViewSlide() {
        TweenUtil.HideObjectBottom(viewRoot, toolkitHidePreset);
    }

    // labelTitle / labelDescription / the four buttons are all UILabel and UIImageButton — they
    // sit inside the USE_UI_NGUI branch above, so BindElements can never bind them and every
    // write has to go by ELEMENT NAME instead.
    //
    // They are also written BEFORE the dialog is shown (GameController sets the title, THEN calls
    // ShowDefault; Reset() hides the buttons at Start), and on the very first show the view may
    // still be loading. So each write is recorded here and replayed from SuppressLegacyView once
    // the view actually lands — the same seam the header's LabelSection uses.
    protected string toolkitTitle = "";
    protected string toolkitDescription = "";

    protected readonly Dictionary<string, bool> toolkitButtonVisible =
        new Dictionary<string, bool>();

    // Element names are the wire contract — they match the scene GameObjects that ButtonEvents
    // broadcasts, which is what UIUtil.IsButtonClicked compares against.
    public const string elementTitle = "LabelTitle";
    public const string elementDescription = "LabelDescription";
    public const string elementButtonOk = "ButtonDialogOk";
    public const string elementButtonCancel = "ButtonDialogCancel";
    public const string elementButtonGo = "ButtonDialogGo";
    public const string elementButtonNext = "ButtonDialogNext";

    protected void SetToolkitButtonVisible(string elementName, bool visible) {

        toolkitButtonVisible[elementName] = visible;

        if(!isToolkitPanel) {
            return;
        }

        if(visible) {
            UIUtil.ShowObject(UIUtil.ResolveDeep(viewRoot, elementName));
        }
        else {
            UIUtil.HideObject(UIUtil.ResolveDeep(viewRoot, elementName));
        }
    }

    // Replays everything written while the view was still loading. Runs from LoadToolkitView's
    // continuation, so viewRoot is alive by the time it is called.
    protected override void SuppressLegacyView() {

        base.SuppressLegacyView();

        UIUtil.SetLabelValue(UIUtil.ResolveDeep(viewRoot, elementTitle), toolkitTitle);
        UIUtil.SetLabelValue(UIUtil.ResolveDeep(viewRoot, elementDescription), toolkitDescription);

        foreach(KeyValuePair<string, bool> pair in toolkitButtonVisible) {

            if(pair.Value) {
                UIUtil.ShowObject(UIUtil.ResolveDeep(viewRoot, pair.Key));
            }
            else {
                UIUtil.HideObject(UIUtil.ResolveDeep(viewRoot, pair.Key));
            }
        }
    }

    // ----------------------------------------------------------------------------------------

    public override void Awake() {
        base.Awake();

        if(Instance != null && this != Instance) {
            //There is already a copy of this script running
            //Destroy(gameObject);
            return;
        }

        Instance = this;
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

        Reset();

        loadData();
    }

    public override void Start() {
        Init();
    }

    public void Reset() {

        SetTitle("");
        SetDescription("");

        HideAllButtons();
    }

    // base.OnEnable/OnDisable are deliberately NOT chained. Chaining OnEnable would register the
    // SAME EVENT_BUTTON_CLICK handler a second time — Messenger is a plain multicast delegate with
    // no dedupe, so every click would fire twice — and would add the panel-type animate listeners
    // this scene-resident dialog has never carried. What IS needed from the base pair is the
    // toolkit view lifecycle, so those two calls are lifted in directly.
    public override void OnEnable() {
        Messenger<string>.AddListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);

        // Warm the view now rather than at the first show — see toolkitPreloadView.
        if(toolkitPreloadView) {
            PreloadToolkitView();
        }
    }

    public override void OnDisable() {
        Messenger<string>.RemoveListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);

        // Symmetric with the preload above: release the view so its PanelRenderer is reclaimed.
        FreeToolkitView();
    }

    public override void OnButtonClickEventHandler(string buttonName) {
        if(UIUtil.IsButtonClicked(buttonDialogOk, buttonName)) {
            HideAll();
            GameController.GameRunningStateRun();
        }
        else if(UIUtil.IsButtonClicked(buttonDialogGo, buttonName)) {
            HideAll();
            GameController.GameRunningStateRun();
        }
        else if(UIUtil.IsButtonClicked(buttonDialogCancel, buttonName)) {
            HideAll();
            GameController.GameRunningStateRun();
        }
    }

    public static void ShowDefault() {
        if(isInst) {
            Instance.showDefault();
        }
    }

    public void showDefault() {
        
        AnimateIn();
        loadData();
    }


    public static void HideAll() {
        if(isInst) {
            Instance.AnimateOut();
        }
    }

    public void hideAll() {
        
        AnimateOut();
    }

    public static void ShowButtonOk() {
        if(isInst) {
            Instance.showButtonOk();
        }
    }

    public static void ShowButtonCancel() {
        if(isInst) {
            Instance.showButtonCancel();
        }
    }

    public static void ShowButtonGo() {
        if(isInst) {
            Instance.showButtonGo();
        }
    }

    public static void HideButtonOk() {
        if(isInst) {
            Instance.hideButtonOk();
        }
    }

    public static void HideButtonCancel() {
        if(isInst) {
            Instance.hideButtonCancel();
        }
    }

    public static void HideButtonGo() {
        if(isInst) {
            Instance.hideButtonGo();
        }
    }

    public static void HideButtonNext() {
        if(isInst) {
            Instance.hideButtonNext();
        }
    }

    public static void HideAllButtons() {
        if(isInst) {
            Instance.hideAllButtons();
        }
    }

    // Each of these keeps driving the legacy widget as well as the view. The legacy button is
    // only HIDDEN under suppression, never destroyed, and OnButtonClickEventHandler still reads
    // its GameObject name through UIUtil.IsButtonClicked — so it has to stay in step.
    public void showButtonOk() {
        UIUtil.ShowButton(buttonDialogOk);
        SetToolkitButtonVisible(elementButtonOk, true);
    }

    public void showButtonCancel() {
        UIUtil.ShowButton(buttonDialogCancel);
        SetToolkitButtonVisible(elementButtonCancel, true);
    }

    public void showButtonGo() {
        UIUtil.ShowButton(buttonDialogGo);
        SetToolkitButtonVisible(elementButtonGo, true);
    }

    public void showButtonNext() {
        UIUtil.ShowButton(buttonDialogNext);
        SetToolkitButtonVisible(elementButtonNext, true);
    }

    public void hideButtonOk() {
        UIUtil.HideButton(buttonDialogOk);
        SetToolkitButtonVisible(elementButtonOk, false);
    }

    public void hideButtonCancel() {
        UIUtil.HideButton(buttonDialogCancel);
        SetToolkitButtonVisible(elementButtonCancel, false);
    }

    public void hideButtonGo() {
        UIUtil.HideButton(buttonDialogGo);
        SetToolkitButtonVisible(elementButtonGo, false);
    }

    public void hideButtonNext() {
        UIUtil.HideButton(buttonDialogNext);
        SetToolkitButtonVisible(elementButtonNext, false);
    }

    // NOTE the legacy defect, reproduced rather than silently corrected: Cancel is listed twice
    // and NEXT is never hidden. That is why the NGUI baseline capture shows NEXT and OK drawn on
    // top of each other. Fixing it changes what the dialog shows, which is a product decision, not
    // part of porting it — so it is left exactly as it was and flagged instead.
    public void hideAllButtons() {
        HideButtonOk();
        HideButtonCancel();
        HideButtonGo();
        HideButtonCancel();
    }

    public static void SetTitle(string titleTo) {
        if(isInst) {
            Instance.setTitle(titleTo);
        }
    }

    public void setTitle(string titleTo) {
        UIUtil.SetLabelValue(labelTitle, titleTo);

        toolkitTitle = titleTo;

        if(isToolkitPanel) {
            UIUtil.SetLabelValue(UIUtil.ResolveDeep(viewRoot, elementTitle), titleTo);
        }
    }

    public static void SetDescription(string descriptionTo) {
        if(isInst) {
            Instance.setDescription(descriptionTo);
        }
    }

    public void setDescription(string descriptionTo) {
        UIUtil.SetLabelValue(labelDescription, descriptionTo);

        toolkitDescription = descriptionTo;

        if(isToolkitPanel) {
            UIUtil.SetLabelValue(UIUtil.ResolveDeep(viewRoot, elementDescription), descriptionTo);
        }
    }

    public static void LoadData() {
        if(Instance != null) {
            Instance.loadData();
        }
    }

    public void loadData() {
        StartCoroutine(loadDataCo());
    }

    IEnumerator loadDataCo() {

        yield return new WaitForSeconds(1f);

        HideAllButtons();
    }
}