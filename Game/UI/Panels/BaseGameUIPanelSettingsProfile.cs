using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
#else
using UnityEngine.UI;
#endif

using Engine.Events;

#if ENABLE_FEATURE_SETTINGS_PROFILE

public class BaseGameUIPanelSettingsProfile : GameUIPanelBase {

    public static GameUIPanelSettingsProfile Instance;

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3

    public UIImageButton buttonProfileFacebook;
    public UIImageButton buttonProfileTwitter;
    public UIImageButton buttonProfileGameNetwork;

    public UIInput inputProfileName;
#else
    // 2.11: agnostic UIRef handles, bound at runtime by name.
    public Engine.UI.UIRef buttonProfileFacebook;
    public Engine.UI.UIRef buttonProfileTwitter;
    public Engine.UI.UIRef buttonProfileGameNetwork;

    public Engine.UI.UIRef inputProfileName;
#endif

    // The toolkit twin of inputProfileName: the view's TextField, named exactly as the legacy
    // UIInput GameObject. Resolved by name in BindElements, not bound as a field -- under NGUI the
    // public field above is a UIInput, which BindElements can never bind (rule 26). Not public, so
    // the reflection bind skips it.
    public static string inputProfileNameElement = "InputProfile";

    protected Engine.UI.UIRef inputProfileNameView;

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

        Messenger<string, string>.AddListener(InputEvents.EVENT_ITEM_CHANGE, OnProfileInputChanged);
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

        // Was AddListener (a real per-hide listener leak — the input handler accumulated every
        // enable/disable cycle); made symmetric with OnEnable's AddListener. Fixed as part of 3A.
        Messenger<string, string>.RemoveListener(InputEvents.EVENT_ITEM_CHANGE, OnProfileInputChanged);

        // Chain to base so UIPanelBase.OnDisable -> FreeToolkitView runs when this panel is pooled
        // away (destroy-on-hide). 3A migration prerequisite.
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

    public virtual void OnProfileInputChanged(string controlName, string data) {

        if(inputProfileName != null
           && controlName == inputProfileName.name) {
            ChangeUsername(data);
        }
    }

    // NOTE (B0a, 2026-10-03): GameProfiles.Current.ChangeUser RESETS the profile (Profile.Reset
    // clears every attribute) whenever the name differs, and SaveProfile then writes that. This is
    // the legacy save path, unchanged; it is why the view authors the field hidden, matching the
    // prefab, where InputProfile is inactive. Owner call before the field is shown.
    public virtual void ChangeUsername(string username) {
        if(inputProfileName == null && !isProfileNameViewBound) {
            return;
        }

        if(inputProfileName != null) {
            UIUtil.SetInputValue(inputProfileName, username);
        }

        // Toolkit twin; a dead ref no-ops, and writing the value it already holds raises no
        // change event, so the handler below cannot re-enter.
        UIUtil.SetInputValue(inputProfileNameView, username);

        GameProfiles.Current.ChangeUser(username);
        GameProfiles.Current.username = username;
        GameState.SaveProfile();
    }

    public bool isProfileNameViewBound {
        get {
            return inputProfileNameView != null && inputProfileNameView.alive;
        }
    }

    // Shows the current username in the toolkit field. Both branches can be live during the
    // migration, but only the view needs it: the legacy UIInput keeps its own text.
    public virtual void SyncProfileNameView() {

        if(!isProfileNameViewBound) {
            return;
        }

        UIUtil.SetInputValue(inputProfileNameView, GameProfiles.Current.username);
    }

    // BindElements is the continuation the async LoadView runs once the view's elements are real.
    // A toolkit TextField has no InputEvents component, so nothing reaches OnProfileInputChanged
    // through the Messenger bus: the change handler is registered here instead, and it runs the
    // same path EVENT_ITEM_CHANGE does. The field is is-delayed, so it commits on Enter / focus
    // loss like the legacy UIInput's OnSubmit.
    public override void BindElements(Engine.UI.UIRef root) {

        base.BindElements(root);

        inputProfileNameView = UIUtil.ResolveDeep(root, inputProfileNameElement);

        SyncProfileNameView();

        Engine.UI.UIInputChange.SetInputHandlerChange(
            inputProfileNameView,
            value => ChangeUsername(value));
    }

    public override void HandleShow() {
        base.HandleShow();

        backgroundDisplayState = UIPanelBackgroundDisplayState.PanelBacker;

        // Re-shows reuse an already-bound view, so BindElements does not run again.
        SyncProfileNameView();
    }

    public virtual void loadData() {
        StartCoroutine(loadDataCo());
    }

    IEnumerator loadDataCo() {

        //ChangeUsername(GameProfiles.Current.username);

        yield return new WaitForSeconds(1f);
    }
}
#endif