using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Engine.Events;
using Engine.Game.App.BaseApp;

#if ENABLE_FEATURE_SETTINGS_AUDIO

public class BaseGameUIPanelSettingsAudio : GameUIPanelBase {

    public static GameUIPanelSettingsAudio Instance;

    public GameObject listItemPrefab;

    // The legacy widgets. Unwired on the NGUI prefab -- audio's NGUI path was never hooked -- so
    // every write through them is a no-op and NGUI behaviour is preserved exactly.
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    public UISlider sliderAudioMusicVolume;
    public UISlider sliderAudioEffectsVolume;
#endif

    // The bitty view's sliders, bound by BindElements. UNCONDITIONAL, and for the same reason the
    // controls page's toggles are: the mirror these replace put the UIRef fields in the `#else` of
    // the block above, but this project compiles the `#if` -- USE_UI_NGUI_2_7 is defined ALONGSIDE
    // USE_UI_TOOLKIT -- so both fields were UISlider, and BindElements binds only fields whose type
    // is exactly UIRef. It skipped both and warned, and the migrated page rendered two sliders with
    // nothing holding them. Second instance of the ngui-if-branch-fields-cannot-bind trap on a
    // settings page; the controls page was the first.
    //
    // Renamed rather than reused: the NGUI names above stay on the prefab's serialised fields.
    // The bind manifest maps these to the same SliderAudioMusicVolume / SliderAudioEffectsVolume
    // elements the old (dead) entries named.
    //
    // UIRef.none rather than null: every backend op no-ops on a ref that is not alive, so an
    // unbound element degrades to "nothing happens" instead of a NullReferenceException on a
    // Messenger callback, which would take every other listener down with it.
    public Engine.UI.UIRef sliderAudioMusic = Engine.UI.UIRef.none;
    public Engine.UI.UIRef sliderAudioEffects = Engine.UI.UIRef.none;

    // The element names, shared by both routes -- the NGUI GameObject names and the bitty view's
    // element names are deliberately the same strings, and the bind manifest keys on them.
    public static string sliderNameAudioMusic = "SliderAudioMusicVolume";
    public static string sliderNameAudioEffects = "SliderAudioEffectsVolume";

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

    public override void Init() {
        base.Init();

        loadData();
    }

    public override void Start() {
        Init();

        /*
		float effectsVolume = (float)GameProfiles.Current.GetAudioEffectsVolume();
		float musicVolume = (float)GameProfiles.Current.GetAudioMusicVolume();
		
		if(sliderMusicVolume != null) {
			sliderMusicVolume.sliderValue = musicVolume;
			sliderMusicVolume.ForceUpdate();
		}
		
		if(sliderEffectsVolume != null) {
			sliderEffectsVolume.sliderValue = effectsVolume;
			sliderEffectsVolume.ForceUpdate();
		}
  */
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

        Messenger<string, float>.AddListener(SliderEvents.EVENT_ITEM_CHANGE, OnSliderChangeEventHandler);
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

        Messenger<string, float>.RemoveListener(SliderEvents.EVENT_ITEM_CHANGE, OnSliderChangeEventHandler);

        // Chain to base so UIPanelBase.OnDisable -> FreeToolkitView runs when this panel is pooled
        // away. This override previously stopped the chain (would leak the toolkit view). 3A prereq.
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
        LogUtil.Log("OnButtonClickEventHandler: " + buttonName);
    }

    // The Messenger route. Only the legacy widgets arrive here -- SliderEvents is a MonoBehaviour
    // on the NGUI slider's own GameObject, and a toolkit slider has no GameObject to carry one.
    // The toolkit sliders come through HandleVolumeChange instead, registered in BindElements.
    public virtual void OnSliderChangeEventHandler(string sliderName, float sliderValue) {

        //LogUtil.Log("OnSliderChangeEventHandler: sliderName:" + sliderName + " sliderValue:" + sliderValue );

        HandleVolumeChange(sliderName, sliderValue);
    }

    // Named comparison, so one body serves both routes.
    //
    // Compared against the ELEMENT names rather than the fields' own, because the legacy sliders
    // are unwired here and the old code dereferenced `.name` on them behind a null guard that only
    // ever evaluated false -- which is the other half of why audio never persisted.
    public virtual void HandleVolumeChange(string sliderName, float sliderValue) {

        if(string.IsNullOrEmpty(sliderName)) {
            return;
        }

        // GameAudio's profile setters, NOT GameProfiles + SaveProfile: they apply the volume live
        // (AudioSystem + GameAudioController, so a drag is audible while it is being dragged) and
        // they skip the save when the stored value already matches. A full profile save is 50-66 ms
        // on the main thread and a drag broadcasts one change PER FRAME -- the unguarded save this
        // replaces would have dropped frames for the length of every drag.
        if(sliderName == sliderNameAudioEffects) {
            GameAudio.SetProfileEffectsVolume(sliderValue);
        }
        else if(sliderName == sliderNameAudioMusic) {
            GameAudio.SetProfileAmbienceVolume(sliderValue);
        }
    }

    // Both branches are LIVE at once during the migration: the legacy prefab is still instantiated
    // (SuppressLegacyView only hides it) and the bitty view sits on top, so each sync writes to
    // whichever of the two is actually there. A dead UIRef no-ops.
    public virtual void SyncSliderValues() {

        float musicVolume = (float)GameProfiles.Current.GetAudioMusicVolume();
        float effectsVolume = (float)GameProfiles.Current.GetAudioEffectsVolume();

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
        UIUtil.SetSliderValue(sliderAudioMusicVolume, musicVolume);
        UIUtil.SetSliderValue(sliderAudioEffectsVolume, effectsVolume);
#endif

        UIUtil.SetSliderValue(sliderAudioMusic, musicVolume);
        UIUtil.SetSliderValue(sliderAudioEffects, effectsVolume);
    }

    // Register the toolkit side once the view exists. BindElements is the continuation the async
    // LoadView runs after the PanelRenderer has built its UXML, so this is the first moment the
    // elements are real -- and it is also where the fields above get their values, so the handlers
    // must be attached after base.BindElements, not before.
    public override void BindElements(Engine.UI.UIRef root) {

        base.BindElements(root);

        UIUtil.SetSliderHandlerChange(
            sliderAudioMusic,
            value => HandleVolumeChange(sliderNameAudioMusic, value));

        UIUtil.SetSliderHandlerChange(
            sliderAudioEffects,
            value => HandleVolumeChange(sliderNameAudioEffects, value));

        // The bitty view authors both sliders at a flat 0.8. Without this the page opened showing
        // 0.8 whatever the profile said, and the first drag from that position wrote a value the
        // player never chose.
        SyncSliderValues();
    }

    public override void HandleShow() {
        base.HandleShow();

        backgroundDisplayState = UIPanelBackgroundDisplayState.PanelBacker;

        // Re-shows reuse an already-bound view, so BindElements does not run again.
        SyncSliderValues();
    }

    public virtual void loadData() {
        StartCoroutine(loadDataCo());
    }

    IEnumerator loadDataCo() {

        yield return new WaitForSeconds(1f);
    }
}
#endif