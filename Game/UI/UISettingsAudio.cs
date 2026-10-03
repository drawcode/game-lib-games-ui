#define DEV
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Engine.Events;
using Engine.Game.Data;

public class UISettingsAudio : GameObjectBehavior {
    
#if USE_UI_NGUI_2_7
    public UISlider sliderMusicVolume;
    public UISlider sliderEffectsVolume;
#elif USE_UI_NGUI_3
    public UISlider sliderMusicVolume;
    public UISlider sliderEffectsVolume;
#else
    // B10: agnostic UIRef handles (was UGUI), the BaseGameHUD pattern. Unbound (null) until
    // something binds them by name; every UIUtil call no-ops on a null ref.
    public Engine.UI.UIRef sliderMusicVolume;
    public Engine.UI.UIRef sliderEffectsVolume;
#endif
    
    public GameObject sliderMusicVolumeObject;
    public GameObject sliderEffectsVolumeObject;

    public void Awake() {
        
    }
    
    public  void Init() {
        
        //loadData();
    }
    
    public void Start() {
        //Init();
        UpdateAudioValues();
    }
    
    public void OnEnable() {
        
        Messenger<string>.AddListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);
        Messenger<string, float>.AddListener(SliderEvents.EVENT_ITEM_CHANGE, OnSliderChangeEventHandler);

        Messenger<GameAudioData>.AddListener(GameAudioMessages.eventAudioVolumeChanged, OnAudioVolumeChangeEventHandler);

    }
    
    public void OnDisable() {
        
        Messenger<string>.RemoveListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);
        Messenger<string, float>.RemoveListener(SliderEvents.EVENT_ITEM_CHANGE, OnSliderChangeEventHandler);

        Messenger<GameAudioData>.RemoveListener(GameAudioMessages.eventAudioVolumeChanged, OnAudioVolumeChangeEventHandler);
    }
    
    void OnButtonClickEventHandler(string buttonName) {
        //LogUtil.Log("OnButtonClickEventHandler: " + buttonName);                  
    }

    void OnAudioVolumeChangeEventHandler(GameAudioData gameAudioData) {

        float volume = (float)gameAudioData.volume;

        if(gameAudioData.code == BaseDataObjectKeys.effects) {
            if(sliderEffectsVolume != null) {
                if(volume != UIUtil.GetSliderValue(sliderEffectsVolume)) {
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
                    UIUtil.SetSliderValue(sliderEffectsVolume, gameAudioData.volume);
#else
                    UIUtil.SetSliderValue(sliderEffectsVolume, volume);
#endif
                }
            }
        }
        else if(gameAudioData.code == BaseDataObjectKeys.music) {
            if(sliderMusicVolume != null) {
                if(volume != UIUtil.GetSliderValue(sliderMusicVolume)) {
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
                    UIUtil.SetSliderValue(sliderMusicVolume, gameAudioData.volume);
#else
                    UIUtil.SetSliderValue(sliderMusicVolume, volume);
#endif
                }
            }
        }
    }

    /*
    void OnSliderChangeEventHandler(string sliderName, float sliderValue) {
        
        //LogUtil.Log("OnSliderChangeEventHandler: sliderName:" + sliderName + " sliderValue:" + sliderValue );

        if (sliderEffectsVolume != null) {
            if (sliderName == sliderEffectsVolume.name) {
                //GameAudio.SetProfileEffectsVolume(sliderValue);
            }
        }
        
        
        if (sliderMusicVolume != null) {
            if (sliderName == sliderMusicVolume.name) {
                //GameAudio.SetProfileAmbienceVolume(sliderValue);
            }
        }
    }*/

    void OnSliderChangeEventHandler(string sliderName, float sliderValue) {

        bool changeAudio = true;

        if(!changeAudio) {
            return;
        }

        if(sliderEffectsVolume != null) {
            if(sliderName == sliderEffectsVolume.name) {
                //GameAudio.SetProfileEffectsVolume(sliderValue);
                GameAudioData gameAudioData = new GameAudioData();
                gameAudioData.code = BaseDataObjectKeys.effects;
                gameAudioData.volume = sliderValue;
                Messenger<GameAudioData>.Broadcast(GameAudioMessages.eventAudioVolumeChanged, gameAudioData);
            }
        }

        if(sliderMusicVolume != null) {
            if(sliderName == sliderMusicVolume.name) {
                //GameAudio.SetProfileAmbienceVolume(sliderValue);
                GameAudioData gameAudioData = new GameAudioData();
                gameAudioData.code = BaseDataObjectKeys.music;
                gameAudioData.volume = sliderValue;
                Messenger<GameAudioData>.Broadcast(GameAudioMessages.eventAudioVolumeChanged, gameAudioData);
            }
        }
    }

    public void UpdateAudioValues() {

        float effectsVolume = (float)GameProfiles.Current.GetAudioEffectsVolume();
        float musicVolume = (float)GameProfiles.Current.GetAudioMusicVolume();
        
        if (sliderMusicVolume != null) {
            
            GameAudio.SetProfileAmbienceVolume(musicVolume);

            UIUtil.SetSliderValue(sliderMusicVolume, musicVolume);
        }
        
        if (sliderEffectsVolume != null) {
            
            GameAudio.SetProfileEffectsVolume(effectsVolume);

            UIUtil.SetSliderValue(sliderEffectsVolume, effectsVolume);
        }
    }
    
    public void loadData() {
        StartCoroutine(loadDataCo());
    }
    
    IEnumerator loadDataCo() {
        
        yield return new WaitForSeconds(1f);
        
        UpdateAudioValues();
    }
    
}

