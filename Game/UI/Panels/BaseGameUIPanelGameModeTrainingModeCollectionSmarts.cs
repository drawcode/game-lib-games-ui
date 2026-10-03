using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Engine.Events;

#if ENABLE_FEATURE_TRAINING

public class BaseGameUIPanelGameModeTrainingModeCollectionSmarts : GameUIPanelBase {

    public static GameUIPanelGameModeTrainingModeCollectionSmarts Instance;

    public GameObject listItemPrefab;

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

    public override void Start() {
        Init();
    }

    public override void Init() {
        base.Init();

        loadData();
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

        // B4 (2026-10-03): chain to the base. UIPanelBase.OnDisable is what calls FreeToolkitView;
        // without this the view leaks its PanelRenderer and the kill switch cannot restore the
        // legacy view. Same fix as the chooser (B0b) and the arcade/challenge screens.
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
        //
    }

    public static void LoadData() {
        if(GameUIPanelGameModeTrainingModeCollectionSmarts.Instance != null) {
            GameUIPanelGameModeTrainingModeCollectionSmarts.Instance.loadData();
        }
    }

    public virtual void loadData() {
        StartCoroutine(loadDataCo());
    }

    IEnumerator loadDataCo() {

        yield return new WaitForSeconds(1f);

    }

    public virtual void ClearList() {
        if(listGridRoot != null) {
            listGridRoot.DestroyChildren();
        }
    }

    public override void AnimateIn() {

        base.AnimateIn();

        loadData();
    }

    public override void AnimateOut() {

        base.AnimateOut();

        ClearList();
    }

    // ----------------------------------------------------------------------------------------
    // B4 (2026-10-03): the toolkit view's SMARTS / HEALTH ENERGY meter.
    //
    // On legacy the meter is driven by a UIGameRPGHealth on the SmartsScore widget, which stops ticking
    // once LoadToolkitView suppresses the container -- so the view reads the profile itself, the
    // same source as that component (and the same model as UIPanelDialogRPGObject's toolkit
    // meter): written on bind, then re-read once a second while the panel is up, and written
    // only on change. The legacy count-up animation is not reproduced.

    public const string elementMeterFill = "ProgressForeground";
    public const string elementMeterPercent = "LabelProgress";

    public const float toolkitMeterInterval = 1f;

    protected float toolkitMeterElapsed = 0f;
    protected double toolkitLastMeter = double.NaN;

    public virtual double GetToolkitMeterValue() {
        return Math.Round(GameProfileCharacters.currentProgress.GetGamePlayerProgressHealth(1), 2);
    }

    // Runs from LoadToolkitView's continuation, every time a view is (re)built.
    public override void BindElements(Engine.UI.UIRef root) {

        base.BindElements(root);

        if(!isToolkitPanel) {
            return;
        }

        RefreshToolkitMeter(true);
    }

    protected virtual void RefreshToolkitMeter(bool force) {

        if(!isToolkitPanel) {
            return;
        }

        double val = GetToolkitMeterValue();

        if(!force && val == toolkitLastMeter) {
            return;
        }

        toolkitLastMeter = val;

        // A plain VisualElement fill: SetSliderValue falls back to width-percent, the same model
        // as NGUI 2.7's UISlider scaling its foreground sprite.
        UIUtil.SetSliderValue(UIUtil.ResolveDeep(viewRoot, elementMeterFill), (float)val);
        UIUtil.SetLabelValue(UIUtil.ResolveDeep(viewRoot, elementMeterPercent), val.ToString("P0"));
    }

    public virtual void Update() {

        if(!isVisible || !isToolkitPanel) {
            return;
        }

        toolkitMeterElapsed += Time.unscaledDeltaTime;

        if(toolkitMeterElapsed < toolkitMeterInterval) {
            return;
        }

        toolkitMeterElapsed = 0f;

        RefreshToolkitMeter(false);
    }
}
#endif
