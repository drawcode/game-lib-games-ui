#define DEV
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using Engine.Events;
using Engine.Utility;
using Engine.UI;

public class UIPanelPause : UIPanelBase {

    public GameObject listItemPrefab;

    public GameObject containerPause;

    // 3F dialogs: the bitty view panel-pause.json (RESUME/RESTART/QUIT + two audio sliders) is authored
    // and renders faithfully, and ShowViewInPlace fixes the timeScale-0 show. BUT the LIVE pause flow
    // still has blocking issues (user 2026-07-20): (1) the async first-show lag lets the pause TAP fall
    // through to the store button that shares the HUD pause button's screen spot; (2) the full-screen
    // view root at overlay order interferes with picking, making the dialog slow/covered/unresponsive.
    // These need view PRELOADING + a picking-model fix + tap-target de-overlap, plus device testing.
    // Reverted to the known-good NGUI pause (return "") until that lands. The view/USS/constants/
    // ShowViewInPlace helpers/slide overrides stay dormant. Re-enable by returning BaseUIPanel.panelPause.
    public override string toolkitViewKey {
        get {
            return "";
        }
    }

    // The pause menu is a HUD-level dialog: it must draw ABOVE the in-game HUD, which loads earlier.
    public override int toolkitSortOrder {
        get {
            return UILayers.overlay;
        }
    }

    // Pause is shown at Time.timeScale == 0, which freezes the scaled-time tween pump — an animated
    // view slide would stay parked off-screen at alpha 0 and never appear (the 2026-07-20 regression:
    // the dialog didn't show, only the menu behind it). Show/hide the view IN PLACE (no tween) so it
    // appears reliably while paused. Legacy pause also appeared in place — its slide target was null.
    protected override void ShowToolkitViewSlide() {
        TweenUtil.ShowViewInPlace(viewRoot);
    }

    protected override void HideToolkitViewSlide() {
        TweenUtil.HideViewInPlace(viewRoot);
    }

    /*
#if USE_UI_NGUI_2_7
#endif
#if USE_UI_NGUI_3
    public UIImageButton buttonResume;
    public UIImageButton buttonRestart;
    public UIImageButton buttonQuit;
    public UIImageButton buttonSettingsAudio;
#else
    public Text buttonResume;
    public Text buttonRestart;
    public Text buttonQuit;
    public Text buttonSettingsAudio;
#endif
*/

    public static UIPanelPause Instance;

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

        loadData();
    }

    public override void Start() {
        Init();
    }

    public override void OnEnable() {
        base.OnEnable();

        Messenger<string>.AddListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);

        Messenger<string>.AddListener(GameMessages.gameLevelPause, OnGameLevelPauseHandler);
        Messenger<string>.AddListener(GameMessages.gameLevelResume, OnGameLevelResumeHandler);
        Messenger<string>.AddListener(GameMessages.gameLevelQuit, OnGameLevelQuitHandler);
    }

    public override void OnDisable() {
        base.OnDisable();

        Messenger<string>.RemoveListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);

        Messenger<string>.RemoveListener(GameMessages.gameLevelPause, OnGameLevelPauseHandler);
        Messenger<string>.RemoveListener(GameMessages.gameLevelResume, OnGameLevelResumeHandler);
        Messenger<string>.RemoveListener(GameMessages.gameLevelQuit, OnGameLevelQuitHandler);
    }

    void OnGameLevelPauseHandler(string levelCode) {
        showDefault();
    }

    void OnGameLevelResumeHandler(string levelCode) {
        hideAll();
    }

    void OnGameLevelQuitHandler(string levelCode) {
        hideAll();
    }

    public override void OnButtonClickEventHandler(string buttonName) {

    }
    
    public static void ShowDefault() {
        if(isInst) {

            Instance.showDefault();
        }
    }

    public void showDefault() {

        ShowCamera();

        AnimateIn();
    }

    public static void HideAll() {
        if(isInst) {

            Instance.hideAll();
        }
    }

    public void hideAll() {

        AnimateOut();

        HideCamera(.5f);

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

        //UpdateAudioValues();
    }

    public override void AnimateIn() {
        base.AnimateIn();

        TweenUtil.ShowObjectRight(containerPause, TweenCoord.local, true, .5f);
    }

    public override void AnimateOut() {
        base.AnimateOut();

        TweenUtil.HideObjectRight(containerPause, TweenCoord.local, true, .5f);
    }
}