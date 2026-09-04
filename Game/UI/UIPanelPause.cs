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
    // and renders faithfully, and ShowViewInPlace fixes the timeScale-0 show. It was reverted to the
    // known-good NGUI pause on 2026-07-20 over two symptoms: (1) the async first-show lag let the pause
    // TAP fall through to the store button sharing the HUD pause button's screen spot; (2) the view
    // "interfered with picking", reading as slow/covered/unresponsive.
    //
    // All three underlying causes are now addressed (2026-07-31), so this is one flip away:
    //   * (1) the tap-steal was NOT the lag — it was the suppressed header coin button keeping a LIVE
    //     collider on the depth-10 menu UICamera, which outranks the depth-6 HUDCamera in NGUI's
    //     descending-depth sort. Fixed in games-ui c805b2d (SuppressCoinButtonCollider).
    //   * the lag itself is fixed by toolkitPreloadView below: the view is built at level load, so
    //     AnimateIn takes the toolkit branch in the same frame as the tap.
    //   * (2) re-checked statically: the view root carries .ngui-root, which UIToolkitBackend
    //     .ConfigurePicking has made PickingMode.Ignore since fc816b8 (well before the regression), so
    //     the root was never the pick target; and overlay (20000) already outranks chrome (10000), so
    //     no other toolkit view can cover it. Modal blocking stays legacy — showUIPanelPause shows the
    //     NGUI gameBackgroundAlertObject behind the dialog.
    //
    // FLIPPED 2026-08-17. The last thing holding this at "" was "cannot be verified in-editor —
    // scripted PlayGame never reaches IsGameRunning=True". That is no longer true: a scripted run
    // DOES reach real gameplay (gameState=GameStarted, HUD live) by driving
    //   GameController.PlayGame() -> click ButtonGameInitFinish -> click ButtonGameReady
    // via UICamera.Notify, pumping EditorApplication.QueuePlayerLoopUpdate() between steps because
    // an unfocused editor barely ticks the player loop. Kill switch UIPlatform.toolkitViewsEnabled
    // still backs this out globally.
    public override string toolkitViewKey {
        get {
            return BaseUIPanel.panelPause;
        }
    }

    // The pause overlay is scene-resident (BaseUIController: not catalog-loaded), so it is enabled at
    // level load — long before the player taps pause. That makes it the case preloading exists for:
    // build the view then, not during the tap that opens it. See UIPanelBase.toolkitPreloadView.
    //
    // Harmless while toolkitViewKey is "": EnsureToolkitView returns early on an empty key.
    public override bool toolkitPreloadView {
        get {
            return true;
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

        Messenger<string, float>.AddListener(
            SliderEvents.EVENT_ITEM_CHANGE, OnSliderChangeEventHandler);

        Messenger<string>.AddListener(GameMessages.gameLevelPause, OnGameLevelPauseHandler);
        Messenger<string>.AddListener(GameMessages.gameLevelResume, OnGameLevelResumeHandler);
        Messenger<string>.AddListener(GameMessages.gameLevelQuit, OnGameLevelQuitHandler);
    }

    public override void OnDisable() {
        base.OnDisable();

        Messenger<string>.RemoveListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);

        Messenger<string, float>.RemoveListener(
            SliderEvents.EVENT_ITEM_CHANGE, OnSliderChangeEventHandler);

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

    // THE PAUSE AUDIO SLIDERS. Dead since long before the migration (iter 19 audit): each legacy
    // slider carries a SliderEvents component, so dragging one broadcasts EVENT_ITEM_CHANGE with
    // its own name — and the only listener in the codebase, UIPanelSettingsAudio, compares against
    // SliderAudioMusicVolume / SliderAudioEffectsVolume. These are called SliderPauseAudio*Volume,
    // so nothing has ever matched them, and nothing ever set their value either.
    //
    // Both routes are served, the same way BaseGameUIPanelSettingsAudio serves them: the Messenger
    // bus for the legacy widgets (kill-switch path), and SetSliderHandlerChange for the toolkit
    // ones — a VisualElement has no GameObject to carry a SliderEvents, so registration is the
    // ONLY way a migrated slider is heard (rule 88).
    public static string sliderNamePauseAudioMusic = "SliderPauseAudioMusicVolume";
    public static string sliderNamePauseAudioEffects = "SliderPauseAudioEffectsVolume";

    public virtual void OnSliderChangeEventHandler(string sliderName, float sliderValue) {
        HandleVolumeChange(sliderName, sliderValue);
    }

    // GameAudio's profile setters, NOT GameProfiles + SaveProfile: they apply the volume live and
    // skip the save when the stored value already matches. A drag broadcasts one change PER FRAME
    // and a full profile save is 50-66 ms on the main thread.
    public virtual void HandleVolumeChange(string sliderName, float sliderValue) {

        if(string.IsNullOrEmpty(sliderName)) {
            return;
        }

        if(sliderName == sliderNamePauseAudioEffects) {
            GameAudio.SetProfileEffectsVolume(sliderValue);
        }
        else if(sliderName == sliderNamePauseAudioMusic) {
            GameAudio.SetProfileAmbienceVolume(sliderValue);
        }
    }

    // The bitty view authors the two sliders at 0.9 / 0.75. Without this the menu opens showing
    // those whatever the profile says, and the first drag from there writes a value the player
    // never chose. A dead UIRef no-ops, so this is safe on both paths.
    public virtual void SyncSliderValues() {

        float musicVolume = (float)GameProfiles.Current.GetAudioMusicVolume();
        float effectsVolume = (float)GameProfiles.Current.GetAudioEffectsVolume();

        UIUtil.SetSliderValue(
            UIUtil.ResolveDeep(viewRoot, sliderNamePauseAudioMusic), musicVolume);
        UIUtil.SetSliderValue(
            UIUtil.ResolveDeep(viewRoot, sliderNamePauseAudioEffects), effectsVolume);
    }

    // The continuation the async LoadView runs once the elements are real — the first moment the
    // handlers can be attached, and the reason the sync happens here rather than in Init.
    public override void BindElements(UIRef root) {

        base.BindElements(root);

        UIUtil.SetSliderHandlerChange(
            UIUtil.ResolveDeep(root, sliderNamePauseAudioMusic),
            value => HandleVolumeChange(sliderNamePauseAudioMusic, value));

        UIUtil.SetSliderHandlerChange(
            UIUtil.ResolveDeep(root, sliderNamePauseAudioEffects),
            value => HandleVolumeChange(sliderNamePauseAudioEffects, value));

        SyncSliderValues();
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

    // The pause menu is the one panel that may animate WHILE the game is frozen: the real slide is
    // panelRightObject (containerPause is unwired in the scene), driven by the AnimationEasing pump,
    // which advances on SCALED time and so stalls at Time.timeScale == 0. Running these tweens on the
    // unscaled clock makes the entrance immune to that.
    //
    // Note this is belt-and-braces, not the primary mechanism: gameRunningStatePause still defers the
    // freeze by 1s so the menu is fully on screen BEFORE the game stops (freezing first left the
    // player staring at a frozen game with no menu). The unscaled clock is what keeps the entrance
    // correct if the freeze ever lands mid-animation.
    public override void AnimateIn() {

        TweenUtil.BeginUnscaledScope();

        try {
            base.AnimateIn();

            // Re-shows reuse an already-bound view, so BindElements does not run again — and the
            // player may have changed the volume in Settings since the last pause.
            SyncSliderValues();

            TweenUtil.ShowObjectRight(containerPause, TweenCoord.local, true, .5f);
        }
        finally {
            TweenUtil.EndUnscaledScope();
        }
    }

    public override void AnimateOut() {

        TweenUtil.BeginUnscaledScope();

        try {
            base.AnimateOut();

            TweenUtil.HideObjectRight(containerPause, TweenCoord.local, true, .5f);
        }
        finally {
            TweenUtil.EndUnscaledScope();
        }
    }
}