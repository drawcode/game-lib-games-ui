using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Engine.Events;
using Engine.UI;

public class BaseGameUIPanelLoader : GameUIPanelBase {

    public static GameUIPanelLoader Instance;

    public GameObject containerObject;

    public GameObject charactersObject;
    public GameObject enemiesObject;
    public GameObject logoObject;
    public GameObject loadingObject;
    public GameObject sliderProgressObject;

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3

    public UILabel labelLoading;
    public UISlider sliderProgress;

#else
    // 2.11: agnostic UIRef handles, bound at runtime by name (no consumers today).
    public Engine.UI.UIRef labelLoading;
    public Engine.UI.UIRef sliderProgress;
#endif

    // Toolkit parallels (3B part 4): bound by BindElements from the panel-loader manifest.
    // Unguarded on purpose (compile in both define branches); GameUISceneRoot pushes progress
    // into the fill refs each frame — no-ops until the view is bound.
    public UIRef labelLoadingRef;
    public UIRef sliderProgressRef;
    public UIRef sliderProgressItemRef;

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

        // Chain to base so UIPanelBase.OnDisable -> FreeToolkitView runs — same 3B prerequisite
        // fix as header/footer/main.
        base.OnDisable();
    }

    // Only the FLAT widgets the view replaces hide: the loading pill cluster, the logo, and the
    // sponsor (the latter two aren't serialized fields — found by their anchor paths). The
    // MainBackground particles stay live and show through the picking-Ignore view.
    protected override void SuppressLegacyView() {

        if(loadingObject != null) {
            loadingObject.Hide();
        }

        Transform logo = transform.Find("Container/AnchorCenter/Logo");

        if(logo != null) {
            logo.gameObject.Hide();
        }

        Transform sponsor = transform.Find("Container/AnchorTopLeft/TopLeft");

        if(sponsor != null) {
            sponsor.gameObject.Hide();
        }
    }

    // Kill switch / teardown: restore the suppressed NGUI widgets so the legacy path renders
    // whole again.
    protected override void FreeToolkitView() {

        labelLoadingRef = UIRef.none;
        sliderProgressRef = UIRef.none;
        sliderProgressItemRef = UIRef.none;

        if(isToolkitPanel) {

            if(loadingObject != null) {
                loadingObject.Show();
            }

            Transform logo = transform.Find("Container/AnchorCenter/Logo");

            if(logo != null) {
                logo.gameObject.Show();
            }

            Transform sponsor = transform.Find("Container/AnchorTopLeft/TopLeft");

            if(sponsor != null) {
                sponsor.gameObject.Show();
            }
        }

        base.FreeToolkitView();
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

    public override void Start() {
        Init();
    }

    public override void Init() {
        base.Init();

        //LoadData();
        AnimateIn();
    }

    public virtual void LoadData() {
        StartCoroutine(LoadDataCo());
    }

    IEnumerator LoadDataCo() {
        yield break;
    }

    public virtual void ShowLogo() {
        if(logoObject != null) {
            logoObject.Show();
            //logoObject.FadeTo(5f, 1f, 0f, LoopType.pingPong);
        }
    }

    public virtual void HideLogo() {
        if(logoObject != null) {
            //logoObject.FadeTo(5f, 1f, 0f, LoopType.pingPong);
        }
    }

    public virtual void ShowCharacters() {
        if(charactersObject != null) {
            charactersObject.Show();
            //charactersObject.FadeTo(5f, 1f, 0f, LoopType.pingPong);
        }
    }

    public virtual void HideCharacters() {
        if(charactersObject != null) {
            //charactersObject.FadeTo(5f, 1f, 0f, LoopType.pingPong);
        }
    }


    public virtual void ShowEnemies() {
        if(enemiesObject != null) {
            enemiesObject.Show();
            //enemiesObject.FadeTo(5f, 1f, 0f, LoopType.pingPong);
        }
    }

    public virtual void HideEnemies() {
        if(enemiesObject != null) {
            //enemiesObject.FadeTo(5f, 1f, 0f, LoopType.pingPong);
        }
    }

    public override void AnimateIn() {

        HideLogo();
        HideEnemies();
        HideCharacters();

        base.AnimateIn();

        ShowLogo();
        ShowEnemies();
        ShowCharacters();
    }

    public override void AnimateOut() {

        HideLogo();
        HideEnemies();
        HideCharacters();

        base.AnimateOut();

    }

    /*

    void OnButtonClickEventHandler(string buttonName) {
        LogUtil.Log("OnButtonClickEventHandler: " + buttonName);
        
        if(buttonName == buttonNorahGlowObject.name 
            || buttonName == buttonNorahStaticObject.name) {
            LogUtil.Log("Norah Clicked: " + buttonName);
        }

    }
    
    void OnListItemClickEventHandler(string listName, string listIndex, bool selected) {
        LogUtil.Log("OnListItemClickEventHandler: listName:" + listName + " listIndex:" + listIndex.ToString() + " selected:" + selected.ToString());

    }

    void OnListItemSelectEventHandler(string listName, string selectName) {
        LogUtil.Log("OnListItemSelectEventHandler: listName:" + listName + " selectName:" + selectName );

        if(listName == "ListState") {

        }
    }

    void OnSliderChangeEventHandler(string sliderName, float sliderValue) {
        LogUtil.Log("OnSliderChangeEventHandler: sliderName:" + sliderName + " sliderValue:" + sliderValue );

        // Change appstate

        if(sliderName == "AudioEffectsSlider") {
            //GameProfiles.Current.SetAudioEffectsVolume(sliderValue);
        }
    }
    
    void OnCheckboxChangeEventHandler(string checkboxName, bool selected) {
        LogUtil.Log("OnCheckboxChangeEventHandler: checkboxName:" + checkboxName + " selected:" + selected );
        
        // Change appstate
        
        if(checkboxName == "DeviceModeBestCheckbox") {
            //CameraDevice.Instance.SetFocusMode(
        }
    }
    */
}