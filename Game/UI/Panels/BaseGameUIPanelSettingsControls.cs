using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Engine.Events;
using Engine.Game.App.BaseApp;

#if ENABLE_FEATURE_SETTINGS_AUDIO

public class BaseGameUIPanelSettingsControls : GameUIPanelBase {

    public static GameUIPanelSettingsControls Instance;

    public GameObject listItemPrefab;

    // 2.11 field mirror (done here as part of the 3A migration): NGUI branch unchanged; the #else
    // branch is the backend-blind Engine.UI.UIRef bound by BindElements from the bitty view's
    // toggle elements (CheckboxControlsVibrate / CheckboxControlsLeft / CheckboxControlsRight).
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    public UICheckbox checkboxControlsHandedRight;
    public UICheckbox checkboxControlsHandedLeft;

    public UICheckbox checkboxControlsVibrate;
#else
    public Engine.UI.UIRef checkboxControlsHandedRight;
    public Engine.UI.UIRef checkboxControlsHandedLeft;

    public Engine.UI.UIRef checkboxControlsVibrate;
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

    public override void Start() {
        Init();
    }

    public override void Init() {
        base.Init();
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

    // Backend-blind under UI Toolkit: the same callers pass whichever field type the branch
    // declares, and UIUtil.SetToggleValue(UIRef) already dispatches to the backend's toggle op.
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    public virtual void ChangeCheckedState(UICheckbox box, bool selected) {
        if(box != null) {
            box.isChecked = selected;
        }
    }
#else
    public virtual void ChangeCheckedState(Engine.UI.UIRef box, bool selected) {
        UIUtil.SetToggleValue(box, selected);
    }
#endif

    public virtual void SyncCheckedState() {

        bool vibrate = GameProfiles.Current.GetControlVibrate();
        ProfileControlHanded controlHanded = GameProfiles.Current.GetControlHanded();

        if(controlHanded == ProfileControlHanded.LEFT) {
            ChangeCheckedState(checkboxControlsHandedRight, false);
            ChangeCheckedState(checkboxControlsHandedLeft, true);
        }
        else if(controlHanded == ProfileControlHanded.RIGHT) {
            ChangeCheckedState(checkboxControlsHandedRight, true);
            ChangeCheckedState(checkboxControlsHandedLeft, false);
        }

        ChangeCheckedState(checkboxControlsVibrate, vibrate);
    }

    public virtual void OnCheckboxChangeEventHandler(string checkboxName, bool selected) {
        //LogUtil.Log("OnCheckboxChangeEventHandler: checkboxName:" + checkboxName + " selected:" + selected );

        // Change appstate

        if(checkboxName == checkboxControlsHandedRight.name) {
            if(selected) {
                GameProfiles.Current.SetControlHanded(
                    ProfileControlHanded.RIGHT);
                ChangeCheckedState(checkboxControlsHandedLeft, false);
            }
        }
        else if(checkboxName == checkboxControlsHandedLeft.name) {
            if(selected) {
                GameProfiles.Current.SetControlHanded(
                    ProfileControlHanded.LEFT);
                ChangeCheckedState(checkboxControlsHandedRight, false);
            }
        }
        else if(checkboxName == checkboxControlsVibrate.name) {
            GameProfiles.Current.SetControlVibrate(selected);
        }

        SyncCheckedState();
        GameState.SaveProfile();

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