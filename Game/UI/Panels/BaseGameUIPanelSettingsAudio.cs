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

    // 2.11 field mirror + 3A migration. These were dead (commented) — audio did nothing on either
    // branch. The #else UIRef fields bind to the bitty view's SliderAudioMusicVolume /
    // SliderAudioEffectsVolume; the NGUI fields stay unwired (audio's NGUI path was never hooked),
    // so the null-guarded handler below is a no-op there — NGUI behavior is preserved exactly.
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    public UISlider sliderAudioMusicVolume;
    public UISlider sliderAudioEffectsVolume;
#else
    public Engine.UI.UIRef sliderAudioMusicVolume;
    public Engine.UI.UIRef sliderAudioEffectsVolume;
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

    public virtual void OnSliderChangeEventHandler(string sliderName, float sliderValue) {

        //LogUtil.Log("OnSliderChangeEventHandler: sliderName:" + sliderName + " sliderValue:" + sliderValue );

        // Restored as part of the 3A migration (it was dead/commented, so audio never persisted).
        // Null-guarded so the NGUI branch — whose fields are unwired — stays a no-op. The toolkit
        // value bridge broadcasts (elementName, value) here on drag; sliderName matches the bound
        // UIRef's name. Initial-value sync from the profile is a follow-up (see 3A notes).
        if(sliderAudioEffectsVolume != null && sliderName == sliderAudioEffectsVolume.name) {
            GameProfiles.Current.SetAudioEffectsVolume(sliderValue);
            GameState.SaveProfile();
        }
        else if(sliderAudioMusicVolume != null && sliderName == sliderAudioMusicVolume.name) {
            GameProfiles.Current.SetAudioMusicVolume(sliderValue);
            GameState.SaveProfile();
        }
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