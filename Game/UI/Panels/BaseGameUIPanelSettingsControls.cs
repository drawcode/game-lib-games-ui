using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Engine.Events;
using Engine.Game.App.BaseApp;

#if ENABLE_FEATURE_SETTINGS_CONTROLS

public class BaseGameUIPanelSettingsControls : GameUIPanelBase {

    public static GameUIPanelSettingsControls Instance;

    public GameObject listItemPrefab;

    // The legacy widgets, wired on the NGUI prefab.
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    public UICheckbox checkboxControlsHandedRight;
    public UICheckbox checkboxControlsHandedLeft;

    public UICheckbox checkboxControlsVibrate;
#endif

    // The bitty view's elements, bound by BindElements. UNCONDITIONAL, and that is the whole
    // point of the change: the mirror these replace put the UIRef fields in the `#else` of the
    // block above, but this project compiles the `#if` -- USE_UI_NGUI_2_7 is defined ALONGSIDE
    // USE_UI_TOOLKIT -- so the fields were UICheckbox, and BindElements binds only fields whose
    // type is exactly UIRef. It skipped all three and warned, and the migrated page rendered
    // three toggles with nothing holding them. See the ngui-if-branch-fields-cannot-bind rule.
    //
    // UIRef.none rather than null: every backend op no-ops on a ref that is not alive, so an
    // unbound element degrades to "nothing happens" instead of a NullReferenceException on a
    // Messenger callback, which would take every other listener down with it.
    public Engine.UI.UIRef toggleControlsHandedRight = Engine.UI.UIRef.none;
    public Engine.UI.UIRef toggleControlsHandedLeft = Engine.UI.UIRef.none;

    public Engine.UI.UIRef toggleControlsVibrate = Engine.UI.UIRef.none;

    // Off-screen edge indicator size. New in this pass; the legacy prefab has no counterpart.
    public Engine.UI.UIRef sliderControlsIndicatorScale = Engine.UI.UIRef.none;

    // The element names, shared by both routes -- the NGUI GameObject names and the bitty view's
    // element names are deliberately the same strings, and the bind manifest keys on them.
    public static string checkboxNameVibrate = "CheckboxControlsVibrate";
    public static string checkboxNameHandedLeft = "CheckboxControlsLeft";
    public static string checkboxNameHandedRight = "CheckboxControlsRight";

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

        // The profile is the record; GameIndicatorConfigs.scale is the live dial the indicators
        // read. Nothing else copies one to the other, so do it wherever the page comes up --
        // a player who set this last session has not had it applied yet this one.
        ApplyIndicatorScale(IndicatorScaleSetting());

        SyncCheckedState();
        loadData();
    }

    public override void OnEnable() {

        //Messenger<string>.AddListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);

        Messenger<string>.AddListener(
            UIControllerMessages.uiPanelAnimateIn,
            OnUIControllerPanelAnimateIn);

        Messenger<string>.AddListener(
            UIControllerMessages.uiPanelAnimateOut,
            OnUIControllerPanelAnimateOut);

        Messenger<string, string>.AddListener(
            UIControllerMessages.uiPanelAnimateType,
            OnUIControllerPanelAnimateType);

        Messenger<string, bool>.AddListener(CheckboxEvents.EVENT_ITEM_CHANGE, OnCheckboxChangeEventHandler);
    }

    public override void OnDisable() {

        //Messenger<string>.RemoveListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);

        Messenger<string>.RemoveListener(
            UIControllerMessages.uiPanelAnimateIn,
            OnUIControllerPanelAnimateIn);

        Messenger<string>.RemoveListener(
            UIControllerMessages.uiPanelAnimateOut,
            OnUIControllerPanelAnimateOut);

        Messenger<string, string>.RemoveListener(
            UIControllerMessages.uiPanelAnimateType,
            OnUIControllerPanelAnimateType);

        Messenger<string, bool>.RemoveListener(CheckboxEvents.EVENT_ITEM_CHANGE, OnCheckboxChangeEventHandler);

        // Chain to base so UIPanelBase.OnDisable -> FreeToolkitView runs when this panel is pooled
        // away (destroy-on-hide). This override previously stopped the chain, which would leak the
        // PanelRenderer once the panel became a toolkit view. Prerequisite for the 3A migration.
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

    }

    // Both branches are LIVE at once during the migration: the legacy prefab is still
    // instantiated (SuppressLegacyView only hides it) and the bitty view sits on top, so each
    // sync writes to whichever of the two is actually there. A dead UIRef no-ops.
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    public virtual void ChangeCheckedState(UICheckbox box, bool selected) {
        if(box != null) {
            box.isChecked = selected;
        }
    }
#endif

    public virtual void ChangeCheckedState(Engine.UI.UIRef box, bool selected) {
        UIUtil.SetToggleValue(box, selected);
    }

    public virtual void SyncCheckedState() {

        bool vibrate = GameProfiles.Current.GetControlVibrate();
        ProfileControlHanded controlHanded = GameProfiles.Current.GetControlHanded();

        bool handedRight = controlHanded == ProfileControlHanded.RIGHT;

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
        ChangeCheckedState(checkboxControlsHandedRight, handedRight);
        ChangeCheckedState(checkboxControlsHandedLeft, !handedRight);
        ChangeCheckedState(checkboxControlsVibrate, vibrate);
#endif

        ChangeCheckedState(toggleControlsHandedRight, handedRight);
        ChangeCheckedState(toggleControlsHandedLeft, !handedRight);
        ChangeCheckedState(toggleControlsVibrate, vibrate);

        UIUtil.SetSliderValue(
            sliderControlsIndicatorScale, IndicatorScaleToSlider(IndicatorScaleSetting()));
    }

    // ------------------------------------------------------------------
    // INDICATOR SIZE
    //
    // The off-screen edge indicators read GameIndicatorConfigs.scale every time they size
    // themselves, so the panel's job is to keep that field and the profile attribute in step.
    // The slider is normalised 0..1 over [scaleMin, scaleMax] because the toolkit Slider in the
    // bitty view carries no authored range.

    public virtual float IndicatorScaleSetting() {
        return GameProfiles.Current.GetControlIndicatorScale();
    }

    public static float IndicatorScaleToSlider(float scale) {

        float span = GameIndicatorConfigs.scaleMax - GameIndicatorConfigs.scaleMin;

        if(span <= 0f) {
            return 0f;
        }

        return Mathf.Clamp01((scale - GameIndicatorConfigs.scaleMin) / span);
    }

    public static float SliderToIndicatorScale(float slider) {

        return GameIndicatorConfigs.scaleMin
            + (Mathf.Clamp01(slider)
                * (GameIndicatorConfigs.scaleMax - GameIndicatorConfigs.scaleMin));
    }

    // Push the saved value into the config the indicators actually read. Called on show as well
    // as on change: the profile is the record, the config is the live dial, and nothing else
    // copies one to the other.
    public virtual void ApplyIndicatorScale(float scale) {

        GameIndicatorConfigs.scale =
            Mathf.Clamp(scale, GameIndicatorConfigs.scaleMin, GameIndicatorConfigs.scaleMax);
    }

    public virtual void OnIndicatorScaleChanged(float sliderValue) {

        float scale = SliderToIndicatorScale(sliderValue);

        GameProfiles.Current.SetControlIndicatorScale(scale);

        ApplyIndicatorScale(scale);

        GameState.SaveProfile();
    }

    // ------------------------------------------------------------------
    // CHANGES

    // The Messenger route. Only the legacy widgets arrive here -- CheckboxEvents is a
    // MonoBehaviour on the NGUI checkbox's own GameObject, and a toolkit toggle has no
    // GameObject to carry one. The toolkit toggles come through HandleControlChange instead,
    // registered in BindElements.
    public virtual void OnCheckboxChangeEventHandler(string checkboxName, bool selected) {
        //LogUtil.Log("OnCheckboxChangeEventHandler: checkboxName:" + checkboxName + " selected:" + selected );

        HandleControlChange(checkboxName, selected);
    }

    // Named comparison, so one body serves both routes: the bitty view's element names are the
    // same strings as the NGUI GameObject names (CheckboxControlsVibrate / -Left / -Right), which
    // is what the bind manifest keys on.
    //
    // Compared against the ELEMENT names rather than the fields' own, because under the toolkit
    // the legacy fields may be unwired and the old code dereferenced `.name` on them
    // unconditionally -- an NRE inside a Messenger callback, which drops every other listener on
    // that event too.
    public virtual void HandleControlChange(string checkboxName, bool selected) {

        if(string.IsNullOrEmpty(checkboxName)) {
            return;
        }

        if(checkboxName == checkboxNameHandedRight) {
            if(selected) {
                GameProfiles.Current.SetControlHanded(
                    ProfileControlHanded.RIGHT);
            }
        }
        else if(checkboxName == checkboxNameHandedLeft) {
            if(selected) {
                GameProfiles.Current.SetControlHanded(
                    ProfileControlHanded.LEFT);
            }
        }
        else if(checkboxName == checkboxNameVibrate) {
            GameProfiles.Current.SetControlVibrate(selected);
        }
        else {
            // Not one of ours. The Messenger event is global.
            return;
        }

        SyncCheckedState();
        GameState.SaveProfile();
    }

    // Register the toolkit side once the view exists. BindElements is the continuation the
    // async LoadView runs after the PanelRenderer has built its UXML, so this is the first
    // moment the elements are real -- and it is also where the fields above get their values,
    // so the handlers must be attached after base.BindElements, not before.
    public override void BindElements(Engine.UI.UIRef root) {

        base.BindElements(root);

        UIUtil.SetToggleHandlerChange(
            toggleControlsVibrate,
            selected => HandleControlChange(checkboxNameVibrate, selected));

        UIUtil.SetToggleHandlerChange(
            toggleControlsHandedLeft,
            selected => HandleControlChange(checkboxNameHandedLeft, selected));

        UIUtil.SetToggleHandlerChange(
            toggleControlsHandedRight,
            selected => HandleControlChange(checkboxNameHandedRight, selected));

        UIUtil.SetSliderHandlerChange(
            sliderControlsIndicatorScale, OnIndicatorScaleChanged);

        SyncCheckedState();
    }

    public override void HandleShow() {
        base.HandleShow();

        backgroundDisplayState = UIPanelBackgroundDisplayState.PanelBacker;
    }

    public virtual void loadData() {
        StartCoroutine(loadDataCo());
    }

    IEnumerator loadDataCo() {

        yield return new WaitForSeconds(1f);
    }
}
#endif